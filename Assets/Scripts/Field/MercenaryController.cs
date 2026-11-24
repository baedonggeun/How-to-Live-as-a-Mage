using UnityEngine;
using UnityEngine.AI;

namespace Mage
{
    /// <summary>숲 용병 하나: 명령 없이 마법사 뒤 대형을 따라오다가, 자기 13m 안·마법사 22m 안의 적과 싸움 (v1 따라와 규칙). 레인저는 13m에서 활, 나머지는 붙어서. 체력 0이면 죽은 시체. 마법사가 죽으면 Flee로 숲 입구까지 도망 (닿으면 무사)</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MercenaryController : MonoBehaviour
    {
        /// <summary>자기에게서 이 안의 적과 싸움 (m, v1)</summary>
        const float FoeRange = 13f;

        /// <summary>마법사에게서 이 안의 적만 상대 (m, v1)</summary>
        const float LeaderGuardRange = 22f;

        /// <summary>활 사거리 (m, v1 사냥꾼)</summary>
        const float RangedReach = 13f;

        /// <summary>붙어 싸울 때 닿는 거리 (m, 두 몸 반지름 밖)</summary>
        const float MeleeReach = 1.3f;

        /// <summary>공격 간격 (초, v1)</summary>
        const float AttackSeconds = 1f;

        /// <summary>체력 기본 (v1: 50 + 전투 × 15)</summary>
        const float HpBase = 50f;

        /// <summary>전투 수치 1당 체력 (v1)</summary>
        const float HpPerCombat = 15f;

        /// <summary>공격 기본 피해 (v1: 3 + 전투 × 1.5)</summary>
        const float DamageBase = 3f;

        /// <summary>전투 수치 1당 피해 (v1)</summary>
        const float DamagePerCombat = 1.5f;

        /// <summary>대형 자리에서 이만큼 멀면 빨리 따라옴 (m, v1)</summary>
        const float CatchUpDistance = 10f;

        /// <summary>빨리 따라올 때 속도 배율 (v1)</summary>
        const float CatchUpScale = 1.45f;

        /// <summary>대형 자리에서 이 안이면 멈춤 (m, v1)</summary>
        const float FollowSlack = 1f;

        /// <summary>도망칠 때 속도 배율 (v1 겁쟁이 도망)</summary>
        const float FleeSpeedScale = 1.15f;

        /// <summary>도망칠 곳에 닿았다고 칠 거리 (m)</summary>
        const float ArriveDistance = 4f;

        /// <summary>싸울 적을 다시 고르는 간격 (초)</summary>
        const float RetargetSeconds = 0.5f;

        /// <summary>길을 다시 구하는 간격 (초)</summary>
        const float RepathSeconds = 0.25f;

        /// <summary>화살 속도 (m/s)</summary>
        const float ArrowSpeed = 30f;

        /// <summary>화살이 날아가는 최대 시간 (초)</summary>
        const float ArrowLifeSeconds = 1.5f;

        /// <summary>시체가 누운 높이 (m)</summary>
        const float CorpseLift = 0.3f;

        /// <summary>대형 자리 (마법사 기준 오른쪽 x·앞 z, v1)</summary>
        static readonly Vector3[] FormationOffsets = { new(-2.3f, 0f, -2.4f), new(2.3f, 0f, -2.4f), new(0f, 0f, -3.8f) };

        /// <summary>화살 색</summary>
        static readonly Color ArrowColor = new(0.55f, 0.4f, 0.25f);

        /// <summary>움직임 (같은 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] NavMeshAgent _agent;

        /// <summary>체력·편 (같은 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldUnitController _unit;

        /// <summary>회색 도형 묶음 (죽으면 눕힘. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _visual;

        /// <summary>몸 콜라이더 (죽으면 끔. 인스펙터에서만 연결)</summary>
        [SerializeField] Collider _collider;

        /// <summary>화살이 나가는 곳 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _muzzle;

        /// <summary>화살 프리팹 (Arrow.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _arrowPrefab;

        /// <summary>현장 규칙 (Init에서 받음)</summary>
        FieldService _service;

        /// <summary>따라갈 마법사</summary>
        Transform _leader;

        /// <summary>이 용병의 대형 자리</summary>
        Vector3 _offset;

        /// <summary>프리팹 걷기 속도 (m/s)</summary>
        float _walkSpeed;

        /// <summary>다음 공격까지 (초)</summary>
        float _cooldown;

        /// <summary>다음 적 고르기까지 (초)</summary>
        float _retarget;

        /// <summary>다음 길 구하기까지 (초)</summary>
        float _repath;

        /// <summary>싸우는 적 (없으면 null)</summary>
        FieldUnitController _foe;

        /// <summary>도망칠 곳 (도망 중이 아니면 null)</summary>
        Transform _fleeTarget;

        /// <summary>죽었는지</summary>
        bool _dead;

        /// <summary>판 데이터의 용병</summary>
        public MercenaryEntry Entry { get; private set; }

        /// <summary>체력·편</summary>
        public FieldUnitController Unit => _unit;

        /// <summary>도망쳐 입구에 닿았거나 쫓는 적이 없어 무사한지</summary>
        public bool IsSafe { get; private set; }

        /// <summary>죽음 (이 용병)</summary>
        public event System.Action<MercenaryController> Died;

        #region Init - 판 데이터 용병·현장 규칙·마법사를 받고 체력(50 + 전투 × 15)·대형 자리를 정해 등록
        /// <param name="entry">판 데이터 용병</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="leader">따라갈 마법사</param>
        /// <param name="index">파티 안 순서 (대형 자리)</param>
        public void Init(MercenaryEntry entry, FieldService service, Transform leader, int index)
        {
            Entry = entry ?? throw new System.ArgumentNullException(nameof(entry));
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            if (leader == null) throw new System.ArgumentNullException(nameof(leader));
            _leader = leader;
            _offset = FormationOffsets[Mathf.Abs(index) % FormationOffsets.Length];
            _walkSpeed = _agent.speed;
            _unit.Init(HpBase + entry.Combat * HpPerCombat);
            _unit.Died += OnUnitDied;
            service.Register(_unit);
        }
        #endregion

        #region Update - 멈췄거나 죽었거나 무사하면 안 함. 도망 중이면 입구로, 아니면 싸울 적이 있으면 싸우고 없으면 따라감
        void Update()
        {
            if (_service == null || _dead || IsSafe) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _cooldown -= dt;
            _repath -= dt;
            if (_fleeTarget != null)
            {
                UpdateFlee();
                return;
            }
            _retarget -= dt;
            if (_retarget <= 0f || _foe == null || !_foe.IsAlive)
            {
                _retarget = RetargetSeconds;
                _foe = PickFoe();
            }
            if (_foe != null) Fight();
            else Follow();
        }
        #endregion

        #region PickFoe - 자기 13m 안 가장 가까운 적이 마법사 22m 안이면 그 적 (v1). 없으면 null
        FieldUnitController PickFoe()
        {
            // 가장 가까운 적
            FieldUnitController foe = _service.Nearest(FieldTeam.Enemy, transform.position, FoeRange);
            if (foe == null) return null;
            return (foe.transform.position - _leader.position).sqrMagnitude <= LeaderGuardRange * LeaderGuardRange ? foe : null;
        }
        #endregion

        #region Fight - 닿으면 멈춰서 1초마다 공격 (레인저는 화살), 아니면 다가감
        void Fight()
        {
            // 적까지 평면 벡터
            Vector3 to = _foe.transform.position - transform.position;
            to.y = 0f;
            // 이 용병이 닿는 거리
            float reach = Entry.Job.Ranged ? RangedReach : MeleeReach + _unit.Radius + _foe.Radius;
            if (to.magnitude > reach)
            {
                MoveTo(_foe.transform.position, _walkSpeed);
                return;
            }
            Halt();
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to);
            if (_cooldown > 0f) return;
            _cooldown = AttackSeconds;
            // 한 번 공격 피해 (v1)
            float damage = DamageBase + Entry.Combat * DamagePerCombat;
            if (Entry.Job.Ranged) Shoot(damage);
            else _foe.TakeDamage(damage, _unit);
        }
        #endregion

        #region Shoot - 화살을 적 겨눌 점으로 쏨 (화살 프리팹이 없으면 바로 피해)
        /// <param name="damage">피해</param>
        void Shoot(float damage)
        {
            if (_arrowPrefab == null)
            {
                _foe.TakeDamage(damage, _unit);
                return;
            }
            // 화살이 나가는 곳
            Vector3 from = _muzzle != null ? _muzzle.position : _unit.AimPoint;
            // 화살 방향
            Vector3 direction = (_foe.AimPoint - from).normalized;
            // 쏜 화살
            SpellProjectileController arrow = Instantiate(_arrowPrefab, from, Quaternion.LookRotation(direction));
            arrow.Launch(direction, ArrowSpeed, ArrowLifeSeconds, damage, 0f, 0f, FieldTeam.Enemy, _unit, _service, ArrowColor);
        }
        #endregion

        #region Follow - 마법사 기준 대형 자리로 감 (10m 넘게 멀면 빨리). 가까우면 멈춤
        void Follow()
        {
            // 대형 자리 (월드)
            Vector3 slot = _leader.position + _leader.rotation * _offset;
            // 대형 자리까지 평면 거리
            float distance = Vector3.ProjectOnPlane(slot - transform.position, Vector3.up).magnitude;
            if (distance <= FollowSlack)
            {
                Halt();
                return;
            }
            MoveTo(slot, distance > CatchUpDistance ? _walkSpeed * CatchUpScale : _walkSpeed);
        }
        #endregion

        #region Flee - 싸움을 그만두고 그 곳으로 도망 (마법사가 죽었을 때)
        /// <param name="target">도망칠 곳 (숲 입구)</param>
        public void Flee(Transform target)
        {
            if (_dead || target == null) return;
            _fleeTarget = target;
            _foe = null;
            _repath = 0f;
        }
        #endregion

        #region UpdateFlee - 입구에 닿으면 무사, 아니면 빨리 감
        void UpdateFlee()
        {
            if ((transform.position - _fleeTarget.position).sqrMagnitude <= ArriveDistance * ArriveDistance)
            {
                MarkSafe();
                return;
            }
            MoveTo(_fleeTarget.position, _walkSpeed * FleeSpeedScale);
        }
        #endregion

        #region MarkSafe - 무사함 (입구에 닿았거나 쫓는 적이 없음). 멈춤
        public void MarkSafe()
        {
            IsSafe = true;
            Halt();
        }
        #endregion

        #region WarpTo - 살아 있으면 그 곳으로 순간이동 (던전 드나들 때 마법사를 따라옴)
        /// <param name="position">갈 곳 (NavMesh 위)</param>
        public void WarpTo(Vector3 position)
        {
            if (_dead || !_agent.enabled) return;
            _agent.Warp(position);
            _foe = null;
            _repath = 0f;
        }
        #endregion

        #region MoveTo - 길 구하는 간격이 됐으면 그 속도로 그 곳까지 길을 구함
        /// <param name="point">갈 곳</param>
        /// <param name="speed">속도 (m/s)</param>
        void MoveTo(Vector3 point, float speed)
        {
            if (!_agent.isOnNavMesh) return;
            _agent.speed = speed;
            _agent.isStopped = false;
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            _agent.SetDestination(point);
        }
        #endregion

        #region Halt - NavMesh 위면 멈춤
        void Halt()
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
        }
        #endregion

        #region OnUnitDied - 죽음: 싸움·도망 그만, 움직임·콜라이더 끄고 눕힘, 죽음 알림
        /// <param name="unit">죽은 몸 (이 용병)</param>
        void OnUnitDied(FieldUnitController unit)
        {
            _dead = true;
            _foe = null;
            _fleeTarget = null;
            Halt();
            _agent.enabled = false;
            _collider.enabled = false;
            _visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _visual.localPosition = Vector3.up * CorpseLift;
            Died?.Invoke(this);
        }
        #endregion

        #region OnDestroy - 몸 알림 구독 해제
        void OnDestroy()
        {
            if (_unit != null) _unit.Died -= OnUnitDied;
        }
        #endregion
    }
}
