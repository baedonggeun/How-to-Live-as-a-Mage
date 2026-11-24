using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Mage
{
    /// <summary>숲 적 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 숲 판 데이터·적 자리·적 프리팹·현장 규칙을 받아, 적 자리마다 수만큼 놓음(쓰러진 키는 쓰러진 곳에 시체, 아니면 자리 둘레에 살아 있는 적. 무리 번호는 자리 순서). 살아 있는 적·시체를 놓을 때와 적이 쓰러질 때 알리고, 쓰러진 곳을 판 데이터에 남김. 리셋으로 살아난 적을 다시 받으면 쓰러진 기록을 비움</summary>
    public sealed class ForestEnemyService
    {
        /// <summary>적 키 형식 ({0} 자리 id, {1} 번호)</summary>
        const string SpawnKeyFormat = "{0}.{1}";

        /// <summary>숲 판 데이터</summary>
        readonly ForestEntry _data;

        /// <summary>적 무리 자리</summary>
        readonly SpawnPointView[] _points;

        /// <summary>적 프리팹</summary>
        readonly EnemyController _prefab;

        /// <summary>현장 규칙 (놓은 적이 씀)</summary>
        readonly FieldService _field;

        /// <summary>숲 적 자리 수 합 (놓을 때 셈. 리셋 70% 판정)</summary>
        public int Total { get; private set; }

        /// <summary>쓰러뜨린 숲 적 수</summary>
        public int DeadCount => _data.Dead.Count;

        /// <summary>살아 있는 적을 놓음 (리셋으로 살아난 적도)</summary>
        public event Action<EnemyController> EnemySpawned;

        /// <summary>시체를 놓음 (시체, 살아나면 돌아갈 집 = 원래 적 자리)</summary>
        public event Action<EnemyController, Vector3> CorpseSpawned;

        /// <summary>숲 적이 쓰러짐</summary>
        public event Action<EnemyController> EnemyDied;

        #region ForestEnemyService - 숲 판 데이터·적 자리·적 프리팹·현장 규칙을 받음
        /// <param name="data">숲 판 데이터</param>
        /// <param name="points">적 무리 자리</param>
        /// <param name="prefab">적 프리팹</param>
        /// <param name="field">현장 규칙</param>
        public ForestEnemyService(ForestEntry data, SpawnPointView[] points, EnemyController prefab, FieldService field)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }
        #endregion

        #region Spawn - 적 자리마다 수만큼 놓음: 쓰러진 키는 쓰러진 곳에 시체, 아니면 자리 둘레에 살아 있는 적 (놓을 때마다 알림). 무리 번호는 자리 순서
        public void Spawn()
        {
            // 무리 번호 (자리마다 하나)
            int pack = 0;
            Total = 0;
            foreach (SpawnPointView point in _points)
            {
                if (point == null || point.Monster == null)
                {
                    Debug.LogWarning("[Forest] 비었거나 몬스터가 없는 적 자리", point);
                    continue;
                }
                pack++;
                Total += point.Count;
                for (int i = 0; i < point.Count; i++)
                {
                    // 이 적 키
                    string key = string.Format(CultureInfo.InvariantCulture, SpawnKeyFormat, point.Id, i);
                    // 쓰러졌는지, 쓰러진 곳
                    bool dead = _data.Dead.TryGetValue(key, out Vector3 corpseAt);
                    // 놓은 적
                    EnemyController enemy = Object.Instantiate(_prefab, FieldService.Sample(dead ? corpseAt : SpawnPosition(point)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    if (dead)
                    {
                        enemy.InitCorpse(point.Monster, key, pack);
                        CorpseSpawned?.Invoke(enemy, point.transform.position);
                        continue;
                    }
                    enemy.Init(point.Monster, _field, key, pack);
                    enemy.Died += OnEnemyDied;
                    EnemySpawned?.Invoke(enemy);
                }
            }
        }
        #endregion

        #region Adopt - 리셋으로 살아난 적을 다시 받음: 쓰러진 기록을 비우고 쓰러짐을 구독, 살아 있는 적으로 알림
        /// <param name="revived">살아난 적</param>
        public void Adopt(IReadOnlyList<EnemyController> revived)
        {
            if (revived == null) throw new ArgumentNullException(nameof(revived));
            _data.Dead.Clear();
            foreach (EnemyController enemy in revived)
            {
                if (enemy == null) continue;
                enemy.Died += OnEnemyDied;
                EnemySpawned?.Invoke(enemy);
            }
        }
        #endregion

        #region OnEnemyDied - 쓰러진 곳을 판 데이터에 남기고 알림
        /// <param name="enemy">쓰러진 적</param>
        void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            _data.Dead[enemy.SpawnKey] = enemy.transform.position;
            EnemyDied?.Invoke(enemy);
        }
        #endregion

        #region SpawnPosition - 적 자리 둘레 흩어 놓는 반지름 안 아무 곳
        /// <param name="point">적 자리</param>
        static Vector3 SpawnPosition(SpawnPointView point)
        {
            // 흩어 놓는 거리
            Vector2 offset = Random.insideUnitCircle * point.Spread;
            return point.transform.position + new Vector3(offset.x, 0f, offset.y);
        }
        #endregion
    }
}
