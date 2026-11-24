using UnityEngine;
using UnityEngine.AI;

namespace Mage
{
    /// <summary>숲 적 하나 (v1 규칙): 집 둘레를 돌다가 알아채는 거리 안에 파티가 오면 무리째 깨어 쫓아옴 → 닿으면 0.26초 준비 뒤 때림. 대상이 46m 넘게 떨어지면 포기하고 그 자리를 집으로. 죽으면 누운 시체. 수치는 MonsterSO, 편·체력은 같은 오브젝트의 FieldUnitController</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyController : MonoBehaviour
    {
        /// <summary>공격 준비 시간 (초, v1)</summary>
        const float WindUpSeconds = 0.26f;

        /// <summary>쫓던 대상이 이보다 멀면 포기 (m, v1)</summary>
        const float GiveUpDistance = 46f;

        /// <summary>마법사를 노리는 적이 마법사를 고르는 거리 (m, v1)</summary>
        const float MageFocusDistance = 26f;

        /// <summary>준비 중 대상이 닿는 거리보다 이만큼 더 멀어지면 준비 취소 (m, v1)</summary>
        const float WindCancelMargin = 0.8f;

        /// <summary>닿는 거리에 더하는 자기 반지름 비율 (v1)</summary>
        const float SelfReachRatio = 0.4f;

        /// <summary>쉴 때 집 둘레에서 돌아다니는 반지름 (m, v1)</summary>
        const float WanderRadius = 8f;

        /// <summary>쉴 때 걷는 속도 배율 (v1)</summary>
        const float WanderSpeedScale = 0.3f;

        /// <summary>다음 돌아다닐 곳을 고르기까지 최소 시간 (초, v1)</summary>
        const float WanderWaitMin = 2f;

        /// <summary>다음 돌아다닐 곳을 고르기까지 최대 시간 (초, v1)</summary>
        const float WanderWaitMax = 5f;

        /// <summary>쫓을 대상을 다시 고르는 간격 (초)</summary>
        const float RetargetSeconds = 0.5f;

        /// <summary>쫓는 길을 다시 구하는 간격 (초)</summary>
        const float RepathSeconds = 0.25f;

        /// <summary>크기 1일 때 몸 반지름 (m)</summary>
        const float BaseRadius = 0.5f;

        /// <summary>크기 1일 때 몸 높이 (m)</summary>
        const float BaseHeight = 1.6f;

        /// <summary>크기 1일 때 콜라이더 반지름 (m)</summary>
        const float BaseColliderRadius = 0.45f;

        /// <summary>돌아다닐 곳을 NavMesh에 붙일 때 찾는 거리 (m)</summary>
        const float SampleDistance = 2f;

        /// <summary>시체가 누운 높이 (몸 반지름 대비)</summary>
        const float CorpseLift = 0.5f;

        /// <summary>떨어진 금화를 주웠다고 칠 거리 (m)</summary>
        const float PickupDistance = 1.5f;

        /// <summary>보관 장소에 닿았다고 칠 거리 (m)</summary>
        const float StoreDistance = 3f;

        /// <summary>회색 도형 색 셰이더 속성 (URP)</summary>
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>적 상태</summary>
        enum EnemyState
        {
            /// <summary>집 둘레를 돌며 쉼</summary>
            Idle,

            /// <summary>대상을 쫓거나 때림</summary>
            Chase,

            /// <summary>떨어진 금화로 감 (줍는 몬스터)</summary>
            Fetch,

            /// <summary>주운 금화를 보관 장소로 가져감</summary>
            Carry,

            /// <summary>죽음 (시체)</summary>
            Dead,
        }

        /// <summary>움직임 (같은 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] NavMeshAgent _agent;

        /// <summary>체력·편 (같은 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldUnitController _unit;

        /// <summary>회색 도형 묶음 (크기·눕힘. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _visual;

        /// <summary>색을 칠할 도형 (인스펙터에서만 연결)</summary>
        [SerializeField] Renderer[] _renderers;

        /// <summary>몸 콜라이더 (크기에 맞춤, 죽으면 끔. 인스펙터에서만 연결)</summary>
        [SerializeField] CapsuleCollider _collider;

        /// <summary>현장 규칙 (Init에서 받음. 시체는 null)</summary>
        FieldService _service;

        /// <summary>지금 상태</summary>
        EnemyState _state;

        /// <summary>집 (쉴 때 이 둘레를 돎. 포기하면 그 자리로 바뀜)</summary>
        Vector3 _home;

        /// <summary>쫓는 대상 (없으면 null)</summary>
        FieldUnitController _target;

        /// <summary>다음 공격까지 남은 시간 (초)</summary>
        float _cooldown;

        /// <summary>공격 준비 남은 시간 (초, 0이면 준비 안 함)</summary>
        float _wind;

        /// <summary>다음 돌아다닐 곳을 고르기까지 (초)</summary>
        float _wanderWait;

        /// <summary>다음 대상 고르기까지 (초)</summary>
        float _retarget;

        /// <summary>다음 길 구하기까지 (초)</summary>
        float _repath;

        /// <summary>26m 안의 마법사를 노리는 적인지 (태어날 때 정함)</summary>
        bool _focusMage;

        /// <summary>현장 규칙에 쫓는 중이라고 알렸는지</summary>
        bool _engaged;

        /// <summary>색 칠하기 (처음 쓸 때 만듦)</summary>
        MaterialPropertyBlock _block;

        /// <summary>몬스터 데이터</summary>
        public MonsterSO Monster { get; private set; }

        /// <summary>적 자리 키 ({자리 id}.{번호}). 쓰러지면 판 데이터에 이 키로 남음</summary>
        public string SpawnKey { get; private set; }

        /// <summary>무리 번호 (같은 적 자리에서 나온 적끼리 같음)</summary>
        public int Pack { get; private set; }

        /// <summary>집 (지도 목표 표시에 씀)</summary>
        public Vector3 Home => _home;

        /// <summary>체력·편</summary>
        public FieldUnitController Unit => _unit;

        /// <summary>쫓는 대상 (쫓는 중이 아니면 null)</summary>
        public FieldUnitController Target => _state == EnemyState.Chase ? _target : null;

        /// <summary>들고 있는 금화 (줍는 몬스터)</summary>
        public int CarriedGold { get; private set; }

        /// <summary>죽음 (이 적)</summary>
        public event System.Action<EnemyController> Died;

        #region Init - 살아 있는 적으로 시작: 크기·색, 체력, 속도, 마법사 노림 여부를 정하고 현장 규칙에 등록
        /// <param name="monster">몬스터 데이터</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="spawnKey">적 자리 키</param>
        /// <param name="pack">무리 번호</param>
        public void Init(MonsterSO monster, FieldService service, string spawnKey, int pack)
        {
            if (monster == null) throw new System.ArgumentNullException(nameof(monster));
            if (service == null) throw new System.ArgumentNullException(nameof(service));
            Setup(monster, spawnKey, pack);
            _service = service;
            _home = transform.position;
            _unit.Init(monster.MaxHp);
            _focusMage = Random.value < monster.MageFocusChance;
            _wanderWait = Random.Range(0f, WanderWaitMax);
            _state = EnemyState.Idle;
            _unit.Damaged += OnDamaged;
            _unit.Died += OnUnitDied;
            service.RegisterEnemy(this);
        }
        #endregion

        #region InitCorpse - 이미 쓰러진 적으로 시작 (다시 들어갈 때 시체): 움직임·콜라이더를 끄고 눕힘. 현장 규칙에 등록 안 함
        /// <param name="monster">몬스터 데이터</param>
        /// <param name="spawnKey">적 자리 키</param>
        /// <param name="pack">무리 번호</param>
        public void InitCorpse(MonsterSO monster, string spawnKey, int pack)
        {
            if (monster == null) throw new System.ArgumentNullException(nameof(monster));
            Setup(monster, spawnKey, pack);
            _unit.InitDead(monster.MaxHp);
            _state = EnemyState.Dead;
            _agent.enabled = false;
            _collider.enabled = false;
            LieDown();
        }
        #endregion

        #region Setup - 데이터·키·무리를 받고 크기(도형·반지름·높이·콜라이더)와 색을 맞춤
        /// <param name="monster">몬스터 데이터</param>
        /// <param name="spawnKey">적 자리 키</param>
        /// <param name="pack">무리 번호</param>
        void Setup(MonsterSO monster, string spawnKey, int pack)
        {
            Monster = monster;
            SpawnKey = spawnKey ?? string.Empty;
            Pack = pack;
            // 이 적의 몸 반지름·높이 (m)
            float radius = BaseRadius * monster.Size;
            float height = BaseHeight * monster.Size;
            _unit.SetRadius(radius);
            _agent.radius = radius;
            _agent.height = height;
            _agent.speed = monster.Speed * WanderSpeedScale;
            _collider.radius = BaseColliderRadius * monster.Size;
            _collider.height = height;
            _collider.center = Vector3.up * (height * 0.5f);
            _visual.localScale = Vector3.one * monster.Size;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, monster.Color);
            foreach (Renderer shape in _renderers)
                if (shape != null) shape.SetPropertyBlock(_block);
        }
        #endregion

        #region Update - 멈췄으면(로딩창·메뉴) 안 함. 상태마다: 쉼(알아채기·줍기·돌아다니기), 줍기, 가져가기, 쫓기·때리기
        void Update()
        {
            if (_service == null || _state == EnemyState.Dead) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _cooldown -= dt;
            _repath -= dt;
            switch (_state)
            {
                case EnemyState.Idle: UpdateIdle(dt); break;
                case EnemyState.Fetch: UpdateFetch(); break;
                case EnemyState.Carry: UpdateCarry(); break;
                default: UpdateChase(dt); break;
            }
        }
        #endregion

        #region UpdateIdle - 알아채면 무리를 깨움. 들고 있는 금화가 있으면 가져가기, 줍는 몬스터면 알아채는 거리 안 떨어진 금화로. 아니면 가끔 집 둘레를 천천히 걸음
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateIdle(float dt)
        {
            if (TryNotice()) return;
            if (CarriedGold > 0 && _service.StashPoint != null)
            {
                _state = EnemyState.Carry;
                _repath = 0f;
                return;
            }
            if (Monster.PicksUpItems && _service.Loot != null && FlatDistance(_service.Loot.position) <= Monster.AggroRange)
            {
                _state = EnemyState.Fetch;
                _repath = 0f;
                return;
            }
            _wanderWait -= dt;
            if (_wanderWait > 0f) return;
            _wanderWait = Random.Range(WanderWaitMin, WanderWaitMax);
            // 집 둘레에서 고른 곳
            Vector2 offset = Random.insideUnitCircle * WanderRadius;
            if (NavMesh.SamplePosition(_home + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, SampleDistance, NavMesh.AllAreas))
                MoveTo(hit.position, Monster.Speed * WanderSpeedScale);
        }
        #endregion

        #region TryNotice - 알아채는 거리 안에 파티가 있으면 무리를 깨움 (깨웠으면 true)
        bool TryNotice()
        {
            // 알아챈 파티 몸 (알아채는 거리 안 가장 가까운 것)
            FieldUnitController seen = _service.Nearest(FieldTeam.Party, transform.position, Monster.AggroRange);
            if (seen == null) return false;
            _service.WakePack(Pack, seen);
            return true;
        }
        #endregion

        #region UpdateFetch - 떨어진 금화로 감. 닿으면 다 주워 가져가기 (보관 장소가 없으면 쉼). 금화가 없어졌으면 쉼
        void UpdateFetch()
        {
            if (TryNotice()) return;
            // 떨어진 금화 지점
            Transform loot = _service.Loot;
            if (loot == null)
            {
                _state = EnemyState.Idle;
                return;
            }
            if (FlatDistance(loot.position) <= PickupDistance)
            {
                CarriedGold += _service.TakeLoot();
                _state = CarriedGold > 0 && _service.StashPoint != null ? EnemyState.Carry : EnemyState.Idle;
                _repath = 0f;
                return;
            }
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            MoveTo(loot.position, Monster.Speed);
        }
        #endregion

        #region UpdateCarry - 보관 장소로 감. 닿으면 보관하고 집으로 돌아가며 쉼
        void UpdateCarry()
        {
            if (TryNotice()) return;
            // 보관 장소
            Transform stash = _service.StashPoint;
            if (stash == null)
            {
                _state = EnemyState.Idle;
                return;
            }
            if (FlatDistance(stash.position) <= StoreDistance)
            {
                _service.StoreLoot(CarriedGold);
                CarriedGold = 0;
                _state = EnemyState.Idle;
                MoveTo(_home, Monster.Speed * WanderSpeedScale);
                return;
            }
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            MoveTo(stash.position, Monster.Speed);
        }
        #endregion

        #region FlatDistance - 그 곳까지 평면 거리
        /// <param name="point">곳</param>
        float FlatDistance(Vector3 point) => Vector3.ProjectOnPlane(point - transform.position, Vector3.up).magnitude;
        #endregion

        #region Wake - 쫓는 중·죽은 게 아니면 깨어 target을 쫓음 (무리 깨우기·맞았을 때. 쉬는 중·금화 줍는 중·가져가는 중 다)
        /// <param name="target">쫓을 몸</param>
        public void Wake(FieldUnitController target)
        {
            if (_state == EnemyState.Chase || _state == EnemyState.Dead || target == null || !target.IsAlive) return;
            _state = EnemyState.Chase;
            _target = target;
            _retarget = RetargetSeconds;
            _repath = 0f;
            SetEngaged(true);
        }
        #endregion

        #region UpdateChase - 대상을 다시 고르고, 46m 넘으면 포기. 닿으면 멈춰서 준비 → 때림, 아니면 쫓아감
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateChase(float dt)
        {
            _retarget -= dt;
            if (_retarget <= 0f || _target == null || !_target.IsAlive)
            {
                _retarget = RetargetSeconds;
                _target = PickTarget();
            }
            if (_target == null || !FieldService.SameLevel(_target.transform.position, transform.position))
            {
                GiveUp();
                return;
            }
            // 대상까지 평면 벡터
            Vector3 to = _target.transform.position - transform.position;
            to.y = 0f;
            // 대상까지 평면 거리
            float distance = to.magnitude;
            if (distance > GiveUpDistance)
            {
                GiveUp();
                return;
            }
            // 닿는 거리 (v1: 공격 거리 + 대상 반지름 + 자기 반지름 × 0.4)
            float reach = Monster.AttackRange + _target.Radius + _unit.Radius * SelfReachRatio;
            if (distance <= reach)
            {
                Halt();
                Face(to);
                if (_wind > 0f)
                {
                    _wind -= dt;
                    if (_wind > 0f) return;
                    _wind = 0f;
                    _cooldown = Monster.AttackInterval;
                    _target.TakeDamage(Monster.Damage, _unit);
                }
                else if (_cooldown <= 0f)
                {
                    _wind = WindUpSeconds;
                }
                return;
            }
            if (_wind > 0f && distance > reach + WindCancelMargin) _wind = 0f;
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            MoveTo(_target.transform.position, Monster.Speed);
        }
        #endregion

        #region PickTarget - 마법사를 노리는 적이고 마법사가 26m 안이면 마법사, 아니면 가장 가까운 파티 몸 (46m 안). 없으면 null
        FieldUnitController PickTarget()
        {
            // 마법사 몸
            FieldUnitController player = _service.Player;
            if (_focusMage && player.IsAlive && (player.transform.position - transform.position).sqrMagnitude <= MageFocusDistance * MageFocusDistance)
                return player;
            return _service.Nearest(FieldTeam.Party, transform.position, GiveUpDistance);
        }
        #endregion

        #region GiveUp - 쫓기를 그만두고 그 자리를 집으로 삼아 쉼
        void GiveUp()
        {
            _state = EnemyState.Idle;
            _target = null;
            _wind = 0f;
            _home = transform.position;
            _wanderWait = Random.Range(WanderWaitMin, WanderWaitMax);
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.ResetPath();
            }
            SetEngaged(false);
        }
        #endregion

        #region SetEngaged - 쫓는 중인지 바뀌었을 때만 현장 규칙에 알림
        /// <param name="engaged">쫓는 중인지</param>
        void SetEngaged(bool engaged)
        {
            if (_engaged == engaged || _service == null) return;
            _engaged = engaged;
            _service.SetEngaged(engaged);
        }
        #endregion

        #region OnDamaged - 마법사가 때렸으면 대상 체력 막대에 알림. 쉬고 있었으면 때린 몸을 쫓게 무리를 깨움
        /// <param name="unit">맞은 몸 (이 적)</param>
        /// <param name="amount">실제 피해</param>
        /// <param name="source">때린 몸 (없으면 null)</param>
        void OnDamaged(FieldUnitController unit, float amount, FieldUnitController source)
        {
            if (source != null && source == _service.Player) _service.ReportHit(_unit);
            if (_state != EnemyState.Chase && _state != EnemyState.Dead && source != null && source.IsAlive) _service.WakePack(Pack, source);
        }
        #endregion

        #region OnUnitDied - 죽음: 쫓기 그만, 움직임·콜라이더 끄고 눕힘, 죽음 알림
        /// <param name="unit">죽은 몸 (이 적)</param>
        void OnUnitDied(FieldUnitController unit)
        {
            _state = EnemyState.Dead;
            SetEngaged(false);
            _target = null;
            _wind = 0f;
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _agent.enabled = false;
            _collider.enabled = false;
            if (CarriedGold > 0)
            {
                _service.DropLoot(transform.position, CarriedGold);
                CarriedGold = 0;
            }
            LieDown();
            Died?.Invoke(this);
        }
        #endregion

        #region Push - 살아 있으면 NavMesh 위로 밀쳐냄 (물 투사)
        /// <param name="offset">밀 거리·방향 (월드)</param>
        public void Push(Vector3 offset)
        {
            if (_state == EnemyState.Dead || !_agent.isOnNavMesh) return;
            _agent.Move(offset);
        }
        #endregion

        #region MoveTo - NavMesh 위면 그 속도로 그 곳까지 길을 구함
        /// <param name="point">갈 곳</param>
        /// <param name="speed">속도 (m/s)</param>
        void MoveTo(Vector3 point, float speed)
        {
            if (!_agent.isOnNavMesh) return;
            _agent.speed = speed;
            _agent.isStopped = false;
            _agent.SetDestination(point);
        }
        #endregion

        #region Halt - NavMesh 위면 멈춤
        void Halt()
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
        }
        #endregion

        #region Face - 평면 방향을 바라봄 (멈춰서 때릴 때)
        /// <param name="flat">바라볼 평면 방향</param>
        void Face(Vector3 flat)
        {
            if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
        }
        #endregion

        #region LieDown - 도형을 옆으로 눕힘 (시체)
        void LieDown() => SetRise(0f);
        #endregion

        #region SetRise - 누움(0) ~ 섬(1) 사이 모습 (리셋 연출이 일어나는 동안 부름)
        /// <param name="t">일어난 비율 (0~1)</param>
        public void SetRise(float t)
        {
            t = Mathf.Clamp01(t);
            _visual.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, 0f, 90f), Quaternion.identity, t);
            _visual.localPosition = Vector3.up * (_unit.Radius * CorpseLift * (1f - t));
        }
        #endregion

        #region Revive - 시체면 다시 일어남 (숲 리셋): 체력을 채우고 움직임·콜라이더를 켜고 그 집에서 쉼, 현장 규칙에 등록
        /// <param name="service">현장 규칙</param>
        /// <param name="home">돌아다닐 집 (원래 적 자리)</param>
        public void Revive(FieldService service, Vector3 home)
        {
            if (_state != EnemyState.Dead || Monster == null) return;
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _home = home;
            _unit.Init(Monster.MaxHp);
            _focusMage = Random.value < Monster.MageFocusChance;
            _wanderWait = Random.Range(0f, WanderWaitMax);
            _collider.enabled = true;
            _agent.enabled = true;
            SetRise(1f);
            _state = EnemyState.Idle;
            _unit.Damaged -= OnDamaged;
            _unit.Damaged += OnDamaged;
            _unit.Died -= OnUnitDied;
            _unit.Died += OnUnitDied;
            service.RegisterEnemy(this);
        }
        #endregion

        #region OnDestroy - 몸 알림 구독 해제
        void OnDestroy()
        {
            if (_unit == null) return;
            _unit.Damaged -= OnDamaged;
            _unit.Died -= OnUnitDied;
        }
        #endregion
    }
}
