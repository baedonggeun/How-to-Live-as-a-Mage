using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>보스 패턴 (같은 오브젝트의 EnemyController와 같이 씀): 쫓는 중에 패턴 간격마다 강타(마법사 자리에 붉은 원 예고 → 지연 뒤 원 안 파티 피해) 또는 불탄(마법사에게). 체력 절반에서 한 번 부하를 부름. 수치는 MonsterSO</summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class BossPatternController : MonoBehaviour
    {
        /// <summary>불탄 속도 (m/s)</summary>
        const float BoltSpeed = 18f;

        /// <summary>불탄 최대 비행 시간 (초)</summary>
        const float BoltLifeSeconds = 3f;

        /// <summary>부하를 부르는 체력 비율 (v1 족장 절반)</summary>
        const float SummonHpRatio = 0.5f;

        /// <summary>불탄 색</summary>
        static readonly Color BoltColor = new(1f, 0.3f, 0.15f);

        /// <summary>같은 보스 (인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _enemy;

        /// <summary>강타 예고 원 프리팹 (Telegraph.prefab, 지름 1. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _telegraphPrefab;

        /// <summary>불탄 프리팹 (SpellProjectile.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _boltPrefab;

        /// <summary>불탄이 나가는 곳 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _muzzle;

        /// <summary>강타 범위 안 몸 (강타 때 다시 씀)</summary>
        readonly List<FieldUnitController> _found = new();

        /// <summary>현장 규칙 (Init에서 받음)</summary>
        FieldService _service;

        /// <summary>부하 부르기 (몬스터, 근처 → 부른 적. FieldState가 넘김)</summary>
        Func<MonsterSO, Vector3, EnemyController> _summon;

        /// <summary>다음 패턴까지 (초)</summary>
        float _timer;

        /// <summary>강타 예고 남은 시간 (초, 0이면 예고 중 아님)</summary>
        float _slamLeft;

        /// <summary>강타 자리</summary>
        Vector3 _slamAt;

        /// <summary>예고 원 (처음 쓸 때 만듦)</summary>
        GameObject _telegraph;

        /// <summary>부하를 불렀는지 (한 번만)</summary>
        bool _summoned;

        #region Init - 현장 규칙·부하 부르기를 받고 첫 패턴 시간을 정함
        /// <param name="service">현장 규칙</param>
        /// <param name="summon">부하 부르기</param>
        public void Init(FieldService service, Func<MonsterSO, Vector3, EnemyController> summon)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _summon = summon;
            _summoned = false;
            _slamLeft = 0f;
            ResetTimer();
        }
        #endregion

        #region Update - 멈췄거나 죽었으면 예고를 숨기고 안 함. 쫓는 중이면 체력 절반 부하, 예고 중이면 지연 뒤 강타, 아니면 간격마다 패턴
        void Update()
        {
            if (_service == null || _enemy == null || _enemy.Monster == null) return;
            if (!_enemy.Unit.IsAlive)
            {
                HideTelegraph();
                return;
            }
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || _enemy.Target == null) return;
            TrySummon();
            if (_slamLeft > 0f)
            {
                _slamLeft -= dt;
                if (_slamLeft <= 0f) Slam();
                return;
            }
            _timer -= dt;
            if (_timer > 0f) return;
            ResetTimer();
            if (_enemy.Monster.Pattern == BossPattern.Slam) StartSlam();
            else if (_enemy.Monster.Pattern == BossPattern.FireBolt) ShootBolt();
        }
        #endregion

        #region ResetTimer - 다음 패턴까지 시간을 최소~최대 사이에서 고름
        void ResetTimer()
        {
            if (_enemy == null || _enemy.Monster == null) return;
            _timer = UnityEngine.Random.Range(_enemy.Monster.PatternIntervalMin, _enemy.Monster.PatternIntervalMax);
        }
        #endregion

        #region TrySummon - 체력이 절반 아래로 처음 내려가면 부하를 그 수만큼 부름
        void TrySummon()
        {
            // 보스 데이터
            MonsterSO monster = _enemy.Monster;
            if (_summoned || _summon == null || monster.SummonCount <= 0 || monster.Summon == null) return;
            if (_enemy.Unit.Hp > _enemy.Unit.MaxHp * SummonHpRatio) return;
            _summoned = true;
            for (int i = 0; i < monster.SummonCount; i++) _summon(monster.Summon, transform.position);
        }
        #endregion

        #region StartSlam - 마법사 자리에 강타 반지름 붉은 원을 띄우고 예고 시간을 셈 (v1)
        void StartSlam()
        {
            _slamAt = _service.Player.transform.position;
            _slamLeft = _enemy.Monster.PatternDelay;
            if (_telegraph == null && _telegraphPrefab != null) _telegraph = Instantiate(_telegraphPrefab);
            if (_telegraph == null) return;
            _telegraph.transform.position = _slamAt;
            _telegraph.transform.localScale = new Vector3(_enemy.Monster.PatternRadius * 2f, 1f, _enemy.Monster.PatternRadius * 2f);
            _telegraph.SetActive(true);
        }
        #endregion

        #region Slam - 원 안 파티 몸에 피해 (동료도 맞음), 예고 숨김
        void Slam()
        {
            _service.FindInRadius(FieldTeam.Party, _slamAt, _enemy.Monster.PatternRadius, _found);
            foreach (FieldUnitController unit in _found) unit.TakeDamage(_enemy.Monster.PatternDamage, _enemy.Unit);
            HideTelegraph();
        }
        #endregion

        #region ShootBolt - 마법사 겨눌 점으로 불탄 (파티를 노림)
        void ShootBolt()
        {
            if (_boltPrefab == null || !_service.Player.IsAlive) return;
            // 불탄이 나가는 곳
            Vector3 from = _muzzle != null ? _muzzle.position : _enemy.Unit.AimPoint;
            // 방향
            Vector3 direction = (_service.Player.AimPoint - from).normalized;
            // 쏜 불탄
            SpellProjectileController bolt = Instantiate(_boltPrefab, from, Quaternion.LookRotation(direction));
            bolt.Launch(direction, BoltSpeed, BoltLifeSeconds, _enemy.Monster.PatternDamage, 0f, 0f, FieldTeam.Party, _enemy.Unit, _service, BoltColor);
        }
        #endregion

        #region HideTelegraph - 예고 원을 숨기고 예고를 멈춤
        void HideTelegraph()
        {
            _slamLeft = 0f;
            if (_telegraph != null) _telegraph.SetActive(false);
        }
        #endregion

        #region OnDestroy - 만든 예고 원을 지움
        void OnDestroy()
        {
            if (_telegraph != null) Destroy(_telegraph);
        }
        #endregion
    }
}
