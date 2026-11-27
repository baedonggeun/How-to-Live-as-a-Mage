using UnityEngine;

namespace Mage
{
    /// <summary>숲 화면·씬 참조 (Forest 씬 Field 오브젝트)</summary>
    public sealed class FieldView : MonoBehaviour
    {
        /// <summary>1인칭 조작 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] PlayerController _player;

        /// <summary>바라본 지점 찾기 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] InteractDetector _detector;

        /// <summary>마법사 몸 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldUnitController _playerUnit;

        /// <summary>현장 시전 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldCastController _cast;

        /// <summary>안개 밝히기 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] FogRevealController _fogReveal;

        /// <summary>숲 HUD 참조 (Canvas/Hud. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _hud;

        /// <summary>숲 HUD 바인더 (Canvas/Hud. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudUIBinder _hudBinder;

        /// <summary>들어오면 서는 곳 (숲 가운데 전초기지 정문 밖, 숲을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _entry;

        /// <summary>죽은 뒤 용병이 도망치는 곳 (닫힌 정문 앞. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _exitPoint;

        /// <summary>차원문 표지 (Portals 자식. 자리는 차원문 캡슐이 정함. 인스펙터에서만 연결)</summary>
        [SerializeField] PortalView[] _portals;

        /// <summary>안개 지도 지역 (x·y = 월드 x·z 최소, m. 지역 가운데에 맞춘 정사각형)</summary>
        [SerializeField] Rect _mapArea = new(-210f, -210f, 420f, 420f);

        /// <summary>지역 가운데 (전초기지 가운데, 월드)</summary>
        [SerializeField] Vector3 _regionCenter = Vector3.zero;

        /// <summary>1지역이 시작하는 반지름 (m, 전초기지 성벽 바깥)</summary>
        [SerializeField, Min(0f)] float _regionInner = 30f;

        /// <summary>지역 하나 폭 (m)</summary>
        [SerializeField, Min(1f)] float _regionWidth = 60f;

        /// <summary>지역 수</summary>
        [SerializeField, Min(1)] int _regionCount = 3;

        /// <summary>적 무리 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] SpawnPointView[] _spawnPoints;

        /// <summary>적 프리팹 (Enemy.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _enemyPrefab;

        /// <summary>용병 프리팹 (Mercenary.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] MercenaryController _mercenaryPrefab;

        /// <summary>떨어진 금화 프리팹 (GoldPile.prefab, 지점 id gold. 인스펙터에서만 연결)</summary>
        [SerializeField] InteractPointView _goldPilePrefab;

        /// <summary>소탕 중 목표 (숲 외곽의 고블린 소탕. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _clearObjective;

        /// <summary>소탕을 끝낸 뒤 목표 (숲 입구로 돌아가기. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _returnObjective;

        /// <summary>숲 시계 (Field 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldClockController _clock;

        /// <summary>숲 리셋 연출 (ResetSequence 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] ForestResetSequence _resetSequence;

        /// <summary>HUD 캔버스 (리셋 연출 동안 끔. 인스펙터에서만 연결)</summary>
        [SerializeField] Canvas _hudCanvas;

        /// <summary>고블린 던전 씬 참조 (Dungeon 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] DungeonView _dungeon;

        /// <summary>1인칭 조작</summary>
        public PlayerController Player => _player;

        /// <summary>바라본 지점 찾기</summary>
        public InteractDetector Detector => _detector;

        /// <summary>마법사 몸</summary>
        public FieldUnitController PlayerUnit => _playerUnit;

        /// <summary>현장 시전</summary>
        public FieldCastController Cast => _cast;

        /// <summary>안개 밝히기</summary>
        public FogRevealController FogReveal => _fogReveal;

        /// <summary>숲 HUD 참조</summary>
        public FieldHudView Hud => _hud;

        /// <summary>숲 HUD 바인더</summary>
        public FieldHudUIBinder HudBinder => _hudBinder;

        /// <summary>들어오면 서는 곳</summary>
        public Transform Entry => _entry;

        /// <summary>죽은 뒤 용병이 도망치는 곳</summary>
        public Transform ExitPoint => _exitPoint;

        /// <summary>차원문 표지</summary>
        public PortalView[] Portals => _portals;

        /// <summary>안개 지도 지역</summary>
        public Rect MapArea => _mapArea;

        /// <summary>지역 가운데</summary>
        public Vector3 RegionCenter => _regionCenter;

        /// <summary>1지역이 시작하는 반지름 (m)</summary>
        public float RegionInner => _regionInner;

        /// <summary>지역 하나 폭 (m)</summary>
        public float RegionWidth => _regionWidth;

        /// <summary>지역 수</summary>
        public int RegionCount => _regionCount;

        /// <summary>적 무리 자리</summary>
        public SpawnPointView[] SpawnPoints => _spawnPoints;

        /// <summary>적 프리팹</summary>
        public EnemyController EnemyPrefab => _enemyPrefab;

        /// <summary>용병 프리팹</summary>
        public MercenaryController MercenaryPrefab => _mercenaryPrefab;

        /// <summary>떨어진 금화 프리팹</summary>
        public InteractPointView GoldPilePrefab => _goldPilePrefab;

        /// <summary>소탕 중 목표</summary>
        public ObjectiveSO ClearObjective => _clearObjective;

        /// <summary>소탕을 끝낸 뒤 목표</summary>
        public ObjectiveSO ReturnObjective => _returnObjective;

        /// <summary>숲 시계</summary>
        public FieldClockController Clock => _clock;

        /// <summary>숲 리셋 연출</summary>
        public ForestResetSequence ResetSequence => _resetSequence;

        /// <summary>HUD 캔버스</summary>
        public Canvas HudCanvas => _hudCanvas;

        /// <summary>고블린 던전 씬 참조</summary>
        public DungeonView Dungeon => _dungeon;
    }
}
