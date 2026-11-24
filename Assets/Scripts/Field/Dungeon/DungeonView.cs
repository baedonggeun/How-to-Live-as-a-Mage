using UnityEngine;

namespace Mage
{
    /// <summary>고블린 던전 씬 참조 (Forest 씬 Dungeon 오브젝트). DungeonService가 받아 씀</summary>
    public sealed class DungeonView : MonoBehaviour
    {
        /// <summary>던전 입구 밖 (나오면 서는 곳, 줍는 몬스터가 물건을 가져가는 곳. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _outside;

        /// <summary>던전 안 시작 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _startPoint;

        /// <summary>던전 적 무리 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] SpawnPointView[] _spawnPoints;

        /// <summary>보스 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _bossPoint;

        /// <summary>보스 후보 (던전을 만들 때 하나를 고름. 인스펙터에서만 연결)</summary>
        [SerializeField] MonsterSO[] _bossChoices;

        /// <summary>보스 프리팹 (Boss.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _bossPrefab;

        /// <summary>적·부하 프리팹 (Enemy.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _enemyPrefab;

        /// <summary>던전 입구 밖</summary>
        public Transform Outside => _outside;

        /// <summary>던전 안 시작 자리</summary>
        public Transform StartPoint => _startPoint;

        /// <summary>던전 적 무리 자리</summary>
        public SpawnPointView[] SpawnPoints => _spawnPoints;

        /// <summary>보스 자리</summary>
        public Transform BossPoint => _bossPoint;

        /// <summary>보스 후보</summary>
        public MonsterSO[] BossChoices => _bossChoices;

        /// <summary>보스 프리팹</summary>
        public EnemyController BossPrefab => _bossPrefab;

        /// <summary>적·부하 프리팹</summary>
        public EnemyController EnemyPrefab => _enemyPrefab;
    }
}
