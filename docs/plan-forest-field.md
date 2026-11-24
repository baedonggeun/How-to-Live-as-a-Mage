# Plan: 6-1 숲 — 3구역 회색 숲, 고정 적·용병 AI, 현장 시전(3초 조준), 안개 지도, 입구 귀환, 죽음과 여관 부활

## 결정 (사용자, 2026-11-23. 기획 12 결정 68~86)

1. 첫 의뢰(튜토리얼 의뢰) = 숲 고블린 소탕: 숲 외곽의 고블린을 다 쓰러뜨리면 목표 달성
2. 숲 입구에서 상호작용(F)해 귀환. 의뢰를 못 끝내고 와도 되고 의뢰는 남음 (의뢰는 기간이 지나지 않으면 남음. 날짜·기간은 6-2)
3. 마법사가 죽으면 들고 있던 물품·장비·금화를 그 자리에 낱개로 떨어뜨리고 여관 방 침대(방을 안 잡았으면 정문)에서 부활. 코덱스는 어떤 경우에도 안 잃음. 못 찾고 또 죽으면 앞에 떨어진 것은 사라짐 (고블린이 주워 가는 건 6-2)
4. 마법사가 죽으면 부활 로딩 동안 용병은 마을(숲 입구)로 도망치고 몬스터는 쫓음. 무사히 도착하면 살고 도중에 죽으면 죽음. 용병은 체력 0이면 바로 죽음 (부축 보류)
5. 숲 단계 범위: 안개 지도(밝히기만, 위험 배율·기습 안 넣음). 부축·물약·두루마리·채집·정산·보상은 보류. 숲 화면 배치는 보류하고 v1 배치로 둠
6. 판정 뒤 3초 조준 (시점만 돌림, 좌클릭이나 3초 끝 발사, 우클릭 취소). Space는 시전 상태에서 바로 판정, 평소엔 점프
7. 룬 하나만 그리면: 불·물처럼 무언가를 만드는 룬만 혼자 던짐 (약함). 응축·직선·차단은 혼자서는 효과 없음. 만드는 룬이 없는 판은 공격 수단 없이 진행
8. 숲 첫 의뢰의 적 = 고블린 무리 + 짐승(늑대 무리·멧돼지). 짐승은 완료 조건 아님
9. 용병 명령 없앰. 지금은 알아서 따라오며 싸우고, 레인저는 활로 멀리서 쏨 (상황별·등급별 행동은 나중)
10. 12시 조작 안내는 전투 상황이 아닐 때만. 전투 상황 = 적이 플레이어를 알아채고 공격하러 오는 순간부터 (첫 튜토리얼 의뢰 전투의 설명창은 6-2)
11. 숲은 고정된 지역: 쓰러뜨린 몬스터는 그대로 남음 (70% 리셋과 그 연출은 6-2)
12. 숲은 3구역, 숲 씬 하나에 전 지역. 차원문은 3구역 중 전초기지에서 먼 쪽 절반의 무작위 위치
13. 점프 Space, 앉기 Ctrl, 달리기 Shift (뒤로는 못 달림). 스태미나를 다 쓰면 못 달림
14. 상호작용은 F 그대로. 물약·두루마리 단축키는 U·I·O·P 줄 (넣을 때 배정, 이번엔 없음)
15. 수치는 전부 임시로 프로토타입(`field-spells.html`)·v1 값

구현 임시 (사용자 확인 전, 기획에 '구현 임시'로 적음):
- 앉기: 누르는 동안, 머리 높이 1.0m·걷기 ×0.5, 앉으면 못 달리고 못 뜀. 적이 덜 알아채는 효과 없음
- 점프 처음 속도 5m/s (약 0.6m), 스태미나 안 씀. 스태미나 최대 100, 달리면 초당 25, 멈추고 1초 뒤 초당 20 회복, 다 쓰면 30%가 찰 때까지 못 달림
- 숲 180 × 720m (구역마다 240m), 띠는 1 외곽·2 중간·3 심층. 2·3구역은 v1 띠별 짐승 (거대 거미·곰 / 변이 짐승·늑대). 무리는 v1보다 많이
- 차원문 자리는 그 판에서 처음 숲에 들어갈 때 정함. 이번엔 들어갈 수 없음
- 죽은 뒤 도망: 덮개 밑에서 게임 속도 6배, 최대 게임 시간 40초. 쫓는 적 없이 3초 지나면 무사로 침
- 소탕을 끝낸 뒤 죽어도, 마을로 오면 의뢰를 끝냄 (입구 귀환과 같게)
- 마법사 체력 124·마나 100·초당 회복 9 (v1). 만드는 룬 혼자: 불 피해 9·마나 3, 물 피해 5·밀쳐내기 2m·마나 4
- 쓰러뜨린 적은 다음에 들어가도 쓰러진 자리에 시체로 남음 (6-2 리셋 연출에서 시체를 씀)
- 숲에서 처음 생략 없이 그린 다중 룬 마법도 코덱스에 등록 (책상 연습과 같은 토스트)

---

## 현행 분석 (`16eeebe`)

- `FieldState`: 판 데이터 디버그 글자(`FieldDebugView`, UI/`field.debug_info`)만 보이고 어디서든 F(`Player/Interact`)를 누르면 `ReturningFromField`를 켜고 `HubState`로
- `Forest.unity`: `Main Camera`, `Directional Light`, `Ground`, `Canvas/InfoPanel`(글자 하나). 플레이어 없음, CanvasScaler 없음
- `Player.prefab`: 루트 `CharacterController`(높이 1.8, 반지름 0.35, 중심 y 0.9) + `PlayerController` + `InteractDetector`, `Head`(y 1.6)에 Camera·AudioListener, `Visual/Body`·`HeadBall`
- `PlayerController`: 이동·달리기·마우스 시점·중력. 점프·앉기·스태미나 없음. 상태가 `enabled`로 켜고 끔
- `SpellDrawController`: UI 칸의 포인터 이벤트로 그림 (좌클릭 시전 상태, 떼도 이어 그림, 대기 3초·`Player/Cast`(Space) 판정, 우클릭 취소). 판정·이름 함수는 쓰는 쪽이 `Begin`으로 넘김. 결과 글·자세히·대기 막대·안내 글은 `SpellDrawView`
- `CodexController`: 비교 그림 = 다중 룬 마법 그림 뒤에 룬 그림 (`ShapeIndex`가 이 번호), `SpellRecognitionCalculator.BuildCandidates(그림, 익힌 룬, 등록한 마법, 생략 가능 수)` → `Judge`(기준 기본값). 이름은 Codex/`codex.rune_spell`, 생략 없이 처음 그린 마법 등록은 Codex/`codex.toast.registered` 토스트
- `TutorialManager`: `Notify`로 대기열 → 12시 안내 하나씩, 이동·시점·달리기는 입력을 재서 배움. `MarkLearned`는 저장만 하고 떠 있는 안내는 안 내림. 숨기는 기능 없음
- `GameManager.ApplyTimeScale`: 로딩창·일시정지 메뉴면 0, 아니면 1. `TransitionManager.ShowCover`는 public이고 게임 시간은 안 건드림. 숲에서 바로 Play하면 디버그 판에 용병·의뢰 없음
- `HubState`: 숲에서 돌아오면 의뢰를 늘 비움. 돌아온 뒤 목표는 `GuildObjective`(의뢰가 없어서). 게시판은 '받음'만 앎
- `UIManager`: 메뉴 창 지도 탭은 자리 글(Menu/`menu.map.placeholder`)뿐. `ShowQuestBoard(quest, taken, onTake)`
- 입력: `Player/Attack` = 왼쪽 버튼·Enter 등, `Crouch` = C, `Jump`·`Cast` = Space. 플레이어 묶음에 오른쪽 버튼 액션 없음
- 패키지: AI Navigation 2.0.10 (NavMeshSurface). 레이어는 기본만
- 프로토타입 `field-spells.html`: 몬스터 표 `MON`, 적(돌아다님·알아채면 무리 깨움·46m 포기·0.26초 준비·마법사 노림), 동료(22·13m 교전, 대형 오프셋, 1초마다 3 + 전투 × 1.5), 주문 효과 표 `SPELL_FX`, 안개 3m 칸·반경 24m·60m 구획 45%

---

## C# 변경

### `Assets/Scripts/Field/MonsterSO.cs` (새 파일, 새 폴더)

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>몬스터 하나 (현장 적). 수치는 v1 값 임시. 숲 적 자리(SpawnPointView)와 의뢰 소탕 대상(QuestSO)이 가리킴</summary>
    [CreateAssetMenu(fileName = "Monster", menuName = "Mage/Monster")]
    public sealed class MonsterSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (goblin, wolf, boar, spider, bear, mutant)</summary>
        [SerializeField] string _id;

        /// <summary>몬스터 이름 (Monster 테이블 monster.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>최대 체력</summary>
        [SerializeField, Min(1f)] float _maxHp = 35f;

        /// <summary>쫓을 때 속도 (m/s)</summary>
        [SerializeField, Min(0.1f)] float _speed = 5.2f;

        /// <summary>한 번 때릴 때 피해</summary>
        [SerializeField, Min(0f)] float _damage = 7f;

        /// <summary>때리고 다음에 때릴 때까지 (초)</summary>
        [SerializeField, Min(0.1f)] float _attackInterval = 1.2f;

        /// <summary>공격이 닿는 거리 (m, 두 몸 반지름 밖으로)</summary>
        [SerializeField, Min(0.1f)] float _attackRange = 1.4f;

        /// <summary>알아채는 거리 (m). 이 안에 파티가 오면 무리 전체가 깨어 쫓아옴</summary>
        [SerializeField, Min(0f)] float _aggroRange = 18f;

        /// <summary>동료를 두고 26m 안의 마법사를 노릴 확률 (0~1, 태어날 때 한 번 정함)</summary>
        [SerializeField, Range(0f, 1f)] float _mageFocusChance;

        /// <summary>몸 크기 배율 (회색 도형·몸 반지름에 곱함)</summary>
        [SerializeField, Min(0.1f)] float _size = 0.82f;

        /// <summary>회색 도형 색</summary>
        [SerializeField] Color _color = Color.gray;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>몬스터 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>최대 체력</summary>
        public float MaxHp => _maxHp;

        /// <summary>쫓을 때 속도 (m/s)</summary>
        public float Speed => _speed;

        /// <summary>한 번 때릴 때 피해</summary>
        public float Damage => _damage;

        /// <summary>때리고 다음에 때릴 때까지 (초)</summary>
        public float AttackInterval => _attackInterval;

        /// <summary>공격이 닿는 거리 (m)</summary>
        public float AttackRange => _attackRange;

        /// <summary>알아채는 거리 (m)</summary>
        public float AggroRange => _aggroRange;

        /// <summary>마법사를 노릴 확률 (0~1)</summary>
        public float MageFocusChance => _mageFocusChance;

        /// <summary>몸 크기 배율</summary>
        public float Size => _size;

        /// <summary>회색 도형 색</summary>
        public Color Color => _color;
    }
}
```

---

### `Assets/Scripts/Quest/QuestSO.cs`

**추가 내용:** 소탕 대상 몬스터. `_days` 필드 아래, `Days` 프로퍼티 아래에 넣음

```csharp
        /// <summary>원정 기간 (일). 식량이 (플레이어 + 용병) × 기간 인분만큼 듦</summary>
        [SerializeField, Min(1)] int _days = 3;

        /// <summary>소탕 대상 몬스터 (숲에서 이 몬스터를 다 쓰러뜨리면 의뢰 목표 달성). 비면 소탕 없음</summary>
        [SerializeField] MonsterSO _target;
```

```csharp
        /// <summary>원정 기간 (일)</summary>
        public int Days => _days;

        /// <summary>소탕 대상 몬스터 (없으면 null)</summary>
        public MonsterSO Target => _target;
```

---

### `Assets/Scripts/Party/JobSO.cs`

**추가 내용:** 활로 쏘는 직업 표시. `_combat` 필드 아래, `Combat` 프로퍼티 아래에 넣음

```csharp
        /// <summary>현장에서 활로 떨어져서 쏘는지 (레인저). 아니면 붙어서 싸움</summary>
        [SerializeField] bool _ranged;
```

```csharp
        /// <summary>현장에서 활로 떨어져서 쏘는지</summary>
        public bool Ranged => _ranged;
```

---

### `Assets/Scripts/Spell/SpellSO.cs`

**추가 내용:** 현장 효과 종류(enum)와 현장 값 6개 (프로토타입 `SPELL_FX`, 임시)

`SpellKind` enum 아래에 새 enum:

```csharp
    /// <summary>현장에서 주문이 내는 효과 (프로토타입 field-spells.html). 위력·범위·시간·재사용 값의 뜻이 효과마다 다름</summary>
    public enum SpellFieldEffect
    {
        /// <summary>현장 효과 없음 (두루마리, 혼자서는 효과가 없는 룬)</summary>
        None,

        /// <summary>화염탄 한 발. 위력 = 피해</summary>
        Bolt,

        /// <summary>화염탄 + 폭발. 위력 = 피해, 범위 = 폭발 반지름 (m, 맞은 적 둘레는 절반 피해)</summary>
        Ball,

        /// <summary>물탄. 위력 = 피해, 범위 = 맞은 적을 밀쳐내는 거리 (m)</summary>
        Water,

        /// <summary>앞쪽 부채꼴 세 발. 위력 = 한 발 피해, 범위 = 가운데 발과 양쪽 발 사이 각도 (도)</summary>
        Fan,

        /// <summary>조준점 불벽. 위력 = 초당 피해, 범위 = 벽 길이 (m), 시간 = 지속 (초)</summary>
        Wall,

        /// <summary>방호막 (조준 없이 바로). 위력 = 받는 피해 배율, 시간 = 지속 (초), 재사용 = 다시 펼칠 수 있을 때까지 (초)</summary>
        Shield,
    }
```

`_parts` 필드 아래:

```csharp
        /// <summary>현장 효과 (두루마리·현장 값이 없는 주문은 None)</summary>
        [SerializeField] SpellFieldEffect _fieldEffect;

        /// <summary>현장 마나 (임시)</summary>
        [SerializeField, Min(0)] int _manaCost;

        /// <summary>현장 위력 (뜻은 SpellFieldEffect)</summary>
        [SerializeField, Min(0f)] float _power;

        /// <summary>현장 범위 (뜻은 SpellFieldEffect)</summary>
        [SerializeField, Min(0f)] float _area;

        /// <summary>현장 지속 시간 (초. 벽·방호막)</summary>
        [SerializeField, Min(0f)] float _duration;

        /// <summary>다시 쓸 수 있을 때까지 (초. 방호막)</summary>
        [SerializeField, Min(0f)] float _cooldown;
```

`Parts` 프로퍼티 아래:

```csharp
        /// <summary>현장 효과</summary>
        public SpellFieldEffect FieldEffect => _fieldEffect;

        /// <summary>현장 마나</summary>
        public int ManaCost => _manaCost;

        /// <summary>현장 위력</summary>
        public float Power => _power;

        /// <summary>현장 범위</summary>
        public float Area => _area;

        /// <summary>현장 지속 시간 (초)</summary>
        public float Duration => _duration;

        /// <summary>다시 쓸 수 있을 때까지 (초)</summary>
        public float Cooldown => _cooldown;
```

---

### `Assets/Scripts/Spell/RuneSO.cs`

**추가 내용:** 룬 하나만 그렸을 때 던지는 값 (만드는 룬만, 임시). `_strokes` 필드 아래, `Strokes` 프로퍼티 아래에 넣음

```csharp
        /// <summary>이 룬 하나만 그렸을 때 현장 효과. 불·물처럼 만드는 룬만 정하고, 나머지는 None (혼자서는 효과 없음)</summary>
        [SerializeField] SpellFieldEffect _soloEffect;

        /// <summary>혼자 던질 때 마나 (임시)</summary>
        [SerializeField, Min(0)] int _soloManaCost;

        /// <summary>혼자 던질 때 위력 (뜻은 SpellFieldEffect, 임시)</summary>
        [SerializeField, Min(0f)] float _soloPower;

        /// <summary>혼자 던질 때 범위 (뜻은 SpellFieldEffect, 임시)</summary>
        [SerializeField, Min(0f)] float _soloArea;
```

```csharp
        /// <summary>이 룬 하나만 그렸을 때 현장 효과 (None = 혼자서는 효과 없음)</summary>
        public SpellFieldEffect SoloEffect => _soloEffect;

        /// <summary>혼자 던질 때 마나</summary>
        public int SoloManaCost => _soloManaCost;

        /// <summary>혼자 던질 때 위력</summary>
        public float SoloPower => _soloPower;

        /// <summary>혼자 던질 때 범위</summary>
        public float SoloArea => _soloArea;
```

---

### `Assets/Scripts/Tutorial/TutorialSO.cs`

**추가 내용:** 배우는 행동 2개. `Draw` 아래에 넣음

```csharp
        /// <summary>주문 그리기 (책상 연습). 12시 안내 없이 처음 판정까지 그리면 배움으로 알림</summary>
        Draw,

        /// <summary>현장 시전 (숲). 12시 안내를 띄우고, 숲에서 처음 판정까지 그리면 게임 코드가 배움으로 알림</summary>
        FieldCast,

        /// <summary>지도 (M). 12시 안내를 띄우고, M을 누르면 게임 코드가 배움으로 알림</summary>
        Map,
```

---

### `Assets/Scripts/Core/RunEntry.cs`

**추가 내용:** 맨 위 `using UnityEngine;` 추가. `OmitLimit` 아래에 숲 판 데이터

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
```

```csharp
        /// <summary>생략 가능 수 (레벨이 생기면 레벨이 정함. 지금은 프로토타입 기본 0)</summary>
        public int OmitLimit { get; set; }

        /// <summary>받은 의뢰의 목표를 숲에서 끝냈는지 (소탕 대상을 다 쓰러뜨림). 마을로 오면 HubState가 의뢰를 끝냄</summary>
        public bool QuestCleared { get; set; }

        /// <summary>끝낸 의뢰 id (게시판이 '끝냄'으로 보임)</summary>
        public HashSet<string> CompletedQuestIds { get; } = new(StringComparer.Ordinal);

        /// <summary>숲에서 쓰러뜨린 적: 적 자리 키({자리 id}.{번호}) → 쓰러진 곳. 다시 들어가도 그 자리에 시체로 남음</summary>
        public Dictionary<string, Vector3> ForestDead { get; } = new(StringComparer.Ordinal);

        /// <summary>숲 안개 지도에서 밝힌 칸 (FogMapService가 만들어 넣음. 아직 안 들어갔으면 null)</summary>
        public bool[] ForestFog { get; set; }

        /// <summary>차원문 위치 (그 판에서 처음 숲에 들어갈 때 3구역 먼 쪽 절반에서 정함). 안 정했으면 null</summary>
        public Vector3? PortalPosition { get; set; }

        /// <summary>떨어진 금화 (마법사가 죽은 자리). 없으면 0. 또 죽으면 앞의 것은 사라지고 새로 떨굼</summary>
        public int DroppedGold { get; set; }

        /// <summary>떨어진 금화 자리</summary>
        public Vector3 DroppedGoldPosition { get; set; }

        /// <summary>숲에서 죽어 마을에서 부활하는 중인지 (HubState가 여관 방 침대·정문에서 시작하고 한 번 쓰면 끔)</summary>
        public bool RespawningFromDeath { get; set; }
```

---

### `Assets/Scripts/Field/FieldUnitController.cs` (새 파일)

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>현장 편</summary>
    public enum FieldTeam
    {
        /// <summary>마법사와 용병</summary>
        Party,

        /// <summary>적</summary>
        Enemy,
    }

    /// <summary>현장에서 맞고 죽는 몸 하나 (마법사·용병·적 공통): 편, 체력, 받는 피해 배율, 맞음·죽음 알림. 움직임은 같은 오브젝트의 다른 Controller가 함</summary>
    public sealed class FieldUnitController : MonoBehaviour
    {
        /// <summary>몸 반지름 최솟값 (m)</summary>
        const float MinRadius = 0.05f;

        /// <summary>편 (인스펙터에서만 정함)</summary>
        [SerializeField] FieldTeam _team;

        /// <summary>몸 반지름 (m). 공격 거리·범위 피해에 더함. 적은 크기에 맞춰 코드가 바꿈</summary>
        [SerializeField, Min(MinRadius)] float _radius = 0.4f;

        /// <summary>겨눌 높이 (m, 발밑 기준)</summary>
        [SerializeField, Min(0f)] float _aimHeight = 1f;

        /// <summary>편</summary>
        public FieldTeam Team => _team;

        /// <summary>몸 반지름 (m)</summary>
        public float Radius => _radius;

        /// <summary>최대 체력</summary>
        public float MaxHp { get; private set; }

        /// <summary>지금 체력</summary>
        public float Hp { get; private set; }

        /// <summary>살아 있는지</summary>
        public bool IsAlive => Hp > 0f;

        /// <summary>받는 피해 배율 (방호막이면 1보다 작음)</summary>
        public float DamageScale { get; set; } = 1f;

        /// <summary>겨눌 점 (월드)</summary>
        public Vector3 AimPoint => transform.position + Vector3.up * _aimHeight;

        /// <summary>맞음 (맞은 몸, 실제 피해, 때린 몸. 때린 몸이 없으면 null)</summary>
        public event Action<FieldUnitController, float, FieldUnitController> Damaged;

        /// <summary>죽음 (죽은 몸)</summary>
        public event Action<FieldUnitController> Died;

        #region Init - 체력을 다 채우고 피해 배율을 되돌림
        /// <param name="maxHp">최대 체력 (0보다 큼)</param>
        public void Init(float maxHp)
        {
            if (maxHp <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "최대 체력은 0보다 커야 함");
            MaxHp = maxHp;
            Hp = maxHp;
            DamageScale = 1f;
        }
        #endregion

        #region InitDead - 이미 죽은 몸으로 둠 (다시 들어갈 때 시체. 알림 없음)
        /// <param name="maxHp">최대 체력 (0보다 큼)</param>
        public void InitDead(float maxHp)
        {
            Init(maxHp);
            Hp = 0f;
        }
        #endregion

        #region SetRadius - 몸 반지름을 바꿈 (적 크기)
        /// <param name="radius">새 반지름 (m)</param>
        public void SetRadius(float radius) => _radius = Mathf.Max(MinRadius, radius);
        #endregion

        #region TakeDamage - 살아 있으면 피해 배율을 곱해 체력을 깎고 맞음 알림. 0이 되면 죽음 알림
        /// <param name="amount">피해 (0 이하면 무시)</param>
        /// <param name="source">때린 몸 (없으면 null)</param>
        public void TakeDamage(float amount, FieldUnitController source)
        {
            if (!IsAlive || amount <= 0f) return;
            // 실제로 깎는 피해 (배율 적용, 남은 체력까지만)
            float dealt = Mathf.Min(Hp, amount * DamageScale);
            Hp -= dealt;
            Damaged?.Invoke(this, dealt, source);
            if (Hp > 0f) return;
            Hp = 0f;
            Died?.Invoke(this);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `FieldState`가 마법사 몸 `Init`, 용병·적은 각 Controller의 `Init`이 부름
- `Damaged`: `EnemyController`(무리 깨우기·마지막으로 때린 적), `FieldHudUIBinder`(맞음 테두리) / `Died`: `EnemyController`·`MercenaryController`(시체), `FieldState`(마법사 죽음)

---

### `Assets/Scripts/Field/FieldService.cs` (새 파일)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 한 판의 현장 규칙 (MonoBehaviour 아님. FieldState가 만들어 Controller들에 넘김): 몸 목록, 가까운 상대·조준 보정 대상·범위 안 몸 찾기, 무리 깨우기, 전투 상황, 마법사가 마지막으로 때린 적</summary>
    public sealed class FieldService
    {
        /// <summary>등록된 몸 (마법사·용병·적, 등록 순서. 죽어도 남음)</summary>
        readonly List<FieldUnitController> _units = new();

        /// <summary>등록된 적 (무리 깨우기·노림 확인용)</summary>
        readonly List<EnemyController> _enemies = new();

        /// <summary>쫓는 중인 적 수 (0보다 크면 전투 상황)</summary>
        int _engaged;

        /// <summary>마법사 몸</summary>
        public FieldUnitController Player { get; }

        /// <summary>전투 상황인지 (적이 알아채고 공격하러 오는 중)</summary>
        public bool InCombat => _engaged > 0;

        /// <summary>마법사가 마지막으로 때린 적 몸 (없으면 null)</summary>
        public FieldUnitController LastHit { get; private set; }

        /// <summary>마법사가 마지막으로 때린 시각 (게임 시간, 초)</summary>
        public float LastHitTime { get; private set; }

        /// <summary>전투 상황이 바뀜 (true = 시작)</summary>
        public event Action<bool> CombatChanged;

        #region FieldService - 마법사 몸을 받아 첫 몸으로 등록
        /// <param name="player">마법사 몸</param>
        public FieldService(FieldUnitController player)
        {
            Player = player != null ? player : throw new ArgumentNullException(nameof(player));
            Register(player);
        }
        #endregion

        #region Register - 몸 등록 (비었거나 이미 있으면 무시)
        /// <param name="unit">등록할 몸</param>
        public void Register(FieldUnitController unit)
        {
            if (unit == null || _units.Contains(unit)) return;
            _units.Add(unit);
        }
        #endregion

        #region RegisterEnemy - 적 등록 (몸도 같이)
        /// <param name="enemy">등록할 적</param>
        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null || _enemies.Contains(enemy)) return;
            _enemies.Add(enemy);
            Register(enemy.Unit);
        }
        #endregion

        #region Nearest - 그 편의 살아 있는 몸 중 기준점에서 가장 가까운 것 (최대 거리 안, 평면 거리). 없으면 null
        /// <param name="team">찾을 편</param>
        /// <param name="from">기준점</param>
        /// <param name="maxDistance">최대 거리 (m)</param>
        public FieldUnitController Nearest(FieldTeam team, Vector3 from, float maxDistance)
        {
            // 찾은 몸과 거리 제곱
            FieldUnitController best = null;
            float bestSq = maxDistance * maxDistance;
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                // 평면 거리 제곱
                float sq = Flat(unit.transform.position - from).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                best = unit;
            }
            return best;
        }
        #endregion

        #region AimTarget - 시선 앞 maxDistance 안에서 시선과 가로 거리가 maxOffset 안인 적 중 가로 거리가 가장 작은 것 (v1 조준 보정). 없으면 null
        /// <param name="ray">시선 (눈 위치·방향)</param>
        /// <param name="maxDistance">앞쪽 최대 거리 (m)</param>
        /// <param name="maxOffset">시선에서 최대 가로 거리 (m)</param>
        public FieldUnitController AimTarget(Ray ray, float maxDistance, float maxOffset)
        {
            // 찾은 적과 가로 거리
            FieldUnitController best = null;
            float bestOffset = maxOffset;
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != FieldTeam.Enemy) continue;
                // 눈에서 적 겨눌 점까지
                Vector3 to = unit.AimPoint - ray.origin;
                // 시선 방향 거리
                float along = Vector3.Dot(to, ray.direction);
                if (along <= 0f || along > maxDistance) continue;
                // 시선에서 가로 거리
                float offset = (to - ray.direction * along).magnitude;
                if (offset >= bestOffset) continue;
                bestOffset = offset;
                best = unit;
            }
            return best;
        }
        #endregion

        #region FindInRadius - 그 편의 살아 있는 몸 중 중심에서 반지름(+몸 반지름) 안인 것을 result에 채움 (평면 거리)
        /// <param name="team">찾을 편</param>
        /// <param name="center">중심</param>
        /// <param name="radius">반지름 (m)</param>
        /// <param name="result">채울 목록 (먼저 비움)</param>
        public void FindInRadius(FieldTeam team, Vector3 center, float radius, List<FieldUnitController> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                if (Flat(unit.transform.position - center).magnitude <= radius + unit.Radius) result.Add(unit);
            }
        }
        #endregion

        #region FindNearSegment - 그 편의 살아 있는 몸 중 선분 a→b에서 거리 radius(+몸 반지름) 안인 것을 result에 채움 (평면, 화염 벽)
        /// <param name="team">찾을 편</param>
        /// <param name="a">선분 시작</param>
        /// <param name="b">선분 끝</param>
        /// <param name="radius">선분에서 거리 (m)</param>
        /// <param name="result">채울 목록 (먼저 비움)</param>
        public void FindNearSegment(FieldTeam team, Vector3 a, Vector3 b, float radius, List<FieldUnitController> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                if (SegmentDistance(unit.transform.position, a, b) <= radius + unit.Radius) result.Add(unit);
            }
        }
        #endregion

        #region WakePack - 같은 무리의 쉬는 적을 다 깨워 target을 쫓게 함
        /// <param name="pack">무리 번호</param>
        /// <param name="target">쫓을 몸</param>
        public void WakePack(int pack, FieldUnitController target)
        {
            foreach (EnemyController enemy in _enemies)
                if (enemy != null && enemy.Pack == pack) enemy.Wake(target);
        }
        #endregion

        #region SetEngaged - 적 하나가 쫓기 시작하거나 그만둠. 전투 상황이 바뀌면 알림
        /// <param name="engaged">쫓기 시작했는지</param>
        public void SetEngaged(bool engaged)
        {
            // 바뀌기 전 전투 상황
            bool was = InCombat;
            _engaged = Mathf.Max(0, _engaged + (engaged ? 1 : -1));
            if (was != InCombat) CombatChanged?.Invoke(InCombat);
        }
        #endregion

        #region IsTargeted - 쫓는 적 중 이 몸을 노리는 것이 있는지 (죽은 뒤 도망 시뮬)
        /// <param name="unit">확인할 몸</param>
        public bool IsTargeted(FieldUnitController unit)
        {
            if (unit == null) return false;
            foreach (EnemyController enemy in _enemies)
                if (enemy != null && enemy.Target == unit) return true;
            return false;
        }
        #endregion

        #region ReportHit - 마법사가 적을 때림 (위 가운데 대상 체력 막대)
        /// <param name="enemy">맞은 적 몸</param>
        public void ReportHit(FieldUnitController enemy)
        {
            LastHit = enemy;
            LastHitTime = Time.time;
        }
        #endregion

        #region Flat - 높이를 뺀 벡터
        /// <param name="v">벡터</param>
        static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
        #endregion

        #region SegmentDistance - 점에서 선분까지 평면 거리
        /// <param name="point">점</param>
        /// <param name="a">선분 시작</param>
        /// <param name="b">선분 끝</param>
        static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            // 평면 좌표
            Vector2 p = new(point.x, point.z);
            Vector2 start = new(a.x, a.z);
            // 선분 방향
            Vector2 along = new Vector2(b.x, b.z) - start;
            // 선분 위 가장 가까운 곳 비율 (0~1)
            float t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - start, along) / along.sqrMagnitude) : 0f;
            return (p - (start + along * t)).magnitude;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/EnemyController.cs` (새 파일)

```csharp
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

        /// <summary>회색 도형 색 셰이더 속성 (URP)</summary>
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>적 상태</summary>
        enum EnemyState
        {
            /// <summary>집 둘레를 돌며 쉼</summary>
            Idle,

            /// <summary>대상을 쫓거나 때림</summary>
            Chase,

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

        #region Update - 멈췄으면(로딩창·메뉴) 안 함. 쉴 때는 알아채기·돌아다니기, 쫓을 때는 쫓기·때리기
        void Update()
        {
            if (_service == null || _state == EnemyState.Dead) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _cooldown -= dt;
            if (_state == EnemyState.Idle) UpdateIdle(dt);
            else UpdateChase(dt);
        }
        #endregion

        #region UpdateIdle - 알아채는 거리 안에 파티가 있으면 무리를 깨움. 없으면 가끔 집 둘레 아무 곳으로 천천히 걸음
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateIdle(float dt)
        {
            // 알아챈 파티 몸 (알아채는 거리 안 가장 가까운 것)
            FieldUnitController seen = _service.Nearest(FieldTeam.Party, transform.position, Monster.AggroRange);
            if (seen != null)
            {
                _service.WakePack(Pack, seen);
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

        #region Wake - 쉬고 있으면 깨어 target을 쫓음 (무리 깨우기·맞았을 때)
        /// <param name="target">쫓을 몸</param>
        public void Wake(FieldUnitController target)
        {
            if (_state != EnemyState.Idle || target == null || !target.IsAlive) return;
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
            if (_target == null)
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
            _repath -= dt;
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
            if (_state == EnemyState.Idle && source != null && source.IsAlive) _service.WakePack(Pack, source);
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
        void LieDown()
        {
            _visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _visual.localPosition = Vector3.up * (_unit.Radius * CorpseLift);
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
```

**호출/구독 관계:**
- `FieldState.SpawnEnemies`가 살아 있으면 `Init`, 쓰러진 키면 `InitCorpse`. `Died`는 `FieldState`가 구독 (판 데이터에 쓰러진 곳, 소탕 대상 수)
- `FieldService.WakePack`이 `Wake`, `SpellProjectileController`(물)가 `Push`

---

### `Assets/Scripts/Field/MercenaryController.cs` (새 파일)

```csharp
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
```

**호출/구독 관계:**
- `FieldState.SpawnParty`가 `Init`, `Died` 구독 (판 데이터 파티에서 뺌). 죽은 뒤 도망은 `FieldState.DeathRoutine`이 `Flee`·`MarkSafe`

---

### `Assets/Scripts/Field/SpellProjectileController.cs` (새 파일)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>날아가는 주문·화살 하나: 매 프레임 구 모양으로 앞을 훑어 처음 닿은 것에서 터짐. 노리는 편의 살아 있는 몸이면 피해(+물이면 밀쳐냄), 몸이 아닌 것(땅·나무·바위)이면 그냥 터짐. 같은 편·죽은 몸·쏜 몸은 지나감. 폭발 범위가 있으면 둘레 몸에 절반 피해. 시간이 다 되면 터지지 않고 사라짐 (v1)</summary>
    public sealed class SpellProjectileController : MonoBehaviour
    {
        /// <summary>터짐이 보이는 시간 (초, v1)</summary>
        const float BurstSeconds = 0.22f;

        /// <summary>폭발 범위 피해 배율 (v1 절반)</summary>
        const float SplashScale = 0.5f;

        /// <summary>폭발 범위가 없을 때 터짐 구 반지름 (m)</summary>
        const float MinBurstRadius = 0.9f;

        /// <summary>한 번에 훑는 콜라이더 최대 수</summary>
        const int MaxHits = 8;

        /// <summary>색 셰이더 속성 (URP)</summary>
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>맞음 판정 반지름 (m)</summary>
        [SerializeField, Min(0.01f)] float _radius = 0.25f;

        /// <summary>날아가는 모습 (인스펙터에서만 연결)</summary>
        [SerializeField] Renderer _visual;

        /// <summary>터질 때 켜는 구 (꺼 둔 자식. 인스펙터에서만 연결)</summary>
        [SerializeField] Renderer _burst;

        /// <summary>훑은 결과 (매 프레임 다시 씀)</summary>
        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

        /// <summary>폭발 범위 안 몸 (터질 때 다시 씀)</summary>
        readonly List<FieldUnitController> _splashUnits = new();

        /// <summary>날아가는 방향 (단위 벡터)</summary>
        Vector3 _direction;

        /// <summary>속도 (m/s)</summary>
        float _speed;

        /// <summary>남은 비행 시간 (초)</summary>
        float _life;

        /// <summary>맞은 몸 피해</summary>
        float _damage;

        /// <summary>폭발 반지름 (m, 0이면 없음)</summary>
        float _splashRadius;

        /// <summary>밀쳐내는 거리 (m, 0이면 없음)</summary>
        float _push;

        /// <summary>노리는 편</summary>
        FieldTeam _targetTeam;

        /// <summary>쏜 몸 (안 맞음, 피해 출처)</summary>
        FieldUnitController _source;

        /// <summary>현장 규칙 (폭발 범위 찾기)</summary>
        FieldService _service;

        /// <summary>터졌거나 사라지는 중인지</summary>
        bool _done;

        /// <summary>터짐이 보일 남은 시간 (초)</summary>
        float _burstLeft;

        #region Launch - 쏨: 방향·속도·비행 시간·피해·폭발·밀쳐내기·노리는 편·쏜 몸·현장 규칙·색을 받음
        /// <param name="direction">방향 (0이면 지금 앞)</param>
        /// <param name="speed">속도 (m/s)</param>
        /// <param name="lifeSeconds">최대 비행 시간 (초)</param>
        /// <param name="damage">맞은 몸 피해</param>
        /// <param name="splashRadius">폭발 반지름 (m, 0이면 없음)</param>
        /// <param name="push">밀쳐내는 거리 (m, 0이면 없음)</param>
        /// <param name="targetTeam">노리는 편</param>
        /// <param name="source">쏜 몸</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="color">색</param>
        public void Launch(Vector3 direction, float speed, float lifeSeconds, float damage, float splashRadius, float push,
            FieldTeam targetTeam, FieldUnitController source, FieldService service, Color color)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _speed = Mathf.Max(0f, speed);
            _life = Mathf.Max(0f, lifeSeconds);
            _damage = damage;
            _splashRadius = Mathf.Max(0f, splashRadius);
            _push = Mathf.Max(0f, push);
            _targetTeam = targetTeam;
            _source = source;
            _done = false;
            transform.rotation = Quaternion.LookRotation(_direction);
            // 색 칠하기
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            if (_visual != null) _visual.SetPropertyBlock(block);
            if (_burst != null)
            {
                _burst.SetPropertyBlock(block);
                _burst.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Update - 멈췄으면 안 함. 터진 뒤엔 터짐을 잠깐 보이고 지움. 날아가는 중엔 이번 거리만큼 훑어 닿으면 터지고, 아니면 나아가다 시간이 다 되면 사라짐
        void Update()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_done)
            {
                _burstLeft -= dt;
                if (_burstLeft <= 0f) Destroy(gameObject);
                return;
            }
            if (_service == null) return;
            // 이번 프레임 날아갈 거리
            float step = _speed * dt;
            if (TryHit(step, out Vector3 point, out FieldUnitController unit))
            {
                Explode(point, unit);
                return;
            }
            transform.position += _direction * step;
            _life -= dt;
            if (_life <= 0f) Finish(false);
        }
        #endregion

        #region TryHit - 이번 거리만큼 구로 훑어 가장 가까운 '닿을 것'을 찾음: 노리는 편의 산 몸이거나 몸이 아닌 콜라이더. 트리거·같은 편·죽은 몸·쏜 몸은 건너뜀
        /// <param name="step">훑을 거리 (m)</param>
        /// <param name="point">닿은 곳</param>
        /// <param name="unit">닿은 몸 (몸이 아니면 null)</param>
        bool TryHit(float step, out Vector3 point, out FieldUnitController unit)
        {
            point = transform.position;
            unit = null;
            // 훑은 콜라이더 수
            int count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, step, ~0, QueryTriggerInteraction.Ignore);
            // 가장 가까운 닿은 거리
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                // 이번 콜라이더
                RaycastHit hit = _hits[i];
                if (hit.collider == null || hit.distance >= best) continue;
                // 콜라이더의 몸 (몸이 아니면 null)
                FieldUnitController other = hit.collider.GetComponentInParent<FieldUnitController>();
                if (other != null && (other == _source || other.Team != _targetTeam || !other.IsAlive)) continue;
                best = hit.distance;
                unit = other;
                // 처음부터 겹쳐 있으면 hit.point가 0이라 지금 자리
                point = hit.distance > 0f ? hit.point : transform.position;
            }
            return best < float.MaxValue;
        }
        #endregion

        #region Explode - 닿은 몸 피해(+밀쳐내기), 폭발 범위 둘레 몸 절반 피해, 터짐을 보임
        /// <param name="point">터진 곳</param>
        /// <param name="unit">닿은 몸 (없으면 null)</param>
        void Explode(Vector3 point, FieldUnitController unit)
        {
            transform.position = point;
            if (unit != null)
            {
                unit.TakeDamage(_damage, _source);
                if (_push > 0f && unit.TryGetComponent(out EnemyController enemy))
                    enemy.Push(Vector3.ProjectOnPlane(_direction, Vector3.up).normalized * _push);
            }
            if (_splashRadius > 0f)
            {
                _service.FindInRadius(_targetTeam, point, _splashRadius, _splashUnits);
                foreach (FieldUnitController other in _splashUnits)
                    if (other != unit) other.TakeDamage(_damage * SplashScale, _source);
            }
            Finish(true);
        }
        #endregion

        #region Finish - 날아가는 모습을 끄고, 터졌으면 터짐 구를 폭발 크기로 잠깐 켬
        /// <param name="burst">터짐을 보일지</param>
        void Finish(bool burst)
        {
            _done = true;
            if (_visual != null) _visual.enabled = false;
            _burstLeft = 0f;
            if (!burst || _burst == null) return;
            _burst.transform.localScale = Vector3.one * (Mathf.Max(MinBurstRadius, _splashRadius) * 2f);
            _burst.gameObject.SetActive(true);
            _burstLeft = BurstSeconds;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/FlameWallController.cs` (새 파일)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>화염 벽 하나 (v1 화염 벽): 시선에 수직인 막대. 0.5초마다 막대에서 1.3m(+몸 반지름) 안 적에게 피해, 지속 시간이 끝나면 사라짐</summary>
    public sealed class FlameWallController : MonoBehaviour
    {
        /// <summary>피해 간격 (초, v1)</summary>
        const float TickSeconds = 0.5f;

        /// <summary>막대에서 피해가 닿는 거리 (m, v1)</summary>
        const float HitRadius = 1.3f;

        /// <summary>벽 도형 두께·높이 (m)</summary>
        const float VisualWidth = 0.4f;
        const float VisualHeight = 1.6f;

        /// <summary>벽 도형 (Cube. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _visual;

        /// <summary>피해 줄 몸 (틱마다 다시 씀)</summary>
        readonly List<FieldUnitController> _found = new();

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>벽을 만든 몸 (피해 출처)</summary>
        FieldUnitController _source;

        /// <summary>막대 양 끝</summary>
        Vector3 _start;
        Vector3 _end;

        /// <summary>틱마다 피해</summary>
        float _damagePerTick;

        /// <summary>남은 시간 (초)</summary>
        float _left;

        /// <summary>다음 틱까지 (초)</summary>
        float _tick;

        #region Launch - 그 자리에 막대를 세움: 가운데·방향·길이·지속·초당 피해·만든 몸·현장 규칙
        /// <param name="center">가운데 (땅)</param>
        /// <param name="along">막대 방향 (평면, 0이면 오른쪽)</param>
        /// <param name="length">길이 (m)</param>
        /// <param name="seconds">지속 (초)</param>
        /// <param name="damagePerSecond">초당 피해</param>
        /// <param name="source">만든 몸</param>
        /// <param name="service">현장 규칙</param>
        public void Launch(Vector3 center, Vector3 along, float length, float seconds, float damagePerSecond, FieldUnitController source, FieldService service)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _source = source;
            // 평면 막대 방향
            Vector3 flat = Vector3.ProjectOnPlane(along, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.right;
            flat.Normalize();
            _start = center - flat * (length * 0.5f);
            _end = center + flat * (length * 0.5f);
            transform.SetPositionAndRotation(center, Quaternion.LookRotation(flat));
            if (_visual != null)
            {
                _visual.localScale = new Vector3(VisualWidth, VisualHeight, length);
                _visual.localPosition = Vector3.up * (VisualHeight * 0.5f);
            }
            _damagePerTick = damagePerSecond * TickSeconds;
            _left = seconds;
            _tick = 0f;
        }
        #endregion

        #region Update - 멈췄으면 안 함. 틱마다 막대 둘레 적 피해, 시간이 다 되면 지움
        void Update()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || _service == null) return;
            _left -= dt;
            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick += TickSeconds;
                _service.FindNearSegment(FieldTeam.Enemy, _start, _end, HitRadius, _found);
                foreach (FieldUnitController unit in _found) unit.TakeDamage(_damagePerTick, _source);
            }
            if (_left <= 0f) Destroy(gameObject);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/FieldCastSpec.cs` (새 파일)

```csharp
namespace Mage
{
    /// <summary>현장에서 시전할 주문 하나 (판정 뒤 만듦): 효과·마나·위력·범위·시간·재사용, 이름(지금 언어)·일치율. 정확(90% 이상)이면 위력·방호막 지속 ×1.4 (v1)</summary>
    public readonly struct FieldCastSpec
    {
        /// <summary>정확할 때 배율 (v1)</summary>
        const float ExactScale = 1.4f;

        /// <summary>현장 효과</summary>
        public SpellFieldEffect Effect { get; }

        /// <summary>마나</summary>
        public int ManaCost { get; }

        /// <summary>위력 (뜻은 SpellFieldEffect)</summary>
        public float Power { get; }

        /// <summary>범위 (뜻은 SpellFieldEffect)</summary>
        public float Area { get; }

        /// <summary>지속 시간 (초)</summary>
        public float Duration { get; }

        /// <summary>다시 쓸 수 있을 때까지 (초)</summary>
        public float Cooldown { get; }

        /// <summary>이름 (지금 언어, 조준 글에 씀)</summary>
        public string Name { get; }

        /// <summary>전체 일치율 (0~1)</summary>
        public float Accuracy { get; }

        /// <summary>정확하게 그렸는지 (일치율 90% 이상)</summary>
        public bool IsExact => Accuracy >= SpellRecognitionCalculator.ExactAccuracy;

        /// <summary>위력·지속에 곱할 배율 (정확 1.4, 아니면 1)</summary>
        public float Scale => IsExact ? ExactScale : 1f;

        #region FieldCastSpec - 값을 받아 만듦
        /// <param name="effect">현장 효과</param>
        /// <param name="manaCost">마나</param>
        /// <param name="power">위력</param>
        /// <param name="area">범위</param>
        /// <param name="duration">지속 시간 (초)</param>
        /// <param name="cooldown">재사용 (초)</param>
        /// <param name="name">이름</param>
        /// <param name="accuracy">전체 일치율</param>
        public FieldCastSpec(SpellFieldEffect effect, int manaCost, float power, float area, float duration, float cooldown, string name, float accuracy)
        {
            Effect = effect;
            ManaCost = manaCost;
            Power = power;
            Area = area;
            Duration = duration;
            Cooldown = cooldown;
            Name = name ?? string.Empty;
            Accuracy = accuracy;
        }
        #endregion

        #region FromSpell - 다중 룬 마법의 현장 값으로 만듦
        /// <param name="spell">마법</param>
        /// <param name="name">이름 (지금 언어)</param>
        /// <param name="accuracy">전체 일치율</param>
        public static FieldCastSpec FromSpell(SpellSO spell, string name, float accuracy) =>
            new(spell.FieldEffect, spell.ManaCost, spell.Power, spell.Area, spell.Duration, spell.Cooldown, name, accuracy);
        #endregion

        #region FromRune - 룬 하나를 혼자 던질 때 값으로 만듦 (지속·재사용 없음)
        /// <param name="rune">룬</param>
        /// <param name="name">이름 (지금 언어)</param>
        /// <param name="accuracy">전체 일치율</param>
        public static FieldCastSpec FromRune(RuneSO rune, string name, float accuracy) =>
            new(rune.SoloEffect, rune.SoloManaCost, rune.SoloPower, rune.SoloArea, 0f, 0f, name, accuracy);
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/FieldCastController.cs` (새 파일)

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>현장 시전: 좌클릭하면 시전 상태 → 화면 가운데 그리기 칸에 펜이 생기고 커서를 잠근 채 마우스 이동량으로 그림 (그리기 칸 SpellDrawController에 가상 펜으로 넘김) → 대기·Space 판정 → 3초 조준(시점만, 좌클릭이나 3초 끝 발사, 우클릭 취소) → 효과. 방호막은 조준 없이 바로. 마나(초당 9 회복)·방호막 재사용, 만드는 룬 혼자 던지기, 생략 없이 처음 그린 다중 룬 마법 등록. 그리는 동안 이동·시점, 조준 중 이동을 막음. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class FieldCastController : MonoBehaviour
    {
        /// <summary>그리기·발사 입력 액션 경로 (전역 액션, 왼쪽 버튼)</summary>
        const string DrawActionPath = "Player/Attack";

        /// <summary>취소 입력 액션 경로 (전역 액션, 오른쪽 버튼)</summary>
        const string CancelActionPath = "Player/CancelCast";

        /// <summary>펜을 움직일 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>현장 시전 튜토리얼 id (처음 판정까지 그리면 배움)</summary>
        const string FieldCastTutorialId = "field_cast";

        /// <summary>판정 뒤 조준 시간 (초, v1)</summary>
        const float AimSeconds = 3f;

        /// <summary>최대 마나 (v1: 100 + 마나 × 8, 마나 0. 임시)</summary>
        const float ManaMaxValue = 100f;

        /// <summary>초당 마나 회복 (v1)</summary>
        const float ManaRegen = 9f;

        /// <summary>조준 보정: 앞쪽 최대 거리 (m, v1)</summary>
        const float AimMaxDistance = 60f;

        /// <summary>조준 보정: 시선에서 최대 가로 거리 (m, v1)</summary>
        const float AimMaxOffset = 3.5f;

        /// <summary>주문 탄 속도 (m/s, v1)</summary>
        const float ProjectileSpeed = 34f;

        /// <summary>주문 탄 최대 비행 시간 (초, v1)</summary>
        const float ProjectileLife = 1.9f;

        /// <summary>탄이 눈 앞 이만큼에서 나감 (m, v1)</summary>
        const float MuzzleOffset = 0.7f;

        /// <summary>화염 벽 조준점 최대 거리 (m, v1)</summary>
        const float WallMaxDistance = 40f;

        /// <summary>판정 글·그린 획이 남는 시간 (초, v1)</summary>
        const float ClearSeconds = 1.4f;

        /// <summary>알림 글이 남는 시간 (초)</summary>
        const float NoteSeconds = 1.6f;

        /// <summary>조준 글 키 ({0} 이름, {1} 일치율 %, {2} 남은 초)</summary>
        const string AimKey = "field.aim";

        /// <summary>정확 조준 글 키 (같은 자리 값)</summary>
        const string AimExactKey = "field.aim_exact";

        /// <summary>알림: 마나 모자람</summary>
        const string NoManaKey = "field.note.no_mana";

        /// <summary>알림: 방호막 재사용 대기 ({0} 남은 초)</summary>
        const string ShieldCooldownKey = "field.note.shield_cooldown";

        /// <summary>알림: 두루마리는 현장에서 안 만듦</summary>
        const string ScrollKey = "field.note.scroll";

        /// <summary>알림: 혼자서는 효과 없는 룬 ({0} 룬 주문 이름)</summary>
        const string RuneAloneKey = "field.note.rune_alone";

        /// <summary>알림: 현장 효과가 아직 없는 주문 ({0} 주문 이름)</summary>
        const string NoEffectKey = "field.note.no_effect";

        /// <summary>알림: 조준 취소</summary>
        const string CanceledKey = "field.note.canceled";

        /// <summary>룬 주문 이름 키 (Codex 테이블, {0} 룬 이름)</summary>
        const string RuneSpellKey = "codex.rune_spell";

        /// <summary>마법 등록 토스트 키 (Codex 테이블, {0} 마법 이름)</summary>
        const string RegisteredKey = "codex.toast.registered";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>코덱스 문구 테이블 (룬 주문 이름·등록 토스트)</summary>
        static readonly TableReference CodexTable = "Codex";

        /// <summary>불 탄 색</summary>
        static readonly Color FireColor = new(1f, 0.55f, 0.15f);

        /// <summary>물 탄 색</summary>
        static readonly Color WaterColor = new(0.35f, 0.65f, 1f);

        /// <summary>시전 상태</summary>
        enum CastState
        {
            /// <summary>Init 전</summary>
            Off,

            /// <summary>그리지도 겨누지도 않음</summary>
            Idle,

            /// <summary>그리기 칸에 그리는 중 (판정 전)</summary>
            Drawing,

            /// <summary>판정 뒤 3초 조준</summary>
            Aiming,
        }

        /// <summary>그리기 칸 (HUD의 SpellDrawController. 펜 좌표는 이 칸 가운데 기준. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawController _draw;

        /// <summary>숲 HUD (조준점·조준 원·조준 글·알림 글·펜 점. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _hud;

        /// <summary>겨누는 눈 (플레이어 Head. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>주문 탄 프리팹 (SpellProjectile.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _projectilePrefab;

        /// <summary>화염 벽 프리팹 (FlameWall.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] FlameWallController _wallPrefab;

        /// <summary>펜 감도 (마우스 이동량 1당 그리기 칸 단위, 캔버스 배율로 나눔)</summary>
        [SerializeField, Min(0.1f)] float _penSpeed = 1f;

        /// <summary>익힌 룬 id (비교 그림을 만들 때 채움)</summary>
        readonly HashSet<string> _learnedIds = new(StringComparer.Ordinal);

        /// <summary>판정 비교 그림 (등록이 바뀌면 다시 만듦)</summary>
        List<SpellCandidateEntry> _candidates = new();

        /// <summary>판정 비교 그림 원본 (다중 룬 마법 순서 뒤에 룬 순서. ShapeIndex가 이 번호)</summary>
        SpellShapeEntry[] _shapes;

        /// <summary>다중 룬 마법 전부 (DataManager 순서)</summary>
        IReadOnlyList<SpellSO> _spells;

        /// <summary>룬 전부 (DataManager 순서)</summary>
        IReadOnlyList<RuneSO> _runes;

        /// <summary>이번 판 데이터</summary>
        RunEntry _run;

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>1인칭 조작 (잠금)</summary>
        PlayerController _player;

        /// <summary>마법사 몸 (방호막·피해 출처)</summary>
        FieldUnitController _unit;

        /// <summary>그리기 칸이 든 캔버스 (펜 감도를 화면 크기와 맞춤)</summary>
        Canvas _canvas;

        /// <summary>그리기·발사 입력</summary>
        InputAction _drawAction;

        /// <summary>취소 입력</summary>
        InputAction _cancelAction;

        /// <summary>펜 입력 (마우스 이동량)</summary>
        InputAction _lookAction;

        /// <summary>지금 시전 상태</summary>
        CastState _state;

        /// <summary>펜 자리 (그리기 칸 가운데 기준)</summary>
        Vector2 _pen;

        /// <summary>펜을 누르고 있는지</summary>
        bool _penDown;

        /// <summary>조준 중인 주문</summary>
        FieldCastSpec _aim;

        /// <summary>조준 남은 시간 (초)</summary>
        float _aimLeft;

        /// <summary>조준 글에 마지막으로 넣은 남은 시간 (0.1초 단위, 바뀔 때만 글을 다시 씀)</summary>
        int _aimShown = -1;

        /// <summary>판정 글·획을 지울 때까지 (초, 0이면 안 지움)</summary>
        float _clearLeft;

        /// <summary>알림 글을 숨길 때까지 (초)</summary>
        float _noteLeft;

        /// <summary>방호막 남은 시간 (초)</summary>
        float _shieldLeft;

        /// <summary>방호막을 다시 펼칠 수 있을 때까지 (초)</summary>
        float _shieldCooldown;

        /// <summary>지금 마나</summary>
        public float Mana { get; private set; }

        /// <summary>최대 마나</summary>
        public float ManaMax => ManaMaxValue;

        #region Awake - 연결 확인, 입력 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_draw == null || _hud == null || _eye == null || _projectilePrefab == null || _wallPrefab == null)
                Debug.LogError("[Cast] 그리기 칸·HUD·눈·탄·벽 연결 안 됨", this);
            _drawAction = FindAction(DrawActionPath);
            _cancelAction = FindAction(CancelActionPath);
            _lookAction = FindAction(LookActionPath);
            if (_draw != null) _canvas = _draw.GetComponentInParent<Canvas>();
            enabled = false;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Cast] {path} 액션 없음");
            return action;
        }
        #endregion

        #region Init - 판 데이터·주문·룬·현장 규칙·조작·몸을 받아 비교 그림을 만들고 마나를 채움, 그리기 칸에 판정을 넘김, HUD 조준 표시를 끔
        /// <param name="run">이번 판 데이터</param>
        /// <param name="spells">다중 룬 마법 전부</param>
        /// <param name="runes">룬 전부</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="player">1인칭 조작</param>
        /// <param name="unit">마법사 몸</param>
        public void Init(RunEntry run, IReadOnlyList<SpellSO> spells, IReadOnlyList<RuneSO> runes, FieldService service, PlayerController player, FieldUnitController unit)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _runes = runes ?? throw new ArgumentNullException(nameof(runes));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _unit = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
            _shapes = spells.Select(spell => spell != null ? spell.Shape : null)
                .Concat(runes.Select(rune => rune != null ? rune.Shape : null)).ToArray();
            RebuildCandidates();
            Mana = ManaMax;
            _state = CastState.Idle;
            if (_draw != null) _draw.Begin(NameAt, Judge, OnJudged);
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.NoteText.gameObject.SetActive(false);
            _hud.Pen.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
        }
        #endregion

        #region OnEnable - 그리기·취소 입력 구독
        void OnEnable()
        {
            if (_drawAction != null)
            {
                _drawAction.performed += OnDrawPressed;
                _drawAction.canceled += OnDrawReleased;
            }
            if (_cancelAction != null) _cancelAction.performed += OnCancelPressed;
        }
        #endregion

        #region OnDisable - 입력 구독 해제, 그리던 것·겨누던 것을 그만둠 (상태가 끄거나 마법사가 죽을 때)
        void OnDisable()
        {
            if (_drawAction != null)
            {
                _drawAction.performed -= OnDrawPressed;
                _drawAction.canceled -= OnDrawReleased;
            }
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPressed;
            if (_state == CastState.Drawing && _draw != null) _draw.CancelCast();
            else if (_state == CastState.Aiming) EndCast();
        }
        #endregion

        #region Update - 멈췄으면 안 함. 마나 회복, 방호막, 알림 글·판정 글 지우기, 그리는 중엔 펜, 조준 중엔 남은 시간
        void Update()
        {
            if (_state == CastState.Off) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Mana = Mathf.Min(ManaMax, Mana + ManaRegen * dt);
            UpdateShield(dt);
            if (_noteLeft > 0f)
            {
                _noteLeft -= dt;
                if (_noteLeft <= 0f) _hud.NoteText.gameObject.SetActive(false);
            }
            if (_clearLeft > 0f && _state != CastState.Drawing)
            {
                _clearLeft -= dt;
                if (_clearLeft <= 0f) _draw.Begin(NameAt, Judge, OnJudged);
            }
            if (_state == CastState.Drawing) UpdatePen();
            else if (_state == CastState.Aiming) UpdateAim(dt);
        }
        #endregion

        #region OnDrawPressed - 좌클릭: 조준 중이면 발사, 아니면 (시전 상태가 아니면 시작하고) 펜을 누름
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnDrawPressed(InputAction.CallbackContext context)
        {
            if (_state == CastState.Aiming)
            {
                Fire();
                return;
            }
            if (_state == CastState.Idle) StartDrawing();
            if (_state != CastState.Drawing) return;
            _penDown = true;
            _draw.PenDown(_pen);
        }
        #endregion

        #region OnDrawReleased - 좌클릭을 뗌: 펜을 뗌 (그 뒤 대기 시간이 흐름)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnDrawReleased(InputAction.CallbackContext context)
        {
            if (_state != CastState.Drawing || !_penDown) return;
            _penDown = false;
            _draw.PenUp();
        }
        #endregion

        #region OnCancelPressed - 우클릭: 그리는 중이면 취소(판정 칸이 '직접 취소'로 끝냄), 조준 중이면 조준 취소
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_state == CastState.Drawing)
            {
                _draw.CancelCast();
                return;
            }
            if (_state != CastState.Aiming) return;
            Note(CanceledKey);
            EndCast();
        }
        #endregion

        #region StartDrawing - 시전 상태 시작: 이동·시점 잠금, 펜을 가운데에, 펜 점을 켜고 조준점을 끔
        void StartDrawing()
        {
            _state = CastState.Drawing;
            _clearLeft = 0f;
            _pen = Vector2.zero;
            _player.MoveLocked = true;
            _player.LookLocked = true;
            _hud.Crosshair.SetActive(false);
            _hud.Pen.gameObject.SetActive(true);
            _hud.Pen.anchoredPosition = _pen;
        }
        #endregion

        #region UpdatePen - 마우스 이동량으로 펜을 옮기고 그리기 칸 안(반지름 = 짧은 변 절반)으로 자름. 누르고 있으면 획에 점을 더함
        void UpdatePen()
        {
            // 캔버스 배율 (화면 픽셀 → 칸 단위)
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            // 이번 프레임 펜 이동 (칸 단위)
            Vector2 delta = _lookAction != null ? _lookAction.ReadValue<Vector2>() * (_penSpeed / scale) : Vector2.zero;
            // 그리기 칸 크기
            Rect area = ((RectTransform)_draw.transform).rect;
            _pen = Vector2.ClampMagnitude(_pen + delta, Mathf.Min(area.width, area.height) * 0.5f);
            _hud.Pen.anchoredPosition = _pen;
            if (_penDown) _draw.PenMove(_pen);
        }
        #endregion

        #region OnJudged - 판정 뒤: 펜 점을 끄고 판정 글을 잠시 뒤 지우게 함, 직접 취소가 아니면 현장 시전 배움. 시전이면 주문 값을 만들어 막을 이유가 없으면 방호막은 바로, 나머지는 3초 조준
        /// <param name="spec">판정 결과</param>
        void OnJudged(SpellJudgeSpec spec)
        {
            _penDown = false;
            _hud.Pen.gameObject.SetActive(false);
            _clearLeft = ClearSeconds;
            if (spec.Reason != SpellJudgeReason.Canceled) TutorialManager.Instance.MarkLearned(FieldCastTutorialId);
            if (!spec.IsCast || !TryMakeCast(spec, out FieldCastSpec cast) || !CanCast(cast))
            {
                EndCast();
                return;
            }
            if (cast.Effect == SpellFieldEffect.Shield)
            {
                Cast(cast);
                EndCast();
                return;
            }
            _aim = cast;
            _aimLeft = AimSeconds;
            _aimShown = -1;
            _state = CastState.Aiming;
            _player.LookLocked = false;
            _hud.AimRing.SetActive(true);
            RefreshAimText();
        }
        #endregion

        #region TryMakeCast - 판정한 그림으로 현장 값을 만듦. 두루마리·현장 효과 없는 주문·혼자 효과 없는 룬이면 알림 후 false. 다중 룬 마법을 생략 없이 그렸으면 등록
        /// <param name="spec">시전 판정</param>
        /// <param name="cast">만든 현장 값</param>
        bool TryMakeCast(SpellJudgeSpec spec, out FieldCastSpec cast)
        {
            cast = default;
            // 판정한 그림 번호
            int index = spec.Candidate.ShapeIndex;
            // 이름 (지금 언어)
            string name = NameAt(index);
            if (index >= 0 && index < _spells.Count)
            {
                // 그린 다중 룬 마법
                SpellSO spell = _spells[index];
                if (spell == null) return false;
                if (spell.Kind == SpellKind.Scroll)
                {
                    Note(ScrollKey);
                    return false;
                }
                if (spec.Candidate.Omit.Count == 0) Register(spell);
                if (spell.FieldEffect == SpellFieldEffect.None)
                {
                    Note(NoEffectKey, name);
                    return false;
                }
                cast = FieldCastSpec.FromSpell(spell, name, spec.Accuracy);
                return true;
            }
            // 룬 번호
            int runeIndex = index - _spells.Count;
            // 그린 룬
            RuneSO rune = runeIndex >= 0 && runeIndex < _runes.Count ? _runes[runeIndex] : null;
            if (rune == null) return false;
            if (rune.SoloEffect == SpellFieldEffect.None)
            {
                Note(RuneAloneKey, name);
                return false;
            }
            cast = FieldCastSpec.FromRune(rune, name, spec.Accuracy);
            return true;
        }
        #endregion

        #region CanCast - 방호막 재사용 대기·마나가 모자라면 알림 후 false
        /// <param name="cast">쓸 주문</param>
        bool CanCast(FieldCastSpec cast)
        {
            if (cast.Effect == SpellFieldEffect.Shield && _shieldCooldown > 0f)
            {
                Note(ShieldCooldownKey, Mathf.CeilToInt(_shieldCooldown));
                return false;
            }
            if (Mana >= cast.ManaCost) return true;
            Note(NoManaKey);
            return false;
        }
        #endregion

        #region UpdateAim - 조준 남은 시간을 셈. 다 되면 발사, 아니면 조준 글 갱신
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateAim(float dt)
        {
            _aimLeft -= dt;
            if (_aimLeft <= 0f)
            {
                Fire();
                return;
            }
            RefreshAimText();
        }
        #endregion

        #region RefreshAimText - 조준 글 "이름 일치율% · 남은 초 · 좌클릭 시전 · 우클릭 취소" (0.1초가 바뀔 때만 다시 씀, 정확이면 정확 표시)
        void RefreshAimText()
        {
            // 보일 남은 시간 (0.1초 단위)
            int tenths = Mathf.CeilToInt(Mathf.Max(0f, _aimLeft) * 10f);
            if (tenths == _aimShown) return;
            _aimShown = tenths;
            // 조준 글의 String Reference
            var text = _hud.AimText.StringReference;
            text.SetReference(Table, _aim.IsExact ? AimExactKey : AimKey);
            text.Arguments = new object[] { _aim.Name, Percent(_aim.Accuracy), (tenths / 10f).ToString("F1", CultureInfo.InvariantCulture) };
            _hud.AimText.gameObject.SetActive(true);
            _hud.AimText.RefreshString();
        }
        #endregion

        #region Fire - 조준을 끝내고, 마나·재사용을 다시 보고 되면 시전
        void Fire()
        {
            if (_state != CastState.Aiming) return;
            // 쏠 주문 (EndCast가 상태를 바꾸기 전에 꺼냄)
            FieldCastSpec cast = _aim;
            EndCast();
            if (CanCast(cast)) Cast(cast);
        }
        #endregion

        #region EndCast - 시전 상태를 끝냄: 이동·시점 잠금 해제, 조준 원·글·펜 점 끄고 조준점 켬
        void EndCast()
        {
            _state = _state == CastState.Off ? CastState.Off : CastState.Idle;
            _penDown = false;
            if (_player != null)
            {
                _player.MoveLocked = false;
                _player.LookLocked = false;
            }
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.Pen.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
        }
        #endregion

        #region Cast - 마나를 쓰고 효과를 냄 (정확이면 위력 ×1.4): 탄·폭발 탄·물탄·부채꼴 세 발·화염 벽·방호막
        /// <param name="cast">쓸 주문</param>
        void Cast(FieldCastSpec cast)
        {
            Mana -= cast.ManaCost;
            // 정확 배율을 곱한 위력
            float power = cast.Power * cast.Scale;
            switch (cast.Effect)
            {
                case SpellFieldEffect.Bolt:
                    Shoot(AimDirection(), power, 0f, 0f, FireColor);
                    break;
                case SpellFieldEffect.Ball:
                    Shoot(AimDirection(), power, cast.Area, 0f, FireColor);
                    break;
                case SpellFieldEffect.Water:
                    Shoot(AimDirection(), power, 0f, cast.Area, WaterColor);
                    break;
                case SpellFieldEffect.Fan:
                    // 가운데 방향
                    Vector3 direction = AimDirection();
                    Shoot(Quaternion.AngleAxis(-cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor);
                    Shoot(direction, power, 0f, 0f, FireColor);
                    Shoot(Quaternion.AngleAxis(cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor);
                    break;
                case SpellFieldEffect.Wall:
                    PlaceWall(cast, power);
                    break;
                case SpellFieldEffect.Shield:
                    _shieldLeft = cast.Duration * cast.Scale;
                    _shieldCooldown = cast.Cooldown;
                    _unit.DamageScale = cast.Power;
                    break;
            }
        }
        #endregion

        #region AimDirection - 조준 보정 대상(시선 60m 안·가로 3.5m 안 적)이 있으면 그 겨눌 점으로, 없으면 시선 방향 (v1)
        Vector3 AimDirection()
        {
            // 시선
            var ray = new Ray(_eye.position, _eye.forward);
            // 보정 대상 (없으면 null)
            FieldUnitController target = _service.AimTarget(ray, AimMaxDistance, AimMaxOffset);
            return target != null ? (target.AimPoint - _eye.position).normalized : _eye.forward;
        }
        #endregion

        #region Shoot - 눈 앞에서 탄 한 발을 쏨 (적 편을 노림)
        /// <param name="direction">방향</param>
        /// <param name="damage">피해</param>
        /// <param name="splash">폭발 반지름 (m)</param>
        /// <param name="push">밀쳐내기 (m)</param>
        /// <param name="color">색</param>
        void Shoot(Vector3 direction, float damage, float splash, float push, Color color)
        {
            // 탄이 나갈 곳
            Vector3 from = _eye.position + direction * MuzzleOffset;
            // 쏜 탄
            SpellProjectileController shot = Instantiate(_projectilePrefab, from, Quaternion.LookRotation(direction));
            shot.Launch(direction, ProjectileSpeed, ProjectileLife, damage, splash, push, FieldTeam.Enemy, _unit, _service, color);
        }
        #endregion

        #region PlaceWall - 시선이 40m 안에서 닿은 곳(적이면 그 발밑, 없으면 앞 40m 땅)에 시선과 수직인 불벽
        /// <param name="cast">화염 벽 주문</param>
        /// <param name="damagePerSecond">초당 피해 (정확 배율 적용)</param>
        void PlaceWall(FieldCastSpec cast, float damagePerSecond)
        {
            // 시선
            var ray = new Ray(_eye.position, _eye.forward);
            // 시선의 평면 앞 방향
            Vector3 flatForward = Vector3.ProjectOnPlane(_eye.forward, Vector3.up).normalized;
            // 벽 가운데 (기본: 앞 40m 마법사 발 높이)
            Vector3 center = _unit.transform.position + flatForward * WallMaxDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, WallMaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // 닿은 몸 (몸이 아니면 null)
                FieldUnitController unit = hit.collider.GetComponentInParent<FieldUnitController>();
                center = unit != null ? unit.transform.position : hit.point;
            }
            // 세운 벽
            FlameWallController wall = Instantiate(_wallPrefab, center, Quaternion.identity);
            wall.Launch(center, _eye.right, cast.Area, cast.Duration, damagePerSecond, _unit, _service);
        }
        #endregion

        #region UpdateShield - 방호막 재사용 대기를 줄이고, 지속이 끝나면 받는 피해 배율을 되돌림
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateShield(float dt)
        {
            if (_shieldCooldown > 0f) _shieldCooldown -= dt;
            if (_shieldLeft <= 0f) return;
            _shieldLeft -= dt;
            if (_shieldLeft <= 0f) _unit.DamageScale = 1f;
        }
        #endregion

        #region Note - 조준점 아래 알림 글을 잠깐 띄움 (숲 테이블)
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        void Note(string key, params object[] args)
        {
            // 알림 글의 String Reference
            var text = _hud.NoteText.StringReference;
            text.SetReference(Table, key);
            text.Arguments = args;
            _hud.NoteText.gameObject.SetActive(true);
            _hud.NoteText.RefreshString();
            _noteLeft = NoteSeconds;
        }
        #endregion

        #region Register - 등록 안 한 다중 룬 마법이면 등록하고 토스트, 비교 그림을 다시 만듦 (생략본이 생김)
        /// <param name="spell">생략 없이 그린 마법</param>
        void Register(SpellSO spell)
        {
            if (_run.CollectedSpells.Contains(spell)) return;
            _run.CollectedSpells.Add(spell);
            UIManager.Instance.ShowToast(CodexTable, RegisteredKey, spell.Name.GetLocalizedString());
            RebuildCandidates();
        }
        #endregion

        #region RebuildCandidates - 익힌 룬·등록한 마법·생략 가능 수로 판정 비교 그림을 다시 만듦 (코덱스와 같은 방식)
        void RebuildCandidates()
        {
            _learnedIds.Clear();
            foreach (RuneSO rune in _run.LearnedRunes)
                if (rune != null) _learnedIds.Add(rune.Id);
            // 등록한 마법 id
            var collected = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpellSO spell in _run.CollectedSpells)
                if (spell != null) collected.Add(spell.Id);
            _candidates = SpellRecognitionCalculator.BuildCandidates(_shapes, _learnedIds, collected, _run.OmitLimit);
        }
        #endregion

        #region Judge - 그린 획 판정 (기준은 기본값, 임시)
        /// <param name="strokes">그린 획</param>
        SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes) =>
            SpellRecognitionCalculator.Judge(strokes, _candidates, _learnedIds, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        #endregion

        #region NameAt - 판정 그림 번호의 이름 (지금 언어): 다중 룬 마법 이름, 그 뒤 번호는 '불 룬 주문'
        /// <param name="index">판정 ShapeIndex</param>
        string NameAt(int index)
        {
            if (_spells == null || _runes == null || index < 0) return string.Empty;
            if (index < _spells.Count) return _spells[index] != null ? _spells[index].Name.GetLocalizedString() : string.Empty;
            // 룬 번호
            int rune = index - _spells.Count;
            if (rune >= _runes.Count || _runes[rune] == null) return string.Empty;
            return new LocalizedString(CodexTable, RuneSpellKey).GetLocalizedString(_runes[rune].Name.GetLocalizedString());
        }
        #endregion

        #region Percent - 0~1을 % 정수로
        /// <param name="value">0~1 값</param>
        static int Percent(float value) => Mathf.RoundToInt(value * 100f);
        #endregion
    }
}
```

**호출/구독 관계:**
- `FieldState.OnSceneReady`가 `Init`, `SetControl`이 `enabled`로 켜고 끔 (켜지면 `Player/Attack`·`Player/CancelCast` 구독)
- 그리기 칸 `SpellDrawController`: `Begin(NameAt, Judge, OnJudged)`, `PenDown`·`PenMove`·`PenUp`·`CancelCast`를 부름. Space 바로 판정은 그리기 칸이 그대로 받음
- `PlayerController.MoveLocked`·`LookLocked`를 켜고 끔. HUD는 `FieldHudView`의 조준점·조준 원·조준 글·알림 글·펜 점
- `FieldHudUIBinder`가 `Mana`·`ManaMax`를 읽음

---

### `Assets/Scripts/Field/FogMapService.cs` (새 파일)

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 안개 지도 (v1, MonoBehaviour 아님): 지역을 3m 칸으로 나눠 걸으면 반경 24m를 밝힘. 진행도는 60m 구획 단위로, 구획 칸의 45%가 밝혀지면 그 구획을 셈. 밝힌 칸 배열은 판 데이터에 그대로 남음</summary>
    public sealed class FogMapService
    {
        /// <summary>안개 칸 한 변 (m, v1)</summary>
        public const float CellSize = 3f;

        /// <summary>걸을 때 밝히는 반지름 (m, v1)</summary>
        public const float RevealRadius = 24f;

        /// <summary>진행도 구획 한 변 (m, v1)</summary>
        public const float BlockSize = 60f;

        /// <summary>구획을 셀 밝힌 칸 비율 (v1)</summary>
        public const float BlockThreshold = 0.45f;

        /// <summary>칸마다 밝혔는지 (가로 Width × 세로 Height, 아래 줄부터)</summary>
        readonly bool[] _revealed;

        /// <summary>구획마다 밝힌 칸 수</summary>
        readonly int[] _blockRevealed;

        /// <summary>구획마다 전체 칸 수</summary>
        readonly int[] _blockCells;

        /// <summary>구획마다 셌는지</summary>
        readonly bool[] _blockCounted;

        /// <summary>센 구획 수</summary>
        int _counted;

        /// <summary>지역 (x = 월드 x 최소, y = 월드 z 최소, 너비·높이 m)</summary>
        public Rect Area { get; }

        /// <summary>가로 칸 수</summary>
        public int Width { get; }

        /// <summary>세로 칸 수</summary>
        public int Height { get; }

        /// <summary>밝힌 칸 배열 (판 데이터에 넣음)</summary>
        public bool[] Revealed => _revealed;

        /// <summary>진행도 (센 구획 / 전체 구획, 0~1)</summary>
        public float Progress => _blockCounted.Length > 0 ? (float)_counted / _blockCounted.Length : 0f;

        /// <summary>밝힐 때마다 오름 (지도 그림을 다시 그릴지)</summary>
        public int Version { get; private set; }

        #region FogMapService - 지역과 앞서 밝힌 칸(없거나 크기가 다르면 새로)을 받아 구획 수를 셈
        /// <param name="area">지역 (x·y = 월드 x·z 최소)</param>
        /// <param name="revealed">앞서 밝힌 칸 (판 데이터, 없으면 null)</param>
        public FogMapService(Rect area, bool[] revealed)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentException("지역 크기가 0", nameof(area));
            Area = area;
            Width = Mathf.CeilToInt(area.width / CellSize);
            Height = Mathf.CeilToInt(area.height / CellSize);
            _revealed = revealed != null && revealed.Length == Width * Height ? revealed : new bool[Width * Height];
            // 구획 가로·세로 수
            int blocksX = Mathf.CeilToInt(area.width / BlockSize);
            int blocksZ = Mathf.CeilToInt(area.height / BlockSize);
            _blockRevealed = new int[blocksX * blocksZ];
            _blockCells = new int[blocksX * blocksZ];
            _blockCounted = new bool[blocksX * blocksZ];
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                {
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockCells[block]++;
                    if (_revealed[z * Width + x]) _blockRevealed[block]++;
                }
            for (int b = 0; b < _blockCounted.Length; b++) Recount(b);
        }
        #endregion

        #region Reveal - 위치에서 반지름 24m 안 칸을 밝힘. 새로 밝힌 칸이 있으면 구획을 다시 세고 Version을 올림
        /// <param name="position">월드 위치</param>
        public void Reveal(Vector3 position)
        {
            // 위치 칸 좌표
            int cx = Mathf.FloorToInt((position.x - Area.xMin) / CellSize);
            int cz = Mathf.FloorToInt((position.z - Area.yMin) / CellSize);
            // 반지름 칸 수
            int reach = Mathf.CeilToInt(RevealRadius / CellSize);
            // 구획 가로 수
            int blocksX = Mathf.CeilToInt(Area.width / BlockSize);
            // 새로 밝힌 칸이 있는지
            bool changed = false;
            for (int z = Mathf.Max(0, cz - reach); z <= Mathf.Min(Height - 1, cz + reach); z++)
                for (int x = Mathf.Max(0, cx - reach); x <= Mathf.Min(Width - 1, cx + reach); x++)
                {
                    // 칸 번호
                    int index = z * Width + x;
                    if (_revealed[index]) continue;
                    // 칸 가운데 월드 x·z
                    float wx = Area.xMin + (x + 0.5f) * CellSize;
                    float wz = Area.yMin + (z + 0.5f) * CellSize;
                    if ((new Vector2(wx - position.x, wz - position.z)).sqrMagnitude > RevealRadius * RevealRadius) continue;
                    _revealed[index] = true;
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockRevealed[block]++;
                    Recount(block);
                    changed = true;
                }
            if (changed) Version++;
        }
        #endregion

        #region IsRevealed - 칸을 밝혔는지 (범위 밖은 false)
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        public bool IsRevealed(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Height && _revealed[z * Width + x];
        #endregion

        #region ToNormalized - 월드 위치를 지역 안 비율 (0~1, x·z)로
        /// <param name="world">월드 위치</param>
        public Vector2 ToNormalized(Vector3 world) => new((world.x - Area.xMin) / Area.width, (world.z - Area.yMin) / Area.height);
        #endregion

        #region BlockOf - 칸의 구획 번호
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        /// <param name="blocksX">구획 가로 수</param>
        static int BlockOf(int x, int z, int blocksX) =>
            Mathf.FloorToInt(z * CellSize / BlockSize) * blocksX + Mathf.FloorToInt(x * CellSize / BlockSize);
        #endregion

        #region Recount - 구획이 45%를 넘었으면 셈 (한 번 세면 안 풀림)
        /// <param name="block">구획 번호</param>
        void Recount(int block)
        {
            if (_blockCounted[block] || _blockCells[block] == 0) return;
            if (_blockRevealed[block] < _blockCells[block] * BlockThreshold) return;
            _blockCounted[block] = true;
            _counted++;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/FogRevealController.cs` (새 파일)

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>붙은 오브젝트(숲 플레이어)가 1m 움직일 때마다 안개 지도를 밝힘. 안개 지도는 상태가 Init으로 넘김</summary>
    public sealed class FogRevealController : MonoBehaviour
    {
        /// <summary>이만큼 움직일 때마다 밝힘 (m)</summary>
        const float MinMove = 1f;

        /// <summary>안개 지도 (없으면 안 함)</summary>
        FogMapService _fog;

        /// <summary>마지막으로 밝힌 위치</summary>
        Vector3 _last;

        /// <summary>아직 한 번도 안 밝혔는지</summary>
        bool _first;

        #region Init - 안개 지도를 받음 (다음 프레임에 지금 자리를 밝힘)
        /// <param name="fog">안개 지도</param>
        public void Init(FogMapService fog)
        {
            _fog = fog ?? throw new System.ArgumentNullException(nameof(fog));
            _first = true;
        }
        #endregion

        #region Update - 처음이거나 1m 넘게 움직였으면 지금 자리를 밝힘
        void Update()
        {
            if (_fog == null) return;
            // 지금 위치
            Vector3 position = transform.position;
            if (!_first && (position - _last).sqrMagnitude < MinMove * MinMove) return;
            _first = false;
            _last = position;
            _fog.Reveal(position);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/MapView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>지도 칸 참조 (숲 HUD 미니맵, 메뉴 창 지도 탭). 표시 셋은 그림의 자식, 앵커 가운데</summary>
    public sealed class MapView : MonoBehaviour
    {
        /// <summary>안개 지도 그림 (인스펙터에서만 연결)</summary>
        [SerializeField] RawImage _image;

        /// <summary>플레이어 표시 (위가 앞인 화살표. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _player;

        /// <summary>현재 목표 표시 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _objective;

        /// <summary>숲 입구 표시 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _exit;

        /// <summary>지도가 없을 때 글 (메뉴 지도 탭의 자리 글. 미니맵은 비움)</summary>
        [SerializeField] GameObject _placeholder;

        /// <summary>진행도 글 (Field/field.map_progress, {0} %. 미니맵은 비움)</summary>
        [SerializeField] LocalizeStringEvent _progress;

        /// <summary>안개 지도 그림</summary>
        public RawImage Image => _image;

        /// <summary>플레이어 표시</summary>
        public RectTransform Player => _player;

        /// <summary>현재 목표 표시</summary>
        public RectTransform Objective => _objective;

        /// <summary>숲 입구 표시</summary>
        public RectTransform Exit => _exit;

        /// <summary>지도가 없을 때 글 (없으면 null)</summary>
        public GameObject Placeholder => _placeholder;

        /// <summary>진행도 글 (없으면 null)</summary>
        public LocalizeStringEvent Progress => _progress;
    }
}
```

---

### `Assets/Scripts/UI/Binders/MapUIBinder.cs` (새 파일)

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>지도 칸 ← 안개 지도·플레이어·현재 목표·숲 입구 (출처는 상태가 Bind로 넘김). 밝힌 칸이 바뀌면 그림을 다시 칠하고 매 프레임 표시를 옮김. 보는 범위가 0이면 지역 전체(메뉴 지도 탭), 아니면 플레이어를 가운데 둔 그 범위(미니맵). 출처가 없으면 자리 글만</summary>
    public sealed class MapUIBinder : MonoBehaviour
    {
        /// <summary>안 밝힌 칸 색</summary>
        static readonly Color32 HiddenColor = new(18, 20, 24, 230);

        /// <summary>밝힌 칸 색</summary>
        static readonly Color32 ShownColor = new(112, 128, 96, 255);

        /// <summary>같은 칸의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] MapView _view;

        /// <summary>보는 범위 한 변 (m). 0이면 지역 전체</summary>
        [SerializeField, Min(0f)] float _windowMeters;

        /// <summary>안개 지도 (없으면 자리 글만)</summary>
        FogMapService _fog;

        /// <summary>플레이어</summary>
        Transform _player;

        /// <summary>현재 목표 위치 (없으면 null을 돌려줌)</summary>
        Func<Vector3?> _objective;

        /// <summary>숲 입구 위치</summary>
        Vector3 _exit;

        /// <summary>안개 지도 그림 (칸 하나 = 픽셀 하나, 아래 줄부터)</summary>
        Texture2D _texture;

        /// <summary>그림 픽셀 (다시 칠할 때 씀)</summary>
        Color32[] _pixels;

        /// <summary>마지막으로 칠한 안개 지도 Version</summary>
        int _drawnVersion = -1;

        /// <summary>마지막으로 보인 진행도 %</summary>
        int _shownPercent = -1;

        #region Awake - 연결 확인, 출처가 없을 때 모습 (자리 글)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Map] MapView 연결 안 됨", this);
                return;
            }
            ShowBound(false);
        }
        #endregion

        #region Bind - 안개 지도·플레이어·목표·입구를 받고 그림을 (크기가 다르면 새로) 만들어 지도를 보임
        /// <param name="fog">안개 지도</param>
        /// <param name="player">플레이어</param>
        /// <param name="objective">현재 목표 위치 (없으면 null을 돌려주는 함수, 함수가 null이면 표시 안 함)</param>
        /// <param name="exit">숲 입구 위치</param>
        public void Bind(FogMapService fog, Transform player, Func<Vector3?> objective, Vector3 exit)
        {
            _fog = fog ?? throw new ArgumentNullException(nameof(fog));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _objective = objective;
            _exit = exit;
            if (_texture == null || _texture.width != fog.Width || _texture.height != fog.Height)
            {
                if (_texture != null) Destroy(_texture);
                _texture = new Texture2D(fog.Width, fog.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _pixels = new Color32[fog.Width * fog.Height];
            }
            _drawnVersion = -1;
            _shownPercent = -1;
            if (_view == null) return;
            _view.Image.texture = _texture;
            ShowBound(true);
        }
        #endregion

        #region Unbind - 출처를 놓고 자리 글로
        public void Unbind()
        {
            _fog = null;
            _player = null;
            _objective = null;
            ShowBound(false);
        }
        #endregion

        #region ShowBound - 지도(그림·진행도)와 자리 글 중 하나를 보임
        /// <param name="bound">출처가 있는지</param>
        void ShowBound(bool bound)
        {
            if (_view == null) return;
            _view.Image.gameObject.SetActive(bound);
            if (_view.Placeholder != null) _view.Placeholder.SetActive(!bound);
            if (_view.Progress != null) _view.Progress.gameObject.SetActive(bound);
        }
        #endregion

        #region LateUpdate - 출처가 있으면 밝힌 칸이 바뀌었을 때 다시 칠하고, 표시를 옮기고, 진행도 글을 갱신
        void LateUpdate()
        {
            if (_fog == null || _player == null || _view == null) return;
            if (_fog.Version != _drawnVersion) Redraw();
            PlaceMarkers();
            RefreshProgress();
        }
        #endregion

        #region Redraw - 칸마다 밝혔으면 땅색, 아니면 어두운 색으로 칠함
        void Redraw()
        {
            for (int z = 0; z < _fog.Height; z++)
                for (int x = 0; x < _fog.Width; x++)
                    _pixels[z * _fog.Width + x] = _fog.IsRevealed(x, z) ? ShownColor : HiddenColor;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            _drawnVersion = _fog.Version;
        }
        #endregion

        #region PlaceMarkers - 보는 범위(전체 또는 플레이어 가운데)를 그림 uv로 정하고 플레이어(방향 포함)·목표·입구 표시를 옮김. 범위 밖 표시는 끔
        void PlaceMarkers()
        {
            // 플레이어 위치 (지역 비율)
            Vector2 player = _fog.ToNormalized(_player.position);
            // 보는 범위 크기 (지역 비율)
            Vector2 window = _windowMeters > 0f ? new Vector2(_windowMeters / _fog.Area.width, _windowMeters / _fog.Area.height) : Vector2.one;
            // 보는 범위 왼쪽 아래 (지역 비율)
            Vector2 origin = _windowMeters > 0f ? player - window * 0.5f : Vector2.zero;
            _view.Image.uvRect = new Rect(origin, window);
            Place(_view.Player, player, origin, window);
            if (_view.Player != null) _view.Player.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
            PlaceOptional(_view.Objective, _objective?.Invoke(), origin, window);
            PlaceOptional(_view.Exit, _exit, origin, window);
        }
        #endregion

        #region Place - 지역 비율 위치를 그림 안 자리로 옮김 (범위 밖이면 끔)
        /// <param name="marker">표시</param>
        /// <param name="normalized">지역 비율 위치</param>
        /// <param name="origin">보는 범위 왼쪽 아래</param>
        /// <param name="window">보는 범위 크기</param>
        void Place(RectTransform marker, Vector2 normalized, Vector2 origin, Vector2 window)
        {
            if (marker == null) return;
            // 보는 범위 안 비율 (0~1)
            Vector2 local = (normalized - origin) / window;
            // 범위 안인지
            bool inside = local.x >= 0f && local.x <= 1f && local.y >= 0f && local.y <= 1f;
            marker.gameObject.SetActive(inside);
            if (!inside) return;
            // 그림 칸 크기
            Rect rect = _view.Image.rectTransform.rect;
            marker.anchoredPosition = new Vector2((local.x - 0.5f) * rect.width, (local.y - 0.5f) * rect.height);
        }
        #endregion

        #region PlaceOptional - 위치가 있으면 옮기고 없으면 끔
        /// <param name="marker">표시</param>
        /// <param name="world">월드 위치 (없으면 null)</param>
        /// <param name="origin">보는 범위 왼쪽 아래</param>
        /// <param name="window">보는 범위 크기</param>
        void PlaceOptional(RectTransform marker, Vector3? world, Vector2 origin, Vector2 window)
        {
            if (marker == null) return;
            if (world == null)
            {
                marker.gameObject.SetActive(false);
                return;
            }
            Place(marker, _fog.ToNormalized(world.Value), origin, window);
        }
        #endregion

        #region RefreshProgress - 진행도 %가 바뀌었을 때만 글을 다시 씀 (글이 없으면 안 함)
        void RefreshProgress()
        {
            if (_view.Progress == null) return;
            // 지금 진행도 %
            int percent = Mathf.RoundToInt(_fog.Progress * 100f);
            if (percent == _shownPercent) return;
            _shownPercent = percent;
            _view.Progress.StringReference.Arguments = new object[] { percent };
            _view.Progress.RefreshString();
        }
        #endregion

        #region OnDestroy - 만든 그림을 지움
        void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/StaminaBarView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>스태미나 막대 참조 (전초기지·숲 HUD)</summary>
    public sealed class StaminaBarView : MonoBehaviour
    {
        /// <summary>채움 (Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _fill;

        /// <summary>막대 전체 투명도 (가득 차면 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>채움</summary>
        public Image Fill => _fill;

        /// <summary>막대 전체 투명도</summary>
        public CanvasGroup Group => _group;
    }
}
```

---

### `Assets/Scripts/UI/Binders/StaminaUIBinder.cs` (새 파일)

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>스태미나 막대 ← 1인칭 조작 스태미나 (출처는 상태가 Bind로 넘김). 줄어 있으면 나타나고 가득 차면 사라짐</summary>
    public sealed class StaminaUIBinder : MonoBehaviour
    {
        /// <summary>나타나고 사라지는 빠르기 (초당 투명도)</summary>
        const float FadeSpeed = 4f;

        /// <summary>가득 찼다고 볼 비율</summary>
        const float FullRatio = 0.999f;

        /// <summary>같은 막대의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] StaminaBarView _view;

        /// <summary>1인칭 조작 (없으면 숨김)</summary>
        PlayerController _player;

        #region Awake - 연결 확인, 처음엔 숨김
        void Awake()
        {
            if (_view == null) Debug.LogError("[Stamina] StaminaBarView 연결 안 됨", this);
            else _view.Group.alpha = 0f;
        }
        #endregion

        #region Bind - 1인칭 조작을 받음 (null이면 놓음)
        /// <param name="player">1인칭 조작</param>
        public void Bind(PlayerController player) => _player = player;
        #endregion

        #region Update - 스태미나 비율을 채우고, 줄어 있으면 나타나게·가득이면 사라지게 (실제 시간)
        void Update()
        {
            if (_view == null) return;
            // 스태미나 비율 (출처가 없으면 가득)
            float ratio = _player != null && _player.StaminaMax > 0f ? _player.Stamina / _player.StaminaMax : 1f;
            _view.Fill.fillAmount = ratio;
            _view.Group.alpha = Mathf.MoveTowards(_view.Group.alpha, ratio < FullRatio ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/PartyRowView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD 동료 줄 하나 참조 (이름·체력)</summary>
    public sealed class PartyRowView : MonoBehaviour
    {
        /// <summary>이름 (키는 바인더가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>체력 채움 (Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _hpFill;

        /// <summary>줄 투명도 (죽으면 흐리게. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>이름</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>체력 채움</summary>
        public Image HpFill => _hpFill;

        /// <summary>줄 투명도</summary>
        public CanvasGroup Group => _group;
    }
}
```

---

### `Assets/Scripts/UI/Views/FieldHudView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD 참조 (Forest 씬 Canvas/Hud). 배치는 v1 임시 (기획 08 HUD)</summary>
    public sealed class FieldHudView : MonoBehaviour
    {
        /// <summary>체력 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _hpFill;

        /// <summary>체력 글자 (Field/field.hud.hp. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _hpText;

        /// <summary>마나 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _manaFill;

        /// <summary>마나 글자 (Field/field.hud.mana. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _manaText;

        /// <summary>스태미나 막대 (인스펙터에서만 연결)</summary>
        [SerializeField] StaminaUIBinder _stamina;

        /// <summary>동료 줄 (파티 순서, 남는 줄은 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] PartyRowView[] _partyRows;

        /// <summary>조준점 (그리는 중·조준 중엔 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _crosshair;

        /// <summary>조준 원 (조준 중에만. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _aimRing;

        /// <summary>조준 글 (키는 FieldCastController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _aimText;

        /// <summary>알림 글 (키는 FieldCastController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _noteText;

        /// <summary>펜 점 (그리기 칸 자식, 그리는 중에만. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _pen;

        /// <summary>구역·지도 진행도 글 (Field/field.zone_line. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _zoneText;

        /// <summary>미니맵 (인스펙터에서만 연결)</summary>
        [SerializeField] MapUIBinder _minimap;

        /// <summary>마지막으로 때린 적 체력 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _targetRoot;

        /// <summary>마지막으로 때린 적 이름 (키는 바인더가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _targetName;

        /// <summary>마지막으로 때린 적 체력 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _targetFill;

        /// <summary>맞음·체력 낮음 붉은 화면 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _hurt;

        /// <summary>상호작용 안내 글 (UI/common.interact_prompt, {0} 키 이름·{1} 지점 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _interactPrompt;

        /// <summary>체력 채움</summary>
        public Image HpFill => _hpFill;

        /// <summary>체력 글자</summary>
        public LocalizeStringEvent HpText => _hpText;

        /// <summary>마나 채움</summary>
        public Image ManaFill => _manaFill;

        /// <summary>마나 글자</summary>
        public LocalizeStringEvent ManaText => _manaText;

        /// <summary>스태미나 막대</summary>
        public StaminaUIBinder Stamina => _stamina;

        /// <summary>동료 줄</summary>
        public PartyRowView[] PartyRows => _partyRows;

        /// <summary>조준점</summary>
        public GameObject Crosshair => _crosshair;

        /// <summary>조준 원</summary>
        public GameObject AimRing => _aimRing;

        /// <summary>조준 글</summary>
        public LocalizeStringEvent AimText => _aimText;

        /// <summary>알림 글</summary>
        public LocalizeStringEvent NoteText => _noteText;

        /// <summary>펜 점</summary>
        public RectTransform Pen => _pen;

        /// <summary>구역·지도 진행도 글</summary>
        public LocalizeStringEvent ZoneText => _zoneText;

        /// <summary>미니맵</summary>
        public MapUIBinder Minimap => _minimap;

        /// <summary>마지막으로 때린 적 체력 묶음</summary>
        public GameObject TargetRoot => _targetRoot;

        /// <summary>마지막으로 때린 적 이름</summary>
        public LocalizeStringEvent TargetName => _targetName;

        /// <summary>마지막으로 때린 적 체력 채움</summary>
        public Image TargetFill => _targetFill;

        /// <summary>맞음·체력 낮음 붉은 화면</summary>
        public CanvasGroup Hurt => _hurt;

        /// <summary>상호작용 안내 글</summary>
        public LocalizeStringEvent InteractPrompt => _interactPrompt;
    }
}
```

---

### `Assets/Scripts/UI/Binders/FieldHudUIBinder.cs` (새 파일)

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD ← 마법사 체력·마나·스태미나, 동료 체력, 구역·지도 진행도, 마지막으로 때린 적 체력(2.5초), 맞음(0.35초)·체력 30% 미만 붉은 화면 (v1). 출처는 FieldState가 Bind로 넘김 (조준·알림 글·펜은 FieldCastController)</summary>
    public sealed class FieldHudUIBinder : MonoBehaviour
    {
        /// <summary>때린 적 체력을 보이는 시간 (초, v1)</summary>
        const float TargetSeconds = 2.5f;

        /// <summary>맞았을 때 붉은 화면 시간 (초, v1)</summary>
        const float HurtSeconds = 0.35f;

        /// <summary>이 비율 아래면 체력 낮음 (v1)</summary>
        const float LowHpRatio = 0.3f;

        /// <summary>체력 낮음 붉은 화면 투명도</summary>
        const float LowHpAlpha = 0.35f;

        /// <summary>죽은 동료 줄 투명도</summary>
        const float DeadRowAlpha = 0.4f;

        /// <summary>체력 글 키 ({0} 지금, {1} 최대)</summary>
        const string HpKey = "field.hud.hp";

        /// <summary>마나 글 키 ({0} 지금, {1} 최대)</summary>
        const string ManaKey = "field.hud.mana";

        /// <summary>구역 줄 키 ({0} 구역 이름, {1} 지도 %)</summary>
        const string ZoneLineKey = "field.zone_line";

        /// <summary>구역 이름 키 형식 ({0} 구역 번호 1~3)</summary>
        const string ZoneKeyFormat = "field.zone.{0}";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>같은 HUD의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _view;

        /// <summary>마법사 몸 (없으면 안 함)</summary>
        FieldUnitController _player;

        /// <summary>현장 시전 (마나)</summary>
        FieldCastController _cast;

        /// <summary>동료 (파티 순서)</summary>
        IReadOnlyList<MercenaryController> _mercs;

        /// <summary>현장 규칙 (마지막으로 때린 적)</summary>
        FieldService _service;

        /// <summary>안개 지도 (진행도)</summary>
        FogMapService _fog;

        /// <summary>지금 구역 번호 (1~3)</summary>
        Func<int> _zoneAt;

        /// <summary>맞음 붉은 화면 남은 시간 (초)</summary>
        float _hurtLeft;

        /// <summary>마지막으로 보인 체력·마나 (정수)</summary>
        int _shownHp = -1;
        int _shownMana = -1;

        /// <summary>마지막으로 보인 구역·지도 %</summary>
        int _shownZone = -1;
        int _shownPercent = -1;

        /// <summary>마지막으로 이름을 넣은 때린 적</summary>
        FieldUnitController _shownTarget;

        #region Bind - 출처를 받고 맞음 알림 구독, 스태미나 막대에 조작을 넘김, 동료 줄 이름을 넣음 (남는 줄 숨김)
        /// <param name="player">마법사 몸</param>
        /// <param name="controller">1인칭 조작 (스태미나)</param>
        /// <param name="cast">현장 시전 (마나)</param>
        /// <param name="mercs">동료 (파티 순서)</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="fog">안개 지도</param>
        /// <param name="zoneAt">지금 구역 번호</param>
        public void Bind(FieldUnitController player, PlayerController controller, FieldCastController cast, IReadOnlyList<MercenaryController> mercs,
            FieldService service, FogMapService fog, Func<int> zoneAt)
        {
            Unbind();
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _cast = cast != null ? cast : throw new ArgumentNullException(nameof(cast));
            _mercs = mercs ?? throw new ArgumentNullException(nameof(mercs));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _fog = fog ?? throw new ArgumentNullException(nameof(fog));
            _zoneAt = zoneAt ?? throw new ArgumentNullException(nameof(zoneAt));
            _player.Damaged += OnPlayerDamaged;
            _shownHp = _shownMana = _shownZone = _shownPercent = -1;
            _shownTarget = null;
            _hurtLeft = 0f;
            if (_view == null) return;
            _view.Stamina.Bind(controller);
            for (int i = 0; i < _view.PartyRows.Length; i++)
            {
                // 이 줄
                PartyRowView row = _view.PartyRows[i];
                // 이 줄에 넣을 동료가 있는지
                bool has = i < _mercs.Count && _mercs[i] != null;
                row.gameObject.SetActive(has);
                if (!has) continue;
                // 동료 이름
                LocalizedString name = _mercs[i].Entry.Name;
                row.NameText.StringReference.SetReference(name.TableReference, name.TableEntryReference);
            }
        }
        #endregion

        #region Unbind - 맞음 알림 구독 해제, 출처를 놓음 (상태를 나갈 때)
        public void Unbind()
        {
            if (_player != null) _player.Damaged -= OnPlayerDamaged;
            _player = null;
            _cast = null;
            _mercs = null;
            _service = null;
            _fog = null;
            _zoneAt = null;
            if (_view != null) _view.Stamina.Bind(null);
        }
        #endregion

        #region OnDestroy - 출처를 놓음
        void OnDestroy() => Unbind();
        #endregion

        #region OnPlayerDamaged - 맞으면 붉은 화면 시간을 채움
        /// <param name="unit">맞은 몸 (마법사)</param>
        /// <param name="amount">실제 피해</param>
        /// <param name="source">때린 몸</param>
        void OnPlayerDamaged(FieldUnitController unit, float amount, FieldUnitController source) => _hurtLeft = HurtSeconds;
        #endregion

        #region Update - 출처가 있으면 막대·동료·구역·때린 적·붉은 화면 갱신
        void Update()
        {
            if (_player == null || _view == null) return;
            RefreshBar(_view.HpFill, _view.HpText, HpKey, _player.Hp, _player.MaxHp, ref _shownHp);
            RefreshBar(_view.ManaFill, _view.ManaText, ManaKey, _cast.Mana, _cast.ManaMax, ref _shownMana);
            RefreshParty();
            RefreshZone();
            RefreshTarget();
            RefreshHurt();
        }
        #endregion

        #region RefreshBar - 채움을 맞추고, 보이는 정수가 바뀌었을 때만 글을 다시 씀
        /// <param name="fill">채움</param>
        /// <param name="text">글자</param>
        /// <param name="key">글 키</param>
        /// <param name="value">지금 값</param>
        /// <param name="max">최대 값</param>
        /// <param name="shown">마지막으로 보인 정수</param>
        static void RefreshBar(Image fill, LocalizeStringEvent text, string key, float value, float max, ref int shown)
        {
            fill.fillAmount = max > 0f ? value / max : 0f;
            // 보일 정수 (올림)
            int rounded = Mathf.CeilToInt(value);
            if (rounded == shown) return;
            shown = rounded;
            text.StringReference.SetReference(Table, key);
            text.StringReference.Arguments = new object[] { rounded, Mathf.CeilToInt(max) };
            text.RefreshString();
        }
        #endregion

        #region RefreshParty - 동료 줄 체력 채움, 죽었으면 흐리게
        void RefreshParty()
        {
            for (int i = 0; i < _view.PartyRows.Length && i < _mercs.Count; i++)
            {
                // 이 동료
                MercenaryController merc = _mercs[i];
                if (merc == null) continue;
                // 이 줄
                PartyRowView row = _view.PartyRows[i];
                row.HpFill.fillAmount = merc.Unit.MaxHp > 0f ? merc.Unit.Hp / merc.Unit.MaxHp : 0f;
                row.Group.alpha = merc.Unit.IsAlive ? 1f : DeadRowAlpha;
            }
        }
        #endregion

        #region RefreshZone - 구역이나 지도 %가 바뀌었을 때만 "구역 이름 · 지도 N%"를 다시 씀
        void RefreshZone()
        {
            // 지금 구역·지도 %
            int zone = _zoneAt();
            int percent = Mathf.RoundToInt(_fog.Progress * 100f);
            if (zone == _shownZone && percent == _shownPercent) return;
            _shownZone = zone;
            _shownPercent = percent;
            // 구역 이름 (지금 언어)
            string zoneName = new LocalizedString(Table, string.Format(CultureInfo.InvariantCulture, ZoneKeyFormat, zone)).GetLocalizedString();
            // 구역 줄의 String Reference
            var text = _view.ZoneText.StringReference;
            text.SetReference(Table, ZoneLineKey);
            text.Arguments = new object[] { zoneName, percent };
            _view.ZoneText.RefreshString();
        }
        #endregion

        #region RefreshTarget - 마법사가 2.5초 안에 때린 산 적이 있으면 이름·체력을 보이고, 아니면 숨김
        void RefreshTarget()
        {
            // 마지막으로 때린 적
            FieldUnitController target = _service.LastHit;
            // 보일지
            bool show = target != null && target.IsAlive && Time.time - _service.LastHitTime <= TargetSeconds;
            _view.TargetRoot.SetActive(show);
            if (!show) return;
            _view.TargetFill.fillAmount = target.MaxHp > 0f ? target.Hp / target.MaxHp : 0f;
            if (target == _shownTarget) return;
            _shownTarget = target;
            if (!target.TryGetComponent(out EnemyController enemy) || enemy.Monster == null) return;
            // 적 이름
            LocalizedString name = enemy.Monster.Name;
            _view.TargetName.StringReference.SetReference(name.TableReference, name.TableEntryReference);
        }
        #endregion

        #region RefreshHurt - 맞음 시간과 체력 낮음 중 큰 쪽으로 붉은 화면 투명도
        void RefreshHurt()
        {
            if (_hurtLeft > 0f) _hurtLeft -= Time.deltaTime;
            // 맞음 투명도
            float hurt = _hurtLeft > 0f ? _hurtLeft / HurtSeconds : 0f;
            // 체력 낮음 투명도
            float low = _player.MaxHp > 0f && _player.Hp / _player.MaxHp < LowHpRatio ? LowHpAlpha : 0f;
            _view.Hurt.alpha = Mathf.Max(hurt, low);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/SpawnPointView.cs` (새 파일)

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>숲에 고정된 적 무리 자리 표지 (씬 오브젝트, 로직 없음). 상태가 이 자리 둘레에 몬스터를 수만큼 놓음. 적 키는 {id}.{번호}</summary>
    public sealed class SpawnPointView : MonoBehaviour
    {
        /// <summary>자리 id (씬 안에서 겹치지 않게. 판 데이터 쓰러진 적 키에 씀. 인스펙터에서만 정함)</summary>
        [SerializeField] string _id;

        /// <summary>놓을 몬스터 (인스펙터에서만 연결)</summary>
        [SerializeField] MonsterSO _monster;

        /// <summary>놓을 수</summary>
        [SerializeField, Min(1)] int _count = 3;

        /// <summary>이 자리에서 흩어 놓는 반지름 (m, v1 무리 ±5)</summary>
        [SerializeField, Min(0f)] float _spread = 5f;

        /// <summary>자리 id</summary>
        public string Id => _id;

        /// <summary>놓을 몬스터</summary>
        public MonsterSO Monster => _monster;

        /// <summary>놓을 수</summary>
        public int Count => _count;

        /// <summary>흩어 놓는 반지름 (m)</summary>
        public float Spread => _spread;
    }
}
```

---

### `Assets/Scripts/UI/Views/FieldView.cs` (새 파일)

```csharp
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

        /// <summary>들어오면 서는 곳 (숲 입구 안쪽, 숲을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _entry;

        /// <summary>죽은 뒤 용병이 도망치는 곳 (숲 입구 표지판 앞. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _exitPoint;

        /// <summary>차원문 (자리는 판 데이터. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _portal;

        /// <summary>차원문을 놓을 수 있는 곳 (3구역 중 전초기지에서 먼 쪽 절반, 월드)</summary>
        [SerializeField] Bounds _portalArea = new(new Vector3(0f, 0f, 660f), new Vector3(160f, 0f, 110f));

        /// <summary>안개 지도 지역 (x·y = 월드 x·z 최소, m)</summary>
        [SerializeField] Rect _mapArea = new(-90f, 0f, 180f, 720f);

        /// <summary>구역 하나 깊이 (m. 입구에서 z로 잼)</summary>
        [SerializeField, Min(1f)] float _zoneDepth = 240f;

        /// <summary>구역 수</summary>
        [SerializeField, Min(1)] int _zoneCount = 3;

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

        /// <summary>차원문</summary>
        public Transform Portal => _portal;

        /// <summary>차원문을 놓을 수 있는 곳</summary>
        public Bounds PortalArea => _portalArea;

        /// <summary>안개 지도 지역</summary>
        public Rect MapArea => _mapArea;

        /// <summary>구역 하나 깊이 (m)</summary>
        public float ZoneDepth => _zoneDepth;

        /// <summary>구역 수</summary>
        public int ZoneCount => _zoneCount;

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
    }
}
```

---

### `Assets/Scripts/Flow/FieldState.cs` (전체 교체)

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>숲. 3구역 회색 숲에 마법사·용병·적(쓰러뜨린 적은 쓰러진 자리에 시체)을 두고 현장 시전·안개 지도·12시 안내(전투 중엔 숨김)를 켬. 숲 입구에서 F로 귀환, 소탕 대상을 다 쓰러뜨리면 의뢰 목표 달성. 죽으면 금화를 떨구고 덮개 밑에서 용병 도망을 빠르게 돌린 뒤 여관 방 침대(방이 없으면 정문)에서 부활</summary>
    public sealed class FieldState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름 (지역이 늘면 의뢰 데이터에서 받음)</summary>
        public const string Scene = "Forest";

        /// <summary>지도 탭 입력 액션 경로 (전역 액션, M. 누르면 지도 튜토리얼 배움)</summary>
        const string MapActionPath = "UI/Map";

        /// <summary>현장 시전 튜토리얼 id</summary>
        const string FieldCastTutorialId = "field_cast";

        /// <summary>지도 튜토리얼 id</summary>
        const string MapTutorialId = "map";

        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 F를 쓰면 배움)</summary>
        const string InteractTutorialId = "interact";

        /// <summary>숲 입구 지점 id (귀환)</summary>
        const string ExitId = "forest_exit";

        /// <summary>떨어진 금화 지점 id (줍기)</summary>
        const string GoldId = "gold";

        /// <summary>마법사 최대 체력 (v1: 100 + 생존 4 × 6, 임시)</summary>
        const float PlayerMaxHp = 124f;

        /// <summary>놓을 자리를 NavMesh에 붙일 때 찾는 거리 (m)</summary>
        const float SampleDistance = 4f;

        /// <summary>용병을 입구 둘레에 놓는 간격 (m)</summary>
        const float PartySpread = 2f;

        /// <summary>죽은 뒤 도망 시뮬 게임 속도 (임시)</summary>
        const float DeathGameSpeed = 6f;

        /// <summary>죽은 뒤 도망 시뮬 최대 게임 시간 (초, 임시)</summary>
        const float DeathMaxSeconds = 40f;

        /// <summary>쫓는 적 없이 이만큼 지나면 무사 (게임 초, 임시)</summary>
        const float SafeCalmSeconds = 3f;

        /// <summary>적 키 형식 ({0} 자리 id, {1} 번호)</summary>
        const string SpawnKeyFormat = "{0}.{1}";

        /// <summary>소탕 목표 달성 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestClearedKey = "field.quest_cleared";

        /// <summary>금화를 주웠을 때 토스트 키 ({0} 금화)</summary>
        const string GoldPickedKey = "field.gold_picked";

        /// <summary>동료를 잃었을 때 토스트 키 ({0} 이름)</summary>
        const string MercenaryLostKey = "field.mercenary_lost";

        /// <summary>목표가 바뀌었을 때 토스트 키 ({0} 목표 이름, Menu 테이블)</summary>
        const string ObjectiveUpdatedKey = "menu.journal.updated";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (죽은 뒤 도망 시뮬 중엔 못 엶)</summary>
        public override bool UsesPauseMenu => !_dying;

        /// <summary>이 씬의 화면 참조</summary>
        FieldView _view;

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>안개 지도</summary>
        FogMapService _fog;

        /// <summary>데려온 용병 (파티 순서. 죽어도 남음)</summary>
        readonly List<MercenaryController> _mercs = new();

        /// <summary>살아 있는 소탕 대상</summary>
        readonly List<EnemyController> _targets = new();

        /// <summary>떨어진 금화 지점 (없으면 null)</summary>
        InteractPointView _goldPile;

        /// <summary>지도 탭 입력 (Exit에서 구독 해제)</summary>
        InputAction _map;

        /// <summary>죽은 뒤 도망 시뮬 코루틴 (GameManager가 돌림. 없으면 null)</summary>
        Coroutine _death;

        /// <summary>죽어서 마을로 가는 중인지</summary>
        bool _dying;

        /// <summary>들어올 때 목표가 바뀌었는지 (Reveal에서 알림)</summary>
        bool _objectiveChanged;

        #region OnSceneReady - 현장 규칙·안개 지도, 차원문 자리, 마법사를 입구에, 용병·적(쓰러진 적은 시체)·떨어진 금화를 놓고, 시전·안개·HUD·지도를 연결, 목표를 정함 (덮개 밑)
        protected override void OnSceneReady()
        {
            _view = FindView<FieldView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            _service = new FieldService(_view.PlayerUnit);
            _fog = new FogMapService(_view.MapArea, run.ForestFog);
            run.ForestFog = _fog.Revealed;
            PlacePortal(run);
            _view.Player.PlaceAt(_view.Entry);
            _view.PlayerUnit.Init(PlayerMaxHp);
            SpawnParty(run);
            SpawnEnemies(run);
            SpawnGold(run);
            _view.Cast.Init(run, DataManager.Instance.Spells, DataManager.Instance.Runes, _service, _view.Player, _view.PlayerUnit);
            _view.FogReveal.Init(_fog);
            _view.HudBinder.Bind(_view.PlayerUnit, _view.Player, _view.Cast, _mercs, _service, _fog, ZoneAt);
            _view.Hud.Minimap.Bind(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            UIManager.Instance.SetMapSource(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            _view.Hud.InteractPrompt.gameObject.SetActive(false);
            if (QuestActive(run) && _targets.Count == 0) run.QuestCleared = true;
            _objectiveChanged = UpdateObjective(false);
        }
        #endregion

        #region Reveal - 커서 잠금, 지점·죽음·전투 상황·M 구독, 조작 켬, 안 배운 12시 안내 알림, 들어올 때 목표가 바뀌었으면 알림 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.PlayerUnit.Died += OnPlayerDied;
            _service.CombatChanged += OnCombatChanged;
            _map = InputSystem.actions.FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMapPressed;
            else Debug.LogError($"[Field] {MapActionPath} 액션 없음. 지도 튜토리얼을 못 배움");
            SetControl(true);
            TutorialManager.Instance.Notify(FieldCastTutorialId);
            TutorialManager.Instance.Notify(MapTutorialId);
            if (_objectiveChanged) NotifyObjective();
        }
        #endregion

        #region Exit - 도망 시뮬을 멈추고 게임 속도를 되돌림, 구독 해제, 조작 끔, HUD·지도 출처를 놓음, 안내 숨김 풀고 정리, 자막 숨김, 커서 풂
        public override void Exit()
        {
            if (_death != null)
            {
                Game.StopCoroutine(_death);
                _death = null;
            }
            Game.SetGameSpeed(1f);
            if (_view != null)
            {
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.PlayerUnit.Died -= OnPlayerDied;
                SetControl(false);
                _view.HudBinder.Unbind();
                _view.Hud.Minimap.Unbind();
            }
            if (_service != null) _service.CombatChanged -= OnCombatChanged;
            if (_map != null) _map.performed -= OnMapPressed;
            UIManager.Instance.ClearMapSource();
            TutorialManager.Instance.SetSuppressed(false);
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region HandleCancel - 죽어서 마을로 가는 중엔 Esc를 먹음 (일시정지 메뉴 안 엶)
        public override bool HandleCancel() => _dying;
        #endregion

        #region SetControl - 조작·지점 찾기·현장 시전 켜고 끄기 (씬이 내려갔으면 무시)
        /// <param name="on">켤지</param>
        void SetControl(bool on)
        {
            if (_view == null) return;
            _view.Player.enabled = on;
            _view.Detector.enabled = on;
            _view.Cast.enabled = on;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글
            var prompt = _view.Hud.InteractPrompt;
            if (target == null)
            {
                prompt.gameObject.SetActive(false);
                return;
            }
            prompt.StringReference.Arguments = new object[] { _view.Detector.KeyName, target.Label.GetLocalizedString() };
            prompt.gameObject.SetActive(true);
            prompt.RefreshString();
        }
        #endregion

        #region OnInteracted - 처음 쓰면 상호작용 배움. 숲 입구면 귀환, 떨어진 금화면 줍기. 모르는 id는 경고
        /// <param name="target">상호작용 키를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
            switch (target.Id)
            {
                case ExitId: Return(); break;
                case GoldId: PickGold(target); break;
                default: Debug.LogWarning($"[Field] 모르는 지점 id: {target.Id} ({target.name})"); break;
            }
        }
        #endregion

        #region Return - 숲에서 돌아감 표시 후 전초기지로 (정문 밖에서 시작)
        void Return()
        {
            if (_dying) return;
            Game.Run.ReturningFromField = true;
            Game.ChangeState(new HubState());
        }
        #endregion

        #region PickGold - 떨어진 금화를 다 주워 금화에 더하고 지점을 지움
        /// <param name="pile">떨어진 금화 지점</param>
        void PickGold(InteractPointView pile)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 주운 금화
            int gold = run.DroppedGold;
            run.Gold += gold;
            run.DroppedGold = 0;
            Object.Destroy(pile.gameObject);
            _goldPile = null;
            UIManager.Instance.ShowToast(Table, GoldPickedKey, gold);
        }
        #endregion

        #region SpawnParty - 파티 용병을 입구 둘레(마법사 뒤 양옆)에 놓고 마법사를 따라오게 함
        /// <param name="run">이번 판 데이터</param>
        void SpawnParty(RunEntry run)
        {
            for (int i = 0; i < run.Party.Count; i++)
            {
                // 입구 기준 자리 (뒤 양옆)
                Vector3 offset = new((i % 2 == 0 ? -1f : 1f) * PartySpread, 0f, -PartySpread);
                // 놓을 곳
                Vector3 position = Sample(_view.Entry.position + _view.Entry.rotation * offset);
                // 놓은 용병
                MercenaryController merc = Object.Instantiate(_view.MercenaryPrefab, position, _view.Entry.rotation);
                merc.Init(run.Party[i], _service, _view.Player.transform, i);
                merc.Died += OnMercenaryDied;
                _mercs.Add(merc);
            }
        }
        #endregion

        #region SpawnEnemies - 적 자리마다 수만큼 놓음: 쓰러진 키는 쓰러진 곳에 시체, 아니면 자리 둘레에 살아 있는 적. 무리 번호는 자리 순서. 의뢰 소탕 대상이면 따로 셈
        /// <param name="run">이번 판 데이터</param>
        void SpawnEnemies(RunEntry run)
        {
            // 무리 번호 (자리마다 하나)
            int pack = 0;
            foreach (SpawnPointView point in _view.SpawnPoints)
            {
                if (point == null || point.Monster == null)
                {
                    Debug.LogWarning("[Field] 비었거나 몬스터가 없는 적 자리", point);
                    continue;
                }
                pack++;
                for (int i = 0; i < point.Count; i++)
                {
                    // 이 적 키
                    string key = string.Format(CultureInfo.InvariantCulture, SpawnKeyFormat, point.Id, i);
                    // 쓰러졌는지, 쓰러진 곳
                    bool dead = run.ForestDead.TryGetValue(key, out Vector3 corpseAt);
                    // 놓을 곳
                    Vector3 position = Sample(dead ? corpseAt : SpawnPosition(point));
                    // 놓은 적
                    EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    if (dead)
                    {
                        enemy.InitCorpse(point.Monster, key, pack);
                        continue;
                    }
                    enemy.Init(point.Monster, _service, key, pack);
                    enemy.Died += OnEnemyDied;
                    if (IsQuestTarget(run, point.Monster)) _targets.Add(enemy);
                }
            }
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

        #region Sample - 가까운 NavMesh 위 자리 (없으면 그대로)
        /// <param name="position">놓으려는 곳</param>
        static Vector3 Sample(Vector3 position) =>
            NavMesh.SamplePosition(position, out NavMeshHit hit, SampleDistance, NavMesh.AllAreas) ? hit.position : position;
        #endregion

        #region SpawnGold - 떨어진 금화가 있으면 그 자리에 금화 지점을 놓음
        /// <param name="run">이번 판 데이터</param>
        void SpawnGold(RunEntry run)
        {
            if (run.DroppedGold <= 0) return;
            _goldPile = Object.Instantiate(_view.GoldPilePrefab, run.DroppedGoldPosition, Quaternion.identity);
        }
        #endregion

        #region PlacePortal - 이 판에서 처음이면 3구역 먼 쪽 절반에서 차원문 자리를 정해 판 데이터에 남기고, 차원문을 그 자리에
        /// <param name="run">이번 판 데이터</param>
        void PlacePortal(RunEntry run)
        {
            if (run.PortalPosition == null)
            {
                // 놓을 수 있는 곳
                Bounds area = _view.PortalArea;
                // 고른 곳
                var picked = new Vector3(Random.Range(area.min.x, area.max.x), area.center.y, Random.Range(area.min.z, area.max.z));
                run.PortalPosition = Sample(picked);
            }
            _view.Portal.position = run.PortalPosition.Value;
        }
        #endregion

        #region OnEnemyDied - 쓰러진 곳을 판 데이터에 남김. 소탕 대상이 다 쓰러졌으면 목표 달성
        /// <param name="enemy">쓰러진 적</param>
        void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            Game.Run.ForestDead[enemy.SpawnKey] = enemy.transform.position;
            if (_targets.Remove(enemy) && _targets.Count == 0 && QuestActive(Game.Run)) ClearQuest();
        }
        #endregion

        #region QuestActive - 받은 의뢰가 있고 아직 목표를 안 끝냈는지
        /// <param name="run">이번 판 데이터</param>
        static bool QuestActive(RunEntry run) => run.Quest != null && !run.QuestCleared;
        #endregion

        #region IsQuestTarget - 이 몬스터가 받은 의뢰의 소탕 대상인지
        /// <param name="run">이번 판 데이터</param>
        /// <param name="monster">몬스터</param>
        static bool IsQuestTarget(RunEntry run, MonsterSO monster) => QuestActive(run) && run.Quest.Target != null && run.Quest.Target == monster;
        #endregion

        #region ClearQuest - 목표 달성 표시, 목표를 '숲 입구로 돌아가기'로, 토스트
        void ClearQuest()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            run.QuestCleared = true;
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestClearedKey, run.Quest.Title.GetLocalizedString());
        }
        #endregion

        #region UpdateObjective - 받은 의뢰가 없으면 없음, 목표를 안 끝냈으면 소탕, 끝냈으면 돌아가기. 바뀌었으면 판 데이터에 넣고 (notify면) 알림. 바뀌었으면 true
        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
        bool UpdateObjective(bool notify)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 정한 목표
            ObjectiveSO next = run.Quest == null ? null : run.QuestCleared ? _view.ReturnObjective : _view.ClearObjective;
            if (next == run.CurrentObjective) return false;
            run.CurrentObjective = next;
            if (notify) NotifyObjective();
            return true;
        }
        #endregion

        #region NotifyObjective - 지금 목표가 있으면 "임무 일지 갱신: 이름" 토스트
        void NotifyObjective()
        {
            // 이번 판 현재 목표
            ObjectiveSO objective = Game.Run.CurrentObjective;
            if (objective == null) return;
            UIManager.Instance.ShowToast(MenuTable, ObjectiveUpdatedKey, objective.Title.GetLocalizedString());
        }
        #endregion

        #region ObjectivePosition - 지도 목표 표시: 소탕 중이면 남은 대상 집의 가운데, 끝냈으면 숲 입구, 아니면 없음
        Vector3? ObjectivePosition()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Quest == null) return null;
            if (run.QuestCleared) return _view.Entry.position;
            // 남은 대상 집의 합과 수
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (EnemyController enemy in _targets)
            {
                if (enemy == null) continue;
                sum += enemy.Home;
                count++;
            }
            return count > 0 ? sum / count : null;
        }
        #endregion

        #region ZoneAt - 플레이어가 있는 구역 번호 (입구에서 z로 잰 깊이, 1~구역 수)
        int ZoneAt() => Mathf.Clamp(Mathf.FloorToInt((_view.Player.transform.position.z - _view.MapArea.yMin) / _view.ZoneDepth) + 1, 1, _view.ZoneCount);
        #endregion

        #region OnCombatChanged - 전투 상황이면 12시 안내를 숨기고, 끝나면 다시 보임
        /// <param name="inCombat">전투 상황인지</param>
        void OnCombatChanged(bool inCombat) => TutorialManager.Instance.SetSuppressed(inCombat);
        #endregion

        #region OnMapPressed - M을 누르면 지도 배움
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnMapPressed(InputAction.CallbackContext context) => TutorialManager.Instance.MarkLearned(MapTutorialId);
        #endregion

        #region OnMercenaryDied - 판 데이터 파티에서 빼고 토스트
        /// <param name="merc">죽은 용병</param>
        void OnMercenaryDied(MercenaryController merc)
        {
            merc.Died -= OnMercenaryDied;
            Game.Run.Party.Remove(merc.Entry);
            UIManager.Instance.ShowToast(Table, MercenaryLostKey, merc.Entry.Name.GetLocalizedString());
        }
        #endregion

        #region OnPlayerDied - 한 번만: 조작 끔, 금화를 떨굼, 안내 정리, 덮개를 덮고 도망 시뮬 시작
        /// <param name="unit">죽은 몸 (마법사)</param>
        void OnPlayerDied(FieldUnitController unit)
        {
            if (_dying) return;
            _dying = true;
            SetControl(false);
            DropGold(Game.Run);
            TutorialManager.Instance.Clear();
            TransitionManager.Instance.ShowCover();
            _death = Game.StartCoroutine(DeathRoutine());
        }
        #endregion

        #region DropGold - 앞에 떨어진 금화는 사라지고, 들고 있던 금화를 죽은 자리에 떨굼 (없으면 떨어진 것 없음)
        /// <param name="run">이번 판 데이터</param>
        void DropGold(RunEntry run)
        {
            if (_goldPile != null) Object.Destroy(_goldPile.gameObject);
            _goldPile = null;
            run.DroppedGold = run.Gold;
            run.DroppedGoldPosition = _view.Player.transform.position;
            run.Gold = 0;
        }
        #endregion

        #region DeathRoutine - 살아 있는 용병을 숲 입구로 도망치게 하고 게임 속도를 올려 돌림: 입구에 닿거나 쫓는 적 없이 3초면 무사, 도중에 죽으면 죽음 (OnMercenaryDied). 다 정해지거나 40초가 지나면 속도를 되돌리고 부활 표시 후 전초기지로
        IEnumerator DeathRoutine()
        {
            foreach (MercenaryController merc in _mercs)
                if (merc != null && merc.Unit.IsAlive) merc.Flee(_view.ExitPoint);
            // 용병마다 쫓는 적 없이 지난 시간 (게임 초)
            var calm = new Dictionary<MercenaryController, float>();
            // 지난 게임 시간 (초)
            float elapsed = 0f;
            Game.SetGameSpeed(DeathGameSpeed);
            while (elapsed < DeathMaxSeconds)
            {
                yield return null;
                // 이번 프레임 게임 시간 (초)
                float dt = Time.deltaTime;
                elapsed += dt;
                // 아직 정해지지 않은 용병이 있는지
                bool pending = false;
                foreach (MercenaryController merc in _mercs)
                {
                    if (merc == null || !merc.Unit.IsAlive || merc.IsSafe) continue;
                    calm.TryGetValue(merc, out float quiet);
                    quiet = _service.IsTargeted(merc.Unit) ? 0f : quiet + dt;
                    calm[merc] = quiet;
                    if (quiet >= SafeCalmSeconds) merc.MarkSafe();
                    else pending = true;
                }
                if (!pending) break;
            }
            Game.SetGameSpeed(1f);
            _death = null;
            Game.Run.RespawningFromDeath = true;
            Game.ChangeState(new HubState());
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `GameManager.SwitchState`가 `Begin`(→ `OnSceneReady`) → 덮개 해제 → `Reveal`, 나갈 때 `Exit`
- 구독: `InteractDetector.TargetChanged`·`Interacted`, `FieldUnitController.Died`(마법사), `FieldService.CombatChanged`, `UI/Map.performed`, `EnemyController.Died`, `MercenaryController.Died`
- 부름: `TutorialManager.Notify`·`MarkLearned`·`SetSuppressed`·`Clear`, `UIManager.SetMapSource`·`ClearMapSource`·`ShowToast`, `TransitionManager.ShowCover`, `GameManager.SetGameSpeed`·`StartCoroutine`·`ChangeState`

---

### `Assets/Scripts/Player/PlayerController.cs` (전체 교체)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>1인칭 조작: 이동·달리기(뒤로는 못 달림, 스태미나)·점프·앉기(누르는 동안)·마우스 시점·중력. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함. 현장 시전이 이동·시점을 잠금</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>이동 입력 액션 경로 (전역 액션, WASD)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션, 왼쪽 Shift)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>점프 입력 액션 경로 (전역 액션, Space. 시전 상태에선 같은 키가 바로 판정이라 이동 잠금으로 막힘)</summary>
        const string JumpActionPath = "Player/Jump";

        /// <summary>앉기 입력 액션 경로 (전역 액션, 왼쪽 Ctrl. 누르는 동안)</summary>
        const string CrouchActionPath = "Player/Crouch";

        /// <summary>땅에 있을 때 아래로 누르는 속도 (m/s). 경사·턱에서 뜨지 않게</summary>
        const float GroundedFallSpeed = -2f;

        /// <summary>이보다 뒤로 가는 입력이면 못 달림 (앞뒤 입력 값)</summary>
        const float BackwardInput = -0.01f;

        /// <summary>움직이는 중으로 볼 이동 입력 크기</summary>
        const float MovingInput = 0.1f;

        /// <summary>몸 (충돌·이동. 인스펙터에서만 연결)</summary>
        [SerializeField] CharacterController _body;

        /// <summary>머리 (카메라 자리, 위아래로 돎·앉으면 낮아짐. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _head;

        /// <summary>걷기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _walkSpeed = 5.6f;

        /// <summary>달리기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _sprintSpeed = 8.2f;

        /// <summary>마우스 감도 (이동량 1당 도)</summary>
        [SerializeField, Min(0f)] float _lookSensitivity = 0.1f;

        /// <summary>위아래로 볼 수 있는 한도 (도)</summary>
        [SerializeField, Range(0f, 89f)] float _pitchLimit = 85f;

        /// <summary>중력 가속도 (m/s², 아래가 음수)</summary>
        [SerializeField] float _gravity = -20f;

        /// <summary>점프 처음 속도 (m/s, 임시. 중력 20이면 약 0.6m)</summary>
        [SerializeField, Min(0f)] float _jumpSpeed = 5f;

        /// <summary>앉았을 때 머리 높이 (m, 임시)</summary>
        [SerializeField, Min(0.2f)] float _crouchEyeHeight = 1f;

        /// <summary>앉았을 때 걷기 속도 배율 (임시)</summary>
        [SerializeField, Range(0.1f, 1f)] float _crouchSpeedScale = 0.5f;

        /// <summary>머리가 앉고 서는 빠르기 (m/s)</summary>
        [SerializeField, Min(0.1f)] float _eyeMoveSpeed = 6f;

        /// <summary>최대 스태미나 (임시)</summary>
        [SerializeField, Min(1f)] float _staminaMax = 100f;

        /// <summary>달릴 때 초당 스태미나 소모 (임시)</summary>
        [SerializeField, Min(0f)] float _sprintCost = 25f;

        /// <summary>쉴 때 초당 스태미나 회복 (임시)</summary>
        [SerializeField, Min(0f)] float _staminaRegen = 20f;

        /// <summary>달리기를 멈추고 회복이 시작되기까지 (초, 임시)</summary>
        [SerializeField, Min(0f)] float _regenDelay = 1f;

        /// <summary>다 쓴 뒤 다시 달릴 수 있는 스태미나 비율 (0~1, 임시)</summary>
        [SerializeField, Range(0f, 1f)] float _recoverRatio = 0.3f;

        /// <summary>이동 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        /// <summary>점프 입력. 없으면 null</summary>
        InputAction _jump;

        /// <summary>앉기 입력. 없으면 null</summary>
        InputAction _crouch;

        /// <summary>지금 위아래 각도 (도, 위를 보면 음수)</summary>
        float _pitch;

        /// <summary>지금 세로 속도 (m/s, 아래가 음수)</summary>
        float _fallSpeed;

        /// <summary>선 머리 높이 (프리팹 Head 높이)</summary>
        float _standEyeHeight;

        /// <summary>지금 머리 높이 (앉고 설 때 이 값을 옮김)</summary>
        float _eyeHeight;

        /// <summary>회복이 시작되기까지 남은 시간 (초)</summary>
        float _regenWait;

        /// <summary>스태미나를 다 써서 회복될 때까지 못 달리는지</summary>
        bool _exhausted;

        /// <summary>머리 (카메라 자리)</summary>
        public Transform Head => _head;

        /// <summary>지금 스태미나</summary>
        public float Stamina { get; private set; }

        /// <summary>최대 스태미나</summary>
        public float StaminaMax => _staminaMax;

        /// <summary>앉아 있는지</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>이번 프레임 달렸는지</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>이동·달리기·점프·앉기 잠금 (현장 시전이 그리는 중·조준 중에 켬)</summary>
        public bool MoveLocked { get; set; }

        /// <summary>시점 잠금 (현장 시전이 그리는 중에 켬)</summary>
        public bool LookLocked { get; set; }

        #region Awake - 연결 확인, 입력 액션 찾기, 선 머리 높이·스태미나 채움, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_body == null || _head == null) Debug.LogError("[Player] 몸·머리 연결 안 됨");
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
            _jump = FindAction(JumpActionPath);
            _crouch = FindAction(CrouchActionPath);
            _standEyeHeight = _head != null ? _head.localPosition.y : 1.6f;
            _eyeHeight = _standEyeHeight;
            Stamina = _staminaMax;
            enabled = false;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Player] {path} 액션 없음");
            return action;
        }
        #endregion

        #region PlaceAt - 지점 위치·좌우 방향으로 옮기고 위아래 각도·세로 속도·머리 높이 초기화 (CharacterController는 켜진 채 옮기면 무시돼서 잠깐 끔)
        /// <param name="point">옮길 지점</param>
        public void PlaceAt(Transform point)
        {
            if (point == null) throw new System.ArgumentNullException(nameof(point));
            _body.enabled = false;
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(0f, point.eulerAngles.y, 0f));
            _body.enabled = true;
            _pitch = 0f;
            _fallSpeed = 0f;
            _head.localRotation = Quaternion.identity;
            _eyeHeight = _standEyeHeight;
            SetEyeHeight();
        }
        #endregion

        #region OnDisable - 꺼지면 달리는 중 표시를 끔
        void OnDisable() => IsSprinting = false;
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지) 아무것도 안 함. 아니면 시점 → 이동
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            Look();
            Move();
        }
        #endregion

        #region Look - 시점이 잠기지 않았으면 마우스 이동량으로 좌우는 몸, 위아래는 머리
        void Look()
        {
            if (_look == null || LookLocked) return;
            // 이번 프레임 회전량 (도)
            Vector2 delta = _look.ReadValue<Vector2>() * _lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -_pitchLimit, _pitchLimit);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        #endregion

        #region Move - 잠기지 않았으면 몸 기준 앞·옆으로 걷기·앉아 걷기·달리기(뒤로는 안 됨, 스태미나), 땅이면 눌러 붙이고 점프, 아니면 중력. 머리 높이를 앉음에 맞춤
        void Move()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            // 이동 입력 (x 옆, y 앞. 대각선도 1을 안 넘게. 잠기면 0)
            Vector2 input = MoveLocked || _move == null ? Vector2.zero : Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            IsCrouching = !MoveLocked && _crouch != null && _crouch.IsPressed();
            // 움직이는 중인지
            bool moving = input.sqrMagnitude >= MovingInput * MovingInput;
            // 달리려는지 (뒤로 가는 입력·앉음이면 아님)
            bool wantSprint = !MoveLocked && _sprint != null && _sprint.IsPressed() && moving && input.y > BackwardInput && !IsCrouching;
            IsSprinting = wantSprint && !_exhausted && Stamina > 0f;
            UpdateStamina(dt);
            // 지금 속도
            float speed = IsSprinting ? _sprintSpeed : _walkSpeed * (IsCrouching ? _crouchSpeedScale : 1f);
            // 몸 기준 평면 이동 방향
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            // 땅에 있는지
            bool grounded = _body.isGrounded;
            if (grounded && _fallSpeed < 0f) _fallSpeed = GroundedFallSpeed;
            if (grounded && !MoveLocked && !IsCrouching && _jump != null && _jump.WasPressedThisFrame()) _fallSpeed = _jumpSpeed;
            else if (!grounded) _fallSpeed += _gravity * dt;
            _body.Move((direction * speed + Vector3.up * _fallSpeed) * dt);
            UpdateEye(dt);
        }
        #endregion

        #region UpdateStamina - 달리면 줄고 다 쓰면 지침. 안 달리면 잠깐 뒤 회복, 회복 비율을 넘으면 지침이 풀림
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateStamina(float dt)
        {
            if (IsSprinting)
            {
                Stamina = Mathf.Max(0f, Stamina - _sprintCost * dt);
                _regenWait = _regenDelay;
                if (Stamina <= 0f) _exhausted = true;
                return;
            }
            if (_regenWait > 0f)
            {
                _regenWait -= dt;
                return;
            }
            Stamina = Mathf.Min(_staminaMax, Stamina + _staminaRegen * dt);
            if (_exhausted && Stamina >= _staminaMax * _recoverRatio) _exhausted = false;
        }
        #endregion

        #region UpdateEye - 머리 높이를 앉음·섬 쪽으로 옮김
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateEye(float dt)
        {
            _eyeHeight = Mathf.MoveTowards(_eyeHeight, IsCrouching ? _crouchEyeHeight : _standEyeHeight, _eyeMoveSpeed * dt);
            SetEyeHeight();
        }
        #endregion

        #region SetEyeHeight - 머리 로컬 높이를 지금 머리 높이로
        void SetEyeHeight()
        {
            if (_head == null) return;
            // 머리 로컬 위치
            Vector3 position = _head.localPosition;
            position.y = _eyeHeight;
            _head.localPosition = position;
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- 기존 그대로 `HubState`·`FieldState`가 `enabled`·`PlaceAt`. 새로 `FieldCastController`가 `MoveLocked`·`LookLocked`, `StaminaUIBinder`가 `Stamina`·`StaminaMax`를 읽음

---

### `Assets/Scripts/UI/Controllers/SpellDrawController.cs`

**변경 내용:** 가상 펜 입력(`PenDown`·`PenMove`·`PenUp`)을 열고 포인터 처리가 그걸 거침, 가운데 안내 글 끄기 칸

클래스 요약 끝에 한 줄 더함:

```csharp
    /// <summary>주문 그리기 칸: 좌클릭으로 시전 상태, 버튼을 떼도 이어 그리기, 마지막 입력 뒤 대기 시간이 지나거나 Space(Player/Cast)면 판정, 우클릭은 취소. 그린 획을 칸 좌표로 모으고, 판정은 받은 함수가 하고 결과 글은 여기서 보임. 대기 시간은 게임 시간 (일시정지 중엔 멈춤). 포인터 대신 PenDown·PenMove·PenUp으로 칸 좌표를 직접 넣을 수 있음 (숲: 커서를 잠근 채 마우스 이동량으로 그림)</summary>
```

`_waitSeconds` 아래:

```csharp
        /// <summary>마지막 입력 뒤 판정까지 시간 (초, 게임 시간. 임시 3초)</summary>
        [SerializeField, Min(0.5f)] float _waitSeconds = 3f;

        /// <summary>그리기 전 가운데 안내 글을 보일지 (책상 연습은 켬, 숲은 끔)</summary>
        [SerializeField] bool _showHint = true;
```

`ShowGuide`:

```diff
             if (shape == null) _view.Guide.Clear();
             else _view.Guide.SetStrokes(shape.PartStrokes.SelectMany(part => part), true);
-            if (!_casting) _view.Hint.gameObject.SetActive(shape == null);
+            if (!_casting) _view.Hint.gameObject.SetActive(_showHint && shape == null);
```

`OnPointerDown`·`OnDrag`·`OnPointerUp`:

```diff
         public void OnPointerDown(PointerEventData eventData)
         {
             if (!IsActive) return;
             if (eventData.button == PointerEventData.InputButton.Right)
             {
                 CancelCast();
                 return;
             }
             if (eventData.button != PointerEventData.InputButton.Left) return;
-            if (!_casting) StartCast();
-            EndStroke();
-            _stroke = new List<Vector2> { ToLocal(eventData) };
-            _idle = 0f;
-            RefreshInk();
+            PenDown(ToLocal(eventData));
         }
```

```diff
         public void OnDrag(PointerEventData eventData)
         {
-            if (_stroke == null || eventData.button != PointerEventData.InputButton.Left) return;
-            // 칸 좌표 점
-            Vector2 point = ToLocal(eventData);
-            if ((point - _stroke[_stroke.Count - 1]).sqrMagnitude < MinPointGap * MinPointGap) return;
-            _stroke.Add(point);
-            _idle = 0f;
-            RefreshInk();
+            if (eventData.button != PointerEventData.InputButton.Left) return;
+            PenMove(ToLocal(eventData));
         }
```

```diff
         public void OnPointerUp(PointerEventData eventData)
         {
             if (eventData.button != PointerEventData.InputButton.Left) return;
-            EndStroke();
+            PenUp();
         }
```

`OnPointerUp` region 아래에 새 메서드 3개:

```csharp
        #region PenDown - 펜을 누름: 그리기를 받는 중이면 (시전 상태가 아니면 시작하고) 그 점에서 새 획 시작
        /// <param name="point">칸 좌표 점 (가운데 기준)</param>
        public void PenDown(Vector2 point)
        {
            if (!IsActive) return;
            if (!_casting) StartCast();
            EndStroke();
            _stroke = new List<Vector2> { point };
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region PenMove - 그리는 중이면 점을 더함 (앞 점과 너무 가까우면 뺌)
        /// <param name="point">칸 좌표 점</param>
        public void PenMove(Vector2 point)
        {
            if (_stroke == null) return;
            if ((point - _stroke[_stroke.Count - 1]).sqrMagnitude < MinPointGap * MinPointGap) return;
            _stroke.Add(point);
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region PenUp - 펜을 뗌: 그리던 획 끝 (그 뒤 대기 시간이 흐름)
        public void PenUp() => EndStroke();
        #endregion
```

`ResetDrawing`:

```diff
             _view.Result.gameObject.SetActive(false);
             _view.Detail.gameObject.SetActive(false);
-            _view.Hint.gameObject.SetActive(true);
+            _view.Hint.gameObject.SetActive(_showHint);
```

**호출/구독 관계:**
- 책상 연습(`CodexController`)은 그대로 포인터 이벤트. 숲은 `FieldCastController`가 `PenDown`·`PenMove`·`PenUp`·`CancelCast`를 부름 (숲 그리기 칸 Image는 Raycast Target 끔)

---

### `Assets/Scripts/Managers/TutorialManager.cs`

**변경 내용:** 전투 중 숨김(`SetSuppressed`), 게임 코드가 배움을 알리면 떠 있는 안내도 끝냄

`_gap` 필드 아래:

```csharp
        /// <summary>다음 안내까지 남은 쉬는 시간 (초)</summary>
        float _gap;

        /// <summary>전투 상황이라 12시 안내를 숨기는지 (다루던 안내는 그대로 두고 화면에서만 숨김)</summary>
        bool _suppressed;
```

`MarkLearned` 교체 + 새 메서드:

```csharp
        #region MarkLearned - 배움으로 기록 (상호작용·지도·현장 시전처럼 게임 코드가 처음 쓴 걸 알림). 새로 배웠으면 파일 저장. 지금 다루는 안내면 끝냄
        /// <param name="id">튜토리얼 id</param>
        public void MarkLearned(string id)
        {
            SaveLearned(id);
            if (_current != null && string.Equals(_current.Id, id, System.StringComparison.Ordinal)) Complete();
        }
        #endregion

        #region SaveLearned - 이 PC 배운 기록에 넣고 새로 배웠으면 파일 저장
        /// <param name="id">튜토리얼 id</param>
        void SaveLearned(string id)
        {
            // 기기 설정 담당 (배운 기록이 여기 있음)
            var settings = SettingsManager.Instance;
            if (settings != null && settings.Device.AddLearnedTutorial(id)) settings.Save();
        }
        #endregion

        #region SetSuppressed - 전투 상황이면 12시 안내를 숨기고, 끝나면 떠 있던 안내를 다시 보임 (숲)
        /// <param name="suppressed">숨길지</param>
        public void SetSuppressed(bool suppressed)
        {
            if (suppressed == _suppressed) return;
            _suppressed = suppressed;
            if (_prompt != null && _current != null && _shown) _prompt.Root.SetActive(!suppressed);
        }
        #endregion
```

`Clear`:

```diff
             _shown = false;
             _gap = 0f;
+            _suppressed = false;
             if (_prompt != null) _prompt.Root.SetActive(false);
```

`Show`:

```diff
             _prompt.Text.StringReference.SetReference(_current.Prompt.TableReference, _current.Prompt.TableEntryReference);
-            _prompt.Root.SetActive(true);
+            _prompt.Root.SetActive(!_suppressed);
```

`Complete`:

```diff
         void Complete()
         {
-            MarkLearned(_current.Id);
+            SaveLearned(_current.Id);
             _current = null;
```

**호출/구독 관계:**
- `FieldState.OnCombatChanged`가 `SetSuppressed`, `Exit`가 `SetSuppressed(false)`·`Clear`. `MarkLearned`는 기존(상호작용·그리기) + `FieldState`(지도)·`FieldCastController`(현장 시전)

---

### `Assets/Scripts/Managers/UIManager.cs`

**변경 내용:** 메뉴 창 지도 탭 지도 연결(`SetMapSource`·`ClearMapSource`), 게시판에 '끝냄'

`_questBoard` 필드 아래:

```csharp
        /// <summary>임무 게시판 창 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] QuestBoardController _questBoard;

        /// <summary>메뉴 창 지도 탭 지도 (Boot 씬 UICanvas/GameMenu/…/MapPage/Map. 출처는 상태가 SetMapSource로 넘김)</summary>
        [SerializeField] MapUIBinder _menuMap;
```

`InitWindows`의 `_questBoard` 검사 아래:

```csharp
            if (_menuMap == null) Debug.LogError("[UI] 메뉴 지도 MapUIBinder 연결 안 됨");
```

`ShowQuestBoard` 교체:

```csharp
        #region ShowQuestBoard - 게시판 창을 띄우고 게임 멈춤 (의뢰·할 일은 상태가 넘김)
        /// <param name="quest">보일 의뢰</param>
        /// <param name="taken">이미 받았는지</param>
        /// <param name="completed">이미 끝냈는지</param>
        /// <param name="onTake">받기를 누르면 할 일</param>
        public void ShowQuestBoard(QuestSO quest, bool taken, bool completed, Action onTake)
        {
            if (_questBoard == null)
            {
                Debug.LogError("[UI] QuestBoardController 연결 안 됨");
                return;
            }
            _questBoard.Show(quest, taken, completed, onTake);
            RefreshMenuPause();
        }
        #endregion
```

`HideQuestBoard` 아래 새 메서드:

```csharp
        #region SetMapSource - 메뉴 창 지도 탭에 안개 지도·플레이어·목표·입구를 넘김 (숲이 부름)
        /// <param name="fog">안개 지도</param>
        /// <param name="player">플레이어</param>
        /// <param name="objective">현재 목표 위치 (없으면 null을 돌려줌)</param>
        /// <param name="exit">숲 입구 위치</param>
        public void SetMapSource(FogMapService fog, Transform player, Func<Vector3?> objective, Vector3 exit)
        {
            if (_menuMap != null) _menuMap.Bind(fog, player, objective, exit);
        }
        #endregion

        #region ClearMapSource - 메뉴 창 지도 탭을 자리 글로 (숲을 나갈 때)
        public void ClearMapSource()
        {
            if (_menuMap != null) _menuMap.Unbind();
        }
        #endregion
```

---

### `Assets/Scripts/UI/Controllers/QuestBoardController.cs`

**변경 내용:** 끝낸 의뢰는 버튼을 못 누르고 '끝냄'

```diff
         /// <summary>받은 뒤 버튼 글자 키</summary>
         const string TakenKey = "hub.board.taken";
+
+        /// <summary>끝낸 의뢰 버튼 글자 키</summary>
+        const string DoneKey = "hub.board.done";
```

```diff
-        #region Show - 의뢰 이름·설명·기간을 넣고 켬. 이미 받았으면 버튼을 '받음'으로
+        #region Show - 의뢰 이름·설명·기간을 넣고 켬. 이미 받았으면 버튼을 '받음', 끝냈으면 '끝냄'으로
         /// <param name="quest">보일 의뢰</param>
         /// <param name="taken">이미 받았는지</param>
+        /// <param name="completed">이미 끝냈는지</param>
         /// <param name="onTake">받기를 누르면 할 일</param>
-        public void Show(QuestSO quest, bool taken, Action onTake)
+        public void Show(QuestSO quest, bool taken, bool completed, Action onTake)
```

```diff
             _view.QuestDays.RefreshString();
-            SetTaken(taken);
+            SetTaken(taken, completed);
         }
```

```diff
             _onTake?.Invoke();
-            SetTaken(true);
+            SetTaken(true, false);
         }
```

```diff
-        #region SetTaken - 받았으면 버튼을 못 누르게 하고 글자를 '받음'으로
+        #region SetTaken - 받았거나 끝냈으면 버튼을 못 누르게 하고 글자를 '끝냄'·'받음'으로
         /// <param name="taken">받았는지</param>
-        void SetTaken(bool taken)
+        /// <param name="completed">끝냈는지</param>
+        void SetTaken(bool taken, bool completed)
         {
-            _view.TakeButton.interactable = !taken;
-            _view.TakeLabel.StringReference.SetReference(Table, taken ? TakenKey : TakeKey);
+            _view.TakeButton.interactable = !taken && !completed;
+            _view.TakeLabel.StringReference.SetReference(Table, completed ? DoneKey : taken ? TakenKey : TakeKey);
         }
```

---

### `Assets/Scripts/Managers/GameManager.cs`

**변경 내용:** 게임 속도(죽은 뒤 도망 시뮬), 숲 씬에서 바로 Play하면 디버그 판에 원정 채움. 맨 위 `using System.Collections.Generic;` 추가

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
```

`_menuPaused` 필드 아래:

```csharp
        /// <summary>게임 속도 (로딩창·일시정지가 아닐 때 Time.timeScale. 기본 1, 숲 죽음 뒤 도망 시뮬만 올림)</summary>
        float _gameSpeed = 1f;
```

`Start`:

```diff
             Run = start is TitleState or CharacterCreationState ? new RunEntry() : RunEntry.CreateDebug(DataManager.Instance.Runes, DataManager.Instance.Spells);
+            if (start is FieldState) FillDebugExpedition(Run);
             ChangeState(start);
```

`CreateStartState` region 아래 새 메서드:

```csharp
        #region FillDebugExpedition - 숲에서 바로 Play할 때 디버그 판에 원정을 채움: 첫 후보 추첨에서 용병 2명(나머지는 후보), 첫 의뢰, 방, 출발함 (숲 소탕·죽은 뒤 여관 부활 확인용)
        /// <param name="run">디버그 판 데이터</param>
        static void FillDebugExpedition(RunEntry run)
        {
            // 첫 후보 추첨
            List<MercenaryEntry> drawn = RecruitPool.Draw(DataManager.Instance.Jobs);
            foreach (MercenaryEntry entry in drawn)
            {
                if (run.Party.Count < HubState.RequiredMercenaries) run.Party.Add(entry);
                else run.RecruitCandidates.Add(entry);
            }
            run.CandidatesDrawn = true;
            // 의뢰 목록
            IReadOnlyList<QuestSO> quests = DataManager.Instance.Quests;
            run.Quest = quests.Count > 0 ? quests[0] : null;
            run.HasRoom = true;
            run.HasDeparted = true;
        }
        #endregion
```

`ApplyTimeScale` 교체 + 새 메서드:

```csharp
        #region SetGameSpeed - 게임 속도를 바꿈 (0 이상. 로딩창·일시정지가 아니면 바로 적용)
        /// <param name="speed">게임 속도 (1 = 보통)</param>
        public void SetGameSpeed(float speed)
        {
            _gameSpeed = Mathf.Max(0f, speed);
            ApplyTimeScale();
        }
        #endregion

        #region ApplyTimeScale - 멈출 이유(로딩창·일시정지 메뉴)가 하나라도 있으면 게임 시간 0, 없으면 게임 속도
        void ApplyTimeScale() => Time.timeScale = _covered || _menuPaused ? 0f : _gameSpeed;
        #endregion
```

---

### `Assets/Scripts/UI/Views/HubView.cs`

**변경 내용:** 여관 방 침대 부활 자리, 숲 목표, 스태미나 막대. `_deskSeat` 필드 아래, `DeskSeat` 프로퍼티 아래에 넣음

```csharp
        /// <summary>숲에서 죽어 깨어나는 곳 (여관 방 침대 옆, 방 문을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _bedPoint;

        /// <summary>의뢰를 받고 한 번 나간 뒤 목표 (숲 외곽의 고블린 소탕. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _forestObjective;

        /// <summary>스태미나 막대 (Canvas/Hud/Stamina. 인스펙터에서만 연결)</summary>
        [SerializeField] StaminaUIBinder _stamina;
```

```csharp
        /// <summary>숲에서 죽어 깨어나는 곳</summary>
        public Transform BedPoint => _bedPoint;

        /// <summary>의뢰를 받고 한 번 나간 뒤 목표</summary>
        public ObjectiveSO ForestObjective => _forestObjective;

        /// <summary>스태미나 막대</summary>
        public StaminaUIBinder Stamina => _stamina;
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 죽어서 오면 여관 방 침대(방이 없으면 정문)에서 시작, 숲에서 오면 소탕을 끝냈을 때만 의뢰를 끝냄, 숲 목표, 게시판 '끝냄', 스태미나 막대

`GuardWelcomeKey` 아래 키 3개:

```csharp
        /// <summary>경비병: 숲에서 돌아왔을 때 자막 키</summary>
        const string GuardWelcomeKey = "hub.guard.welcome";

        /// <summary>숲에서 죽어 여관 방에서 깨어났을 때 토스트 키</summary>
        const string RespawnInnKey = "hub.respawn.inn";

        /// <summary>숲에서 죽어 정문에서 깨어났을 때 토스트 키 (방을 안 잡았을 때)</summary>
        const string RespawnGateKey = "hub.respawn.gate";

        /// <summary>의뢰를 끝냈을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestDoneKey = "hub.quest_done";
```

`_returning` 필드 아래:

```csharp
        /// <summary>이번 진입이 숲에서 죽어 부활한 것인지 (여관 방 침대·정문에서 시작. OnSceneReady에서 정함)</summary>
        bool _respawning;
```

`OnSceneReady`:

```diff
-        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 숲에서 돌아왔으면 정문 밖, 아니면 시작 위치. 파티 수·금화 표시, 안내 글 숨김, 코덱스 표지(이름·마법진)
+        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 숲에서 죽었으면 여관 방 침대(방이 없으면 정문 안쪽), 숲에서 돌아왔으면 정문 밖, 아니면 시작 위치. 파티 수·금화 표시, 안내 글 숨김, 코덱스 표지(이름·마법진), 스태미나 막대
         protected override void OnSceneReady()
         {
             _view = FindView<HubView>();
             if (_view == null) return;
             _arriving = !Game.Run.HasArrived;
-            _returning = !_arriving && Game.Run.ReturningFromField;
-            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _returning ? _view.ReturnPoint : _view.Spawn);
+            _respawning = !_arriving && Game.Run.RespawningFromDeath;
+            _returning = !_arriving && !_respawning && Game.Run.ReturningFromField;
+            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _respawning ? RespawnPoint() : _returning ? _view.ReturnPoint : _view.Spawn);
             _view.Player.gameObject.SetActive(!_arriving);
             _view.Hud.SetActive(!_arriving);
             _view.InteractPrompt.gameObject.SetActive(false);
             RefreshParty();
             RefreshMoney();
             _view.Codex.ShowCover(Game.Run.PlayerName, DataManager.Instance.Runes);
+            _view.Stamina.Bind(_view.Player);
         }
```

`Reveal`:

```diff
-        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작. 숲에서 돌아왔으면 의뢰를 비우고 경비병 자막 → 끝나면 목표 알림
+        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작. 숲에서 죽어 왔으면 소탕을 끝냈을 때 의뢰를 끝내고 깨어남 토스트. 숲에서 돌아왔으면 소탕을 끝냈을 때 의뢰를 끝내고(토스트) 경비병 자막 → 끝나면 목표 알림
         public override void Reveal()
         {
             if (_view == null) return;
             Cursor.lockState = CursorLockMode.Locked;
             if (_arriving)
             {
                 _view.Opening.Finished += OnOpeningFinished;
                 _view.Opening.Play();
                 return;
             }
             StartControl();
+            if (_respawning)
+            {
+                Game.Run.RespawningFromDeath = false;
+                Game.Run.ReturningFromField = false;
+                FinishQuestIfCleared(false);
+                UpdateObjective(false);
+                UIManager.Instance.ShowToast(Table, Game.Run.HasRoom ? RespawnInnKey : RespawnGateKey);
+                return;
+            }
             if (!_returning)
             {
                 UpdateObjective(false);
                 return;
             }
-            // 숲에서 돌아옴: 받은 의뢰는 다녀온 걸로 비움 (정산은 나중)
+            // 숲에서 돌아옴: 소탕을 끝냈으면 의뢰를 끝냄 (정산은 나중. 못 끝냈으면 의뢰는 남음)
             Game.Run.ReturningFromField = false;
-            Game.Run.Quest = null;
+            FinishQuestIfCleared(true);
             UpdateObjective(false);
             UIManager.Instance.ShowSubtitle(new LocalizedString(Table, GuardKey), new LocalizedString(Table, GuardWelcomeKey), NotifyObjective);
         }
```

`Reveal` region 아래 새 메서드 2개:

```csharp
        #region RespawnPoint - 숲에서 죽어 깨어나는 곳: 방을 잡았으면 여관 방 침대 옆, 아니면 정문 안쪽
        Transform RespawnPoint() => Game.Run.HasRoom ? _view.BedPoint : _view.Spawn;
        #endregion

        #region FinishQuestIfCleared - 받은 의뢰의 소탕을 끝냈으면 끝낸 의뢰로 남기고 비움 (notify면 토스트)
        /// <param name="notify">"의뢰를 끝냈어" 토스트를 띄울지</param>
        void FinishQuestIfCleared(bool notify)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Quest == null || !run.QuestCleared) return;
            // 끝낸 의뢰
            QuestSO quest = run.Quest;
            run.CompletedQuestIds.Add(quest.Id);
            run.Quest = null;
            run.QuestCleared = false;
            if (notify) UIManager.Instance.ShowToast(Table, QuestDoneKey, quest.Title.GetLocalizedString());
        }
        #endregion
```

`OpenBoard`:

```diff
             // 붙어 있는 의뢰
             QuestSO quest = quests[0];
-            UIManager.Instance.ShowQuestBoard(quest, Game.Run.Quest == quest, () => TakeQuest(quest));
+            UIManager.Instance.ShowQuestBoard(quest, Game.Run.Quest == quest, Game.Run.CompletedQuestIds.Contains(quest.Id), () => TakeQuest(quest));
```

`TakeQuest`:

```diff
         void TakeQuest(QuestSO quest)
         {
             Game.Run.Quest = quest;
+            Game.Run.QuestCleared = false;
             if (!UpdateObjective(true)) UIManager.Instance.ShowToast(Table, QuestTakenKey, quest.Title.GetLocalizedString());
         }
```

`NextObjective`:

```diff
-        #region NextObjective - 판 데이터로 지금 목표를 정함: 방 → 길드(파티·의뢰) → 정문 → (첫 출발 뒤) 없음
+        #region NextObjective - 판 데이터로 지금 목표를 정함: 방 → 길드(파티·의뢰) → 정문 → (의뢰를 받고 나간 뒤) 숲 소탕 → (끝낸 뒤) 없음
         ObjectiveSO NextObjective()
         {
             // 이번 판 데이터
             RunEntry run = Game.Run;
             if (!run.HasRoom) return _view.FirstObjective;
-            if (run.Party.Count < RequiredMercenaries || run.Quest == null) return _view.GuildObjective;
+            if (run.Quest != null && run.HasDeparted) return _view.ForestObjective;
+            if (run.Party.Count < RequiredMercenaries || run.Quest == null) return run.CompletedQuestIds.Count > 0 ? null : _view.GuildObjective;
             return run.HasDeparted ? null : _view.DepartObjective;
         }
```

---

### `Assets/Scripts/UI/Views/FieldDebugView.cs` (지움)

`.cs`·`.meta` 지움 (숲 디버그 글자는 `FieldView`·HUD로 바뀜)

---

## 입력 변경

### `Assets/InputSystem_Actions.inputactions`

1. `Player/Crouch` 키보드 바인딩 (id `36e52cba-0905-478e-a818-f4bfcb9f3b9a`) `<Keyboard>/c` → `<Keyboard>/leftCtrl`
2. `Player/Attack`의 `<Keyboard>/enter` 바인딩 (id `b3c1c7f0-bd20-4ee7-a0f1-899b24bca6d7`) 지움 (Enter로 시전 상태가 되지 않게)
3. `Player`에 새 액션 `CancelCast`: Button, id `5d1c3a27-8e4f-4b6a-9f02-7c4e1d8b3a65`, expectedControlType `Button`. 바인딩 `<Mouse>/rightButton`, groups `;Keyboard&Mouse`, id `9b7e2f40-3c1d-4a8e-b5f6-1d2c3e4f5a6b`

`Player/Jump`(Space)·`Player/Cast`(Space)는 그대로 (시전 상태로 나눔)

---

## 에셋 변경

### `Assets/Art/Placeholder/Materials/` (새로, 재질 6개)

| 파일 | 셰이더 | 색 | 그 밖 |
|---|---|---|---|
| `Enemy.mat` | URP Lit | 흰색 (코드가 `_BaseColor`로 몬스터 색) | Smoothness 0.2 |
| `Spell.mat` | URP Unlit | 흰색 (코드가 불·물 색) | |
| `Burst.mat` | URP Unlit | (1, 1, 1, 0.45) | Surface Transparent |
| `FlameWall.mat` | URP Unlit | (1, 0.45, 0.1, 0.8) | Surface Transparent |
| `Gold.mat` | URP Lit | (1, 0.8, 0.2) | Metallic 0.6 |
| `Portal.mat` | URP Unlit | (0.45, 0.25, 0.8) | |

### `Assets/Data/Monsters/` (새로, MonsterSO 6개. v1 `MON` 값, 임시)

| 파일 | `_id` | `_name` (Monster 테이블) | 체력 | 속도 | 피해 | 간격 | 거리 | 알아챔 | 마법사 노림 | 크기 | 색 (RGB 0~1) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `Goblin.asset` | goblin | `monster.goblin.name` | 35 | 5.2 | 7 | 1.2 | 1.4 | 18 | 0.15 | 0.82 | (0.33, 0.5, 0.24) |
| `Wolf.asset` | wolf | `monster.wolf.name` | 40 | 7.6 | 6 | 1 | 1.5 | 20 | 0.35 | 0.62 | (0.44, 0.46, 0.49) |
| `Boar.asset` | boar | `monster.boar.name` | 85 | 6.4 | 12 | 1.5 | 1.6 | 14 | 0 | 0.78 | (0.37, 0.29, 0.22) |
| `Spider.asset` | spider | `monster.spider.name` | 55 | 6.8 | 8 | 1 | 1.6 | 18 | 0 | 0.7 | (0.2, 0.18, 0.23) |
| `Bear.asset` | bear | `monster.bear.name` | 190 | 5.4 | 20 | 1.7 | 1.9 | 16 | 0 | 1.25 | (0.27, 0.2, 0.16) |
| `Mutant.asset` | mutant | `monster.mutant.name` | 100 | 7 | 11 | 1.2 | 1.8 | 24 | 0 | 0.95 | (0.37, 0.27, 0.63) |

### `Assets/Data/Spells/` (현장 값, 프로토타입 `SPELL_FX`·공격 6 기준, 임시)

| 파일 | `_fieldEffect` | `_manaCost` | `_power` | `_area` | `_duration` | `_cooldown` |
|---|---|---|---|---|---|---|
| `FlameShot.asset` | Bolt | 6 | 15 | 0 | 0 | 0 |
| `Fireball.asset` | Ball | 14 | 22 | 2.6 | 0 | 0 |
| `WaterShot.asset` | Water | 8 | 9 | 4 | 0 | 0 |
| `FlameFan.asset` | Fan | 12 | 9 | 15 | 0 | 0 |
| `FlameWall.asset` | Wall | 18 | 14 | 6 | 4 | 0 |
| `Shield.asset` | Shield | 30 | 0.4 | 0 | 4 | 9 |
| 두루마리 3개 | None | 0 | 0 | 0 | 0 | 0 |

### `Assets/Data/Runes/` (혼자 던지기, 임시)

| 파일 | `_soloEffect` | `_soloManaCost` | `_soloPower` | `_soloArea` |
|---|---|---|---|---|
| `Fire.asset` | Bolt | 3 | 9 | 0 |
| `Water.asset` | Water | 4 | 5 | 2 |
| 나머지 5개 (응축·직선·차단·확산·벽) | None | 0 | 0 | 0 |

### 그 밖

- `Assets/Data/Jobs/Ranger.asset`: `_ranged` = 켬
- `Assets/Data/Quests/Forest01.asset`: `_target` = `Goblin.asset`
- `Assets/Data/Tutorials/` (새로, TutorialSO):

| 파일 | `_id` | 키 (Tutorial 테이블) | `_action` | `_walkSecondsBeforeShow` |
|---|---|---|---|---|
| `FieldCast.asset` | field_cast | `tutorial.field_cast.prompt`·`.title`·`.body` | FieldCast | 0 |
| `Map.asset` | map | `tutorial.map.prompt`·`.title`·`.body` | Map | 4 |

- `Assets/Data/Objectives/` (새로, ObjectiveSO): `ForestClear.asset` (`forest_clear`, `objective.forest_clear.title`·`.desc`), `ForestReturn.asset` (`forest_return`, `objective.forest_return.title`·`.desc`)

---

## Prefab 변경

공통: 미리보기 씬에서 만들어 `PrefabUtility.SaveAsPrefabAsset`. 도형 자식은 콜라이더를 지움

### `Assets/Prefabs/World/Enemy.prefab` (새로)

1. 루트 `Enemy`: NavMeshAgent (Agent Type Humanoid, Radius 0.5, Height 1.6, Speed 5, Angular Speed 720, Acceleration 30, Stopping Distance 0), CapsuleCollider (반지름 0.45, 높이 1.6, 중심 (0, 0.8, 0)), `FieldUnitController` (Enemy, 반지름 0.5, 겨눌 높이 0.9), `EnemyController`
2. `Visual` (원점) 아래 `Body` Capsule (위치 (0, 0.7, 0), 스케일 (0.8, 0.7, 0.8)), `Head` Sphere (위치 (0, 1.45, 0), 스케일 0.45). 둘 다 `Enemy.mat`
3. EnemyController 칸: `_agent`, `_unit` → 루트, `_visual` → Visual, `_renderers` = Body·Head, `_collider` → 루트 CapsuleCollider

### `Assets/Prefabs/World/Mercenary.prefab` (새로)

1. 루트 `Mercenary`: NavMeshAgent (Humanoid, Radius 0.35, Height 1.8, Speed 5.6, Angular Speed 720, Acceleration 30), CapsuleCollider (0.35, 1.8, 중심 (0, 0.9, 0)), `FieldUnitController` (Party, 반지름 0.35, 겨눌 높이 1.2), `MercenaryController`
2. `Visual` 아래 `Body` Capsule ((0, 0.9, 0), (0.7, 0.9, 0.7)), `HeadBall` Sphere ((0, 1.6, 0), 0.3). 둘 다 `Npc.mat`. `Muzzle` 빈 오브젝트 (0, 1.4, 0.4)
3. MercenaryController 칸: `_agent`, `_unit`, `_visual`, `_collider`, `_muzzle` → Muzzle, `_arrowPrefab` = `Arrow.prefab`

### `Assets/Prefabs/World/SpellProjectile.prefab` (새로)

1. 루트 `SpellProjectile` + `SpellProjectileController` (`_radius` 0.25)
2. `Visual` Sphere 스케일 0.35 `Spell.mat`, `Burst` Sphere 스케일 1 `Burst.mat` (꺼 둠)
3. 칸: `_visual` → Visual MeshRenderer, `_burst` → Burst MeshRenderer

### `Assets/Prefabs/World/Arrow.prefab` (새로, SpellProjectile 변형)

`_radius` 0.12, `Visual` 스케일 (0.08, 0.08, 0.6), `Burst` 스케일 0.5

### `Assets/Prefabs/World/FlameWall.prefab` (새로)

루트 `FlameWall` + `FlameWallController` (`_visual` → Visual), `Visual` Cube `FlameWall.mat`

### `Assets/Prefabs/World/GoldPile.prefab` (새로)

1. 루트 `GoldPile`: BoxCollider (0.8, 0.5, 0.8, 중심 (0, 0.25, 0)), `InteractPointView` (`gold`, Field/`field.gold_pile`)
2. `Visual` Cylinder ((0, 0.1, 0), (0.6, 0.1, 0.6)) `Gold.mat`

### `Assets/Prefabs/World/Portal.prefab` (새로)

1. 루트 `Portal`: BoxCollider (6, 8, 1, 중심 (0, 4, 0)), NavMeshObstacle (Box, 크기 (6, 8, 1), 중심 (0, 4, 0), Carve 켬)
2. `Visual` 아래 `Ring` Cylinder (위치 (0, 4, 0), 회전 (90, 0, 0), 스케일 (6, 0.3, 6)) `Portal.mat`, `PillarLeft`·`PillarRight` Cube ((±3.2, 4, 0), (0.6, 8, 0.6)) `Road.mat`

---

## 씬 변경

### `Assets/Scenes/Forest.unity` (다시 구성)

지움: `Main Camera`(Player 머리 카메라를 씀), `Ground`, `Canvas`(InfoPanel). `Directional Light`는 둠

1. `Forest` (원점) + NavMeshSurface (Agent Type Humanoid, Collect Objects: Current Object Hierarchy, Include Layers: Everything, Use Geometry: Physics Colliders). 자식:
   - `Ground`: Cube (0, -0.5, 360), 스케일 (180, 1, 720), `Ground.mat`
   - `Bounds`: Cube 4개 (MeshRenderer 끔): North (0, 3, 721) (182, 6, 2), South (0, 3, -1) (182, 6, 2), East (91, 3, 360) (2, 6, 722), West (-91, 3, 360) (2, 6, 722)
   - `Trees`: `Tree.prefab` 인스턴스. 빌더가 시드 4201로: 1구역(z 0~240) 230그루, 2구역(240~480) 270그루, 3구역(480~720) 310그루, 크기 0.8~1.3배·방향 무작위. 안 놓는 곳: 입구 길(|x| < 8, z < 40), 적 자리 8m 안, 가장자리 2m 안, 다른 나무 3m 안
   - `Rocks`: Cube 30개 (구역마다 10개, 스케일 x·z 2~5·y 1.5~3, `Road.mat`), 안 놓는 곳은 나무와 같음
2. `Entrance` (원점):
   - `Signpost`: `Signpost.prefab` (0, 0, 4), yaw 180. Add Component → `InteractPointView` (`forest_exit`, Field/`field.exit`)
   - `EntryPoint` (0, 0, 10), yaw 0 (숲을 봄) / `ExitPoint` (0, 0, 6)
3. `Portal`: `Portal.prefab` 인스턴스 (원점. 판 데이터 자리로 옮겨짐)
4. `Spawns` (원점) 아래 SpawnPointView 20개 (흩음 5, 이 순서 = 무리 번호):

| id | 위치 | 몬스터 | 수 |
|---|---|---|---|
| goblin_a | (-45, 0, 95) | Goblin | 3 |
| goblin_b | (40, 0, 150) | Goblin | 3 |
| goblin_c | (-10, 0, 205) | Goblin | 3 |
| wolf_a | (55, 0, 60) | Wolf | 3 |
| wolf_b | (-60, 0, 175) | Wolf | 2 |
| boar_a | (-25, 0, 45) | Boar | 1 |
| boar_b | (65, 0, 215) | Boar | 1 |
| spider_a | (-50, 0, 280) | Spider | 3 |
| spider_b | (30, 0, 330) | Spider | 2 |
| spider_c | (-20, 0, 420) | Spider | 3 |
| spider_d | (55, 0, 445) | Spider | 2 |
| bear_a | (60, 0, 300) | Bear | 1 |
| bear_b | (-60, 0, 380) | Bear | 1 |
| mutant_a | (-40, 0, 520) | Mutant | 2 |
| mutant_b | (45, 0, 575) | Mutant | 1 |
| mutant_c | (-15, 0, 650) | Mutant | 2 |
| mutant_d | (50, 0, 700) | Mutant | 1 |
| wolf_c | (0, 0, 560) | Wolf | 4 |
| wolf_d | (-60, 0, 690) | Wolf | 3 |

5. `Player`: `Player.prefab` 인스턴스 (0, 0, 10). Add Component → `FieldUnitController` (Party, 반지름 0.35, 겨눌 높이 1.2), `FieldCastController` (`_draw` → Canvas/Hud/Draw, `_hud` → Canvas/Hud, `_eye` → Player/Head, `_projectilePrefab` = SpellProjectile.prefab, `_wallPrefab` = FlameWall.prefab, `_penSpeed` 1), `FogRevealController`
6. `Canvas`: Screen Space Overlay, CanvasScaler (Scale With Screen Size, 1920 × 1080, Match 0.5), GraphicRaycaster. `Hud` (꽉 채움) + `FieldHudView` + `FieldHudUIBinder` (`_view` → 같은 오브젝트). Hud 자식 (뒤가 위):
   - `Hurt`: 꽉 채움, Image (0.75, 0.08, 0.08, 0.55) Raycast Target 끔, CanvasGroup (Alpha 0, Interactable·Blocks Raycasts 끔)
   - `ZoneText`: LocalizedText (Field/`field.zone_line`), 앵커·피벗 (0, 1), 위치 (40, -40), 크기 (800, 50), 글자 34, 왼쪽 정렬
   - `PartyRows`: 앵커·피벗 (0, 1), 위치 (40, -110), 크기 (420, 140), VerticalLayoutGroup (Spacing 8, Control Child Size 켬). 자식 `PartyRow` 2개: 크기 (420, 64), `PartyRowView` + CanvasGroup. 아래 `Name` LocalizedText (글자 26, 높이 34, 키는 코드), `HpBack` Image (0, 0, 0, 0.5) (300, 12) 아래 `HpFill` Image (0.3, 0.75, 0.45) Filled Horizontal 꽉 채움
   - `Minimap`: 앵커·피벗 (1, 1), 위치 (-40, -40), 크기 (240, 240), Image (0, 0, 0, 0.5), `MapView` + `MapUIBinder` (`_windowMeters` 72). 자식 `Map` RawImage 꽉 채움 (Raycast 끔) 아래 `Exit` Image 흰색 12 × 12, `Objective` Image (1, 0.8, 0.2) 14 × 14, `Player` Image (1, 0.85, 0.4) 10 × 16 (위로 긴 막대 = 앞). 셋 다 앵커 가운데
     - MapView 칸: `_image` → Map, `_player`·`_objective`·`_exit`, `_placeholder`·`_progress` 비움
   - `Target`: 앵커·피벗 (0.5, 1), 위치 (0, -150), 크기 (500, 60), 꺼 둠. `Name` LocalizedText (글자 28, 가운데), `Back` Image (0, 0, 0, 0.5) (480, 12) 아래 `Fill` (0.85, 0.2, 0.2) Filled Horizontal
   - `Bars`: 앵커·피벗 (0, 0), 위치 (40, 40), 크기 (420, 160). `HpText` LocalizedText (Field/`field.hud.hp`, 글자 24, 위치 (0, 130)), `HpBack` (400, 18) (0, 100) 아래 `HpFill` (0.85, 0.2, 0.2); `ManaText` (Field/`field.hud.mana`, (0, 70)), `ManaBack` (0, 40) 아래 `ManaFill` (0.25, 0.5, 0.95); `Stamina` (0, 12) 크기 (400, 10): `StaminaBarView` + `StaminaUIBinder` + CanvasGroup (Alpha 0), `Back` (0, 0, 0, 0.5) 아래 `Fill` (0.95, 0.85, 0.3) Filled Horizontal. 채움은 전부 Filled Horizontal Fill Origin Left
   - `Crosshair`: 가운데, Image 흰색 8 × 8
   - `AimRing`: 가운데, Image `Knob` 스프라이트 (1, 0.82, 0.35, 0.35) 40 × 40, 꺼 둠
   - `AimText`: LocalizedText (키는 코드), 가운데 기준 (0, -70), (1100, 44), 글자 28, 색 (1, 0.85, 0.45), 꺼 둠
   - `NoteText`: LocalizedText, (0, -120), (1100, 44), 글자 26, 꺼 둠
   - `InteractPrompt`: LocalizedText (UI/`common.interact_prompt`), (0, -170), (900, 44), 글자 28
   - `Draw`: 가운데, 크기 (972, 972), Image (1, 1, 1, 0) Raycast Target 끔, `SpellDrawView` + `SpellDrawController` (`_view` → 같은 오브젝트, `_waitSeconds` 3, `_showHint` 끔). 자식: `Guide` StrokeDrawer (꽉 채움, 색 알파 0), `Ink` StrokeDrawer (꽉 채움, 색 (1, 0.85, 0.42), 굵기 6), `Pen` Image (1, 0.9, 0.5) 12 × 12 가운데 꺼 둠, `Timer` Image Filled Horizontal (0.95, 0.85, 0.3) 가운데 기준 (0, -260) (300, 8) 꺼 둠, `Result` LocalizedText (0, -200) (1100, 44) 글자 28, `Detail` LocalizedText (0, -240) (1100, 36) 글자 22, `Hint` LocalizedText (Cast/`cast.hint`) 꺼 둠
     - SpellDrawView 칸: `_guide`·`_ink`·`_timer`·`_result`·`_detail`·`_hint`
   - FieldHudView 칸 전부 연결 (`_stamina` → Bars/Stamina의 StaminaUIBinder, `_minimap` → Minimap의 MapUIBinder, `_pen` → Draw/Pen, `_interactPrompt` → InteractPrompt 등). StaminaBarView 칸 `_fill` → Stamina/Back/Fill, `_group` → Stamina의 CanvasGroup, StaminaUIBinder `_view` → 같은 오브젝트
7. `Field` (원점) + `FieldView`: `_player`·`_detector`·`_playerUnit`·`_cast`·`_fogReveal` → Player, `_hud`·`_hudBinder` → Canvas/Hud, `_entry` → Entrance/EntryPoint, `_exitPoint` → Entrance/ExitPoint, `_portal` → Portal, `_portalArea` 가운데 (0, 0, 660) 크기 (160, 0, 110), `_mapArea` (-90, 0, 180, 720), `_zoneDepth` 240, `_zoneCount` 3, `_spawnPoints` = Spawns 자식 20개 (순서대로), `_enemyPrefab`·`_mercenaryPrefab`·`_goldPilePrefab`, `_clearObjective` = ForestClear, `_returnObjective` = ForestReturn
8. NavMeshSurface Bake → `Assets/Scenes/Forest/NavMesh-Forest.asset`
9. 저장 → 다시 열어 FieldView·FieldHudView·SpellDrawView·MapView 칸, 적 자리 20개 몬스터, NavMesh 데이터 연결 확인

### `Assets/Scenes/Outpost.unity`

1. `Interiors/RoomInterior/BedPoint` 새로: (0.4, 0, 1), yaw 180 (침대 옆, 방 문을 봄)
2. `Canvas/Hud/Stamina` 새로: 앵커·피벗 (0, 0), 위치 (40, 40), 크기 (400, 10), CanvasGroup (Alpha 0), `StaminaBarView` + `StaminaUIBinder`. `Back` Image (0, 0, 0, 0.5) 꽉 채움 아래 `Fill` (0.95, 0.85, 0.3) Filled Horizontal
3. HubView: `_bedPoint` → BedPoint, `_forestObjective` = ForestClear.asset, `_stamina` → Canvas/Hud/Stamina의 StaminaUIBinder
4. 저장 → 다시 열어 HubView 새 칸 3개 확인

### `Assets/Scenes/Boot.unity`

1. `UICanvas/GameMenu/…/MapPage/Map` 새로: 앵커 가운데, 위치 (0, 20), 크기 (170, 680), `MapView` + `MapUIBinder` (`_windowMeters` 0). 자식 `Image` RawImage 꽉 채움 아래 `Exit`·`Objective`·`Player` (미니맵과 같은 모양), `Progress` LocalizedText (Field/`field.map_progress`) 앵커 (0.5, 0) 위치 (0, -30) 크기 (400, 40) 글자 26
   - MapView 칸: `_image` → Map/Image, 표시 셋, `_placeholder` → MapPage의 기존 자리 글 (Menu/`menu.map.placeholder`), `_progress` → Progress
2. `Managers` UIManager: `_menuMap` → MapPage/Map의 MapUIBinder
3. `Managers` DataManager: `_tutorials` 끝에 `FieldCast.asset`, `Map.asset`
4. 저장 → 다시 열어 확인

---

## 다국어 변경

1. 시트 새 탭 `Field`·`Monster` (머리글 15칸)

**`Field`**

| Key | Comment | ko | en |
|---|---|---|---|
| `field.hud.hp` | 숲 HUD 체력. {0} 지금, {1} 최대 | 체력 {0} / {1} | HP {0} / {1} |
| `field.hud.mana` | 숲 HUD 마나. {0} 지금, {1} 최대 | 마나 {0} / {1} | Mana {0} / {1} |
| `field.zone.1` | 1구역 이름 (임시) | 숲 외곽 | Forest Edge |
| `field.zone.2` | 2구역 이름 (임시) | 숲 중간 | Mid Forest |
| `field.zone.3` | 3구역 이름 (임시) | 숲 심층 | Deep Forest |
| `field.zone_line` | 숲 HUD 왼쪽 위. {0} 구역 이름, {1} 지도 % | {0} · 지도 {1}% | {0} · Map {1}% |
| `field.map_progress` | 메뉴 지도 탭 진행도. {0} % | 지도 {0}% 밝힘 | Map {0}% explored |
| `field.aim` | 조준 글. {0} 주문 이름, {1} 일치율 %, {2} 남은 초 | {0} {1}% · {2}초 · 좌클릭 시전 · 우클릭 취소 | {0} {1}% · {2}s · Left-click to cast · Right-click to cancel |
| `field.aim_exact` | 조준 글 (정확, 90% 이상). 자리 값 같음 | {0} {1}% (정확) · {2}초 · 좌클릭 시전 · 우클릭 취소 | {0} {1}% (precise) · {2}s · Left-click to cast · Right-click to cancel |
| `field.note.no_mana` | 알림: 마나 모자람 | 마나가 부족하다 | Not enough mana |
| `field.note.shield_cooldown` | 알림: 방호막 재사용 대기. {0} 남은 초 | 방호막은 {0}초 뒤에 다시 펼칠 수 있다 | The shield is ready again in {0}s |
| `field.note.scroll` | 알림: 현장에서 두루마리 그림 | 두루마리는 현장에서 만들지 않는다 | Scrolls aren't made in the field |
| `field.note.rune_alone` | 알림: 혼자서는 효과 없는 룬. {0} 룬 주문 이름 | 혼자서는 효과가 없다: {0} | Does nothing on its own: {0} |
| `field.note.no_effect` | 알림: 현장 효과가 아직 없는 주문. {0} 주문 이름 | 아직 현장에서 쓸 수 없다: {0} | Can't be used in the field yet: {0} |
| `field.note.canceled` | 알림: 조준 취소 | 취소 | Canceled |
| `field.exit` | 숲 입구 표지판 지점 이름 | 돌아가기 | Return |
| `field.gold_pile` | 떨어진 금화 지점 이름 | 떨어진 금화 | Dropped Gold |
| `field.gold_picked` | 토스트: 떨어진 금화를 주움. {0} 금화 | 주운 금화: {0} | Picked up gold: {0} |
| `field.quest_cleared` | 토스트: 소탕 목표 달성. {0} 의뢰 이름 | 목표 달성: {0}. 숲 입구로 돌아가자 | Goal complete: {0}. Head back to the forest entrance |
| `field.mercenary_lost` | 토스트: 용병 죽음. {0} 이름 | 동료를 잃었다: {0} | You lost a companion: {0} |

**`Monster`**

| Key | Comment | ko | en |
|---|---|---|---|
| `monster.goblin.name` | 몬스터 이름 | 고블린 | Goblin |
| `monster.wolf.name` | 몬스터 이름 | 늑대 | Wolf |
| `monster.boar.name` | 몬스터 이름 | 멧돼지 | Boar |
| `monster.spider.name` | 몬스터 이름 | 거대 거미 | Giant Spider |
| `monster.bear.name` | 몬스터 이름 | 곰 | Bear |
| `monster.mutant.name` | 몬스터 이름 (차원문 마력에 변이) | 변이 짐승 | Mutated Beast |

2. 기존 탭 줄 추가

| 탭 | Key | Comment | ko | en |
|---|---|---|---|---|
| Tutorial | `tutorial.field_cast.prompt` | 12시 안내: 숲에서 처음 | 좌클릭으로 주문 그리기 | Left-click to draw a spell |
| Tutorial | `tutorial.field_cast.title` | 튜토리얼 탭 제목 | 현장에서 주문 쓰기 | Casting in the Field |
| Tutorial | `tutorial.field_cast.body` | 튜토리얼 탭 설명 | 좌클릭하면 화면 가운데에 그린다. 마우스를 움직이면 펜이 따라오고, 버튼을 떼도 3초 안에 이어 그릴 수 있다. Space를 누르면 바로 판정한다. 맞으면 3초 동안 겨눴다가 좌클릭으로 쏜다 (3초가 지나면 저절로 나감). 우클릭은 취소. 불·물 룬은 혼자 그려도 던질 수 있다. | Left-click to draw in the middle of the screen. The pen follows your mouse, and you can keep drawing for 3 seconds after letting go. Press Space to judge right away. If it matches, aim for 3 seconds and left-click to fire (it fires on its own when time runs out). Right-click cancels. Fire and Water runes can be thrown on their own. |
| Tutorial | `tutorial.map.prompt` | 12시 안내: 숲에서 조금 걸은 뒤 | M으로 지도 보기 | Press M for the map |
| Tutorial | `tutorial.map.title` | 튜토리얼 탭 제목 | 지도 | Map |
| Tutorial | `tutorial.map.body` | 튜토리얼 탭 설명 | 숲은 걸으며 밝힌다. 밝힌 곳은 남는다. 오른쪽 위 미니맵에 가까운 곳이, M을 누르면 숲 전체가 보인다. | Walk to reveal the forest map. Revealed areas stay revealed. The minimap at the top right shows your surroundings; press M to see the whole forest. |
| Objective | `objective.forest_clear.title` | 목표 이름 | 숲 외곽의 고블린 소탕 | Clear the Goblins at the Forest Edge |
| Objective | `objective.forest_clear.desc` | 목표 설명 | 숲 외곽의 고블린을 모두 쓰러뜨린다. 의뢰를 못 끝내도 숲 입구에서 돌아올 수 있다. | Defeat every goblin at the forest edge. You can return from the forest entrance even if you haven't finished. |
| Objective | `objective.forest_return.title` | 목표 이름 | 숲 입구로 돌아가기 | Return to the Forest Entrance |
| Objective | `objective.forest_return.desc` | 목표 설명 | 고블린을 모두 쓰러뜨렸다. 숲 입구에서 F로 돌아가면 의뢰가 끝난다. | All the goblins are down. Return at the forest entrance with F to finish the quest. |
| Hub | `hub.board.done` | 게시판 버튼: 끝낸 의뢰 | 끝냄 | Done |
| Hub | `hub.quest_done` | 토스트: 의뢰를 끝냄. {0} 의뢰 이름 | 의뢰를 끝냈어: {0} | Quest complete: {0} |
| Hub | `hub.respawn.inn` | 토스트: 숲에서 죽어 여관 방에서 깨어남 | 여관 방에서 정신을 차렸다 | You came to in your room at the inn |
| Hub | `hub.respawn.gate` | 토스트: 숲에서 죽어 정문에서 깨어남 (방 없음) | 정문 앞에서 정신을 차렸다 | You came to by the gate |

3. 고침: `Tip`/`tip.return.body` → ko "숲 입구에서 F를 누르면 전초기지로 돌아가", en "Press F at the forest entrance to return to the outpost"
4. 지움: `UI`/`field.debug_info` (시트 줄 지움 → UI 컬렉션 `RemoveEntry` + 언어 표마다 `RemoveEntry(id)` + SetDirty)
5. 탭을 키트 CSV 형식으로 `Assets/Localization/CSV/`에 씀: Field·Monster (새로), Tutorial·Objective·Hub·Tip·UI
6. `LocalizationEditorSettings.CreateStringTableCollection`으로 `Field`·`Monster` (`Assets/Localization/Tables`). Preload 안 함 (숲 씬 글자는 로딩창이 기다림)
7. `Import Tables from CSV` → `Check Missing Translations`·`Check Missing Glyphs`

---

## 시전 방식 변경 (구현 뒤, 사용자 요청 2026-11-24)

위 C# 절은 처음 구현(좌클릭 시전·3초 대기 판정) 기준. 겹치는 부분은 아래가 이김.

### 동작

- **F 꾹(0.35초 이상) = 시전 상태**. 누른 채 좌클릭으로 그리고 **F를 떼면 판정**. Space는 바로 판정, 우클릭·Esc는 취소. 3초 대기 자동 판정은 숲·책상 모두 없앰 (`Timer` 오브젝트 지움)
- **F 0.35초 미만 = 상호작용**. 숲은 키를 **뗄 때** 적용 (`InteractDetector._tapOnRelease` 켬), 전초기지는 누를 때 바로 (그대로). 기준값은 `InteractDetector.HoldSeconds` 하나를 그리기 칸도 같이 씀
- 시전 상태가 되면 화면 가운데에 **반투명 양피지**(`SpellDrawView.Board`, 알파 0.85)가 나오고 펜은 양피지 안에서만 움직임. F를 떼면 양피지·선이 같이 사라짐. 양피지 그림은 임시 (`Assets/Art/Placeholder/UI/Parchment.png`, 코드로 만든 512×512). 잉크는 먹색, 펜 점은 붉은 갈색
- **책상 연습 = 숲과 같은 그리기 칸**: 같은 `SpellDrawController`가 F 꾹·양피지·판정·취소를 다 처리. 연습은 성공해도 결과 글만 남기고 쏘지 않음
- **성공**: `SpellSO`·`RuneSO`에 `_successSound`·`_effectPrefab` 칸을 만듦. 비어 있으면 소리 없음·임시 모양 그대로, 넣으면 소리가 나고 투사체·불벽에 효과가 붙음 (방호막은 효과 프리팹이 없으면 `FieldHudView._shield` 푸른 막). 책상 연습도 성공 소리는 남
- **실패**(그림이 마법이 아님. 빈 그림·직접 취소는 해당 없음): 실패 소리(`DataManager._castFailSound`) + 붉은 번쩍 + 체력 −10·마나 −15·스태미나 −30 (`FieldCastController` `FailHp`·`FailMana`·`FailStamina`, 임시)
- **우클릭 취소**: 그린 그림을 그 자리에서 판정 → 마법이면 **룬 수 × 1초 갈무리** (`SpellDrawController.RecoverSecondsPerRune`. 룬 주문 1초·2룬 마법 2초…) 동안 이동·시점·그리기·F가 잠기고 남은 시간 글이 뜸. 마법이 아닌 그림은 바로 풀림. 3초 조준 중 우클릭도 조준 중인 주문의 룬 수만큼 갈무리
- **첫 전투 설명창**: 마법사를 알아챈 적이 처음 생기면 게임을 멈추고 가운데에 설명창 (`TutorialWindowView`·`TutorialWindowController`, `Combat.asset` id `combat`, 6장, 다음·닫기·Esc, 이 PC에서 한 번). 12시 '좌클릭으로 주문 그리기' 안내와 `field_cast` 튜토리얼(에셋·키 3개)은 없앰

### 바꾼 파일

- 코드: `InteractDetector`(뗄 때 상호작용), `PlayerController`(`DrainStamina`), `SpellJudgeSpec`(`IsFailure`), `SpellSO`·`RuneSO`(소리·효과 칸), `DataManager`(`_castFailSound`), `TutorialSO`(`Window` 값·`_pages`), `SpellProjectileController`·`FlameWallController`(효과 프리팹 붙임), `SpellDrawView`·`SpellDrawController`(시전 패드로 새로), `FieldCastSpec`, `FieldHudView`(`_pen` → `_shield`), `FieldCastController`, `CodexController`(책상 성공 소리), `UIManager`(`ShowTutorialWindow`), `FieldState`(첫 전투 설명창). 새로: `TutorialWindowView`·`TutorialWindowController`
- 에셋·씬: `Parchment.png` 새로, `Data/Tutorials/Combat.asset` 새로·`FieldCast.asset` 지움, Boot `UICanvas/TutorialWindow`·UIManager·DataManager 연결, Forest `Hud/Draw/Board`·`Hud/Shield`·잉크·펜 색·`_resultSeconds` 1.4·Player `_tapOnRelease`, Outpost 책상 `DrawArea/Board`·`Pen`
- 다국어: Tutorial(`tutorial.combat.*` 8개 새로, `field_cast` 3개 지움, `draw.body` 고침), Cast(`cast.hint` 고침, `cast.recover`·`cast.result.fail` 새로), UI(`common.next`·`common.page_of` 새로). 시트·CSV·Unity 테이블 키 수 같음 (Tutorial 24·Cast 18·UI 28)

### 사용자가 채울 것 (지금은 비어 있어 조용히 무시)

- 주문별 성공 소리·효과: `Assets/Data/Spells/*.asset`의 `Success Sound`·`Effect Prefab`. 룬 혼자 던질 때는 `Assets/Data/Runes/*.asset`
- 실패 소리: Boot `Managers` DataManager의 `Cast Fail Sound`
- 양피지 그림: `Parchment.png`를 바꾸거나 Board 이미지 스프라이트 교체

### 6-2와 겹침

`plan-forest-dungeon.md`(6-2)의 설명창 내용은 이번에 6-1에 먼저 들어가서 2026-11-24에 6-2 plan에서 지움: `TutorialWindowView`·`TutorialWindowController`·`UIManager`·`TutorialSO` 변경, `Combat.asset`·Boot 연결, `tutorial.combat.*`·`common.next`·`common.page_of` 키, `FieldState`의 설명창·`field_cast` 알림, 확인 목록·커밋 메시지 줄. 6-2에서 튜토리얼 의뢰만 설명창을 띄우려던 `QuestSO._isTutorial`도 같이 뺌 (이제 어떤 적이든 첫 전투). 6-2 plan의 `FieldState` diff는 지금 코드(설명창 있는 `OnCombatChanged`·`CombatTutorialId`) 기준으로 이어짐.

---

## 주의사항

- 그리기 칸(`SpellDrawController`)은 숲·책상 연습이 같이 씀. 시전 상태에선 커서를 잠그고 `Look` 이동량으로 펜을 옮겨 양피지 안에서 그림 (`_penSpeed`: 마우스 1픽셀 = 펜 1픽셀. 책상 월드 캔버스도 화면 픽셀로 환산). 칸 Image는 Raycast Target을 끔. 시전 중엔 캔버스 GraphicRaycaster를 꺼서 잠긴 커서 자리의 버튼이 안 눌리게 함. 펜 좌표 기준은 칸 가운데
- Space는 `Player/Jump`·`Player/Cast` 둘 다. 시전 상태면 PlayerController가 잠겨 점프 안 하고 그리기 칸이 바로 판정, 아니면 그리기 칸이 무시하고 점프. 책상에서는 조작이 꺼져 있어 점프 안 함
- 그리는 동안 시점·이동 잠금, 조준 중엔 이동만 잠금. 게임 시간은 그대로 흐름 (적이 다가옴)
- 메뉴·일시정지가 열리면 Player 묶음이 꺼지며 좌클릭이 canceled로 와서 펜을 뗌. 대기·조준 시간은 게임 시간이라 멈춤
- 적·용병은 NavMeshAgent. 놓을 때 `NavMesh.SamplePosition`으로 4m 안 NavMesh에 붙임 (시체 자리도). Agent를 부르기 전에 `isOnNavMesh` 확인. 시체는 놓은 직후 agent·콜라이더를 끔
- 차원문은 판마다 자리가 달라 NavMeshObstacle(Carve)로 길을 막음 (굽는 NavMesh엔 안 들어감)
- 적 무리 번호 = Spawns 자식 순서. 적 키 `{자리 id}.{번호}`가 `ForestDead`에 남아 다시 들어가도 그 자리에 시체 (리셋은 6-2). 자리 id를 바꾸면 앞 판 기록과 안 맞음
- 소탕 대상 수는 들어올 때 살아 있는 대상만 셈. 다 쓰러진 채 들어오면 바로 목표 달성으로 둠
- 전투 상황은 적의 쫓음 시작·그만둠 수로 셈 (`SetEngaged`). 적 하나가 바뀔 때만 한 번씩 더하고 빼서 균형이 맞음
- 죽은 뒤 도망 시뮬은 `TransitionManager.ShowCover`(게임 시간 그대로) 밑에서 `GameManager.SetGameSpeed(6)`. 그동안 Esc·M·J 막힘 (`UsesPauseMenu`·`HandleCancel`). `ChangeState`의 로딩창이 덮개를 다시 덮고, `Exit`에서 속도를 1로 되돌림
- 금화는 낱개 물건 하나로 셈 (6-1엔 떨굴 물건이 금화뿐). 또 죽으면 앞 금화 지점을 지우고 새로 씀
- 쓰러진 용병은 판 데이터 파티에서 바로 빠짐 (길드 남은 후보로 다시 영입)
- 소탕을 끝내고 죽어도 부활하면 의뢰를 끝냄 (토스트는 깨어남 토스트만)
- 숲에서 처음 생략 없이 그린 다중 룬 마법은 코덱스와 같은 토스트로 등록하고 비교 그림을 다시 만듦
- 구역 이름은 구역·지도 %가 바뀔 때 다시 읽음 (숲에서 언어를 바꾸면 그때 바뀜)
- 마우스 감도(PlayerController `_lookSensitivity`)와 펜 감도(`_penSpeed`)는 따로
- 날짜·의뢰 기간·고블린 줍기·던전·리셋은 6-2 (전투 설명창은 시전 방식 변경 때 6-1에 먼저 들어감)

---

## 진행 추적

- [x] 묶음 Logic — 새로: MonsterSO, FieldUnitController, FieldService, EnemyController, MercenaryController, SpellProjectileController, FlameWallController, FieldCastSpec, FieldCastController, FogMapService, FogRevealController, SpawnPointView, MapView, MapUIBinder, StaminaBarView, StaminaUIBinder, PartyRowView, FieldHudView, FieldHudUIBinder, FieldView / 고침: QuestSO, JobSO, SpellSO, RuneSO, TutorialSO, RunEntry, PlayerController, SpellDrawController, TutorialManager, UIManager, GameManager, QuestBoardController, HubView, HubState, FieldState / 지움: FieldDebugView. 컴파일 에러·경고 0
- [x] 묶음 Input — Crouch Ctrl, Attack Enter 뺌, CancelCast 추가
- [x] 묶음 Editor-Localization — 시트 Field·Monster 탭, Tutorial·Objective·Hub·Tip 줄, UI `field.debug_info` 지움, CSV, 테이블 2개 만들기, Import, 누락 검사
- [x] 묶음 Prefab·Data — 재질 6개, 프리팹 7개, MonsterSO 6개, 주문·룬·직업·의뢰 값, TutorialSO 2개, ObjectiveSO 2개
- [x] 묶음 Editor-Scene — Forest 다시 구성 + NavMesh 굽기, Outpost(BedPoint·Stamina·HubView), Boot(메뉴 지도·UIManager·DataManager), 저장 뒤 다시 열어 확인
- [x] 묶음 Cast-F — 시전 방식 변경 (F 꾹·양피지·실패 벌칙·우클릭 갈무리·책상 연습 같게·첫 전투 설명창). 컴파일 에러·경고 0, 씬 저장 뒤 다시 열어 연결 확인, 누락 번역은 ko·en 없음·빠진 글자 없음
- [ ] 묶음 Verify — 사용자 확인

구현 중 plan과 다르게 한 것 (2026-11-23):
- `MapUIBinder.Awake`: `_fog`가 없을 때만 `ShowBound(false)`. 메뉴 창이 꺼진 채 시작해서 첫 M에서 Awake가 늦게 돌면 이미 Bind된 지도를 자리 글로 되돌리기 때문
- 숲 적 자리는 plan 표대로 19곳 (본문·커밋 메시지의 '20곳'은 표 줄 수와 안 맞음. 표 기준으로 둠)
- 시트 UI 탭 `field.debug_info` 줄·UI 컬렉션·언어 표 값 지움 완료. 시트 새 탭 Field(sheetId 1100100110)·Monster(1100100111)

---

## 확인 목록 (사용자, 에디터 Play)

1. Forest 씬에서 Play (디버그 판: 용병 2명·첫 의뢰·방): 로딩창 → 숲 입구. 왼쪽 위 "숲 외곽 · 지도 N%", 오른쪽 위 미니맵(밝힌 곳·화살표·입구·목표), 왼쪽 동료 2줄, 아래 "체력 124 / 124"·"마나 100 / 100", 토스트 "임무 일지 갱신: 숲 외곽의 고블린 소탕" (12시 주문 안내는 없음)
2. 이동: Shift 달리기 → 스태미나 막대가 나타나 줄다 다 쓰면 못 달림 → 쉬면 1초 뒤 차고 가득 차면 사라짐. 뒤로(S)는 Shift를 눌러도 걷기. Space 점프, Ctrl을 누르는 동안 낮아지고 느림 (앉은 채 점프·달리기 안 됨)
3. F를 0.35초 넘게 꾹 → 가운데에 반투명 양피지·펜 점, 시점·이동 멈춤. 누른 채 좌클릭으로 그림 (떼고 이어 그리기), F를 떼면 판정 (양피지·선이 같이 사라지고 결과 글만 1.4초 남음. Space는 바로 판정). 불 룬 하나 → "불 룬 주문 N%" → 조준 원·"… 3.0초 · 좌클릭 시전 · 우클릭 취소" → 좌클릭이나 3초 뒤 작은 불탄. 응축 룬 하나 → "혼자서는 효과가 없다: 응축 룬 주문". 그림 없이 F를 떼면 아무 일 없음
4. 디버그 판은 마법이 다 등록됨: 화염 투사·화염구(폭발)·물 투사(밀쳐냄)·화염 확산(세 발)·화염 벽(조준점 불벽 4초)·방호막(바로, 다시 쓰면 "방호막은 N초 뒤에…"). 마나가 모자라면 "마나가 부족하다". 그리는 동안 WASD 안 됨, 조준 중엔 시점만
5. 고블린 무리 가까이 → 무리째 달려오면서 게임이 멈추고 가운데에 전투 설명창 (6장, 다음·닫기, Esc로도 닫음). 닫으면 다시 흐르고, 이 PC에선 다음 전투에 안 나옴. 맞힌 적은 위 가운데 이름·체력 2.5초. 맞으면 화면이 붉게 번쩍, 체력 30% 밑이면 계속 붉음
6. 용병: 따라오다 가까운 적과 싸움, 레인저는 멀리서 화살. 용병이 쓰러지면 "동료를 잃었다: 이름", 줄이 흐려짐
7. 고블린 9마리를 다 쓰러뜨리면 "목표 달성: 숲 외곽 정리 (임시). 숲 입구로 돌아가자" → 임무 일지 "숲 입구로 돌아가기", 지도 목표 표시가 입구로
8. 입구 표지판을 보면 "[F] 돌아가기" → 전초기지 정문 밖, 경비병 자막, "의뢰를 끝냈어" 토스트. 게시판 버튼 '끝냄', 임무 일지 목표 없음
9. 못 끝내고 돌아오면 의뢰가 남음 (임무 일지 숲 목표). 다시 들어가면 쓰러뜨린 적은 쓰러진 자리에 시체, 남은 적만 움직임, 지도는 밝힌 그대로, 차원문은 같은 자리
10. 죽기: 화면이 덮이고 몇 초 뒤 로딩창 → 여관 방 침대 옆 "여관 방에서 정신을 차렸다", 금화 0 (방이 없는 판이면 정문 안쪽 "정문 앞에서 정신을 차렸다"). 용병은 도망쳐 살았거나 죽음 (영입 창 파티 칸). 다시 숲 → 죽은 자리에 "[F] 떨어진 금화" → "주운 금화: N". 줍기 전에 또 죽으면 앞 금화는 없음
11. M → 메뉴 지도 탭에 숲 전체 (밝힌 곳·나·목표·입구·"지도 N% 밝힘"). 12시 "M으로 지도 보기"는 M을 누르면 사라짐. 전초기지 지도 탭은 자리 글 그대로
12. 전초기지에서도 Space 점프·Ctrl 앉기·스태미나 막대 (아래 왼쪽)
13. 새 게임 → 정상 흐름으로 숲 → 같은 확인. 불·물을 안 고른 판은 혼자 공격할 수단 없음 (용병이 싸움)
14. 영어로 바꾸면 숲 글자·몬스터 이름·튜토리얼·목표 영어
15. 콘솔 에러·경고 없음 (NavMesh 경고 포함)
16. F를 짧게(0.35초 미만) 눌렀다 뗌 → 숲에선 **뗄 때** 상호작용 (입구 표지판·떨어진 금화 줍기), 길게 누르면 상호작용 없이 시전 상태. 전초기지는 누르는 즉시 상호작용 (그대로)
17. 마법이 아닌 그림 판정 (아무 선이나) → 붉은 번쩍·체력 −10·마나 −15·스태미나 −30, 결과 글 "실패: …". 실패 소리는 비어 있어 조용함
18. 시전 상태에서 우클릭: 그린 그림이 불 룬 하나면 "갈무리 중 · 1.0초" 동안 이동·시점·F가 안 됨, 2룬 마법이면 2초. 마법이 아닌 그림이면 바로 풀림. 3초 조준 중 우클릭도 같은 갈무리
19. 성공 소리·효과: 슬롯이 비어 있어 지금은 임시 모양만. 주문 하나에 소리·효과 프리팹을 넣고 시전해 소리가 나고 투사체·불벽에 효과가 붙는지 확인
20. 책상 코덱스 연습 탭: F를 꾹 → 연습 칸에 양피지·펜 점, F를 떼면 판정·결과 글이 칸에 남음 (숲과 같은 방식, 쏘지 않음). 우클릭 갈무리·Space도 같음. 시전 중엔 마우스가 칸 안에서만 움직이고 다른 버튼이 안 눌림
21. 그리는 중 Esc → 시전 취소·메뉴, 닫으면 커서·이동·시점 정상. 조준 중 Esc → 조준 시간 멈췄다 닫으면 이어짐

---

## 커밋 메시지

```
Add — 6-1 숲: 3구역 회색 숲, 고정 적·용병 AI, 현장 시전(3초 조준), 안개 지도, 입구 귀환, 죽음과 여관 부활

- 숲 씬을 3구역(180×720m) 회색 숲으로: 나무·바위, NavMesh, 입구 표지판(F 귀환), 3구역 먼 쪽 절반 무작위 차원문 자리, 고정 적 자리 19곳 (1구역 고블린·늑대·멧돼지, 2구역 거미·곰, 3구역 변이 짐승·늑대)
- 적 AI (v1): 돌아다님 → 알아채면 무리째 쫓음 → 0.26초 준비 뒤 공격, 46m 포기, 일부는 마법사를 노림. 쓰러뜨린 적은 다시 들어가도 시체
- 용병 AI: 명령 없이 따라오며 싸움, 레인저는 활. 체력 0이면 죽어 파티에서 빠짐
- 현장 시전: 좌클릭 시전 상태 → 커서 잠근 채 마우스로 가운데에 그림 → 판정 → 3초 조준 → 발사. 마나·방호막, 주문 효과 6종(프로토타입 값), 만드는 룬(불·물)만 혼자 던짐, 숲에서 처음 그린 마법 등록
- 이동: Space 점프(시전 중엔 바로 판정), Ctrl 앉기, 뒤로 못 달림, 스태미나 (전초기지도)
- 안개 지도(밝히기만): 미니맵, 메뉴 지도 탭. 12시 안내는 전투 중 숨김, 현장 시전·지도 튜토리얼
- 의뢰: 숲 고블린 소탕 → 목표 달성 → 마을로 오면 끝냄, 못 끝내면 남음. 게시판 '끝냄'
- 죽음: 금화를 죽은 자리에 떨굼(또 죽으면 앞 것 사라짐), 덮개 밑에서 용병 도망 시뮬, 여관 방 침대(방이 없으면 정문)에서 부활
- 다국어: Field·Monster 테이블 새로, UI의 field.debug_info 지움

Why: 첫 프로토타입 범위(캐릭터 생성 → 숲)의 마지막 씬. 6단계 결정(기획 12 결정 68~86) 중 숲에서 싸우고 돌아오는 한 바퀴를 먼저 닫음
Alternatives: 커서를 풀어 진짜 커서로 그리기(잠금이 오가며 첫 획이 튐), 용병 명령 3개(사용자가 없앰), 적 매번 무작위 배치(숲 고정으로 바꿈)
```
