# Plan: 5단계 주문서 — 코덱스(주문서 읽기·그리기 연습·도감), 주문 인식기, 주문 도감 탭

## 현행 분석

- 습득 요소: `RunEntry.LearnedElements` (생성 화면에서 7개 중 4개, `CharacterCreationState.StartElementCount` 임시). 디버그 판(`RunEntry.CreateDebug`)은 `DataManager.Elements` 7개 전부 습득. 주문·주문서·도감 데이터 없음
- `ElementSO`: id(fire, condense, line, block, water, spread, wall)·이름·설명·그림(Sprite, 지금 비어 있음). `DataManager._elements` 순서 = 위 순서
- 여관 방(Outpost `Interiors/RoomInterior`, 월드 (20, -40, 0)): `Desk` Cube 로컬 (-1.4, 0.4, 1.6), 스케일 (1.2, 0.8, 0.6), BoxCollider, 상호작용 없음 (윗면 로컬 y 0.8, 가로 x -2.0~-0.8, 깊이 z 1.3~1.9). 방 벽은 ±2.25
- `HubState.OnInteracted`: 문(`DoorView`)이면 페이드 이동, 아니면 id switch (innkeeper·clerk·board·guard). `SetControl(bool)`이 `PlayerController`·`InteractDetector`를 켜고 끔. Detector가 꺼지면 바라보는 지점 null 알림 → 안내 글 숨김
- 플레이어 카메라: `Player/Head` (Camera FOV 70·Near 0.05, AudioListener, 로컬 (0, 1.65, 0)). `PlayerController`는 꺼져 있으면 Head를 안 건드리고, 켜지면 `_pitch`로 Head 로컬 회전을 다시 씀
- Esc: `UIManager.OnCancel`이 확인창 → 설정 → 대화 → 영입 → 게시판 → 메뉴 창 → 일시정지 메뉴 순으로 닫고, 아무것도 없으면 `GameManager.CanPause`일 때 일시정지 메뉴를 엶. 상태가 Esc를 먼저 받는 길 없음
- 메뉴 창 주문 도감 탭: `GameMenu/Panel/Pages/SpellsPage`에 자리 글 `Placeholder`(Menu/`menu.spells.placeholder`, 크기 (1200, 60))만. 튜토리얼 탭은 `TutorialListUIBinder`가 `TutorialListItem` 프리팹(`Button` 변형, 높이 56, `TutorialListItemView` = 버튼 + 글자)으로 목록·자세히를 만듦. 메뉴 창은 `GameManager.Instance.Run`을 직접 읽음 (`JournalUIBinder`)
- 툴팁: `TooltipHandler`(IPointerEnter/Exit, `Set`) → `UIManager.ShowTooltip`. EventSystem은 Boot (`InputSystemUIInputModule`)
- 입력: `Player/Jump`(Space)는 쓰는 코드 없음. 일시정지·메뉴 창이 뜨면 `Player` 묶음이 꺼짐. 입력 래퍼 클래스 생성 안 함 (`InputSystem.actions` 전역)
- 튜토리얼: `TutorialAction` = Move·Look·Sprint·Interact. Interact처럼 12시 안내 없는 것은 게임 코드가 `TutorialManager.MarkLearned`로 배움 처리 (`Measure`·`Goal`은 나머지를 0·MaxValue로 둠)
- 게임 코드는 `Assets/Scripts/Mage.asmdef` (InputSystem·Localization·ResourceManager·TMP·UGUI 참조). 게임 쪽 테스트 어셈블리 없음 (키트만 `Tests/Editor`, nunit precompiled + `UNITY_INCLUDE_TESTS`). Test Framework 1.6.0
- UGUI에는 선을 그리는 내장 그래픽이 없음 → 획은 `Graphic`을 상속한 `StrokeDrawer`로 그림 (새 접미사 Drawer, CLAUDE.md 표에 추가)
- 프로토타입 인식기(`spell-core.js`) 기본값: 점 48개, 회전 ±0.3·±0.15·0, 정확도 눈금 floor 0.4·span 2.5 (32점 기준 환산), 최소 크기 60, 부분 기준 80%, 전체 기준 60%, 남는 선 10%, 생략 0, 대기 3초. 그림 좌표는 화면 기준 y 아래가 + → Unity 데이터는 y를 뒤집어 넣음 (뒤집기는 거리를 안 바꾸고 회전 목록이 대칭이라 판정이 같음)

---

## C# 변경

### `Assets/Scripts/Spell/StrokeEntry.cs` (새 파일)

**추가 내용:** 획 하나의 점 (주문 그림 데이터, 직렬화)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>획 하나의 점 (주문 그림 데이터). y는 위가 +, 단위는 주문 그림 좌표 (대략 -1.5~1.5)</summary>
    [Serializable]
    public sealed class StrokeEntry
    {
        /// <summary>획을 이루는 점 (그린 순서, 2개 이상)</summary>
        [SerializeField] List<Vector2> _points = new();

        /// <summary>획을 이루는 점</summary>
        public IReadOnlyList<Vector2> Points => _points;
    }
}
```

---

### `Assets/Scripts/Spell/SpellPartEntry.cs` (새 파일)

**추가 내용:** 주문 그림 부분 하나 = 요소 + 획들 (직렬화)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>주문 그림의 부분 하나: 어느 요소의 그림인지와 그 획들</summary>
    [Serializable]
    public sealed class SpellPartEntry
    {
        /// <summary>이 부분의 요소</summary>
        [SerializeField] ElementSO _element;

        /// <summary>이 부분의 획 (1개 이상)</summary>
        [SerializeField] List<StrokeEntry> _strokes = new();

        /// <summary>이 부분의 요소</summary>
        public ElementSO Element => _element;

        /// <summary>이 부분의 획</summary>
        public IReadOnlyList<StrokeEntry> Strokes => _strokes;
    }
}
```

---

### `Assets/Scripts/Spell/SpellSO.cs` (새 파일)

**추가 내용:** 주문 SO (id·이름·설명·종류·그림 부분). 인식기에 넘길 순수 데이터(`Shape`)는 처음 쓸 때 만들고 인스펙터에서 고치면 비움

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>주문 종류</summary>
    public enum SpellKind
    {
        /// <summary>현장에서 그려서 쓰는 전투 주문</summary>
        Combat,

        /// <summary>두루마리 (주문 그림 + 차단)</summary>
        Scroll,
    }

    /// <summary>주문 하나 (도감에 실린 그림). 그림은 요소 부분을 합친 것이고 인식기가 통째로 대조함</summary>
    [CreateAssetMenu(fileName = "Spell", menuName = "Mage/Spell")]
    public sealed class SpellSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (flame_shot, fireball …)</summary>
        [SerializeField] string _id;

        /// <summary>주문 이름 (Spell 테이블 spell.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>주문 설명 (Spell 테이블 spell.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>전투 주문인지 두루마리인지</summary>
        [SerializeField] SpellKind _kind;

        /// <summary>그림 부분 (요소마다 하나)</summary>
        [SerializeField] List<SpellPartEntry> _parts = new();

        /// <summary>인식기에 넘길 그림 (처음 쓸 때 만듦. 인스펙터에서 고치면 비움)</summary>
        [NonSerialized] SpellShapeEntry _shape;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>주문 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>주문 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>전투 주문인지 두루마리인지</summary>
        public SpellKind Kind => _kind;

        /// <summary>그림 부분</summary>
        public IReadOnlyList<SpellPartEntry> Parts => _parts;

        /// <summary>인식기에 넘길 그림 (요소 id와 획 점)</summary>
        public SpellShapeEntry Shape => _shape ??= BuildShape();

        #region OnValidate - 인스펙터에서 고치면 만들어 둔 그림을 비움
        void OnValidate() => _shape = null;
        #endregion

        #region BuildShape - 부분마다 요소 id와 획 점 배열을 모아 인식기용 그림을 만듦 (요소가 비면 빈 id)
        SpellShapeEntry BuildShape()
        {
            // 부분마다 요소 id
            var elementIds = new string[_parts.Count];
            // 부분마다 획 → 점
            var strokes = new Vector2[_parts.Count][][];
            for (int i = 0; i < _parts.Count; i++)
            {
                // 이 부분
                SpellPartEntry part = _parts[i];
                elementIds[i] = part.Element != null ? part.Element.Id : string.Empty;
                strokes[i] = part.Strokes.Select(stroke => stroke.Points.ToArray()).ToArray();
            }
            return new SpellShapeEntry(_id, elementIds, strokes);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Spell/SpellShapeEntry.cs` (새 파일)

**추가 내용:** 인식기에 넘기는 주문 그림 (SO와 떨어진 순수 데이터. EditMode 테스트가 SO 없이 만듦)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>인식기에 넘기는 주문 그림 (SO와 떨어진 순수 데이터): id, 부분마다 요소 id와 획 점. y는 위가 +</summary>
    public sealed class SpellShapeEntry
    {
        /// <summary>주문 id</summary>
        public string Id { get; }

        /// <summary>부분마다 요소 id (부분 순서)</summary>
        public IReadOnlyList<string> PartElementIds { get; }

        /// <summary>부분마다 획 → 점 (부분 순서)</summary>
        public IReadOnlyList<Vector2[][]> PartStrokes { get; }

        #region SpellShapeEntry - 부분 수가 맞는지 검사하고 만듦
        /// <param name="id">주문 id</param>
        /// <param name="partElementIds">부분마다 요소 id</param>
        /// <param name="partStrokes">부분마다 획 → 점</param>
        public SpellShapeEntry(string id, IReadOnlyList<string> partElementIds, IReadOnlyList<Vector2[][]> partStrokes)
        {
            if (partElementIds == null) throw new ArgumentNullException(nameof(partElementIds));
            if (partStrokes == null) throw new ArgumentNullException(nameof(partStrokes));
            if (partElementIds.Count != partStrokes.Count) throw new ArgumentException("요소 id 수와 부분 수가 다름", nameof(partStrokes));
            Id = id ?? string.Empty;
            PartElementIds = partElementIds;
            PartStrokes = partStrokes;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Spell/SpellCandidateEntry.cs` (새 파일)

**추가 내용:** 판정에 쓸 비교 그림 하나 (주문 그림 또는 생략본). 점 구름·부분 점은 만들 때 한 번 계산 (`SpellRecognitionCalculator.BuildCandidates`만 만듦)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>판정에 쓸 비교 그림 하나 (주문 그림 또는 부분을 뺀 생략본). 점 구름·부분 점은 만들 때 한 번 계산. SpellRecognitionCalculator.BuildCandidates가 만듦</summary>
    public sealed class SpellCandidateEntry
    {
        /// <summary>BuildCandidates에 넘긴 주문 그림 목록 번호</summary>
        public int ShapeIndex { get; }

        /// <summary>원래 주문 그림</summary>
        public SpellShapeEntry Shape { get; }

        /// <summary>뺀 부분 번호 (오름차순. 생략이 없으면 빈 목록)</summary>
        public IReadOnlyList<int> Omit { get; }

        /// <summary>주문의 요소를 다 익혔는지 (안 익혀도 비교는 함: 이유를 보여 주려고)</summary>
        public bool Learned { get; }

        /// <summary>전체 비교용 정규화 점 구름 (CloudPoints개)</summary>
        public Vector2[] Cloud { get; }

        /// <summary>남긴 부분의 요소 id (부분 순서). 부분 일치율 순서와 같음</summary>
        public IReadOnlyList<string> PartElementIds { get; }

        /// <summary>부분 검사용 촘촘한 템플릿 점</summary>
        public Vector2[] Dense { get; }

        /// <summary>촘촘한 점마다 남긴 부분 번호</summary>
        public int[] DenseParts { get; }

        /// <summary>맞추기용 성긴 템플릿 점</summary>
        public Vector2[] Sparse { get; }

        /// <summary>촘촘한 점 테두리 상자</summary>
        public Rect Bounds { get; }

        /// <summary>테두리 상자 긴 변 (0이면 1)</summary>
        public float Size { get; }

        #region SpellCandidateEntry - 계산해 둔 값을 받아 만듦 (인식기만 부름)
        /// <param name="shapeIndex">주문 그림 목록 번호</param>
        /// <param name="shape">원래 주문 그림</param>
        /// <param name="omit">뺀 부분 번호</param>
        /// <param name="learned">요소를 다 익혔는지</param>
        /// <param name="cloud">전체 비교용 점 구름</param>
        /// <param name="partElementIds">남긴 부분의 요소 id</param>
        /// <param name="dense">촘촘한 템플릿 점</param>
        /// <param name="denseParts">촘촘한 점마다 부분 번호</param>
        /// <param name="sparse">성긴 템플릿 점</param>
        /// <param name="bounds">촘촘한 점 테두리 상자</param>
        /// <param name="size">테두리 상자 긴 변</param>
        internal SpellCandidateEntry(int shapeIndex, SpellShapeEntry shape, int[] omit, bool learned, Vector2[] cloud,
            string[] partElementIds, Vector2[] dense, int[] denseParts, Vector2[] sparse, Rect bounds, float size)
        {
            ShapeIndex = shapeIndex;
            Shape = shape;
            Omit = omit;
            Learned = learned;
            Cloud = cloud;
            PartElementIds = partElementIds;
            Dense = dense;
            DenseParts = denseParts;
            Sparse = sparse;
            Bounds = bounds;
            Size = size;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Spell/SpellJudgeSpec.cs` (새 파일)

**추가 내용:** 판정 결과 (시전이면 고른 그림, 취소면 이유·가장 가까운 그림·부분 일치율)

```csharp
using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>판정이 시전을 못 한 이유 (시전이면 None)</summary>
    public enum SpellJudgeReason
    {
        /// <summary>시전</summary>
        None,

        /// <summary>그린 것이 없음 (점 2개 미만)</summary>
        Empty,

        /// <summary>그림이 너무 작음</summary>
        Small,

        /// <summary>비교할 주문 그림이 없음</summary>
        NoCandidates,

        /// <summary>맞는 주문이 없음</summary>
        NoMatch,

        /// <summary>부분 일치율·남는 선이 모자람 (덜 그림)</summary>
        Parts,

        /// <summary>습득하지 않은 요소가 든 주문</summary>
        Unlearned,

        /// <summary>같은 그림인 주문이 둘 이상</summary>
        Ambiguous,

        /// <summary>직접 취소 (Esc·우클릭)</summary>
        Canceled,
    }

    /// <summary>판정 결과 하나 (그릴 때마다 새로). 시전이면 고른 그림, 취소면 이유와 이유를 보일 가장 가까운 그림</summary>
    public readonly struct SpellJudgeSpec
    {
        /// <summary>시전했는지</summary>
        public bool IsCast { get; }

        /// <summary>취소 이유 (시전이면 None)</summary>
        public SpellJudgeReason Reason { get; }

        /// <summary>시전한 그림 또는 이유를 보일 그림. 없으면 null</summary>
        public SpellCandidateEntry Candidate { get; }

        /// <summary>Candidate의 전체 일치율 (0~1)</summary>
        public float Accuracy { get; }

        /// <summary>Candidate 남긴 부분마다 일치율 (0~1, PartElementIds 순서. 부분 검사를 안 했으면 빈 목록)</summary>
        public IReadOnlyList<float> PartCoverage { get; }

        /// <summary>남는 선 비율 (0~1)</summary>
        public float Extra { get; }

        /// <summary>Unlearned: 안 익힌 요소 id (주문 부분 순서)</summary>
        public IReadOnlyList<string> MissingElementIds { get; }

        /// <summary>Ambiguous: 같은 그림인 주문의 주문 그림 목록 번호 (처음 나온 순서)</summary>
        public IReadOnlyList<int> AmbiguousShapes { get; }

        /// <summary>이 판정에 쓴 부분 기준 (0~1, 이유 글에 씀)</summary>
        public float PartMin { get; }

        #region SpellJudgeSpec - 값을 받아 만듦 (빈 목록은 null로 받아도 됨)
        /// <param name="isCast">시전했는지</param>
        /// <param name="reason">취소 이유</param>
        /// <param name="candidate">시전한 그림 또는 이유를 보일 그림</param>
        /// <param name="accuracy">전체 일치율</param>
        /// <param name="partCoverage">부분마다 일치율</param>
        /// <param name="extra">남는 선 비율</param>
        /// <param name="missingElementIds">안 익힌 요소 id</param>
        /// <param name="ambiguousShapes">같은 그림인 주문 번호</param>
        /// <param name="partMin">부분 기준</param>
        public SpellJudgeSpec(bool isCast, SpellJudgeReason reason, SpellCandidateEntry candidate, float accuracy,
            IReadOnlyList<float> partCoverage, float extra, IReadOnlyList<string> missingElementIds, IReadOnlyList<int> ambiguousShapes, float partMin)
        {
            IsCast = isCast;
            Reason = reason;
            Candidate = candidate;
            Accuracy = accuracy;
            PartCoverage = partCoverage ?? Array.Empty<float>();
            Extra = extra;
            MissingElementIds = missingElementIds ?? Array.Empty<string>();
            AmbiguousShapes = ambiguousShapes ?? Array.Empty<int>();
            PartMin = partMin;
        }
        #endregion

        #region Cancel - 그림 없이 이유만 있는 취소 (빈 그림·너무 작음·비교 그림 없음·직접 취소)
        /// <param name="reason">취소 이유</param>
        public static SpellJudgeSpec Cancel(SpellJudgeReason reason) => new(false, reason, null, 0f, null, 0f, null, null, 0f);
        #endregion
    }
}
```

---

### `Assets/Scripts/Spell/SpellRecognitionCalculator.cs` (새 파일)

**추가 내용:** 프로토타입 `spell-core.js` 인식기 이식 (순수 함수). 비교 그림 만들기(`BuildCandidates`), 판정(`Judge`), 다시 뽑기·정규화·$P 거리·정확도 환산은 테스트용으로 공개. 정렬은 JS의 안정 정렬과 같게 같은 값이면 원래 순서

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>주문 그림 인식 (순수 함수 모음): 여러 획을 점 구름 하나로 합쳐 $P로 비교 그림과 전체 일치율을 재고, 부분마다 몇 %를 따라 그렸는지 본 뒤 시전·취소를 정함. 좌표는 y 위가 +. 그린 획 단위는 그리기 칸 단위, 주문 그림은 대략 -1.5~1.5</summary>
    public static class SpellRecognitionCalculator
    {
        /// <summary>전체 비교 점 구름의 점 수 (합친 그림은 부분이 많아서 v1 32보다 늘림)</summary>
        public const int CloudPoints = 48;

        /// <summary>그린 그림 테두리 상자 긴 변이 이보다 작으면 '너무 작다' (그리기 칸 단위)</summary>
        public const float MinDrawSize = 60f;

        /// <summary>정확(위력 ×1.4)으로 칠 전체 일치율</summary>
        public const float ExactAccuracy = 0.9f;

        /// <summary>부분 기준 기본값 (부분마다 이 비율 이상 따라 그려야 함, 임시)</summary>
        public const float DefaultPartMin = 0.8f;

        /// <summary>전체 기준 기본값 (전체 일치율, 임시)</summary>
        public const float DefaultAllMin = 0.6f;

        /// <summary>남는 선(주문 그림 어디에도 안 걸친 그린 선) 한도</summary>
        public const float MaxExtra = 0.1f;

        /// <summary>정확도 눈금이 맞춰진 점 수 (v1). 거리는 점 수에 비례해서 이 기준으로 환산</summary>
        const int ScalePoints = 32;

        /// <summary>정확도 눈금: 이 거리까지는 100%</summary>
        const float AccuracyFloor = 0.4f;

        /// <summary>정확도 눈금: 100%에서 0%까지 거리 폭</summary>
        const float AccuracySpan = 2.5f;

        /// <summary>얇은 그림으로 칠 짧은 변/긴 변 비율 (이보다 작으면 가로세로를 같은 비율로 맞춤)</summary>
        const float ThinRatio = 0.3f;

        /// <summary>점수 차이가 이 안이고 사실상 같은 그림이면 생략이 적은 쪽</summary>
        const float TieScore = 0.03f;

        /// <summary>템플릿끼리 닮음이 이 이상이면 사실상 같은 그림</summary>
        const float SameShape = 0.9f;

        /// <summary>부분 검사에서 '따라 그렸다'로 칠 거리 (주문 그림 크기 대비)</summary>
        const float CoverRadius = 0.08f;

        /// <summary>맞출 때 가로세로 배율 차이 한도</summary>
        const float MaxAspect = 1.25f;

        /// <summary>다른 주문이 남는 선을 이만큼 더 적게 설명하면 그 주문을 덜 그린 것으로 봄</summary>
        const float ExplainGap = 0.04f;

        /// <summary>다른 주문의 전체 일치율이 이만큼 더 높으면 그 주문을 덜 그린 것으로 봄</summary>
        const float RivalAccuracyGap = 0.08f;

        /// <summary>전체 일치율이 (전체 기준 − 이 값) 이상인 후보만 부분 검사 (1등은 늘)</summary>
        const float PartCheckMargin = 0.15f;

        /// <summary>맞추기에 쓸 그린 점 수</summary>
        const int AlignPoints = 64;

        /// <summary>덮인 비율을 잴 그린 점 수</summary>
        const int CoverPoints = 160;

        /// <summary>부분 검사 촘촘한 템플릿 점 간격 (주문 그림 좌표)</summary>
        const float DenseStep = 0.03f;

        /// <summary>맞추기용 성긴 템플릿 점 간격 (주문 그림 좌표)</summary>
        const float SparseStep = 0.09f;

        /// <summary>맞추기 되풀이 최대 횟수</summary>
        const int AlignIterations = 12;

        /// <summary>맞추기에서 짝이 가까운 순으로 남길 비율 (먼 15%는 버림)</summary>
        const float AlignKeep = 0.85f;

        /// <summary>맞추기 배율 한도: 시작 배율의 이 배수 안</summary>
        const float AlignScaleMin = 0.8f;

        /// <summary>맞추기 배율 한도: 시작 배율의 이 배수 안</summary>
        const float AlignScaleMax = 1.25f;

        /// <summary>그린 그림을 돌려 볼 각도 (라디안, ±17°)</summary>
        static readonly float[] Turns = { -0.3f, -0.15f, 0f, 0.15f, 0.3f };

        /// <summary>판정 중 후보 하나의 점수 (판정마다 새로)</summary>
        sealed class RankEntry
        {
            /// <summary>후보 목록 번호 (전체 일치율이 같을 때 순서)</summary>
            public int Order;

            /// <summary>전체 일치율 순으로 정렬한 뒤 자리 (점수가 같을 때 순서)</summary>
            public int Position;

            /// <summary>비교 그림</summary>
            public SpellCandidateEntry Candidate;

            /// <summary>전체 일치율 (0~1)</summary>
            public float Accuracy;

            /// <summary>가장 가까웠을 때 회전 (라디안)</summary>
            public float Angle;

            /// <summary>부분 검사를 했는지</summary>
            public bool Checked;

            /// <summary>남긴 부분마다 일치율</summary>
            public float[] Coverage = Array.Empty<float>();

            /// <summary>남는 선 비율</summary>
            public float Extra = 1f;

            /// <summary>부분 일치율 평균과 (1 − 남는 선)의 평균</summary>
            public float Fit;

            /// <summary>(전체 일치율 + Fit) / 2. 시전 조건을 넘은 후보만 씀</summary>
            public float Score;
        }

        /// <summary>그린 점을 템플릿 좌표로 옮기는 변환 (가로·세로 배율 + 이동)</summary>
        readonly struct AlignSpec
        {
            /// <summary>가로 배율</summary>
            public readonly float Sx;

            /// <summary>세로 배율</summary>
            public readonly float Sy;

            /// <summary>가로 이동</summary>
            public readonly float Tx;

            /// <summary>세로 이동</summary>
            public readonly float Ty;

            #region AlignSpec - 값을 받아 만듦
            /// <param name="sx">가로 배율</param>
            /// <param name="sy">세로 배율</param>
            /// <param name="tx">가로 이동</param>
            /// <param name="ty">세로 이동</param>
            public AlignSpec(float sx, float sy, float tx, float ty)
            {
                Sx = sx;
                Sy = sy;
                Tx = tx;
                Ty = ty;
            }
            #endregion

            #region Apply - 점 하나를 옮김
            /// <param name="p">그린 점</param>
            public Vector2 Apply(Vector2 p) => new(Sx * p.x + Tx, Sy * p.y + Ty);
            #endregion
        }

        #region BuildCandidates - 비교할 그림을 만듦: 주문 그림 전부(미습득 요소가 든 것도, 이유를 보이려고)와, 도감에 등록한 주문은 생략 가능 수까지 부분을 뺀 생략본
        /// <param name="shapes">도감 주문 그림 (번호가 결과의 ShapeIndex. 빈 칸·부분 없는 그림은 건너뜀)</param>
        /// <param name="learnedElementIds">익힌 요소 id</param>
        /// <param name="collectedSpellIds">도감에 등록한 주문 id</param>
        /// <param name="omitLimit">생략 가능 수 (0이면 생략본 없음)</param>
        public static List<SpellCandidateEntry> BuildCandidates(IReadOnlyList<SpellShapeEntry> shapes, ICollection<string> learnedElementIds,
            ICollection<string> collectedSpellIds, int omitLimit)
        {
            if (shapes == null) throw new ArgumentNullException(nameof(shapes));
            if (learnedElementIds == null) throw new ArgumentNullException(nameof(learnedElementIds));
            if (collectedSpellIds == null) throw new ArgumentNullException(nameof(collectedSpellIds));
            // 만든 비교 그림
            var result = new List<SpellCandidateEntry>();
            for (int s = 0; s < shapes.Count; s++)
            {
                // 이 주문 그림
                SpellShapeEntry shape = shapes[s];
                if (shape == null || shape.PartElementIds.Count == 0) continue;
                // 요소를 다 익혔는지
                bool learned = true;
                foreach (string id in shape.PartElementIds)
                    if (!learnedElementIds.Contains(id))
                    {
                        learned = false;
                        break;
                    }
                // 부분 수
                int partCount = shape.PartElementIds.Count;
                // 뺄 수 있는 부분 수 (도감에 등록한 주문만, 부분을 다 뺄 순 없음)
                int maxOmit = collectedSpellIds.Contains(shape.Id) ? Mathf.Min(Mathf.Max(0, omitLimit), partCount - 1) : 0;
                for (int k = 0; k <= maxOmit; k++)
                    foreach (int[] omit in Combinations(partCount, k))
                        result.Add(CreateCandidate(s, shape, omit, learned));
            }
            return result;
        }
        #endregion

        #region Judge - 그린 획을 비교 그림마다 대조해 시전·취소를 정함. 돌려 본 각도마다 전체 일치율을 재고, 기준 근처 이상은 부분 검사
        /// <param name="strokes">그린 획 (그리기 칸 단위, 점 2개 이상인 획)</param>
        /// <param name="candidates">BuildCandidates로 만든 비교 그림</param>
        /// <param name="learnedElementIds">익힌 요소 id (안 익힌 요소 이유에 씀)</param>
        /// <param name="partMin">부분 기준 (0~1)</param>
        /// <param name="allMin">전체 기준 (0~1)</param>
        public static SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes, IReadOnlyList<SpellCandidateEntry> candidates,
            ICollection<string> learnedElementIds, float partMin, float allMin)
        {
            if (strokes == null) throw new ArgumentNullException(nameof(strokes));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (learnedElementIds == null) throw new ArgumentNullException(nameof(learnedElementIds));
            // 그린 점 전부
            var points = new List<Vector2>();
            foreach (Vector2[] stroke in strokes)
                if (stroke != null) points.AddRange(stroke);
            if (points.Count < 2) return SpellJudgeSpec.Cancel(SpellJudgeReason.Empty);
            // 그린 그림 테두리 상자
            Rect box = Bounds(points);
            if (Mathf.Max(box.width, box.height) < MinDrawSize) return SpellJudgeSpec.Cancel(SpellJudgeReason.Small);
            if (candidates.Count == 0) return SpellJudgeSpec.Cancel(SpellJudgeReason.NoCandidates);

            // 돌리는 중심 (그린 점 평균)
            Vector2 center = Vector2.zero;
            foreach (Vector2 p in points) center += p;
            center /= points.Count;
            // 후보마다 가장 가까운 거리와 그때 각도
            var distance = new float[candidates.Count];
            var angle = new float[candidates.Count];
            for (int i = 0; i < distance.Length; i++) distance[i] = float.PositiveInfinity;
            foreach (float turn in Turns)
            {
                // 이 각도로 돌린 그린 그림의 점 구름
                Vector2[] cloud = Normalize(Rotate(strokes, center, turn));
                for (int i = 0; i < candidates.Count; i++)
                {
                    // 이 후보와의 거리
                    float d = CloudDistance(cloud, candidates[i].Cloud);
                    if (d < distance[i])
                    {
                        distance[i] = d;
                        angle[i] = turn;
                    }
                }
            }
            // 전체 일치율 높은 순 (같으면 후보 순서)
            var ranks = new List<RankEntry>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                ranks.Add(new RankEntry { Order = i, Candidate = candidates[i], Accuracy = ToAccuracy(distance[i]), Angle = angle[i] });
            ranks.Sort(ByAccuracy);
            for (int i = 0; i < ranks.Count; i++)
            {
                ranks[i].Position = i;
                if (i == 0 || ranks[i].Accuracy >= allMin - PartCheckMargin) CheckParts(strokes, center, ranks[i]);
            }
            return Pick(ranks, learnedElementIds, partMin, allMin);
        }
        #endregion

        #region Pick - 시전 조건을 넘은 것 중 점수가 가장 높은 그림. 더 닮았는데 부분이 모자라 떨어진 주문이 있으면 덜 그린 것으로 보고 취소, 사실상 같은 그림이면 생략이 적은 쪽, 미습득·애매하면 취소
        /// <param name="ranks">전체 일치율 높은 순 후보</param>
        /// <param name="learnedElementIds">익힌 요소 id</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="allMin">전체 기준</param>
        static SpellJudgeSpec Pick(List<RankEntry> ranks, ICollection<string> learnedElementIds, float partMin, float allMin)
        {
            // 시전 조건을 넘은 후보 (점수 높은 순, 같으면 전체 일치율 순서)
            var passed = new List<RankEntry>();
            foreach (RankEntry r in ranks)
                if (Passes(r, partMin, allMin))
                {
                    r.Score = (r.Accuracy + r.Fit) / 2f;
                    passed.Add(r);
                }
            if (passed.Count == 0)
            {
                // 이유를 보일 그림 (전체 일치율 1등)
                RankEntry first = ranks[0];
                return Result(false, first.Checked && first.Accuracy >= allMin ? SpellJudgeReason.Parts : SpellJudgeReason.NoMatch, first, partMin);
            }
            passed.Sort(ByScore);
            // 점수 1등
            RankEntry top = passed[0];
            // 더 닮았는데 부분이 모자라 떨어진 주문 (전체 일치율 높은 순 첫째): 그 주문을 덜 그린 것으로 봄
            foreach (RankEntry r in ranks)
                if (r.Checked && !Passes(r, partMin, allMin) && r.Accuracy >= allMin
                    && (r.Extra < top.Extra - ExplainGap || r.Accuracy >= top.Accuracy + RivalAccuracyGap))
                    return Result(false, SpellJudgeReason.Parts, r, partMin);
            // 1등과 사실상 같은 그림 무리
            var same = new List<RankEntry>();
            foreach (RankEntry r in passed)
                if (r == top || (r.Score >= top.Score - TieScore && Similarity(r.Candidate, top.Candidate) >= SameShape)) same.Add(r);
            // 무리에서 가장 적은 생략 수
            int fewest = int.MaxValue;
            foreach (RankEntry r in same) fewest = Mathf.Min(fewest, r.Candidate.Omit.Count);
            // 생략이 가장 적은 그림들, 그중 요소를 다 익힌 것
            var group = new List<RankEntry>();
            var usable = new List<RankEntry>();
            foreach (RankEntry r in same)
            {
                if (r.Candidate.Omit.Count != fewest) continue;
                group.Add(r);
                if (r.Candidate.Learned) usable.Add(r);
            }
            if (usable.Count == 0)
                return Result(false, SpellJudgeReason.Unlearned, group[0], partMin, MissingElements(group[0].Candidate.Shape, learnedElementIds));
            // 겹친 주문 번호 (처음 나온 순서)
            var shapes = new List<int>();
            foreach (RankEntry r in usable)
                if (!shapes.Contains(r.Candidate.ShapeIndex)) shapes.Add(r.Candidate.ShapeIndex);
            if (shapes.Count > 1) return Result(false, SpellJudgeReason.Ambiguous, usable[0], partMin, null, shapes);
            return Result(true, SpellJudgeReason.None, usable[0], partMin);
        }
        #endregion

        #region Result - 후보 점수로 결과를 만듦
        /// <param name="isCast">시전했는지</param>
        /// <param name="reason">취소 이유</param>
        /// <param name="rank">결과에 담을 후보</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="missing">안 익힌 요소 id</param>
        /// <param name="ambiguous">같은 그림인 주문 번호</param>
        static SpellJudgeSpec Result(bool isCast, SpellJudgeReason reason, RankEntry rank, float partMin,
            IReadOnlyList<string> missing = null, IReadOnlyList<int> ambiguous = null) =>
            new(isCast, reason, rank.Candidate, rank.Accuracy, rank.Checked ? rank.Coverage : null, rank.Extra, missing, ambiguous, partMin);
        #endregion

        #region Passes - 시전 조건: 부분 검사를 했고 전체 일치율 ≥ 전체 기준, 부분마다 ≥ 부분 기준, 남는 선 ≤ 한도
        /// <param name="rank">볼 후보</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="allMin">전체 기준</param>
        static bool Passes(RankEntry rank, float partMin, float allMin)
        {
            if (!rank.Checked || rank.Accuracy < allMin || rank.Extra > MaxExtra) return false;
            foreach (float coverage in rank.Coverage)
                if (coverage < partMin) return false;
            return true;
        }
        #endregion

        #region ByAccuracy - 전체 일치율 높은 순, 같으면 후보 순서
        /// <param name="x">앞 후보</param>
        /// <param name="y">뒤 후보</param>
        static int ByAccuracy(RankEntry x, RankEntry y) => x.Accuracy != y.Accuracy ? y.Accuracy.CompareTo(x.Accuracy) : x.Order.CompareTo(y.Order);
        #endregion

        #region ByScore - 점수 높은 순, 같으면 전체 일치율 순 자리
        /// <param name="x">앞 후보</param>
        /// <param name="y">뒤 후보</param>
        static int ByScore(RankEntry x, RankEntry y) => x.Score != y.Score ? y.Score.CompareTo(x.Score) : x.Position.CompareTo(y.Position);
        #endregion

        #region MissingElements - 주문 부분 요소 중 안 익힌 것 (부분 순서)
        /// <param name="shape">주문 그림</param>
        /// <param name="learnedElementIds">익힌 요소 id</param>
        static List<string> MissingElements(SpellShapeEntry shape, ICollection<string> learnedElementIds)
        {
            // 안 익힌 요소 id
            var missing = new List<string>();
            foreach (string id in shape.PartElementIds)
                if (!learnedElementIds.Contains(id)) missing.Add(id);
            return missing;
        }
        #endregion

        #region Similarity - 비교 그림끼리 닮음 (정확도 눈금)
        /// <param name="a">비교 그림</param>
        /// <param name="b">비교 그림</param>
        static float Similarity(SpellCandidateEntry a, SpellCandidateEntry b) => ToAccuracy(CloudDistance(a.Cloud, b.Cloud));
        #endregion

        #region CheckParts - 후보의 부분 검사: 그 각도로 돌린 그림을 템플릿 위에 맞추고, 부분마다 덮인 비율과 남는 선을 잼
        /// <param name="strokes">그린 획</param>
        /// <param name="center">돌리는 중심</param>
        /// <param name="rank">검사할 후보 (결과를 여기에 넣음)</param>
        static void CheckParts(IReadOnlyList<Vector2[]> strokes, Vector2 center, RankEntry rank)
        {
            // 후보 각도로 돌린 그린 획
            List<Vector2[]> turned = Rotate(strokes, center, rank.Angle);
            // 비교 그림
            SpellCandidateEntry tp = rank.Candidate;
            rank.Checked = true;
            if (!Align(ResampleStrokes(turned, AlignPoints), tp, out AlignSpec fit))
            {
                rank.Coverage = new float[tp.PartElementIds.Count];
                rank.Extra = 1f;
                rank.Fit = 0f;
                return;
            }
            Cover(ResampleStrokes(turned, CoverPoints), fit, tp, out float[] coverage, out float extra);
            rank.Coverage = coverage;
            rank.Extra = extra;
            // 부분 일치율 평균
            float mean = 0f;
            foreach (float c in coverage) mean += c;
            mean /= coverage.Length;
            rank.Fit = (mean + 1f - extra) / 2f;
        }
        #endregion

        #region Align - 그린 점을 템플릿 좌표로 옮기는 변환을 맞춤 (ICP): 짝이 먼 15%는 버리고 가로·세로 배율과 이동을 되풀이해 맞춤. 시작 배율 세 가지 중 남는 거리(+비율 벌점)가 가장 작은 것. 못 맞추면 false
        /// <param name="drawn">그린 점 (다시 뽑은 것)</param>
        /// <param name="tp">비교 그림</param>
        /// <param name="result">맞춘 변환</param>
        static bool Align(Vector2[] drawn, SpellCandidateEntry tp, out AlignSpec result)
        {
            result = default;
            // 그린 점·템플릿 테두리 상자
            Rect db = Bounds(drawn);
            Rect tb = tp.Bounds;
            // 그린 그림 가로·세로 (0이면 아주 작은 값)
            float dw = db.width > 0f ? db.width : 1e-6f;
            float dh = db.height > 0f ? db.height : 1e-6f;
            // 시작 배율 세 가지 (가로세로 따로, 세로 기준, 가로 기준)
            Vector2[] starts =
            {
                new(tb.width / dw, tb.height / dh),
                new(tb.height / dh, tb.height / dh),
                new(tb.width / dw, tb.width / dw),
            };
            // 그린 점 수, 남길 짝 수
            int n = drawn.Length;
            int keep = Mathf.Min(n, Mathf.Max(3, Mathf.FloorToInt(n * AlignKeep)));
            // 짝 거리 제곱, 짝 템플릿 점, 거리 순 번호 (되풀이마다 다시 씀)
            var dist = new float[n];
            var nearest = new Vector2[n];
            var order = new int[n];
            // 지금까지 가장 작은 벌점
            float bestPenalty = float.PositiveInfinity;
            bool found = false;
            foreach (Vector2 start in starts)
            {
                if (!(start.x > 0f && start.y > 0f) || float.IsInfinity(start.x) || float.IsInfinity(start.y)) continue;
                // 시작 배율 크기
                float s0 = Mathf.Sqrt(start.x * start.y);
                // 지금 변환
                float sx = start.x, sy = start.y;
                float tx = tb.center.x - sx * db.center.x, ty = tb.center.y - sy * db.center.y;
                // 남긴 짝의 평균 거리 제곱
                float residual = float.PositiveInfinity;
                for (int it = 0; it < AlignIterations; it++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        // 지금 변환으로 옮긴 그린 점
                        float x = sx * drawn[i].x + tx, y = sy * drawn[i].y + ty;
                        float m = float.PositiveInfinity;
                        Vector2 q = default;
                        foreach (Vector2 t in tp.Sparse)
                        {
                            float dx = x - t.x, dy = y - t.y, d = dx * dx + dy * dy;
                            if (d < m)
                            {
                                m = d;
                                q = t;
                            }
                        }
                        dist[i] = m;
                        nearest[i] = q;
                        order[i] = i;
                    }
                    Array.Sort(order, (a, b) => dist[a] != dist[b] ? dist[a].CompareTo(dist[b]) : a.CompareTo(b));
                    residual = 0f;
                    for (int k = 0; k < keep; k++) residual += dist[order[k]];
                    residual /= keep;
                    FitAxis(drawn, nearest, order, keep, 0, out float mpx, out float mqx, out float fitX);
                    FitAxis(drawn, nearest, order, keep, 1, out float mpy, out float mqy, out float fitY);
                    // 새 배율 (한쪽이 0 이하면 다른 쪽)
                    float nsx = fitX > 0f ? fitX : fitY;
                    float nsy = fitY > 0f ? fitY : fitX;
                    if (!(nsx > 0f && nsy > 0f)) break;
                    nsx = Mathf.Clamp(nsx, s0 * AlignScaleMin, s0 * AlignScaleMax);
                    nsy = Mathf.Clamp(nsy, s0 * AlignScaleMin, s0 * AlignScaleMax);
                    if (nsx / nsy > MaxAspect) nsx = nsy * MaxAspect;
                    else if (nsx / nsy < 1f / MaxAspect) nsx = nsy / MaxAspect;
                    // 새 이동
                    float ntx = mqx - nsx * mpx, nty = mqy - nsy * mpy;
                    // 이번에 바뀐 양
                    float moved = Mathf.Abs(ntx - tx) + Mathf.Abs(nty - ty) + (Mathf.Abs(nsx - sx) + Mathf.Abs(nsy - sy)) * Mathf.Max(dw, dh);
                    sx = nsx;
                    sy = nsy;
                    tx = ntx;
                    ty = nty;
                    if (moved < 1e-3f * tp.Size) break;
                }
                // 가로세로를 다르게 늘린 만큼 벌점 (덜 그린 그림을 늘려 맞추는 것보다 비율을 지킨 쪽)
                float penalty = residual * (1f + 2f * Mathf.Abs(Mathf.Log(sx / sy)));
                if (found && penalty >= bestPenalty) continue;
                found = true;
                bestPenalty = penalty;
                result = new AlignSpec(sx, sy, tx, ty);
            }
            return found;
        }
        #endregion

        #region FitAxis - 한 축의 최소제곱 배율: 남긴 짝의 그린 값 평균·템플릿 값 평균, 공분산/분산 (분산이 거의 0이면 0)
        /// <param name="drawn">그린 점</param>
        /// <param name="nearest">짝 템플릿 점</param>
        /// <param name="order">거리 순 번호</param>
        /// <param name="keep">남길 짝 수</param>
        /// <param name="axis">0 가로, 1 세로</param>
        /// <param name="meanP">그린 값 평균</param>
        /// <param name="meanQ">템플릿 값 평균</param>
        /// <param name="scale">배율</param>
        static void FitAxis(Vector2[] drawn, Vector2[] nearest, int[] order, int keep, int axis, out float meanP, out float meanQ, out float scale)
        {
            meanP = 0f;
            meanQ = 0f;
            for (int k = 0; k < keep; k++)
            {
                meanP += drawn[order[k]][axis];
                meanQ += nearest[order[k]][axis];
            }
            meanP /= keep;
            meanQ /= keep;
            // 공분산·분산 합
            float covariance = 0f, variance = 0f;
            for (int k = 0; k < keep; k++)
            {
                // 그린 값 편차
                float a = drawn[order[k]][axis] - meanP;
                covariance += a * (nearest[order[k]][axis] - meanQ);
                variance += a * a;
            }
            scale = variance > 1e-9f ? covariance / variance : 0f;
        }
        #endregion

        #region Cover - 그린 점마다 가장 가까운 템플릿 부분에 배정하고, 부분 템플릿 점은 그 부분에 배정된 그린 점이 거리 안에 있을 때만 덮인 걸로 침 (다른 부분 선이 대신 채우지 않게). 부분마다 덮인 비율과 남는 선
        /// <param name="drawn">그린 점 (다시 뽑은 것)</param>
        /// <param name="fit">맞춘 변환</param>
        /// <param name="tp">비교 그림</param>
        /// <param name="coverage">부분마다 덮인 비율</param>
        /// <param name="extra">남는 선 비율</param>
        static void Cover(Vector2[] drawn, AlignSpec fit, SpellCandidateEntry tp, out float[] coverage, out float extra)
        {
            // 따라 그린 걸로 칠 거리 제곱
            float r2 = CoverRadius * tp.Size * (CoverRadius * tp.Size);
            // 부분 수
            int parts = tp.PartElementIds.Count;
            // 부분마다 배정된 그린 점 (템플릿 좌표)
            var byPart = new List<Vector2>[parts];
            for (int j = 0; j < parts; j++) byPart[j] = new List<Vector2>();
            // 어느 템플릿 점과도 거리 밖인 그린 점 수
            int off = 0;
            foreach (Vector2 p in drawn)
            {
                // 템플릿 좌표로 옮긴 점
                Vector2 moved = fit.Apply(p);
                float m = float.PositiveInfinity;
                int part = 0;
                for (int k = 0; k < tp.Dense.Length; k++)
                {
                    float d = (moved - tp.Dense[k]).sqrMagnitude;
                    if (d < m)
                    {
                        m = d;
                        part = tp.DenseParts[k];
                    }
                }
                if (m > r2) off++;
                else byPart[part].Add(moved);
            }
            // 부분마다 덮인 템플릿 점 수·전체 수
            var hit = new int[parts];
            var total = new int[parts];
            for (int k = 0; k < tp.Dense.Length; k++)
            {
                int part = tp.DenseParts[k];
                total[part]++;
                if (NearestSq(tp.Dense[k], byPart[part]) <= r2) hit[part]++;
            }
            coverage = new float[parts];
            for (int j = 0; j < parts; j++) coverage[j] = total[j] > 0 ? (float)hit[j] / total[j] : 0f;
            extra = drawn.Length > 0 ? (float)off / drawn.Length : 1f;
        }
        #endregion

        #region NearestSq - 점 목록 중 가장 가까운 점까지 거리 제곱 (목록이 비면 무한대)
        /// <param name="point">기준 점</param>
        /// <param name="points">찾을 점 목록</param>
        static float NearestSq(Vector2 point, List<Vector2> points)
        {
            // 지금까지 가장 가까운 거리 제곱
            float m = float.PositiveInfinity;
            foreach (Vector2 q in points) m = Mathf.Min(m, (point - q).sqrMagnitude);
            return m;
        }
        #endregion

        #region CreateCandidate - 남길 부분으로 점 구름·부분 점·테두리 상자를 계산해 비교 그림을 만듦
        /// <param name="shapeIndex">주문 그림 목록 번호</param>
        /// <param name="shape">주문 그림</param>
        /// <param name="omit">뺄 부분 번호</param>
        /// <param name="learned">요소를 다 익혔는지</param>
        static SpellCandidateEntry CreateCandidate(int shapeIndex, SpellShapeEntry shape, int[] omit, bool learned)
        {
            // 남긴 부분의 요소 id, 획
            var keptIds = new List<string>();
            var keptParts = new List<Vector2[][]>();
            for (int i = 0; i < shape.PartElementIds.Count; i++)
            {
                if (Array.IndexOf(omit, i) >= 0) continue;
                keptIds.Add(shape.PartElementIds[i]);
                keptParts.Add(shape.PartStrokes[i]);
            }
            // 남긴 부분의 획 전부
            var strokes = new List<Vector2[]>();
            foreach (Vector2[][] part in keptParts) strokes.AddRange(part);
            PartPoints(keptParts, DenseStep, out Vector2[] dense, out int[] denseParts);
            PartPoints(keptParts, SparseStep, out Vector2[] sparse, out _);
            // 촘촘한 점 테두리 상자
            Rect bounds = Bounds(dense);
            // 테두리 상자 긴 변 (0이면 1)
            float size = Mathf.Max(bounds.width, bounds.height);
            if (!(size > 0f)) size = 1f;
            return new SpellCandidateEntry(shapeIndex, shape, omit, learned, Normalize(strokes), keptIds.ToArray(), dense, denseParts, sparse, bounds, size);
        }
        #endregion

        #region PartPoints - 부분 검사용 템플릿 점: 부분 번호를 붙여 획마다 간격 step으로 다시 뽑음
        /// <param name="parts">부분마다 획</param>
        /// <param name="step">점 간격 (주문 그림 좌표)</param>
        /// <param name="points">템플릿 점</param>
        /// <param name="partIndices">점마다 부분 번호</param>
        static void PartPoints(IReadOnlyList<Vector2[][]> parts, float step, out Vector2[] points, out int[] partIndices)
        {
            // 뽑은 점, 부분 번호
            var pts = new List<Vector2>();
            var indices = new List<int>();
            for (int j = 0; j < parts.Count; j++)
                foreach (Vector2[] stroke in parts[j])
                {
                    if (stroke == null || stroke.Length == 0) continue;
                    // 이 획에서 뽑을 점 수
                    int count = Mathf.Max(2, Mathf.CeilToInt(PathLength(stroke) / step) + 1);
                    foreach (Vector2 q in Resample(stroke, count))
                    {
                        pts.Add(q);
                        indices.Add(j);
                    }
                }
            points = pts.ToArray();
            partIndices = indices.ToArray();
        }
        #endregion

        #region Combinations - 0~n-1에서 k개 고르는 조합 (오름차순, 사전 순)
        /// <param name="n">고를 수의 범위</param>
        /// <param name="k">고를 개수</param>
        static List<int[]> Combinations(int n, int k)
        {
            // 만든 조합
            var result = new List<int[]>();
            AddCombinations(0, n, k, new List<int>(k), result);
            return result;
        }
        #endregion

        #region AddCombinations - 조합 되부름: 지금까지 고른 것 뒤로 하나씩 더함
        /// <param name="start">이번에 고를 수의 시작</param>
        /// <param name="n">고를 수의 범위</param>
        /// <param name="k">고를 개수</param>
        /// <param name="current">지금까지 고른 수</param>
        /// <param name="result">다 고른 조합을 넣을 목록</param>
        static void AddCombinations(int start, int n, int k, List<int> current, List<int[]> result)
        {
            if (current.Count == k)
            {
                result.Add(current.ToArray());
                return;
            }
            for (int i = start; i < n; i++)
            {
                current.Add(i);
                AddCombinations(i + 1, n, k, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
        #endregion

        #region PathLength - 점 줄 길이 (null이면 0)
        /// <param name="points">점 줄</param>
        public static float PathLength(IReadOnlyList<Vector2> points)
        {
            if (points == null) return 0f;
            // 지금까지 길이
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }
        #endregion

        #region Resample - 획 하나를 길이 기준 count개 점으로 다시 뽑음 (길이가 0이면 첫 점 count개)
        /// <param name="points">획 (점 1개 이상)</param>
        /// <param name="count">뽑을 점 수 (2 이상)</param>
        public static Vector2[] Resample(IReadOnlyList<Vector2> points, int count)
        {
            if (points == null || points.Count == 0) throw new ArgumentException("빈 획", nameof(points));
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), count, "점 수는 2 이상");
            // 다시 뽑는 중인 점 (사이에 새 점을 끼워 넣음)
            var pts = new List<Vector2>(points);
            // 점 사이 간격
            float interval = PathLength(pts) / (count - 1);
            if (!(interval > 0f)) return Fill(pts[0], count);
            // 뽑은 점
            var result = new List<Vector2>(count) { pts[0] };
            // 마지막으로 뽑은 뒤 지난 길이
            float walked = 0f;
            Walk(pts, interval, ref walked, result);
            return Finish(result, pts[pts.Count - 1], count);
        }
        #endregion

        #region ResampleStrokes - 여러 획을 한 줄처럼 길이 기준 count개 점으로 (길이는 획 안에서만, 획 사이는 안 이음. 그리는 순서·방향·획 수와 상관없음)
        /// <param name="strokes">획 목록</param>
        /// <param name="count">뽑을 점 수 (2 이상)</param>
        public static Vector2[] ResampleStrokes(IReadOnlyList<Vector2[]> strokes, int count)
        {
            if (strokes == null) throw new ArgumentNullException(nameof(strokes));
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), count, "점 수는 2 이상");
            // 점이 있는 첫 획
            Vector2[] first = null;
            foreach (Vector2[] stroke in strokes)
                if (stroke != null && stroke.Length > 0)
                {
                    first = stroke;
                    break;
                }
            if (first == null) return Fill(Vector2.zero, count);
            // 모든 획 길이 합
            float total = 0f;
            foreach (Vector2[] stroke in strokes) total += PathLength(stroke);
            if (!(total > 0f)) return Fill(first[0], count);
            // 점 사이 간격
            float interval = total / (count - 1);
            // 뽑은 점
            var result = new List<Vector2>(count) { first[0] };
            // 마지막으로 뽑은 뒤 지난 길이 (획을 넘어 이어짐)
            float walked = 0f;
            // 마지막 획의 끝 점
            Vector2 last = first[0];
            foreach (Vector2[] stroke in strokes)
            {
                if (stroke == null || stroke.Length == 0) continue;
                // 이 획 점 (사이에 새 점을 끼워 넣음)
                var pts = new List<Vector2>(stroke);
                Walk(pts, interval, ref walked, result);
                last = pts[pts.Count - 1];
            }
            return Finish(result, last, count);
        }
        #endregion

        #region Walk - 점 줄을 따라가며 간격마다 점을 뽑아 넣음 (뽑은 자리를 줄에 끼워 거기서 다시 셈)
        /// <param name="pts">따라갈 점 줄 (뽑은 점이 끼워짐)</param>
        /// <param name="interval">점 사이 간격</param>
        /// <param name="walked">마지막으로 뽑은 뒤 지난 길이 (이어서 씀)</param>
        /// <param name="result">뽑은 점을 넣을 목록</param>
        static void Walk(List<Vector2> pts, float interval, ref float walked, List<Vector2> result)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                // 이번 선분
                Vector2 a = pts[i - 1], b = pts[i];
                float d = Vector2.Distance(a, b);
                if (walked + d >= interval && d > 0f)
                {
                    // 간격이 차는 자리
                    Vector2 q = a + (interval - walked) / d * (b - a);
                    result.Add(q);
                    pts.Insert(i, q);
                    walked = 0f;
                }
                else
                {
                    walked += d;
                }
            }
        }
        #endregion

        #region Finish - 모자라면 마지막 점으로 채우고 넘치면 자름 (반올림 오차)
        /// <param name="result">뽑은 점</param>
        /// <param name="last">채울 점</param>
        /// <param name="count">점 수</param>
        static Vector2[] Finish(List<Vector2> result, Vector2 last, int count)
        {
            while (result.Count < count) result.Add(last);
            if (result.Count > count) result.RemoveRange(count, result.Count - count);
            return result.ToArray();
        }
        #endregion

        #region Fill - 같은 점 count개
        /// <param name="point">채울 점</param>
        /// <param name="count">점 수</param>
        static Vector2[] Fill(Vector2 point, int count)
        {
            // 같은 점 배열
            var result = new Vector2[count];
            for (int i = 0; i < count; i++) result[i] = point;
            return result;
        }
        #endregion

        #region Normalize - 점 구름 정규화: CloudPoints개로 다시 뽑기 → 가로·세로를 따로 0~1로 (얇은 그림은 같은 비율) → 무게중심을 원점으로
        /// <param name="strokes">획 목록</param>
        public static Vector2[] Normalize(IReadOnlyList<Vector2[]> strokes)
        {
            // 다시 뽑은 점
            Vector2[] points = ResampleStrokes(strokes, CloudPoints);
            // 테두리 상자
            Rect box = Bounds(points);
            // 맞출 가로·세로 (0이면 1)
            float w = box.width, h = box.height;
            if (w == 0f) w = 1f;
            if (h == 0f) h = 1f;
            if (Mathf.Min(w, h) / Mathf.Max(w, h) < ThinRatio) w = h = Mathf.Max(w, h);
            // 무게중심
            Vector2 mean = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2((points[i].x - box.xMin) / w, (points[i].y - box.yMin) / h);
                mean += points[i];
            }
            mean /= points.Length;
            for (int i = 0; i < points.Length; i++) points[i] -= mean;
            return points;
        }
        #endregion

        #region CloudDistance - $P 점 구름 거리: 시작 점을 √n칸씩 바꿔 가며 양방향 탐욕 매칭 중 가장 작은 값
        /// <param name="a">점 구름 (점 수가 b와 같음)</param>
        /// <param name="b">점 구름</param>
        public static float CloudDistance(Vector2[] a, Vector2[] b)
        {
            if (a == null || b == null || a.Length == 0 || a.Length != b.Length) throw new ArgumentException("점 수가 같은 점 구름 둘이 필요");
            // 시작 점을 바꿀 간격
            int step = Mathf.Max(1, (int)Math.Floor(Math.Sqrt(a.Length)));
            // 지금까지 가장 작은 거리
            float best = float.PositiveInfinity;
            for (int i = 0; i < a.Length; i += step) best = Mathf.Min(best, Mathf.Min(GreedyCloud(a, b, i), GreedyCloud(b, a, i)));
            return best;
        }
        #endregion

        #region GreedyCloud - a의 start부터 차례로 b에서 아직 안 쓴 가장 가까운 점과 짝짓고, 늦게 짝지을수록 가중치를 줄여 거리를 더함
        /// <param name="a">짝을 찾는 쪽</param>
        /// <param name="b">짝이 되는 쪽</param>
        /// <param name="start">a의 시작 점 번호</param>
        static float GreedyCloud(Vector2[] a, Vector2[] b, int start)
        {
            // 점 수
            int n = a.Length;
            // b에서 이미 짝지은 점
            var used = new bool[n];
            // 가중 거리 합
            float sum = 0f;
            // 지금 a 점 번호
            int i = start;
            do
            {
                // 가장 가까운 안 쓴 b 점과 거리
                int pick = 0;
                float nearest = float.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    if (used[j]) continue;
                    float d = Vector2.Distance(a[i], b[j]);
                    if (d < nearest)
                    {
                        nearest = d;
                        pick = j;
                    }
                }
                used[pick] = true;
                sum += (1f - (float)((i - start + n) % n) / n) * nearest;
                i = (i + 1) % n;
            } while (i != start);
            return sum;
        }
        #endregion

        #region ToAccuracy - $P 거리를 정확도(0~1)로: 점 수 비례로 32점 기준 환산 뒤 v1 눈금
        /// <param name="distance">CloudDistance 값</param>
        public static float ToAccuracy(float distance) =>
            Mathf.Clamp01(1f - (distance * ScalePoints / CloudPoints - AccuracyFloor) / AccuracySpan);
        #endregion

        #region Rotate - 획을 중심 기준으로 돌림 (돌린 점은 중심이 원점)
        /// <param name="strokes">획 목록</param>
        /// <param name="center">돌리는 중심</param>
        /// <param name="angle">각도 (라디안)</param>
        static List<Vector2[]> Rotate(IReadOnlyList<Vector2[]> strokes, Vector2 center, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            // 돌린 획
            var result = new List<Vector2[]>(strokes.Count);
            foreach (Vector2[] stroke in strokes)
            {
                if (stroke == null) continue;
                // 이 획을 돌린 점
                var turned = new Vector2[stroke.Length];
                for (int i = 0; i < stroke.Length; i++)
                {
                    Vector2 d = stroke[i] - center;
                    turned[i] = new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
                }
                result.Add(turned);
            }
            return result;
        }
        #endregion

        #region Bounds - 점들의 테두리 상자
        /// <param name="points">점 목록 (1개 이상)</param>
        static Rect Bounds(IReadOnlyList<Vector2> points)
        {
            float x0 = float.PositiveInfinity, y0 = float.PositiveInfinity, x1 = float.NegativeInfinity, y1 = float.NegativeInfinity;
            foreach (Vector2 p in points)
            {
                x0 = Mathf.Min(x0, p.x);
                y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x);
                y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
        #endregion
    }
}
```

**호출 관계:**
- `CodexController.RebuildCandidates` → `BuildCandidates` (책을 열 때·요소 습득·도감 등록 뒤)
- `CodexController.Judge` → `Judge` (`SpellDrawController`가 판정 때 부름)
- `SpellRecognitionCalculatorTests` → `BuildCandidates`·`Judge`·`Resample`

---

### `Assets/Scripts/Core/RunEntry.cs`

**변경 내용:** 도감 등록 주문·가진 요소 주문서·가진 주문 주문서·생략 가능 수 추가. 디버그 판은 요소 앞에서부터 생성 화면 시작 수(4)만 익히고 나머지 요소는 요소 주문서로, 안 익힌 요소가 든 주문은 주문 주문서로 줌 (책상 확인용)

`StartGold` 아래:

```csharp
        /// <summary>디버그 판에서 처음부터 익히는 요소 수 (요소 목록 앞에서부터. 생성 화면 시작 요소 수와 같음, 임시)</summary>
        const int DebugLearnedCount = CharacterCreationState.StartElementCount;
```

`CurrentObjective` 아래:

```csharp
        /// <summary>도감에 등록한 주문 (생략 없이 처음 완성·주문서를 다 읽음. 등록 순서). 생략은 등록한 주문에만</summary>
        public List<SpellSO> CollectedSpells { get; } = new();

        /// <summary>가진 요소 주문서 (아직 다 안 읽은 것. 다 읽으면 빠짐)</summary>
        public List<ElementSO> ElementScrolls { get; } = new();

        /// <summary>가진 주문 주문서 (아직 다 안 읽은 것. 다 읽으면 빠짐)</summary>
        public List<SpellSO> SpellScrolls { get; } = new();

        /// <summary>생략 가능 수 (레벨이 생기면 레벨이 정함. 지금은 프로토타입 기본 0)</summary>
        public int OmitLimit { get; set; }
```

`CreateDebug` region 통째로 바꿈:

```csharp
        #region CreateDebug - 에디터에서 중간 씬부터 Play할 때 쓰는 판 데이터 (도착한 판). 요소는 앞에서부터 생성 화면 시작 수만큼 익히고 나머지는 요소 주문서로, 안 익힌 요소가 든 주문은 주문 주문서로 줌
        /// <param name="elements">요소 목록 (DataManager 순서, 빈 칸은 건너뜀)</param>
        /// <param name="spells">주문 목록 (빈 칸은 건너뜀)</param>
        public static RunEntry CreateDebug(IReadOnlyList<ElementSO> elements, IReadOnlyList<SpellSO> spells)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (spells == null) throw new ArgumentNullException(nameof(spells));
            // 디버그 판 데이터. 빌드에선 늘 Title부터 시작해서 이 이름이 화면에 안 나옴
            var run = new RunEntry { PlayerName = "디버그 마법사", HasArrived = true };
            foreach (ElementSO element in elements)
            {
                if (element == null) continue;
                if (run.LearnedElements.Count < DebugLearnedCount) run.LearnedElements.Add(element);
                else run.ElementScrolls.Add(element);
            }
            foreach (SpellSO spell in spells)
            {
                if (spell == null) continue;
                foreach (SpellPartEntry part in spell.Parts)
                    if (part.Element != null && !run.LearnedElements.Contains(part.Element))
                    {
                        run.SpellScrolls.Add(spell);
                        break;
                    }
            }
            return run;
        }
        #endregion
```

---

### `Assets/Scripts/Managers/DataManager.cs`

**변경 내용:** 주문 목록 칸(Boot에서 연결)과 시작 때 검사, id로 요소 찾기

`Quests` 프로퍼티 아래:

```csharp
        /// <summary>도감 주문 전부 (Boot에서 연결. 이 순서가 인식기 비교 순서)</summary>
        [SerializeField] SpellSO[] _spells;

        /// <summary>도감 주문 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<SpellSO> Spells => _spells;
```

`InitSingleton`: region 설명을 `팁·튜토리얼·직업·의뢰·주문 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침`으로, `ValidateQuests();` 다음 줄에 추가:

```csharp
            ValidateSpells();
```

`FindTutorial` region 아래:

```csharp
        #region FindElement - id로 요소 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">요소 id</param>
        public ElementSO FindElement(string id)
        {
            if (string.IsNullOrEmpty(id) || _elements == null) return null;
            foreach (ElementSO element in _elements)
                if (element != null && string.Equals(element.Id, id, StringComparison.Ordinal)) return element;
            return null;
        }
        #endregion
```

`ValidateQuests` region 아래:

```csharp
        #region ValidateSpells - 주문 목록 검사: 비었으면 경고(책상 연습에 비교할 그림이 없음), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔, 그림 부분 없음, 부분 검사
        void ValidateSpells()
        {
            if (_spells == null || _spells.Length == 0)
            {
                Debug.LogWarning("[Data] 주문 목록 비어 있음. 책상 연습에 비교할 그림이 없음");
                _spells = Array.Empty<SpellSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _spells.Length; i++)
            {
                // 검사할 주문
                SpellSO spell = _spells[i];
                if (spell == null)
                    Debug.LogError($"[Data] 주문 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(spell.Id))
                    Debug.LogError($"[Data] {spell.name}의 id 비어 있음");
                else if (!ids.Add(spell.Id))
                    Debug.LogError($"[Data] 주문 id 겹침: {spell.Id} ({spell.name})");
                else if (spell.Name.IsEmpty || spell.Description.IsEmpty)
                    Debug.LogError($"[Data] {spell.name}의 이름·설명 키 비어 있음");
                else if (spell.Parts.Count == 0)
                    Debug.LogError($"[Data] {spell.name}의 그림 부분 없음");
                else
                    ValidateParts(spell);
            }
        }
        #endregion

        #region ValidateParts - 주문 그림 부분 검사: 요소 빔·요소 목록에 없음·같은 요소 두 번, 획 없음·점 2개 미만 획
        /// <param name="spell">검사할 주문</param>
        void ValidateParts(SpellSO spell)
        {
            // 이 주문에서 이미 본 요소
            var seen = new HashSet<ElementSO>();
            foreach (SpellPartEntry part in spell.Parts)
            {
                if (part.Element == null)
                {
                    Debug.LogError($"[Data] {spell.name}의 그림 부분 요소 비어 있음");
                    continue;
                }
                if (_elements == null || Array.IndexOf(_elements, part.Element) < 0)
                    Debug.LogError($"[Data] {spell.name}의 {part.Element.name}가 요소 목록에 없음");
                if (!seen.Add(part.Element))
                    Debug.LogError($"[Data] {spell.name}에 {part.Element.name} 부분이 둘 이상");
                if (part.Strokes.Count == 0)
                    Debug.LogError($"[Data] {spell.name}의 {part.Element.name} 부분에 획 없음");
                foreach (StrokeEntry stroke in part.Strokes)
                    if (stroke.Points.Count < 2)
                    {
                        Debug.LogError($"[Data] {spell.name}의 {part.Element.name} 부분에 점 2개 미만 획");
                        break;
                    }
            }
        }
        #endregion
```

---

### `Assets/Scripts/Managers/GameManager.cs`

**변경 내용:** 디버그 판에 주문 목록을 넘김, Esc를 상태에 먼저 넘기는 길

`Start` 안:

```csharp
            Run = start is TitleState or CharacterCreationState ? new RunEntry() : RunEntry.CreateDebug(DataManager.Instance.Elements, DataManager.Instance.Spells);   // ← 주문 목록 추가
```

`SetMenuPaused` region 아래:

```csharp
        #region HandleCancel - 열린 공용 창이 없을 때 Esc를 지금 상태에 먼저 넘김 (전환 중엔 안 넘김). 상태가 처리했으면 true (UIManager가 부름)
        public bool HandleCancel() => !_isSwitching && _state != null && _state.HandleCancel();
        #endregion
```

---

### `Assets/Scripts/Flow/FlowStateBase.cs`

**변경 내용:** `Exit` region 아래에 Esc 받기 자리

```csharp
        #region HandleCancel - 열린 공용 창이 없을 때 Esc를 상태가 먼저 받음. 처리했으면 true (일시정지 메뉴를 안 엶). 기본은 처리 안 함
        public virtual bool HandleCancel() => false;
        #endregion
```

---

### `Assets/Scripts/Managers/UIManager.cs`

**변경 내용:** Esc가 일시정지 메뉴를 열기 전에 상태에 먼저 물어봄, 지금 언어의 목록 구분자로 글을 잇는 `JoinList`

`ToTitleConfirmKey` 아래:

```csharp
        /// <summary>목록 이름 사이 구분자 키 (UI 테이블, 끝 공백 포함)</summary>
        const string ListSeparatorKey = "common.list_separator";
```

`OnCancel` region 통째로 바꿈:

```csharp
        #region OnCancel - Esc: 확인창 → 설정 창 → 대화 → 영입 창 → 게시판 창 → 메뉴 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸으면 상태가 먼저 받고(책상에서 일어나기 등), 상태가 안 받았고 플레이 씬이면 일시정지 메뉴를 엶
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancel(InputAction.CallbackContext context)
        {
            if (_confirm != null && _confirm.gameObject.activeSelf)
                HideConfirm();
            else if (_settings != null && _settings.gameObject.activeSelf)
                HideSettings();
            else if (_dialogue != null && _dialogue.IsOpen)
                HideDialogue();
            else if (_recruit != null && _recruit.IsOpen)
                HideRecruit();
            else if (_questBoard != null && _questBoard.IsOpen)
                HideQuestBoard();
            else if (_gameMenu != null && _gameMenu.IsOpen)
                HideGameMenu();
            else if (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
                HidePauseMenu();
            else if (GameManager.Instance.HandleCancel())
            {
                // 상태가 처리함. 일시정지 메뉴는 안 엶
            }
            else if (_pauseMenu != null && GameManager.Instance.CanPause)
                ShowPauseMenu();
        }
        #endregion
```

두 번째 `ShowToast` region 아래:

```csharp
        #region JoinList - 지금 언어의 목록 구분자(UI/common.list_separator)로 글을 이음. 빈 글은 뺌
        /// <param name="items">이을 글 (이미 지금 언어로 읽은 것)</param>
        public string JoinList(IEnumerable<string> items)
        {
            if (items == null) return string.Empty;
            // 지금 언어 목록 구분자
            string separator = new LocalizedString(UiTable, ListSeparatorKey).GetLocalizedString();
            // 빈 글을 뺀 목록
            var kept = new List<string>();
            foreach (string item in items)
                if (!string.IsNullOrEmpty(item)) kept.Add(item);
            return string.Join(separator, kept);
        }
        #endregion
```

**호출 관계:** `SpellDrawController`(부분·요소·주문 이름 목록), `CodexController`·`SpellCodexUIBinder`(필요한 요소 목록) → `JoinList`

---

### `Assets/Scripts/Tutorial/TutorialSO.cs`

**변경 내용:** `TutorialAction` 끝에 행동 하나 (끝에 붙여서 기존 에셋 값은 그대로)

```csharp
        /// <summary>상호작용 (E). 12시 안내 없이 게임 코드가 처음 쓸 때 배움으로 알림</summary>
        Interact,

        /// <summary>주문 그리기 (책상 연습). 12시 안내 없이 처음 판정까지 그리면 배움으로 알림</summary>
        Draw,   // ← 추가
```

---

### `Assets/Scripts/UI/Drawers/StrokeDrawer.cs` (새 파일)

**추가 내용:** 획을 굵은 선 메시로 그리는 UGUI 그래픽 (로직 없음). 그린 획은 이 그래픽 좌표 그대로, 주문 그림은 칸에 비율을 지켜 맞춤. `Assets/Scripts/UI/Drawers/` 폴더 새로

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>획(점 줄)을 굵은 선 메시로 그리는 UI 그래픽 (로직 없음). 점을 이 그래픽 좌표 그대로 그리거나(그린 획), 칸에 맞춰 그림(주문 그림)</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StrokeDrawer : MaskableGraphic
    {
        /// <summary>선 굵기 (이 그래픽 단위)</summary>
        [SerializeField, Min(0.5f)] float _thickness = 6f;

        /// <summary>칸에 맞춰 그릴 때 둘레 여백 (칸 짧은 변 대비)</summary>
        [SerializeField, Range(0f, 0.45f)] float _fitPadding = 0.1f;

        /// <summary>그릴 획 (점 복사본)</summary>
        readonly List<Vector2[]> _strokes = new();

        /// <summary>칸에 맞춰 그릴지 (false면 점이 이 그래픽 좌표 그대로)</summary>
        bool _fit;

        #region SetStrokes - 그릴 획을 바꾸고 다시 그림 (점은 복사, 빈 획은 뺌)
        /// <param name="strokes">획 → 점 (null이면 지움)</param>
        /// <param name="fit">칸에 맞춰 그릴지 (주문 그림 좌표면 true, 그린 획이면 false)</param>
        public void SetStrokes(IEnumerable<IReadOnlyList<Vector2>> strokes, bool fit)
        {
            _strokes.Clear();
            if (strokes != null)
                foreach (IReadOnlyList<Vector2> stroke in strokes)
                {
                    if (stroke == null || stroke.Count == 0) continue;
                    // 이 획 점 복사본
                    var points = new Vector2[stroke.Count];
                    for (int i = 0; i < points.Length; i++) points[i] = stroke[i];
                    _strokes.Add(points);
                }
            _fit = fit;
            SetVerticesDirty();
        }
        #endregion

        #region Clear - 획을 다 지움
        public void Clear()
        {
            _strokes.Clear();
            SetVerticesDirty();
        }
        #endregion

        #region OnPopulateMesh - 선분마다 굵기만큼 사각형, 점마다 굵기만 한 사각형으로 이음매·끝을 채움
        /// <param name="vh">메시를 쌓을 곳</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_strokes.Count == 0) return;
            GetMapping(out float scale, out Vector2 offset);
            // 굵기 절반
            float half = _thickness * 0.5f;
            foreach (Vector2[] stroke in _strokes)
                for (int i = 0; i < stroke.Length; i++)
                {
                    // 이 점 (그래픽 좌표)
                    Vector2 p = stroke[i] * scale + offset;
                    AddQuad(vh, p + new Vector2(-half, -half), p + new Vector2(-half, half), p + new Vector2(half, half), p + new Vector2(half, -half));
                    if (i == 0) continue;
                    // 앞 점 (그래픽 좌표)
                    Vector2 a = stroke[i - 1] * scale + offset;
                    // 앞 점에서 이 점으로
                    Vector2 direction = p - a;
                    if (direction.sqrMagnitude < 1e-6f) continue;
                    // 선분에 수직인 굵기 절반
                    Vector2 normal = new Vector2(-direction.y, direction.x).normalized * half;
                    AddQuad(vh, a - normal, a + normal, p + normal, p - normal);
                }
        }
        #endregion

        #region GetMapping - 맞춤이면 모든 점의 테두리 상자를 칸 가운데에 비율을 지켜 맞추는 배율·이동, 아니면 그대로
        /// <param name="scale">배율</param>
        /// <param name="offset">이동</param>
        void GetMapping(out float scale, out Vector2 offset)
        {
            scale = 1f;
            offset = Vector2.zero;
            if (!_fit) return;
            // 모든 점의 테두리
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector2[] stroke in _strokes)
                foreach (Vector2 p in stroke)
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            // 이 그래픽 칸
            Rect rect = rectTransform.rect;
            // 둘레 여백
            float pad = Mathf.Min(rect.width, rect.height) * _fitPadding;
            // 그림 크기 (얇은 그림은 아주 작은 값)
            Vector2 size = Vector2.Max(max - min, new Vector2(1e-4f, 1e-4f));
            scale = Mathf.Min((rect.width - 2f * pad) / size.x, (rect.height - 2f * pad) / size.y);
            offset = rect.center - (min + max) * 0.5f * scale;
        }
        #endregion

        #region AddQuad - 사각형 하나 (꼭짓점 4개, 삼각형 2개, 그래픽 색)
        /// <param name="vh">메시를 쌓을 곳</param>
        /// <param name="a">꼭짓점</param>
        /// <param name="b">꼭짓점</param>
        /// <param name="c">꼭짓점</param>
        /// <param name="d">꼭짓점</param>
        void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // 첫 꼭짓점 번호
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/SpellDrawView.cs` (새 파일)

**추가 내용:** 그리기 칸 참조 (칸 루트. 칸 자체 Image가 포인터를 받음)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>주문 그리기 칸 참조 (그리기 칸 루트. 칸 자체 Image가 포인터를 받음)</summary>
    public sealed class SpellDrawView : MonoBehaviour
    {
        /// <summary>따라 그릴 주문 그림 (흐리게, 칸에 맞춤. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _guide;

        /// <summary>그린 획 (칸 좌표 그대로. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _ink;

        /// <summary>판정까지 남은 시간 막대 (Filled 가로. 시전 상태에만 켬. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _timer;

        /// <summary>판정 결과 줄 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _result;

        /// <summary>결과 자세히: 부분 일치율·가장 가까운 주문 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _detail;

        /// <summary>그리기 전 안내 글 (Cast/cast.hint, {0} 대기 초. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _hint;

        /// <summary>따라 그릴 주문 그림</summary>
        public StrokeDrawer Guide => _guide;

        /// <summary>그린 획</summary>
        public StrokeDrawer Ink => _ink;

        /// <summary>판정까지 남은 시간 막대</summary>
        public Image Timer => _timer;

        /// <summary>판정 결과 줄</summary>
        public LocalizeStringEvent Result => _result;

        /// <summary>결과 자세히</summary>
        public LocalizeStringEvent Detail => _detail;

        /// <summary>그리기 전 안내 글</summary>
        public LocalizeStringEvent Hint => _hint;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/SpellDrawController.cs` (새 파일)

**추가 내용:** 그리기 칸 입력·대기·결과 글. 판정 자체는 `Begin`으로 받은 함수가 함 (책상은 `CodexController.Judge`)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>주문 그리기 칸: 좌클릭으로 시전 상태, 버튼을 떼도 이어 그리기, 마지막 입력 뒤 대기 시간이 지나거나 Space(Player/Cast)면 판정, 우클릭은 취소. 그린 획을 칸 좌표로 모으고, 판정은 받은 함수가 하고 결과 글은 여기서 보임. 대기 시간은 게임 시간 (일시정지 중엔 멈춤)</summary>
    public sealed class SpellDrawController : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>바로 판정 입력 액션 경로 (전역 액션, Space)</summary>
        const string CastActionPath = "Player/Cast";

        /// <summary>이보다 가까운 점은 안 넣음 (칸 단위. 점 수를 줄이려고)</summary>
        const float MinPointGap = 2f;

        /// <summary>시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastKey = "cast.result.cast";

        /// <summary>정확 시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastExactKey = "cast.result.cast_exact";

        /// <summary>취소 결과 키 ({0} 이유)</summary>
        const string CancelKey = "cast.result.cancel";

        /// <summary>이유: 그린 것이 없음</summary>
        const string WhyEmptyKey = "cast.why.empty";

        /// <summary>이유: 너무 작음</summary>
        const string WhySmallKey = "cast.why.small";

        /// <summary>이유: 비교할 주문 그림이 없음</summary>
        const string WhyNoneKey = "cast.why.none";

        /// <summary>이유: 맞는 주문이 없음</summary>
        const string WhyNoMatchKey = "cast.why.nomatch";

        /// <summary>이유: 부분이 모자람 ({0} 주문, {1} 모자란 부분, {2} 부분 기준 %)</summary>
        const string WhyPartsKey = "cast.why.parts";

        /// <summary>이유: 안 익힌 요소 ({0} 주문, {1} 요소)</summary>
        const string WhyUnlearnedKey = "cast.why.unlearned";

        /// <summary>이유: 하나로 안 정해짐 ({0} 주문 이름 목록)</summary>
        const string WhyAmbiguousKey = "cast.why.ambiguous";

        /// <summary>이유: 직접 취소</summary>
        const string WhyCanceledKey = "cast.why.canceled";

        /// <summary>부분 하나 ({0} 요소, {1} 일치율 %)</summary>
        const string PartKey = "cast.part";

        /// <summary>남는 선 ({0} %)</summary>
        const string ExtraKey = "cast.extra";

        /// <summary>자세히: 부분 일치율 줄 ({0} 부분 목록)</summary>
        const string PartsKey = "cast.parts";

        /// <summary>자세히: 가장 가까운 주문 ({0} 주문, {1} 일치율 %)</summary>
        const string NearestKey = "cast.nearest";

        /// <summary>주문 그리기 문구 테이블</summary>
        static readonly TableReference Table = "Cast";

        /// <summary>같은 칸의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawView _view;

        /// <summary>마지막 입력 뒤 판정까지 시간 (초, 게임 시간. 임시 3초)</summary>
        [SerializeField, Min(0.5f)] float _waitSeconds = 3f;

        /// <summary>끝난 획 (칸 좌표, 점 2개 이상)</summary>
        readonly List<Vector2[]> _strokes = new();

        /// <summary>그리는 중인 획. 없으면 null</summary>
        List<Vector2> _stroke;

        /// <summary>시전 상태인지 (첫 좌클릭부터 판정·취소까지)</summary>
        bool _casting;

        /// <summary>마지막 입력 뒤 지난 시간 (초)</summary>
        float _idle;

        /// <summary>결과 글에 쓸 주문 목록 (판정의 ShapeIndex 번호와 같은 순서)</summary>
        IReadOnlyList<SpellSO> _spells;

        /// <summary>판정 (그린 획 → 결과). 그리기를 안 받으면 null</summary>
        Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> _judge;

        /// <summary>판정·취소 뒤 할 일</summary>
        Action<SpellJudgeSpec> _onJudged;

        /// <summary>바로 판정 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _cast;

        /// <summary>그리기를 받는 중인지 (Begin ~ End)</summary>
        public bool IsActive => _judge != null;

        /// <summary>시전 상태인지</summary>
        public bool IsCasting => _casting;

        #region Awake - 연결 확인, 바로 판정 액션 찾기, 안내 글에 대기 초를 넣고 빈 칸으로
        void Awake()
        {
            if (_view == null) Debug.LogError("[Draw] SpellDrawView 연결 안 됨", this);
            _cast = InputSystem.actions.FindAction(CastActionPath);
            if (_cast == null) Debug.LogError($"[Draw] {CastActionPath} 액션 없음. Space 바로 판정이 안 됨", this);
            if (_view != null)
            {
                _view.Hint.StringReference.Arguments = new object[] { Mathf.RoundToInt(_waitSeconds) };
                _view.Hint.RefreshString();
            }
            ResetDrawing();
        }
        #endregion

        #region OnDisable - 꺼지면 그리기를 끝냄 (입력 구독 해제)
        void OnDisable() => End();
        #endregion

        #region Begin - 그리기 받기 시작: 판정·결과 할 일과 결과 글에 쓸 주문 목록을 받고, Space 구독, 획·결과를 비움 (다시 부르면 처음부터)
        /// <param name="spells">주문 목록 (판정 결과의 ShapeIndex 번호와 같은 순서)</param>
        /// <param name="judge">그린 획 → 판정</param>
        /// <param name="onJudged">판정·취소 뒤 할 일 (없으면 null)</param>
        public void Begin(IReadOnlyList<SpellSO> spells, Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> judge, Action<SpellJudgeSpec> onJudged)
        {
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _judge = judge ?? throw new ArgumentNullException(nameof(judge));
            _onJudged = onJudged;
            if (_cast != null)
            {
                _cast.performed -= OnCastPerformed;
                _cast.performed += OnCastPerformed;
            }
            ResetDrawing();
        }
        #endregion

        #region End - 그리기 끝: Space 구독 해제, 할 일·획·결과·따라 그릴 그림을 비움 (여러 번 불려도 됨, Awake 전에도 됨)
        public void End()
        {
            if (_cast != null) _cast.performed -= OnCastPerformed;
            _judge = null;
            _onJudged = null;
            _spells = null;
            ResetDrawing();
            if (_view != null) _view.Guide.Clear();
        }
        #endregion

        #region ShowGuide - 따라 그릴 주문 그림을 흐리게 깖 (null이면 지움)
        /// <param name="spell">따라 그릴 주문 (자유 그리기면 null)</param>
        public void ShowGuide(SpellSO spell)
        {
            if (_view == null) return;
            if (spell == null) _view.Guide.Clear();
            else _view.Guide.SetStrokes(spell.Shape.PartStrokes.SelectMany(part => part), true);
        }
        #endregion

        #region CancelCast - 직접 취소 (Esc·우클릭): 시전 상태면 '직접 취소'로 끝냄
        public void CancelCast()
        {
            if (!_casting) return;
            Finish(SpellJudgeSpec.Cancel(SpellJudgeReason.Canceled));
        }
        #endregion

        #region Update - 시전 상태에서 그리는 중이 아니면 대기 시간을 세고 막대를 줄임. 다 차면 판정 (게임 시간)
        void Update()
        {
            if (!_casting || _view == null) return;
            if (_stroke != null)
            {
                _view.Timer.fillAmount = 1f;
                return;
            }
            _idle += Time.deltaTime;
            _view.Timer.fillAmount = Mathf.Clamp01(1f - _idle / _waitSeconds);
            if (_idle >= _waitSeconds) Resolve();
        }
        #endregion

        #region OnInitializePotentialDrag - 끌기 문턱 없이 첫 움직임부터 그림
        /// <param name="eventData">포인터 정보</param>
        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;
        #endregion

        #region OnPointerDown - 우클릭은 취소. 좌클릭은 (시전 상태가 아니면 시작하고) 새 획 시작
        /// <param name="eventData">포인터 정보</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsActive) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                CancelCast();
                return;
            }
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!_casting) StartCast();
            EndStroke();
            _stroke = new List<Vector2> { ToLocal(eventData) };
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region OnDrag - 그리는 중이면 점을 더함 (앞 점과 너무 가까우면 뺌)
        /// <param name="eventData">포인터 정보</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (_stroke == null || eventData.button != PointerEventData.InputButton.Left) return;
            // 칸 좌표 점
            Vector2 point = ToLocal(eventData);
            if ((point - _stroke[_stroke.Count - 1]).sqrMagnitude < MinPointGap * MinPointGap) return;
            _stroke.Add(point);
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region OnPointerUp - 좌클릭을 떼면 획 끝 (칸 밖에서 떼도 옴)
        /// <param name="eventData">포인터 정보</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            EndStroke();
        }
        #endregion

        #region OnCastPerformed - Space: 시전 상태면 그리던 획을 끝내고 바로 판정
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCastPerformed(InputAction.CallbackContext context)
        {
            if (!_casting) return;
            EndStroke();
            Resolve();
        }
        #endregion

        #region StartCast - 시전 상태 시작: 앞 그림·결과 글·안내 글을 지우고 막대를 채움
        void StartCast()
        {
            _casting = true;
            _strokes.Clear();
            _idle = 0f;
            if (_view == null) return;
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            _view.Hint.gameObject.SetActive(false);
            _view.Timer.gameObject.SetActive(true);
            _view.Timer.fillAmount = 1f;
        }
        #endregion

        #region EndStroke - 그리던 획을 끝냄 (점 2개 이상이면 남김)
        void EndStroke()
        {
            if (_stroke == null) return;
            if (_stroke.Count >= 2) _strokes.Add(_stroke.ToArray());
            _stroke = null;
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region Resolve - 그린 획(복사본)으로 판정하고 끝냄
        void Resolve()
        {
            if (_judge == null) return;
            Finish(_judge(_strokes.ToArray()));
        }
        #endregion

        #region Finish - 시전 상태를 끝내고 결과 글을 보인 뒤 할 일 (그린 획은 다음 좌클릭까지 남겨 둠)
        /// <param name="spec">판정 결과</param>
        void Finish(SpellJudgeSpec spec)
        {
            _casting = false;
            _stroke = null;
            _idle = 0f;
            if (_view != null) _view.Timer.gameObject.SetActive(false);
            ShowResult(spec);
            _onJudged?.Invoke(spec);
        }
        #endregion

        #region ResetDrawing - 시전 상태·획·결과 글을 비우고 안내 글을 보임
        void ResetDrawing()
        {
            _casting = false;
            _stroke = null;
            _idle = 0f;
            _strokes.Clear();
            if (_view == null) return;
            _view.Ink.Clear();
            _view.Timer.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            _view.Hint.gameObject.SetActive(true);
        }
        #endregion

        #region RefreshInk - 끝난 획과 그리는 중인 획을 다시 그림
        void RefreshInk()
        {
            if (_view == null) return;
            // 그릴 획 전부
            var all = new List<IReadOnlyList<Vector2>>(_strokes);
            if (_stroke != null) all.Add(_stroke);
            _view.Ink.SetStrokes(all, false);
        }
        #endregion

        #region ToLocal - 포인터 화면 좌표를 이 칸 좌표로 (World Space 캔버스면 누른 카메라 기준)
        /// <param name="eventData">포인터 정보</param>
        Vector2 ToLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);
            return local;
        }
        #endregion

        #region ShowResult - 결과 줄 (시전: "이름 일치율%", 정확이면 표시 / 취소: "취소: 이유")과 자세히 (맞는 주문이 없으면 가장 가까운 주문, 아니면 부분 일치율·남는 선)
        /// <param name="spec">판정 결과</param>
        void ShowResult(SpellJudgeSpec spec)
        {
            if (_view == null) return;
            _view.Hint.gameObject.SetActive(false);
            if (spec.IsCast)
                SetText(_view.Result, spec.Accuracy >= SpellRecognitionCalculator.ExactAccuracy ? CastExactKey : CastKey, SpellName(spec.Candidate), Percent(spec.Accuracy));
            else
                SetText(_view.Result, CancelKey, ReasonText(spec));
            if (spec.Reason == SpellJudgeReason.NoMatch && spec.Candidate != null)
                SetText(_view.Detail, NearestKey, SpellName(spec.Candidate), Percent(spec.Accuracy));
            else if (spec.Candidate != null && spec.PartCoverage.Count > 0)
                SetText(_view.Detail, PartsKey, PartsText(spec, false));
            else
                _view.Detail.gameObject.SetActive(false);
        }
        #endregion

        #region SetText - 글자에 이 테이블 키·자리 값을 넣고 켬
        /// <param name="text">넣을 글자</param>
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        static void SetText(LocalizeStringEvent text, string key, params object[] args)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.SetReference(Table, key);
            reference.Arguments = args;
            text.gameObject.SetActive(true);
            text.RefreshString();
        }
        #endregion

        #region ReasonText - 취소 이유 글 (부분 부족: 모자란 부분, 미습득: 빠진 요소, 애매함: 겹친 주문 이름)
        /// <param name="spec">판정 결과</param>
        string ReasonText(SpellJudgeSpec spec) => spec.Reason switch
        {
            SpellJudgeReason.Parts => Text(WhyPartsKey, SpellName(spec.Candidate), PartsText(spec, true), Percent(spec.PartMin)),
            SpellJudgeReason.Unlearned => Text(WhyUnlearnedKey, SpellName(spec.Candidate), UIManager.Instance.JoinList(spec.MissingElementIds.Select(ElementName))),
            SpellJudgeReason.Ambiguous => Text(WhyAmbiguousKey, UIManager.Instance.JoinList(spec.AmbiguousShapes.Select(SpellNameAt))),
            SpellJudgeReason.Empty => Text(WhyEmptyKey),
            SpellJudgeReason.Small => Text(WhySmallKey),
            SpellJudgeReason.NoCandidates => Text(WhyNoneKey),
            SpellJudgeReason.Canceled => Text(WhyCanceledKey),
            _ => Text(WhyNoMatchKey),
        };
        #endregion

        #region PartsText - 부분 일치율 목록 ("불 96%, …". lowOnly면 부분 기준 아래만) + 남는 선 (lowOnly면 한도를 넘을 때만)
        /// <param name="spec">판정 결과 (Candidate 있음)</param>
        /// <param name="lowOnly">모자란 것만 넣을지</param>
        string PartsText(SpellJudgeSpec spec, bool lowOnly)
        {
            // 넣을 항목 글
            var items = new List<string>();
            for (int i = 0; i < spec.PartCoverage.Count && i < spec.Candidate.PartElementIds.Count; i++)
            {
                // 이 부분 일치율
                float coverage = spec.PartCoverage[i];
                if (lowOnly && coverage >= spec.PartMin) continue;
                items.Add(Text(PartKey, ElementName(spec.Candidate.PartElementIds[i]), Percent(coverage)));
            }
            if (!lowOnly || spec.Extra > SpellRecognitionCalculator.MaxExtra) items.Add(Text(ExtraKey, Percent(spec.Extra)));
            return UIManager.Instance.JoinList(items);
        }
        #endregion

        #region Text - 이 테이블 문구를 지금 언어로 읽음
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        static string Text(string key, params object[] args) => new LocalizedString(Table, key).GetLocalizedString(args);
        #endregion

        #region SpellName - 비교 그림의 주문 이름 (지금 언어, 없으면 빈 글)
        /// <param name="candidate">비교 그림</param>
        string SpellName(SpellCandidateEntry candidate) => candidate != null ? SpellNameAt(candidate.ShapeIndex) : string.Empty;
        #endregion

        #region SpellNameAt - 주문 목록 번호의 주문 이름 (지금 언어, 없으면 빈 글)
        /// <param name="index">주문 목록 번호</param>
        string SpellNameAt(int index) =>
            _spells != null && index >= 0 && index < _spells.Count && _spells[index] != null ? _spells[index].Name.GetLocalizedString() : string.Empty;
        #endregion

        #region ElementName - 요소 id의 이름 (지금 언어, 모르는 id면 id 그대로)
        /// <param name="id">요소 id</param>
        static string ElementName(string id)
        {
            // 찾은 요소
            ElementSO element = DataManager.Instance.FindElement(id);
            return element != null ? element.Name.GetLocalizedString() : id;
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
- `CodexController.SelectPractice` → `Begin(주문 목록, Judge, OnJudged)`·`ShowGuide`. `CodexController.HidePages` → `End`
- `CodexController.CancelCast`(← `HubState.HandleCancel` Esc) → `CancelCast`
- `Player/Cast.performed` → `OnCastPerformed` (Begin ~ End 동안만. 일시정지·메뉴 창이면 `Player` 묶음이 꺼져서 안 옴)

---

### `Assets/Scripts/UI/Views/CodexView.cs` (새 파일)

**추가 내용:** 코덱스 참조 (여관 방 `RoomInterior/Codex` 루트, World Space 캔버스. 책상 위에 늘 펼쳐 두고 입력은 앉아 있을 때만)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>코덱스 참조 (여관 방 RoomInterior/Codex 루트, World Space 캔버스). 왼쪽 장: 이름·탭·목록·일어나기, 오른쪽 장: 읽기·연습</summary>
    public sealed class CodexView : MonoBehaviour
    {
        /// <summary>코덱스 입력 (책상에 앉아 있을 때만 켬. 책은 늘 보임. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>코덱스 이름 (Codex/codex.name, {0} 플레이어 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>주문서 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _scrollsTab;

        /// <summary>연습 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _practiceTab;

        /// <summary>일어나기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _leaveButton;

        /// <summary>목록 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _list;

        /// <summary>목록 항목 프리팹 (버튼 + 글자. 인스펙터에서만 연결)</summary>
        [SerializeField] TutorialListItemView _itemPrefab;

        /// <summary>목록이 비었을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _listEmpty;

        /// <summary>읽기 장 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _readingPage;

        /// <summary>읽기: 주문서 이름 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _readingTitle;

        /// <summary>읽기: 본문 (첫 장 설명, 마지막 장 필요한 요소·익혔다는 글. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _readingBody;

        /// <summary>읽기: 주문 그림 (마지막 장, 주문 주문서. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _readingDrawing;

        /// <summary>읽기: 요소 그림 (마지막 장, 요소 주문서. 요소 그림이 없으면 꺼짐. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _readingIcon;

        /// <summary>읽기: 장 번호 (Codex/codex.page, {0}/{1}. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _readingPageNumber;

        /// <summary>읽기: 이전 장 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _prevButton;

        /// <summary>읽기: 다음 장 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _nextButton;

        /// <summary>연습 장 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _practicePage;

        /// <summary>연습: 따라 그릴 주문 이름 또는 자유 그리기 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _practiceTitle;

        /// <summary>연습: 그리기 칸 (인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawController _draw;

        /// <summary>코덱스 입력</summary>
        public CanvasGroup Group => _group;

        /// <summary>코덱스 이름</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>주문서 탭 버튼</summary>
        public Button ScrollsTab => _scrollsTab;

        /// <summary>연습 탭 버튼</summary>
        public Button PracticeTab => _practiceTab;

        /// <summary>일어나기 버튼</summary>
        public Button LeaveButton => _leaveButton;

        /// <summary>목록 자리</summary>
        public RectTransform List => _list;

        /// <summary>목록 항목 프리팹</summary>
        public TutorialListItemView ItemPrefab => _itemPrefab;

        /// <summary>목록이 비었을 때 글</summary>
        public LocalizeStringEvent ListEmpty => _listEmpty;

        /// <summary>읽기 장</summary>
        public GameObject ReadingPage => _readingPage;

        /// <summary>읽기: 주문서 이름</summary>
        public LocalizeStringEvent ReadingTitle => _readingTitle;

        /// <summary>읽기: 본문</summary>
        public LocalizeStringEvent ReadingBody => _readingBody;

        /// <summary>읽기: 주문 그림</summary>
        public StrokeDrawer ReadingDrawing => _readingDrawing;

        /// <summary>읽기: 요소 그림</summary>
        public Image ReadingIcon => _readingIcon;

        /// <summary>읽기: 장 번호</summary>
        public LocalizeStringEvent ReadingPageNumber => _readingPageNumber;

        /// <summary>읽기: 이전 장 버튼</summary>
        public Button PrevButton => _prevButton;

        /// <summary>읽기: 다음 장 버튼</summary>
        public Button NextButton => _nextButton;

        /// <summary>연습 장</summary>
        public GameObject PracticePage => _practicePage;

        /// <summary>연습: 제목</summary>
        public LocalizeStringEvent PracticeTitle => _practiceTitle;

        /// <summary>연습: 그리기 칸</summary>
        public SpellDrawController Draw => _draw;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/CodexController.cs` (새 파일)

**추가 내용:** 코덱스 탭·목록·읽기·연습. 이름 `{플레이어}'s Codex`는 씬이 준비될 때 상태가 넣음 (`ShowName`). 책은 늘 보이고 입력은 앉아 있을 때만. 주문서는 2장 (설명 → 필요한 요소·그림, 임시)이고 마지막 장에 오면 다 읽음: 요소 주문서는 요소 습득, 주문 주문서는 도감 등록, 그 주문서는 목록에서 빠짐. 연습은 생략 없이 처음 완성하면 도감에. 판 데이터·주문 목록은 상태가 넘김. 연구(퍼즐 + 시간)는 퍼즐이 정해질 때까지 없음

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>코덱스 (마법사가 늘 들고 다니는 책이자 주문 도감. 방에선 책상 위에 펼쳐 두고 앉으면 씀): 왼쪽 장은 이름·탭(주문서·연습)·목록, 오른쪽 장은 읽기·연습. 주문서는 장을 끝까지 넘기면 다 읽음(요소 주문서는 요소 습득, 주문 주문서는 도감 등록), 연습은 그리기 판정(생략 없이 처음 완성하면 도감에). 판 데이터·주문 목록은 상태가 넘김</summary>
    public sealed class CodexController : MonoBehaviour
    {
        /// <summary>왼쪽 장 탭</summary>
        enum Tab
        {
            /// <summary>가진 주문서 (요소 → 주문)</summary>
            Scrolls,

            /// <summary>그리기 연습 (자유 + 도감 주문)</summary>
            Practice,
        }

        /// <summary>주문서 장 수 (설명 → 필요한 요소·그림, 임시). 마지막 장에 오면 다 읽음</summary>
        const int ReadingPageCount = 2;

        /// <summary>주문 그리기 튜토리얼 id (처음 판정까지 그리면 배움)</summary>
        const string DrawTutorialId = "draw";

        /// <summary>주문서 탭이 비었을 때 키</summary>
        const string EmptyScrollsKey = "codex.empty.scrolls";

        /// <summary>요소 주문서 항목 키 ({0} 요소 이름)</summary>
        const string ElementScrollKey = "codex.scroll.element";

        /// <summary>주문 주문서 항목 키 ({0} 주문 이름)</summary>
        const string SpellScrollKey = "codex.scroll.spell";

        /// <summary>장 번호 키 ({0} 지금 장, {1} 장 수)</summary>
        const string PageKey = "codex.page";

        /// <summary>필요한 요소 키 ({0} 요소 목록)</summary>
        const string ElementsKey = "codex.elements";

        /// <summary>요소 주문서 마지막 장 글 키</summary>
        const string ElementLearnedKey = "codex.element.learned";

        /// <summary>자유 그리기 키</summary>
        const string FreeDrawKey = "codex.practice.free";

        /// <summary>도감 등록 토스트 키 ({0} 주문 이름)</summary>
        const string RegisteredKey = "codex.toast.registered";

        /// <summary>요소 습득 토스트 키 ({0} 요소 이름)</summary>
        const string LearnedKey = "codex.toast.learned";

        /// <summary>코덱스 문구 테이블</summary>
        static readonly TableReference Table = "Codex";

        /// <summary>같은 책의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] CodexView _view;

        /// <summary>지금 만든 목록 항목</summary>
        readonly List<TutorialListItemView> _items = new();

        /// <summary>익힌 요소 id (비교 그림을 만들 때 채움)</summary>
        readonly HashSet<string> _learnedIds = new(StringComparer.Ordinal);

        /// <summary>판정 비교 그림 (익힌 요소·도감이 바뀌면 다시 만듦)</summary>
        List<SpellCandidateEntry> _candidates = new();

        /// <summary>이번 판 데이터 (쓰는 동안. 그만 쓰면 null)</summary>
        RunEntry _run;

        /// <summary>도감 주문 전부 (DataManager 순서)</summary>
        IReadOnlyList<SpellSO> _spells;

        /// <summary>주문마다 인식기용 그림 (_spells 순서, 빈 칸은 null)</summary>
        SpellShapeEntry[] _shapes;

        /// <summary>일어나기를 누르면 할 일 (상태가 넘김)</summary>
        Action _onLeave;

        /// <summary>지금 탭</summary>
        Tab _tab;

        /// <summary>읽는 요소 주문서 (없으면 null)</summary>
        ElementSO _readingElement;

        /// <summary>읽는 주문 주문서 (없으면 null)</summary>
        SpellSO _readingSpell;

        /// <summary>읽는 장 번호 (0부터)</summary>
        int _page;

        /// <summary>쓰는 중인지 (앉아 있는 동안)</summary>
        public bool IsOpen => _run != null;

        /// <summary>연습 장에서 그리는 중인지 (Esc를 그리기 취소로 씀)</summary>
        public bool IsCasting => _view != null && _view.Draw.IsCasting;

        #region Awake - 연결 확인, 버튼 연결 (처음 한 번), 장을 다 끄고 입력을 막음 (책상에 앉으면 Open)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Codex] CodexView 연결 안 됨", this);
                return;
            }
            _view.ScrollsTab.onClick.AddListener(() => ShowTab(Tab.Scrolls));
            _view.PracticeTab.onClick.AddListener(() => ShowTab(Tab.Practice));
            _view.LeaveButton.onClick.AddListener(() => _onLeave?.Invoke());
            _view.PrevButton.onClick.AddListener(() => TurnPage(-1));
            _view.NextButton.onClick.AddListener(() => TurnPage(1));
            HidePages();
            SetInteractable(false);
        }
        #endregion

        #region Open - 쓰기 시작: 판 데이터·주문 목록을 받아 비교 그림을 만든 뒤, 가진 주문서가 있으면 주문서 탭, 없으면 연습 탭
        /// <param name="run">이번 판 데이터</param>
        /// <param name="spells">도감 주문 전부 (DataManager 순서)</param>
        /// <param name="onLeave">일어나기를 누르면 할 일</param>
        public void Open(RunEntry run, IReadOnlyList<SpellSO> spells, Action onLeave)
        {
            if (_view == null) return;
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _onLeave = onLeave;
            _shapes = new SpellShapeEntry[spells.Count];
            for (int i = 0; i < spells.Count; i++) _shapes[i] = spells[i] != null ? spells[i].Shape : null;
            RebuildCandidates();
            SetInteractable(true);
            ShowTab(run.ElementScrolls.Count + run.SpellScrolls.Count > 0 ? Tab.Scrolls : Tab.Practice);
        }
        #endregion

        #region Close - 그만 씀: 장·목록을 지우고 입력을 막고 판 데이터를 놓음 (책은 책상 위에 그대로)
        public void Close()
        {
            if (_view == null) return;
            HidePages();
            ClearItems();
            SetInteractable(false);
            _run = null;
            _spells = null;
            _shapes = null;
            _onLeave = null;
            _candidates.Clear();
        }
        #endregion

        #region ShowName - 코덱스 이름을 넣음 ('{플레이어}'s Codex'. 씬이 준비될 때 상태가 부름)
        /// <param name="playerName">플레이어 이름</param>
        public void ShowName(string playerName)
        {
            if (_view == null) return;
            _view.NameText.StringReference.Arguments = new object[] { playerName ?? string.Empty };
            _view.NameText.RefreshString();
        }
        #endregion

        #region CancelCast - 그리는 중이면 그리기를 취소 (Esc)
        public void CancelCast()
        {
            if (_view != null) _view.Draw.CancelCast();
        }
        #endregion

        #region ShowTab - 탭을 바꿈: 그 탭 버튼은 못 누르게, 오른쪽 장을 끄고 목록을 다시 만듦. 연습 탭은 자유 그리기로 바로 시작
        /// <param name="tab">보일 탭</param>
        void ShowTab(Tab tab)
        {
            if (_run == null) return;
            _tab = tab;
            _view.ScrollsTab.interactable = tab != Tab.Scrolls;
            _view.PracticeTab.interactable = tab != Tab.Practice;
            HidePages();
            RebuildList();
            if (tab == Tab.Practice) SelectPractice(null);
        }
        #endregion

        #region RebuildList - 지금 탭 목록을 다시 만듦: 주문서(요소 → 주문), 연습(자유 그리기 + 도감 주문). 비면 빈 글
        void RebuildList()
        {
            ClearItems();
            if (_tab == Tab.Scrolls)
            {
                foreach (ElementSO element in _run.ElementScrolls)
                {
                    // 누를 때 쓸 요소 (반복 변수를 붙잡지 않게)
                    ElementSO picked = element;
                    AddItem(ElementScrollKey, () => SelectScroll(picked, null), element.Name.GetLocalizedString());
                }
                foreach (SpellSO spell in _run.SpellScrolls)
                {
                    // 누를 때 쓸 주문
                    SpellSO picked = spell;
                    AddItem(SpellScrollKey, () => SelectScroll(null, picked), spell.Name.GetLocalizedString());
                }
            }
            else
            {
                AddItem(FreeDrawKey, () => SelectPractice(null));
                foreach (SpellSO spell in _run.CollectedSpells)
                {
                    // 누를 때 쓸 주문
                    SpellSO picked = spell;
                    AddItem(spell.Name, () => SelectPractice(picked));
                }
            }
            // 목록이 비었는지 (연습은 자유 그리기가 늘 있음)
            bool empty = _items.Count == 0;
            _view.ListEmpty.gameObject.SetActive(empty);
            if (empty) _view.ListEmpty.StringReference.SetReference(Table, EmptyScrollsKey);
        }
        #endregion

        #region AddItem - 이 테이블 키·자리 값 글자의 목록 항목
        /// <param name="key">항목 글자 키</param>
        /// <param name="onClick">누르면 할 일</param>
        /// <param name="args">글자 자리 값</param>
        void AddItem(string key, Action onClick, params object[] args)
        {
            // 만든 항목
            TutorialListItemView item = CreateItem(onClick);
            // 항목 글자의 String Reference
            var title = item.Title.StringReference;
            title.SetReference(Table, key);
            title.Arguments = args;
            item.Title.RefreshString();
        }
        #endregion

        #region AddItem - SO 이름 글자의 목록 항목
        /// <param name="label">항목 글자 (SO의 LocalizedString, 읽기만 함)</param>
        /// <param name="onClick">누르면 할 일</param>
        void AddItem(LocalizedString label, Action onClick)
        {
            // 만든 항목
            TutorialListItemView item = CreateItem(onClick);
            item.Title.StringReference.SetReference(label.TableReference, label.TableEntryReference);
        }
        #endregion

        #region CreateItem - 목록 항목 하나를 만들고 버튼을 연결
        /// <param name="onClick">누르면 할 일</param>
        TutorialListItemView CreateItem(Action onClick)
        {
            // 만든 항목
            TutorialListItemView item = Instantiate(_view.ItemPrefab, _view.List);
            item.Button.onClick.AddListener(() => onClick());
            _items.Add(item);
            return item;
        }
        #endregion

        #region ClearItems - 만든 목록 항목을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        void ClearItems()
        {
            foreach (TutorialListItemView item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();
        }
        #endregion

        #region HidePages - 오른쪽 장을 다 끄고 그리기를 끝냄, 읽는 주문서 비움
        void HidePages()
        {
            _view.Draw.End();
            _view.ReadingPage.SetActive(false);
            _view.PracticePage.SetActive(false);
            _readingElement = null;
            _readingSpell = null;
            _page = 0;
        }
        #endregion

        #region SelectScroll - 주문서를 펼침 (첫 장부터)
        /// <param name="element">요소 주문서 (주문 주문서면 null)</param>
        /// <param name="spell">주문 주문서 (요소 주문서면 null)</param>
        void SelectScroll(ElementSO element, SpellSO spell)
        {
            HidePages();
            _readingElement = element;
            _readingSpell = spell;
            _page = 0;
            _view.ReadingPage.SetActive(true);
            ShowReading();
        }
        #endregion

        #region ShowReading - 읽는 장을 채움: 첫 장은 이름·설명, 마지막 장은 이름과 (주문) 필요한 요소·그림 / (요소) 익혔다는 글·요소 그림. 장 번호·넘기기 버튼
        void ShowReading()
        {
            // 읽는 주문서의 이름·설명 (요소 또는 주문)
            LocalizedString name = _readingElement != null ? _readingElement.Name : _readingSpell.Name;
            LocalizedString description = _readingElement != null ? _readingElement.Description : _readingSpell.Description;
            // 마지막 장인지
            bool last = _page == ReadingPageCount - 1;
            _view.ReadingTitle.StringReference.SetReference(name.TableReference, name.TableEntryReference);
            // 본문 글자의 String Reference
            var body = _view.ReadingBody.StringReference;
            if (!last)
            {
                body.Arguments = null;
                body.SetReference(description.TableReference, description.TableEntryReference);
            }
            else if (_readingSpell != null)
            {
                body.SetReference(Table, ElementsKey);
                body.Arguments = new object[] { ElementNames(_readingSpell) };
                _view.ReadingBody.RefreshString();
            }
            else
            {
                body.Arguments = null;
                body.SetReference(Table, ElementLearnedKey);
            }
            _view.ReadingDrawing.gameObject.SetActive(last && _readingSpell != null);
            if (last && _readingSpell != null) _view.ReadingDrawing.SetStrokes(_readingSpell.Shape.PartStrokes.SelectMany(part => part), true);
            // 요소 그림 (없으면 끔)
            Sprite icon = _readingElement != null ? _readingElement.Icon : null;
            _view.ReadingIcon.sprite = icon;
            _view.ReadingIcon.gameObject.SetActive(last && icon != null);
            // 장 번호 글자의 String Reference
            var number = _view.ReadingPageNumber.StringReference;
            number.SetReference(Table, PageKey);
            number.Arguments = new object[] { _page + 1, ReadingPageCount };
            _view.ReadingPageNumber.RefreshString();
            _view.PrevButton.interactable = _page > 0;
            _view.NextButton.interactable = !last;
        }
        #endregion

        #region TurnPage - 장을 넘김. 마지막 장에 오면 다 읽음
        /// <param name="delta">넘길 장 수 (-1 이전, 1 다음)</param>
        void TurnPage(int delta)
        {
            if (_readingElement == null && _readingSpell == null) return;
            // 넘긴 장 번호
            int page = Mathf.Clamp(_page + delta, 0, ReadingPageCount - 1);
            if (page == _page) return;
            _page = page;
            ShowReading();
            if (_page == ReadingPageCount - 1) FinishReading();
        }
        #endregion

        #region FinishReading - 다 읽음: 주문서를 목록에서 빼고, 요소 주문서면 요소 습득·토스트, 주문 주문서면 도감 등록·토스트. 비교 그림·목록을 다시 만듦 (이미 뺀 주문서면 무시)
        void FinishReading()
        {
            if (_readingElement != null)
            {
                if (!_run.ElementScrolls.Remove(_readingElement)) return;
                if (!_run.LearnedElements.Contains(_readingElement))
                {
                    _run.LearnedElements.Add(_readingElement);
                    UIManager.Instance.ShowToast(Table, LearnedKey, _readingElement.Name.GetLocalizedString());
                }
            }
            else
            {
                if (!_run.SpellScrolls.Remove(_readingSpell)) return;
                Register(_readingSpell);
            }
            RebuildCandidates();
            RebuildList();
        }
        #endregion

        #region SelectPractice - 연습 장: 제목을 넣고 그리기를 처음부터 받음. 도감 주문이면 그 그림을 흐리게 깖
        /// <param name="spell">따라 그릴 주문 (자유 그리기면 null)</param>
        void SelectPractice(SpellSO spell)
        {
            HidePages();
            _view.PracticePage.SetActive(true);
            // 제목 글자의 String Reference
            var title = _view.PracticeTitle.StringReference;
            if (spell != null) title.SetReference(spell.Name.TableReference, spell.Name.TableEntryReference);
            else title.SetReference(Table, FreeDrawKey);
            _view.Draw.Begin(_spells, Judge, OnJudged);
            _view.Draw.ShowGuide(spell);
        }
        #endregion

        #region Judge - 그린 획 판정 (기준은 기본값, 임시)
        /// <param name="strokes">그린 획</param>
        SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes) =>
            SpellRecognitionCalculator.Judge(strokes, _candidates, _learnedIds, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        #endregion

        #region OnJudged - 판정 뒤: 주문 그리기를 배움으로 (직접 취소 빼고). 생략 없이 시전한 미등록 주문이면 도감 등록
        /// <param name="spec">판정 결과</param>
        void OnJudged(SpellJudgeSpec spec)
        {
            if (_run == null) return;
            if (spec.Reason != SpellJudgeReason.Canceled) TutorialManager.Instance.MarkLearned(DrawTutorialId);
            if (!spec.IsCast || spec.Candidate.Omit.Count > 0) return;
            // 시전한 주문 번호
            int index = spec.Candidate.ShapeIndex;
            if (index < 0 || index >= _spells.Count || !Register(_spells[index])) return;
            RebuildCandidates();
            if (_tab == Tab.Practice) RebuildList();
        }
        #endregion

        #region Register - 도감에 없으면 등록하고 토스트. 새로 등록했으면 true
        /// <param name="spell">등록할 주문</param>
        bool Register(SpellSO spell)
        {
            if (spell == null || _run.CollectedSpells.Contains(spell)) return false;
            _run.CollectedSpells.Add(spell);
            UIManager.Instance.ShowToast(Table, RegisteredKey, spell.Name.GetLocalizedString());
            return true;
        }
        #endregion

        #region RebuildCandidates - 익힌 요소·도감·생략 가능 수로 판정 비교 그림을 다시 만듦
        void RebuildCandidates()
        {
            _learnedIds.Clear();
            foreach (ElementSO element in _run.LearnedElements)
                if (element != null) _learnedIds.Add(element.Id);
            // 도감에 등록한 주문 id
            var collected = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpellSO spell in _run.CollectedSpells)
                if (spell != null) collected.Add(spell.Id);
            _candidates = SpellRecognitionCalculator.BuildCandidates(_shapes, _learnedIds, collected, _run.OmitLimit);
        }
        #endregion

        #region ElementNames - 주문 그림 부분 요소 이름 목록 (지금 언어)
        /// <param name="spell">주문</param>
        static string ElementNames(SpellSO spell) =>
            UIManager.Instance.JoinList(spell.Parts.Select(part => part.Element != null ? part.Element.Name.GetLocalizedString() : null));
        #endregion

        #region SetInteractable - 코덱스 입력 켜고 끄기 (책은 늘 보이고, 책상에 앉아 있을 때만 누를 수 있음)
        /// <param name="on">켤지</param>
        void SetInteractable(bool on)
        {
            _view.Group.interactable = on;
            _view.Group.blocksRaycasts = on;
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `HubState.OnSceneReady` → `ShowName(PlayerName)`. `HubState.OpenCodex` → `Open(판, DataManager.Spells, LeaveDesk)`. `HubState.LeaveDesk`·`Exit` → `Close`. `HubState.HandleCancel` → `IsCasting`·`CancelCast`
- 일어나기 버튼 → `_onLeave`(= `HubState.LeaveDesk`)
- `SpellDrawController` 판정 → `Judge` → `OnJudged`

---

### `Assets/Scripts/UI/Sequences/DeskSequence.cs` (새 파일)

**추가 내용:** 머리(카메라)를 책상 시점 자리로 옮기고 되돌리는 연출 (게임 시간, 부드럽게)

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>책상 시점 연출: 플레이어 머리(카메라)를 책상 시점 자리로 부드럽게 옮기고, 일어날 때 원래 자리로 되돌림. 게임 시간이라 일시정지 중엔 멈춤</summary>
    public sealed class DeskSequence : MonoBehaviour
    {
        /// <summary>옮기는 데 걸리는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _seconds = 0.5f;

        /// <summary>옮긴 머리 (앉아 있는 동안만. 아니면 null)</summary>
        Transform _head;

        /// <summary>머리 원래 로컬 위치 (앉기 전)</summary>
        Vector3 _homePosition;

        /// <summary>머리 원래 로컬 회전 (앉기 전)</summary>
        Quaternion _homeRotation;

        /// <summary>도는 연출. 없으면 null</summary>
        Coroutine _routine;

        /// <summary>연출이 도는 중인지</summary>
        public bool IsPlaying => _routine != null;

        #region Enter - 머리 원래 자리를 적어 두고 책상 시점 자리로 옮김. 다 옮기면 onDone
        /// <param name="head">옮길 머리 (카메라)</param>
        /// <param name="seat">책상 시점 자리</param>
        /// <param name="onDone">다 옮긴 뒤 할 일</param>
        public void Enter(Transform head, Transform seat, Action onDone)
        {
            if (head == null) throw new ArgumentNullException(nameof(head));
            if (seat == null) throw new ArgumentNullException(nameof(seat));
            Stop();
            _head = head;
            _homePosition = head.localPosition;
            _homeRotation = head.localRotation;
            _routine = StartCoroutine(Move(() => new Pose(seat.position, seat.rotation), onDone));
        }
        #endregion

        #region Exit - 머리를 원래 자리로 되돌림. 다 되돌리면 로컬 자리를 그대로 맞추고 onDone (앉아 있지 않으면 바로 onDone)
        /// <param name="onDone">다 되돌린 뒤 할 일</param>
        public void Exit(Action onDone)
        {
            if (_head == null)
            {
                onDone?.Invoke();
                return;
            }
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Move(HomePose, () =>
            {
                Restore();
                onDone?.Invoke();
            }));
        }
        #endregion

        #region Stop - 연출을 멈추고 머리를 원래 로컬 자리로 바로 되돌림 (상태를 나갈 때)
        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            Restore();
        }
        #endregion

        #region HomePose - 머리 원래 자리 (월드. 부모가 움직여도 따라감)
        Pose HomePose()
        {
            // 머리 부모 (플레이어)
            Transform parent = _head.parent;
            return parent != null
                ? new Pose(parent.TransformPoint(_homePosition), parent.rotation * _homeRotation)
                : new Pose(_homePosition, _homeRotation);
        }
        #endregion

        #region Restore - 머리를 원래 로컬 자리로 맞추고 놓음
        void Restore()
        {
            if (_head == null) return;
            _head.localPosition = _homePosition;
            _head.localRotation = _homeRotation;
            _head = null;
        }
        #endregion

        #region Move - 지금 자리에서 목표 자리로 정해진 시간 동안 부드럽게 (처음·끝 느리게). 끝나면 onDone
        /// <param name="target">목표 자리 (매 프레임 다시 읽음)</param>
        /// <param name="onDone">다 옮긴 뒤 할 일</param>
        IEnumerator Move(Func<Pose> target, Action onDone)
        {
            // 출발 자리 (월드)
            Vector3 fromPosition = _head.position;
            Quaternion fromRotation = _head.rotation;
            // 진행 (0~1)
            float t = 0f;
            while (t < 1f)
            {
                t = _seconds > 0f ? Mathf.Min(1f, t + Time.deltaTime / _seconds) : 1f;
                // 부드러운 진행
                float k = Mathf.SmoothStep(0f, 1f, t);
                // 이번 프레임 목표
                Pose to = target();
                _head.SetPositionAndRotation(Vector3.Lerp(fromPosition, to.position, k), Quaternion.Slerp(fromRotation, to.rotation, k));
                yield return null;
            }
            _routine = null;
            onDone?.Invoke();
        }
        #endregion
    }
}
```

**호출 관계:** `HubState.UseDesk` → `Enter(Player.Head, DeskSeat, OpenCodex)`. `HubState.LeaveDesk` → `Exit(OnDeskLeft)`. `HubState.Exit` → `Stop`

---

### `Assets/Scripts/UI/Views/HubView.cs`

**변경 내용:** `_returnPoint` 아래에 칸 3개, `ReturnPoint` 프로퍼티 아래에 프로퍼티 3개

```csharp
        /// <summary>코덱스 (RoomInterior/Codex의 CodexController. 인스펙터에서만 연결)</summary>
        [SerializeField] CodexController _codex;

        /// <summary>책상 시점 연출 (RoomInterior/Codex의 DeskSequence. 인스펙터에서만 연결)</summary>
        [SerializeField] DeskSequence _deskSequence;

        /// <summary>책상 시점 자리 (머리가 옮겨 갈 위치·방향. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _deskSeat;

        // ...

        /// <summary>코덱스</summary>
        public CodexController Codex => _codex;

        /// <summary>책상 시점 연출</summary>
        public DeskSequence DeskSequence => _deskSequence;

        /// <summary>책상 시점 자리</summary>
        public Transform DeskSeat => _deskSeat;
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 씬 준비 때 코덱스 이름, 코덱스 지점 → 앉기(조작·화면 글자 끔, 커서 풂, 머리 이동 → 코덱스 쓰기), 일어나기·Esc(그리는 중이면 그리기 취소). 클래스 설명에 `여관 방 책상(코덱스: 주문서·연습)` 추가

`RoomDoorId` 아래:

```csharp
        /// <summary>여관 방 책상 위 코덱스 지점 id</summary>
        const string CodexId = "codex";
```

`_returning` 아래:

```csharp
        /// <summary>책상 시점인지 (앉는·일어나는 연출 중 포함)</summary>
        bool _atDesk;
```

`OnSceneReady` 끝 (`RefreshParty();`·`RefreshMoney();` 다음 줄):

```csharp
            _view.Codex.ShowName(Game.Run.PlayerName);
```

`OnInteracted`의 switch (region 설명 `… 여관 주인·접수원·게시판·경비병·코덱스 …`):

```csharp
                case GuardId: TalkGuard(); break;
                case CodexId: UseDesk(); break;   // ← 추가
```

`Exit`의 `if (_view != null)` 안, `SetControl(false);` 앞:

```csharp
                if (_atDesk)
                {
                    _view.Codex.Close();
                    _view.DeskSequence.Stop();
                    _atDesk = false;
                }
```

`UseDoor` region 아래에 새 region 5개:

```csharp
        #region UseDesk - 책상에 앉음: 조작·화면 글자를 끄고 커서를 풀고, 머리를 책상 시점으로 옮긴 뒤 코덱스를 엶
        void UseDesk()
        {
            if (_atDesk) return;
            _atDesk = true;
            SetControl(false);
            _view.Hud.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            _view.DeskSequence.Enter(_view.Player.Head, _view.DeskSeat, OpenCodex);
        }
        #endregion

        #region OpenCodex - 코덱스를 엶 (판 데이터·주문 목록을 넘김, 일어나기는 LeaveDesk)
        void OpenCodex()
        {
            if (_view == null) return;
            _view.Codex.Open(Game.Run, DataManager.Instance.Spells, LeaveDesk);
        }
        #endregion

        #region LeaveDesk - 책상에서 일어남: 코덱스를 그만 쓰고 커서를 잠그고, 머리를 원래 자리로 되돌린 뒤 조작을 돌려줌 (연출 중이면 무시)
        void LeaveDesk()
        {
            if (!_atDesk || _view == null || _view.DeskSequence.IsPlaying) return;
            _view.Codex.Close();
            Cursor.lockState = CursorLockMode.Locked;
            _view.DeskSequence.Exit(OnDeskLeft);
        }
        #endregion

        #region OnDeskLeft - 일어나기 연출이 끝남: 화면 글자·조작 켬
        void OnDeskLeft()
        {
            _atDesk = false;
            if (_view == null) return;
            _view.Hud.SetActive(true);
            SetControl(true);
        }
        #endregion

        #region HandleCancel - Esc: 책상이면 그리는 중엔 그리기 취소, 아니면 일어남 (일시정지 메뉴 안 엶). 책상이 아니면 처리 안 함
        public override bool HandleCancel()
        {
            if (!_atDesk || _view == null) return false;
            if (_view.Codex.IsCasting) _view.Codex.CancelCast();
            else LeaveDesk();
            return true;
        }
        #endregion
```

**호출/구독 관계:**
- `InteractDetector.Interacted`(코덱스, F) → `OnInteracted` → `UseDesk` → `DeskSequence.Enter` → `OpenCodex` → `CodexController.Open`
- 일어나기 버튼·Esc(`UIManager.OnCancel` → `GameManager.HandleCancel` → `HandleCancel`) → `LeaveDesk` → `DeskSequence.Exit` → `OnDeskLeft`
- M·J 메뉴 창은 책상에서도 열림 (`SetMenuPaused`가 커서를 연 뒤 상태로 되돌려서 책상에선 풀린 채)

---

### `Assets/Scripts/UI/Views/GameMenuView.cs`

**변경 내용:** 주문 도감 탭 칸 (코덱스 이름 머리글 포함). `_tutorialEmpty` 아래·`TutorialEmpty` 프로퍼티 아래에 추가 (목록 항목은 `_tutorialItemPrefab`을 같이 씀)

```csharp
        /// <summary>주문 도감: 코덱스 이름 머리글 (Codex/codex.name, {0} 플레이어 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _codexName;

        /// <summary>주문 도감: 주문 목록 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _spellList;

        /// <summary>주문 도감: 고른 주문 자세히 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _spellDetail;

        /// <summary>주문 도감: 주문 이름 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _spellTitle;

        /// <summary>주문 도감: 종류 (전투 주문·두루마리. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _spellKind;

        /// <summary>주문 도감: 필요한 요소 (Menu/menu.spells.elements, {0} 요소 목록. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _spellElements;

        /// <summary>주문 도감: 주문 그림 (인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _spellDrawing;

        /// <summary>주문 도감: 주문 설명 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _spellBody;

        /// <summary>주문 도감: 등록한 주문이 없을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _spellsEmpty;

        // ...

        /// <summary>주문 도감: 코덱스 이름 머리글</summary>
        public LocalizeStringEvent CodexName => _codexName;

        /// <summary>주문 도감: 주문 목록 자리</summary>
        public RectTransform SpellList => _spellList;

        /// <summary>주문 도감: 고른 주문 자세히</summary>
        public GameObject SpellDetail => _spellDetail;

        /// <summary>주문 도감: 주문 이름</summary>
        public LocalizeStringEvent SpellTitle => _spellTitle;

        /// <summary>주문 도감: 종류</summary>
        public LocalizeStringEvent SpellKind => _spellKind;

        /// <summary>주문 도감: 필요한 요소</summary>
        public LocalizeStringEvent SpellElements => _spellElements;

        /// <summary>주문 도감: 주문 그림</summary>
        public StrokeDrawer SpellDrawing => _spellDrawing;

        /// <summary>주문 도감: 주문 설명</summary>
        public LocalizeStringEvent SpellBody => _spellBody;

        /// <summary>주문 도감: 등록한 주문이 없을 때 글</summary>
        public GameObject SpellsEmpty => _spellsEmpty;
```

---

### `Assets/Scripts/UI/Binders/SpellCodexUIBinder.cs` (새 파일)

**추가 내용:** 주문 도감 탭 ← 이번 판 도감 등록 주문 (탭이 켜질 때마다 다시 만듦)

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>주문 도감 탭 ← 이번 판 코덱스 (도감에 등록한 주문). 탭이 켜질 때 코덱스 이름 머리글과 목록을 다시 만들고, 항목을 누르면 이름·종류·필요한 요소·그림·설명</summary>
    public sealed class SpellCodexUIBinder : MonoBehaviour
    {
        /// <summary>필요한 요소 키 ({0} 요소 목록)</summary>
        const string ElementsKey = "menu.spells.elements";

        /// <summary>전투 주문 종류 키</summary>
        const string CombatKey = "menu.spells.kind.combat";

        /// <summary>두루마리 종류 키</summary>
        const string ScrollKey = "menu.spells.kind.scroll";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference Table = "Menu";

        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>지금 만든 목록 항목</summary>
        readonly List<TutorialListItemView> _items = new();

        #region OnEnable - 목록 만들기
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] SpellCodexUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            Rebuild();
        }
        #endregion

        #region Rebuild - 코덱스 이름 머리글을 넣고, 목록 항목을 지우고 등록 순서대로 다시 만듦. 없으면 빈 글, 있으면 첫 주문 자세히
        void Rebuild()
        {
            foreach (TutorialListItemView item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();

            // 이번 판 데이터 (없으면 null)
            RunEntry run = GameManager.Instance.Run;
            // 코덱스 이름 머리글 ({0} 플레이어 이름)
            _view.CodexName.StringReference.Arguments = new object[] { run != null ? run.PlayerName : string.Empty };
            _view.CodexName.RefreshString();
            // 이번 판 도감 등록 주문 (판이 없으면 null)
            IReadOnlyList<SpellSO> spells = run?.CollectedSpells;
            // 목록 첫 주문 (자세히를 처음 보일 것. 없으면 null)
            SpellSO first = null;
            if (spells != null)
                foreach (SpellSO spell in spells)
                {
                    if (spell == null) continue;
                    // 이 주문 항목
                    TutorialListItemView item = Instantiate(_view.TutorialItemPrefab, _view.SpellList);
                    item.Title.StringReference.SetReference(spell.Name.TableReference, spell.Name.TableEntryReference);
                    // 버튼이 누를 때 쓸 주문 (반복 변수를 붙잡지 않게)
                    SpellSO picked = spell;
                    item.Button.onClick.AddListener(() => ShowDetail(picked));
                    _items.Add(item);
                    if (first == null) first = spell;
                }
            _view.SpellsEmpty.SetActive(first == null);
            _view.SpellDetail.SetActive(first != null);
            if (first != null) ShowDetail(first);
        }
        #endregion

        #region ShowDetail - 고른 주문의 이름·종류·필요한 요소·설명 키를 넣고 그림을 그림
        /// <param name="spell">고른 주문</param>
        void ShowDetail(SpellSO spell)
        {
            _view.SpellTitle.StringReference.SetReference(spell.Name.TableReference, spell.Name.TableEntryReference);
            _view.SpellKind.StringReference.SetReference(Table, spell.Kind == SpellKind.Scroll ? ScrollKey : CombatKey);
            // 필요한 요소 글자의 String Reference
            var elements = _view.SpellElements.StringReference;
            elements.SetReference(Table, ElementsKey);
            elements.Arguments = new object[]
            {
                UIManager.Instance.JoinList(spell.Parts.Select(part => part.Element != null ? part.Element.Name.GetLocalizedString() : null)),
            };
            _view.SpellElements.RefreshString();
            _view.SpellBody.StringReference.SetReference(spell.Description.TableReference, spell.Description.TableEntryReference);
            _view.SpellDrawing.SetStrokes(spell.Shape.PartStrokes.SelectMany(part => part), true);
        }
        #endregion
    }
}
```

---

### `Assets/Tests/EditMode/Mage.Tests.EditMode.asmdef` (새 파일)

**추가 내용:** 게임 EditMode 테스트 어셈블리 (`Mage` 참조, 에디터만, 키트 테스트와 같은 형식). `Assets/Tests/EditMode/` 폴더 새로

```json
{
    "name": "Mage.Tests.EditMode",
    "rootNamespace": "Mage.Tests",
    "references": [
        "Mage",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

### `Assets/Tests/EditMode/SpellRecognitionCalculatorTests.cs` (새 파일)

**추가 내용:** 인식기 판정 테스트 21개. 그림은 데이터 에셋과 같은 값(y 위가 +)을 코드로 만들고, 기대값은 프로토타입 `spell-core.js`에 같은 그림(y 뒤집기 전)을 넣어 확인한 값. 회전 각도는 y를 뒤집은 만큼 부호를 바꿈

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Mage.Tests
{
    /// <summary>주문 그림 인식기 판정 (기대값은 프로토타입 spell-core.js에 같은 그림을 넣어 확인한 값. 그림 데이터는 Assets/Data/Spells와 같음)</summary>
    public sealed class SpellRecognitionCalculatorTests
    {
        /// <summary>그림 배율 (주문 그림 좌표 1 = 그리기 칸 100)</summary>
        const float DrawScale = 100f;

        /// <summary>요소 id: 불</summary>
        const string Fire = "fire";

        /// <summary>요소 id: 응축</summary>
        const string Condense = "condense";

        /// <summary>요소 id: 직선</summary>
        const string Line = "line";

        /// <summary>요소 id: 차단</summary>
        const string Block = "block";

        /// <summary>요소 id: 물 (시험용)</summary>
        const string Water = "water";

        /// <summary>요소 id: 확산 (시험용)</summary>
        const string Spread = "spread";

        /// <summary>요소 id: 벽 (시험용)</summary>
        const string Wall = "wall";

        /// <summary>그리기 칸 안 그림 중심</summary>
        static readonly Vector2 DrawCenter = new(300f, 300f);

        /// <summary>요소 전부</summary>
        static readonly string[] AllElements = { Fire, Condense, Line, Block, Water, Spread, Wall };

        /// <summary>불: 정방향 세모</summary>
        static readonly Vector2[] Tri = { new(0f, 1f), new(0.52f, 0.1f), new(-0.52f, 0.1f), new(0f, 1f) };

        /// <summary>직선: 세모 위 꼭짓점에서 내리그음</summary>
        static readonly Vector2[] Shaft = { new(0f, 1f), new(0f, -1f) };

        /// <summary>응축: 세모를 감싼 원</summary>
        static readonly Vector2[] Orb = Circle(32, 0f, 0.45f, 0.9f);

        /// <summary>차단: 화살대 아래 오른쪽 Z</summary>
        static readonly Vector2[] Zed = { new(0.25f, -0.55f), new(0.75f, -0.55f), new(0.25f, -0.95f), new(0.75f, -0.95f) };

        /// <summary>물: 화살대 머리 물결</summary>
        static readonly Vector2[] WaveHead = Wave(24, -0.6f, 0.6f, 0.75f, 0.18f);

        /// <summary>직선 (물 투사)</summary>
        static readonly Vector2[] ShaftW = { new(0f, 0.75f), new(0f, -1f) };

        /// <summary>불 (화염 확산)</summary>
        static readonly Vector2[] TriLow = { new(0f, 0.2f), new(0.52f, -0.7f), new(-0.52f, -0.7f), new(0f, 0.2f) };

        /// <summary>확산: 세모 꼭짓점에서 벌어지는 V</summary>
        static readonly Vector2[] Fan = { new(-0.7f, 1f), new(0f, 0.2f), new(0.7f, 1f) };

        /// <summary>불 (화염 벽)</summary>
        static readonly Vector2[] TriHigh = { new(0f, 0.9f), new(0.52f, 0f), new(-0.52f, 0f), new(0f, 0.9f) };

        /// <summary>벽: 세모 허리를 가로지르는 막대</summary>
        static readonly Vector2[] BarLow = { new(-1.1f, 0.35f), new(1.1f, 0.35f) };

        /// <summary>응축 (방호막)</summary>
        static readonly Vector2[] Orb0 = Circle(32, 0f, 0f, 0.85f);

        /// <summary>벽: 원을 가로지르는 막대</summary>
        static readonly Vector2[] BarMid = { new(-1.1f, 0f), new(1.1f, 0f) };

        /// <summary>차단 (방호막 두루마리)</summary>
        static readonly Vector2[] ZedLow = { new(-0.3f, -1.1f), new(0.3f, -1.1f), new(-0.3f, -1.5f), new(0.3f, -1.5f) };

        /// <summary>주문 그림 9개 (데이터 에셋·DataManager와 같은 순서)</summary>
        static readonly SpellShapeEntry[] Shapes =
        {
            Shape("flame_shot", (Fire, Tri), (Line, Shaft)),
            Shape("fireball", (Fire, Tri), (Condense, Orb), (Line, Shaft)),
            Shape("water_shot", (Water, WaveHead), (Line, ShaftW)),
            Shape("flame_fan", (Fire, TriLow), (Spread, Fan)),
            Shape("flame_wall", (Fire, TriHigh), (Wall, BarLow)),
            Shape("shield", (Condense, Orb0), (Wall, BarMid)),
            Shape("fireball_scroll", (Fire, Tri), (Condense, Orb), (Line, Shaft), (Block, Zed)),
            Shape("flame_shot_scroll", (Fire, Tri), (Line, Shaft), (Block, Zed)),
            Shape("shield_scroll", (Condense, Orb0), (Wall, BarMid), (Block, ZedLow)),
        };

        #region ExactDrawingCastsItsOwnSpell - 주문 그림을 그대로 그리면 그 주문 (생략 없음, 정확)
        /// <param name="id">주문 id</param>
        [Test]
        public void ExactDrawingCastsItsOwnSpell([ValueSource(nameof(SpellIds))] string id)
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Draw(id));
            Assert.That(CastId(spec), Is.EqualTo(id));
            Assert.That(spec.Candidate.Omit, Is.Empty);
            Assert.That(spec.Accuracy, Is.GreaterThanOrEqualTo(SpellRecognitionCalculator.ExactAccuracy));
        }
        #endregion

        #region FireballWithoutCondenseCastsFlameShot - 화염구에서 응축을 빼면 화살표라 화염 투사
        [Test]
        public void FireballWithoutCondenseCastsFlameShot() =>
            Assert.That(CastId(Judge(Without("fireball", Condense))), Is.EqualTo("flame_shot"));
        #endregion

        #region ScrollWithoutBlockCastsCombatSpell - 두루마리에서 차단을 빼면 전투 주문
        [Test]
        public void ScrollWithoutBlockCastsCombatSpell() =>
            Assert.That(CastId(Judge(Without("fireball_scroll", Block))), Is.EqualTo("fireball"));
        #endregion

        #region FlameShotWithoutLineHasNoMatch - 세모만 그리면 맞는 주문 없음
        [Test]
        public void FlameShotWithoutLineHasNoMatch() =>
            Assert.That(Judge(Without("flame_shot", Line)).Reason, Is.EqualTo(SpellJudgeReason.NoMatch));
        #endregion

        #region HalfOrbIsCanceledForParts - 화염구의 원을 반만 그리면 부분 부족 (화염구, 응축이 부분 기준 아래)
        [Test]
        public void HalfOrbIsCanceledForParts()
        {
            // 세모 + 원 앞쪽 절반 + 화살대
            var strokes = new List<Vector2[]>
            {
                Scaled(Tri),
                Scaled(SpellRecognitionCalculator.Resample(Orb, 100).Take(50)),
                Scaled(Shaft),
            };
            // 판정 결과
            SpellJudgeSpec spec = Judge(strokes);
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Parts));
            Assert.That(spec.Candidate.Shape.Id, Is.EqualTo("fireball"));
            // 응축 부분 번호
            int condense = spec.Candidate.PartElementIds.ToList().IndexOf(Condense);
            Assert.That(spec.PartCoverage[condense], Is.LessThan(SpellRecognitionCalculator.DefaultPartMin));
        }
        #endregion

        #region RotatedOrStretchedDrawingsStillCast - 조금 돌리거나 늘려도 시전
        [Test]
        public void RotatedOrStretchedDrawingsStillCast()
        {
            Assert.That(CastId(Judge(Transform(Draw("fireball"), -0.2f, 1.1f, 1f))), Is.EqualTo("fireball"));
            Assert.That(CastId(Judge(Transform(Draw("flame_shot"), 0.25f, 1f, 0.9f))), Is.EqualTo("flame_shot"));
        }
        #endregion

        #region TinyDrawingIsTooSmall - 최소 크기보다 작으면 너무 작음
        [Test]
        public void TinyDrawingIsTooSmall() =>
            Assert.That(Judge(Draw("fireball", 20f)).Reason, Is.EqualTo(SpellJudgeReason.Small));
        #endregion

        #region NothingDrawnIsEmpty - 안 그리면 빈 그림
        [Test]
        public void NothingDrawnIsEmpty() =>
            Assert.That(Judge(new List<Vector2[]>()).Reason, Is.EqualTo(SpellJudgeReason.Empty));
        #endregion

        #region NonSpellShapesAreCanceled - 가로선은 맞는 주문 없음, 네모는 방호막 부분 부족
        [Test]
        public void NonSpellShapesAreCanceled()
        {
            Assert.That(Judge(new List<Vector2[]> { Scaled(new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f) }) }).Reason,
                Is.EqualTo(SpellJudgeReason.NoMatch));
            // 네모 판정 결과
            SpellJudgeSpec square = Judge(new List<Vector2[]>
            {
                Scaled(new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f), new Vector2(-1f, 1f) }),
            });
            Assert.That(square.Reason, Is.EqualTo(SpellJudgeReason.Parts));
            Assert.That(square.Candidate.Shape.Id, Is.EqualTo("shield"));
        }
        #endregion

        #region UnlearnedElementIsCanceled - 응축을 안 익힌 채 화염구를 그리면 미습득 (빠진 요소 응축)
        [Test]
        public void UnlearnedElementIsCanceled()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Draw("fireball"), learned: AllElements.Where(e => e != Condense));
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Unlearned));
            Assert.That(spec.Candidate.Shape.Id, Is.EqualTo("fireball"));
            Assert.That(spec.MissingElementIds, Is.EqualTo(new[] { Condense }));
        }
        #endregion

        #region OmissionNeedsCollectedSpell - 생략은 도감에 등록한 주문만: 미등록이면 부분 부족, 등록했으면 화염구 (직선 생략)
        [Test]
        public void OmissionNeedsCollectedSpell()
        {
            Assert.That(Judge(Without("fireball", Line), omit: 1).Reason, Is.EqualTo(SpellJudgeReason.Parts));
            // 다 등록했을 때 판정 결과
            SpellJudgeSpec spec = Judge(Without("fireball", Line), collected: SpellIds(), omit: 1);
            Assert.That(CastId(spec), Is.EqualTo("fireball"));
            Assert.That(spec.Candidate.Omit, Is.EqualTo(new[] { 2 }));
        }
        #endregion

        #region SameDrawingPrefersFewerOmissions - 생략본이 다른 주문 그림과 같으면 생략이 적은 쪽
        [Test]
        public void SameDrawingPrefersFewerOmissions()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Without("fireball", Condense), collected: SpellIds(), omit: 1);
            Assert.That(CastId(spec), Is.EqualTo("flame_shot"));
            Assert.That(spec.Candidate.Omit, Is.Empty);
        }
        #endregion

        #region LookAlikeOmissionsAreAmbiguous - 세로선 하나는 화염 투사(불 생략)·물 투사(물 생략)라 하나로 안 정해짐
        [Test]
        public void LookAlikeOmissionsAreAmbiguous()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(new List<Vector2[]> { Scaled(new[] { new Vector2(0f, 1f), new Vector2(0f, -1f) }) }, collected: SpellIds(), omit: 1);
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Ambiguous));
            Assert.That(spec.AmbiguousShapes.Select(i => Shapes[i].Id), Is.EquivalentTo(new[] { "flame_shot", "water_shot" }));
        }
        #endregion

        #region SpellIds - 주문 id 전부 (ValueSource·전부 등록용)
        static IEnumerable<string> SpellIds() => Shapes.Select(shape => shape.Id);
        #endregion

        #region Judge - 기본 기준으로 판정 (익힌 요소 기본 전부, 등록 기본 없음, 생략 기본 0)
        /// <param name="strokes">그린 획</param>
        /// <param name="learned">익힌 요소 id</param>
        /// <param name="collected">도감에 등록한 주문 id</param>
        /// <param name="omit">생략 가능 수</param>
        static SpellJudgeSpec Judge(List<Vector2[]> strokes, IEnumerable<string> learned = null, IEnumerable<string> collected = null, int omit = 0)
        {
            // 익힌 요소, 등록한 주문
            var learnedSet = new HashSet<string>(learned ?? AllElements);
            var collectedSet = new HashSet<string>(collected ?? Enumerable.Empty<string>());
            // 비교 그림
            List<SpellCandidateEntry> candidates = SpellRecognitionCalculator.BuildCandidates(Shapes, learnedSet, collectedSet, omit);
            return SpellRecognitionCalculator.Judge(strokes, candidates, learnedSet, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        }
        #endregion

        #region CastId - 시전한 주문 id (취소면 이유를 담은 글, 실패 메시지용)
        /// <param name="spec">판정 결과</param>
        static string CastId(SpellJudgeSpec spec) => spec.IsCast ? spec.Candidate.Shape.Id : "취소 " + spec.Reason;
        #endregion

        #region Draw - 주문 그림을 그리기 칸 크기로 그대로 그림
        /// <param name="id">주문 id</param>
        /// <param name="scale">배율</param>
        static List<Vector2[]> Draw(string id, float scale = DrawScale) => Strokes(Find(id), scale, null);
        #endregion

        #region Without - 주문 그림에서 요소 부분 하나를 빼고 그림
        /// <param name="id">주문 id</param>
        /// <param name="element">뺄 요소 id</param>
        static List<Vector2[]> Without(string id, string element) => Strokes(Find(id), DrawScale, element);
        #endregion

        #region Strokes - 주문 그림의 획을 배율·중심으로 옮김 (뺄 요소 부분은 건너뜀)
        /// <param name="shape">주문 그림</param>
        /// <param name="scale">배율</param>
        /// <param name="skip">뺄 요소 id (없으면 null)</param>
        static List<Vector2[]> Strokes(SpellShapeEntry shape, float scale, string skip)
        {
            // 옮긴 획
            var strokes = new List<Vector2[]>();
            for (int i = 0; i < shape.PartElementIds.Count; i++)
            {
                if (shape.PartElementIds[i] == skip) continue;
                foreach (Vector2[] stroke in shape.PartStrokes[i]) strokes.Add(Scaled(stroke, scale));
            }
            return strokes;
        }
        #endregion

        #region Scaled - 주문 그림 좌표 점을 그리기 칸 좌표로
        /// <param name="points">주문 그림 좌표 점</param>
        /// <param name="scale">배율</param>
        static Vector2[] Scaled(IEnumerable<Vector2> points, float scale = DrawScale) => points.Select(p => DrawCenter + p * scale).ToArray();
        #endregion

        #region Transform - 그림을 점 평균 기준으로 늘리고(가로·세로) 돌림
        /// <param name="strokes">획</param>
        /// <param name="angle">회전 (라디안)</param>
        /// <param name="sx">가로 배율</param>
        /// <param name="sy">세로 배율</param>
        static List<Vector2[]> Transform(List<Vector2[]> strokes, float angle, float sx, float sy)
        {
            // 점 평균
            Vector2 center = Vector2.zero;
            int count = 0;
            foreach (Vector2[] stroke in strokes)
                foreach (Vector2 p in stroke)
                {
                    center += p;
                    count++;
                }
            center /= count;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return strokes.Select(stroke => stroke.Select(p =>
            {
                // 늘린 편차
                float x = (p.x - center.x) * sx, y = (p.y - center.y) * sy;
                return new Vector2(center.x + x * c - y * s, center.y + x * s + y * c);
            }).ToArray()).ToList();
        }
        #endregion

        #region Find - id로 주문 그림
        /// <param name="id">주문 id</param>
        static SpellShapeEntry Find(string id) => Shapes.First(shape => shape.Id == id);
        #endregion

        #region Shape - 부분마다 (요소 id, 획 하나)로 주문 그림
        /// <param name="id">주문 id</param>
        /// <param name="parts">부분 (요소 id, 획)</param>
        static SpellShapeEntry Shape(string id, params (string element, Vector2[] stroke)[] parts) =>
            new(id, parts.Select(part => part.element).ToArray(), parts.Select(part => new[] { part.stroke }).ToArray());
        #endregion

        #region Circle - 원 점 k+1개 (위에서 시작해 시계 방향, 소수 셋째 자리 반올림. 프로토타입 circlePoly를 y 위 +로)
        /// <param name="k">칸 수</param>
        /// <param name="cx">중심 x</param>
        /// <param name="cy">중심 y</param>
        /// <param name="r">반지름</param>
        static Vector2[] Circle(int k, float cx, float cy, float r)
        {
            // 원 점
            var points = new Vector2[k + 1];
            for (int i = 0; i <= k; i++)
            {
                // 각도 (라디안)
                double t = (double)i / k * Math.PI * 2;
                points[i] = new Vector2(Round(cx + r * Math.Sin(t)), Round(cy + r * Math.Cos(t)));
            }
            return points;
        }
        #endregion

        #region Wave - 물결 점 k+1개 (x0 → x1, 세 번 반 출렁임, 소수 셋째 자리 반올림. 프로토타입 wavePoly를 y 위 +로)
        /// <param name="k">칸 수</param>
        /// <param name="x0">시작 x</param>
        /// <param name="x1">끝 x</param>
        /// <param name="cy">가운데 y</param>
        /// <param name="amp">높이</param>
        static Vector2[] Wave(int k, float x0, float x1, float cy, float amp)
        {
            // 물결 점
            var points = new Vector2[k + 1];
            for (int i = 0; i <= k; i++)
            {
                // 진행 (0~1)
                double t = (double)i / k;
                points[i] = new Vector2(Round(x0 + (x1 - x0) * t), Round(cy + amp * Math.Sin(t * Math.PI * 3)));
            }
            return points;
        }
        #endregion

        #region Round - 소수 셋째 자리 반올림 (0에서 먼 쪽, 프로토타입 toFixed(3)과 같음)
        /// <param name="value">값</param>
        static float Round(double value) => (float)Math.Round(value, 3, MidpointRounding.AwayFromZero);
        #endregion
    }
}
```

---

## 입력 변경

### `Assets/InputSystem_Actions.inputactions`

`Player` 묶음에 액션 `Cast` (Button) + 바인딩 `<Keyboard>/space` (그룹 `Keyboard&Mouse`). JSON에 직접 넣음 (기존 줄 그대로). `Jump`도 Space지만 쓰는 코드가 없어서 그대로 둠

1. `Player` `actions`의 마지막 `Sprint` 객체 뒤:

```json
                {
                    "name": "Cast",
                    "type": "Button",
                    "id": "0669208a-4325-4d1b-b56c-1e3f483c76c1",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
```

2. `Player` `bindings`의 마지막 (`<Keyboard>/c`, Crouch, id `36e52cba-…`) 객체 뒤:

```json
                {
                    "name": "",
                    "id": "fae56ce7-9711-4109-a391-d922398be249",
                    "path": "<Keyboard>/space",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Cast",
                    "isComposite": false,
                    "isPartOfComposite": false
                }
```

---

## 에셋 변경

### `Assets/Data/Spells/` (새로, SpellSO 9개)

획 데이터 (y 위가 +. 프로토타입 값의 y를 뒤집음. 원·물결은 소수 셋째 자리 반올림, 0에서 먼 쪽):

| 이름 | 점 |
|---|---|
| TRI | (0, 1) (0.52, 0.1) (-0.52, 0.1) (0, 1) |
| SHAFT | (0, 1) (0, -1) |
| ORB | 원 32칸: 중심 (0, 0.45), 반지름 0.9, 위 (0, 1.35)에서 시계 방향, 점 33개 (`x = 0.9 sin t`, `y = 0.45 + 0.9 cos t`) |
| ZED | (0.25, -0.55) (0.75, -0.55) (0.25, -0.95) (0.75, -0.95) |
| WAVE_HEAD | 물결 24칸: `x = -0.6 + 1.2t`, `y = 0.75 + 0.18 sin(3πt)`, 점 25개 |
| SHAFT_W | (0, 0.75) (0, -1) |
| TRI_LOW | (0, 0.2) (0.52, -0.7) (-0.52, -0.7) (0, 0.2) |
| FAN | (-0.7, 1) (0, 0.2) (0.7, 1) |
| TRI_HIGH | (0, 0.9) (0.52, 0) (-0.52, 0) (0, 0.9) |
| BAR_LOW | (-1.1, 0.35) (1.1, 0.35) |
| ORB0 | 원 32칸: 중심 (0, 0), 반지름 0.85, 위에서 시계 방향, 점 33개 |
| BAR_MID | (-1.1, 0) (1.1, 0) |
| ZED_LOW | (-0.3, -1.1) (0.3, -1.1) (-0.3, -1.5) (0.3, -1.5) |

주문 (`_name` = Spell/`spell.{id}.name`, `_description` = Spell/`spell.{id}.desc`, 부분마다 획 1개):

| 파일 | `_id` | `_kind` | `_parts` (요소: 획) |
|---|---|---|---|
| `FlameShot.asset` | flame_shot | Combat | fire: TRI / line: SHAFT |
| `Fireball.asset` | fireball | Combat | fire: TRI / condense: ORB / line: SHAFT |
| `WaterShot.asset` | water_shot | Combat | water: WAVE_HEAD / line: SHAFT_W |
| `FlameFan.asset` | flame_fan | Combat | fire: TRI_LOW / spread: FAN |
| `FlameWall.asset` | flame_wall | Combat | fire: TRI_HIGH / wall: BAR_LOW |
| `Shield.asset` | shield | Combat | condense: ORB0 / wall: BAR_MID |
| `FireballScroll.asset` | fireball_scroll | Scroll | fire: TRI / condense: ORB / line: SHAFT / block: ZED |
| `FlameShotScroll.asset` | flame_shot_scroll | Scroll | fire: TRI / line: SHAFT / block: ZED |
| `ShieldScroll.asset` | shield_scroll | Scroll | condense: ORB0 / wall: BAR_MID / block: ZED_LOW |

빌더 (에디터에서 한 번, 커밋 안 함. 다국어 단계에서 `Spell` 테이블을 만든 뒤):

```csharp
// Spell 테이블과 Shared Data 이름 참조 (LocalizedString 칸에 넣을 값)
var spellTable = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Spell");
string tableName = "GUID:" + spellTable.SharedData.TableCollectionNameGuid.ToString("N");
// 요소 id → 에셋
var elements = new Dictionary<string, ElementSO>();
foreach (string guid in AssetDatabase.FindAssets("t:ElementSO", new[] { "Assets/Data/Elements" }))
{
    var element = AssetDatabase.LoadAssetAtPath<ElementSO>(AssetDatabase.GUIDToAssetPath(guid));
    elements[element.Id] = element;
}
// 소수 셋째 자리 반올림 (0에서 먼 쪽)
float Round(double v) => (float)Math.Round(v, 3, MidpointRounding.AwayFromZero);
Vector2[] Circle(int k, float cx, float cy, float r) =>
    Enumerable.Range(0, k + 1).Select(i => { double t = (double)i / k * Math.PI * 2; return new Vector2(Round(cx + r * Math.Sin(t)), Round(cy + r * Math.Cos(t))); }).ToArray();
Vector2[] Wave(int k, float x0, float x1, float cy, float amp) =>
    Enumerable.Range(0, k + 1).Select(i => { double t = (double)i / k; return new Vector2(Round(x0 + (x1 - x0) * t), Round(cy + amp * Math.Sin(t * Math.PI * 3))); }).ToArray();
Vector2[] P(params float[] xy) => Enumerable.Range(0, xy.Length / 2).Select(i => new Vector2(xy[2 * i], xy[2 * i + 1])).ToArray();
var tri = P(0f, 1f, .52f, .1f, -.52f, .1f, 0f, 1f);
var shaft = P(0f, 1f, 0f, -1f);
var orb = Circle(32, 0f, .45f, .9f);
var zed = P(.25f, -.55f, .75f, -.55f, .25f, -.95f, .75f, -.95f);
var waveHead = Wave(24, -.6f, .6f, .75f, .18f);
var shaftW = P(0f, .75f, 0f, -1f);
var triLow = P(0f, .2f, .52f, -.7f, -.52f, -.7f, 0f, .2f);
var fan = P(-.7f, 1f, 0f, .2f, .7f, 1f);
var triHigh = P(0f, .9f, .52f, 0f, -.52f, 0f, 0f, .9f);
var barLow = P(-1.1f, .35f, 1.1f, .35f);
var orb0 = Circle(32, 0f, 0f, .85f);
var barMid = P(-1.1f, 0f, 1.1f, 0f);
var zedLow = P(-.3f, -1.1f, .3f, -1.1f, -.3f, -1.5f, .3f, -1.5f);
if (!AssetDatabase.IsValidFolder("Assets/Data/Spells")) AssetDatabase.CreateFolder("Assets/Data", "Spells");
// LocalizedString 칸에 Spell 테이블 키를 넣음
void SetKey(SerializedProperty property, string key)
{
    property.FindPropertyRelative("m_TableReference.m_TableCollectionName").stringValue = tableName;
    property.FindPropertyRelative("m_TableEntryReference.m_KeyId").longValue = spellTable.SharedData.GetId(key);
}
// 주문 에셋 하나
void Create(string file, string id, SpellKind kind, params (string element, Vector2[] stroke)[] parts)
{
    var spell = ScriptableObject.CreateInstance<SpellSO>();
    AssetDatabase.CreateAsset(spell, $"Assets/Data/Spells/{file}.asset");
    var so = new SerializedObject(spell);
    so.FindProperty("_id").stringValue = id;
    SetKey(so.FindProperty("_name"), $"spell.{id}.name");
    SetKey(so.FindProperty("_description"), $"spell.{id}.desc");
    so.FindProperty("_kind").enumValueIndex = (int)kind;
    var list = so.FindProperty("_parts");
    list.arraySize = parts.Length;
    for (int i = 0; i < parts.Length; i++)
    {
        var part = list.GetArrayElementAtIndex(i);
        part.FindPropertyRelative("_element").objectReferenceValue = elements[parts[i].element];
        var strokes = part.FindPropertyRelative("_strokes");
        strokes.arraySize = 1;
        var points = strokes.GetArrayElementAtIndex(0).FindPropertyRelative("_points");
        points.arraySize = parts[i].stroke.Length;
        for (int k = 0; k < parts[i].stroke.Length; k++) points.GetArrayElementAtIndex(k).vector2Value = parts[i].stroke[k];
    }
    so.ApplyModifiedPropertiesWithoutUndo();
}
Create("FlameShot", "flame_shot", SpellKind.Combat, ("fire", tri), ("line", shaft));
Create("Fireball", "fireball", SpellKind.Combat, ("fire", tri), ("condense", orb), ("line", shaft));
Create("WaterShot", "water_shot", SpellKind.Combat, ("water", waveHead), ("line", shaftW));
Create("FlameFan", "flame_fan", SpellKind.Combat, ("fire", triLow), ("spread", fan));
Create("FlameWall", "flame_wall", SpellKind.Combat, ("fire", triHigh), ("wall", barLow));
Create("Shield", "shield", SpellKind.Combat, ("condense", orb0), ("wall", barMid));
Create("FireballScroll", "fireball_scroll", SpellKind.Scroll, ("fire", tri), ("condense", orb), ("line", shaft), ("block", zed));
Create("FlameShotScroll", "flame_shot_scroll", SpellKind.Scroll, ("fire", tri), ("line", shaft), ("block", zed));
Create("ShieldScroll", "shield_scroll", SpellKind.Scroll, ("condense", orb0), ("wall", barMid), ("block", zedLow));
AssetDatabase.SaveAssets();
```

만든 뒤 에셋을 다시 읽어 9개 모두 `_name`·`_description` 키, 부분 요소, 점 수(ORB·ORB0 33, WAVE_HEAD 25) 확인

### `Assets/Data/Tutorials/Draw.asset` (새로, TutorialSO)

`_id` = `draw`, `_prompt` 비움, `_title` = Tutorial/`tutorial.draw.title`, `_body` = Tutorial/`tutorial.draw.body`, `_action` = Draw, `_walkSecondsBeforeShow` = 0

---

## 씬 변경

공통: 새 글자는 `Assets/Localization/Prefabs/LocalizedText.prefab`, 버튼은 `Assets/Prefabs/UI/Button.prefab`. 런타임에 키를 넣는 글자는 글자를 비워 둠. 글자·종이·표지·StrokeDrawer은 Raycast Target 끔 (버튼·그리기 칸만 켬). 빌더에서 에셋은 씬을 연 뒤 쓰기 직전에 읽고, LocalizedString 칸은 SerializedObject로 넣음

### `Assets/Scenes/Boot.unity`

1. `Managers/UIManager/UICanvas/GameMenu/Panel/Pages/SpellsPage`: Add Component → `SpellCodexUIBinder` (`_view` → GameMenu의 GameMenuView)
   - `CodexName` 새로: 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, -10), 크기 (-40, 48), 글자 32, 가운데, 줄바꿈 끔, 키 Codex/`codex.name` (자리 값은 코드가 넣음)
   - `List` 새로: 앵커·피벗 (0, 1), 위치 (20, -70), 크기 (420, 550), VerticalLayoutGroup (Spacing 6, 위 왼쪽, Control Child Size 켬, Child Force Expand Height 끔)
   - `Detail` 새로: 앵커·피벗 (0, 1), 위치 (470, -70), 크기 (800, 560)
     - `Title`: 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, 0), 크기 (0, 50), 글자 34, 줄바꿈 끔
     - `Kind`: 같은 앵커, 위치 (0, -56), 크기 (0, 36), 글자 24, 색 (0.8, 0.75, 0.6)
     - `Elements`: 같은 앵커, 위치 (0, -96), 크기 (0, 40), 글자 26
     - `Drawing`: 빈 오브젝트 + `StrokeDrawer` (`_thickness` 6, `_fitPadding` 0.1, 색 (0.95, 0.9, 0.75, 1), Raycast Target 끔), 앵커·피벗 (0, 1), 위치 (0, -150), 크기 (300, 300)
     - `Body`: 앵커·피벗 (0, 1), 위치 (320, -150), 크기 (480, 400), 글자 26, 줄바꿈, 왼쪽 위 정렬
   - 기존 `Placeholder`(Menu/`menu.spells.placeholder`)는 그대로 = 빈 글
2. GameMenuView 칸: `_codexName` → `CodexName`, `_spellList` → `List`, `_spellDetail` → `Detail`, `_spellTitle`·`_spellKind`·`_spellElements`·`_spellBody` → Detail 자식 LocalizeStringEvent, `_spellDrawing` → `Detail/Drawing`, `_spellsEmpty` → `Placeholder`
3. `Managers/DataManager`: `_spells` = FlameShot, Fireball, WaterShot, FlameFan, FlameWall, Shield, FireballScroll, FlameShotScroll, ShieldScroll (이 순서), `_tutorials` 끝에 `Draw`
4. 저장 → 다시 열어 GameMenuView 새 칸 9개, SpellCodexUIBinder `_view`, DataManager `_spells` 9칸·`_tutorials` 5칸 확인

### `Assets/Scenes/Outpost.unity`

`Interiors/RoomInterior` (월드 (20, -40, 0)) 아래. 책 글자 색은 (0.18, 0.13, 0.08, 1)

1. `Desk`: 바꾸지 않음 (상호작용 지점은 책상 위 코덱스)
2. `DeskSeat` 새로 (빈 오브젝트): 로컬 위치 (-1.4, 1.29, 1.33), 회전 (61, 0, 0) (책상 앞 위에서 책 가운데를 내려다봄)
3. `Codex` 새로 (RectTransform, 코덱스): 로컬 위치 (-1.4, 0.805, 1.6), 회전 (90, 0, 0), 스케일 (0.00075, 0.00075, 0.00075), 크기 (1200, 740) (= 0.9 × 0.555m, 책상 윗면 위에 눕힘. 위쪽이 북쪽)
   - Canvas (Render Mode World Space, Event Camera → `Player/Head`의 Camera), CanvasScaler (기본: Dynamic Pixels Per Unit 1, Reference Pixels Per Unit 100), GraphicRaycaster, CanvasGroup (Alpha 1, Interactable·Blocks Raycasts 끔. 앉으면 코드가 켬), BoxCollider (중심 (0, 0, 0), 크기 (1200, 740, 10) = 0.9 × 0.555 × 0.0075m 판), `InteractPointView` (`_id` `codex`, `_label` Hub/`hub.codex`)
   - Add Component → `CodexView`, `CodexController` (`_view` → 같은 오브젝트), `DeskSequence` (`_seconds` 0.5)
   - `Cover`: 꽉 채움, Image (0.32, 0.2, 0.12, 1)
   - `LeftPage`: 앵커 (0, 0)~(0.5, 1), 오프셋 왼 16·아래 16·오른 -6·위 -16, Image (0.93, 0.89, 0.78, 1)
     - `Name`: 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, -16), 크기 (-40, 48), 글자 32, 가운데, 줄바꿈 끔, 키 Codex/`codex.name` (자리 값은 코드가 넣음)
     - `Tabs`: 앵커·피벗 (0.5, 1), 위치 (0, -76), 크기 (540, 64), HorizontalLayoutGroup (Spacing 10, 가운데, Control Child Size 켬, Force Expand 끔)
       - `ScrollsTab`·`PracticeTab`: Button.prefab, LayoutElement Preferred (200, 60), 라벨 Codex/`codex.tab.scrolls`·`codex.tab.practice`. 각각 Add Component → `TooltipHandler` (`_title` = 같은 탭 키, `_body` = Codex/`codex.tab.scrolls.tip`·`codex.tab.practice.tip`)
     - `List`: 앵커·피벗 (0.5, 1), 위치 (0, -156), 크기 (540, 450), VerticalLayoutGroup (Spacing 6, 위 가운데, Control Child Size 켬, Child Force Expand Height 끔)
     - `ListEmpty`: 앵커·피벗 (0.5, 1), 위치 (0, -166), 크기 (540, 60), 글자 28, 가운데
     - `LeaveButton`: Button.prefab, 앵커·피벗 (0.5, 0), 위치 (0, 20), 크기 (260, 60), 라벨 Codex/`codex.leave`
   - `RightPage`: 앵커 (0.5, 0)~(1, 1), 오프셋 왼 6·아래 16·오른 -16·위 -16, Image (0.93, 0.89, 0.78, 1)
     - `ReadingPage` (꽉 채움, 꺼 둠):
       - `Title`: 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, -24), 크기 (-48, 56), 글자 36, 가운데, 줄바꿈 끔
       - `Body`: 같은 앵커, 위치 (0, -96), 크기 (-56, 160), 글자 26, 줄바꿈, 왼쪽 위 정렬
       - `Drawing`: 빈 오브젝트 + `StrokeDrawer` (`_thickness` 6, `_fitPadding` 0.1, 색 (0.15, 0.1, 0.06, 1)), 앵커 가운데, 위치 (0, -60), 크기 (360, 360), 꺼 둠
       - `Icon`: Image (Preserve Aspect), 앵커 가운데, 위치 (0, -60), 크기 (240, 240), 꺼 둠
       - `PrevButton`: Button.prefab, 앵커·피벗 (0, 0), 위치 (24, 20), 크기 (160, 56), 라벨 Codex/`codex.prev`
       - `NextButton`: Button.prefab, 앵커·피벗 (1, 0), 위치 (-24, 20), 크기 (160, 56), 라벨 Codex/`codex.next`
       - `PageNumber`: 앵커·피벗 (0.5, 0), 위치 (0, 30), 크기 (160, 40), 글자 24, 가운데
     - `PracticePage` (꽉 채움, 꺼 둠):
       - `Title`: 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, -16), 크기 (-48, 48), 글자 32, 가운데, 줄바꿈 끔
       - `DrawArea`: 앵커·피벗 가운데, 위치 (0, 10), 크기 (500, 500), Image (1, 1, 1, 0.35) Raycast Target **켬**. Add Component → `SpellDrawView`, `SpellDrawController` (`_view` → 같은 오브젝트, `_waitSeconds` 3)
         - `Guide`: 꽉 채움 (오프셋 0, 피벗 가운데), `StrokeDrawer` (`_thickness` 10, `_fitPadding` 0.1, 색 (0.3, 0.22, 0.14, 0.25))
         - `Ink`: 꽉 채움 (오프셋 0, 피벗 가운데), `StrokeDrawer` (`_thickness` 6, 색 (0.1, 0.06, 0.03, 1))
         - `Timer`: Image (색 (0.85, 0.65, 0.2, 1), Image Type Filled, Horizontal, Origin Left), 앵커 (0, 1)~(1, 1), 피벗 (0.5, 1), 위치 (0, -4), 크기 (-16, 8), 꺼 둠
         - `Hint`: 앵커 가운데, 크기 (460, 120), 글자 24, 가운데, 줄바꿈, 색 (0.35, 0.3, 0.25, 0.9), 키 Cast/`cast.hint`
       - `Result`: 앵커 (0, 0)~(1, 0), 피벗 (0.5, 0), 위치 (0, 52), 크기 (-40, 40), 글자 26, 가운데, 꺼 둠
       - `Detail`: 같은 앵커, 위치 (0, 16), 크기 (-40, 34), 글자 20, 가운데, 꺼 둠
   - CodexView 칸: `_group` → Codex의 CanvasGroup, `_nameText` → `LeftPage/Name`, 탭 2개·`_leaveButton`, `_list` → `LeftPage/List`, `_itemPrefab` → `Assets/Prefabs/UI/TutorialListItem.prefab`, `_listEmpty` → `LeftPage/ListEmpty`, 읽기 칸 8개 → `ReadingPage` 자식(`_readingPage` = ReadingPage), `_practicePage` → PracticePage, `_practiceTitle` → `PracticePage/Title`, `_draw` → `DrawArea`의 SpellDrawController
   - SpellDrawView 칸: `_guide` → `Guide`, `_ink` → `Ink`, `_timer` → `Timer`, `_result` → `PracticePage/Result`, `_detail` → `PracticePage/Detail`, `_hint` → `Hint`
4. HubView: `_codex` → Codex의 CodexController, `_deskSequence` → Codex의 DeskSequence, `_deskSeat` → `DeskSeat`
5. 저장 → 다시 열어 Codex Event Camera·CanvasGroup Alpha 1·BoxCollider·지점 id `codex`, CodexView 칸 전부, SpellDrawView 6칸, HubView 새 칸 3개, 코덱스 지점 이름 키 확인. Scene 뷰에서 DeskSeat 시점(Align View to Selected)으로 코덱스가 화면 가운데 들어오는지 확인

---

## 다국어 변경

1. 시트에 새 탭 3개 (머리글 15칸), 기존 탭 4개에 줄 추가:

**Spell** (새로)

| Key | Comment | ko | en |
|---|---|---|---|
| `spell.flame_shot.name` | 주문 이름 | 화염 투사 | Flame Shot |
| `spell.flame_shot.desc` | 주문 설명 (임시) | 불을 직선으로 날린다. 화염탄 한 발 | Launches fire in a straight line. One fire bolt. |
| `spell.fireball.name` | 주문 이름 | 화염구 | Fireball |
| `spell.fireball.desc` | 주문 설명 (임시) | 불을 만들어 응축한 뒤 직선으로 날린다. 맞으면 둘레가 터진다 | Creates fire, condenses it and launches it straight. Bursts on impact. |
| `spell.water_shot.name` | 주문 이름 (시험용) | 물 투사 | Water Shot |
| `spell.water_shot.desc` | 주문 설명 (시험용) | (시험용) 물을 직선으로 날려 맞은 적을 밀쳐낸다 | (Test) Launches water straight, pushing the target back. |
| `spell.flame_fan.name` | 주문 이름 (시험용) | 화염 확산 | Flame Fan |
| `spell.flame_fan.desc` | 주문 설명 (시험용) | (시험용) 불을 부채꼴로 퍼뜨려 세 갈래로 날린다 | (Test) Spreads fire into a fan of three bolts. |
| `spell.flame_wall.name` | 주문 이름 (시험용) | 화염 벽 | Flame Wall |
| `spell.flame_wall.desc` | 주문 설명 (시험용) | (시험용) 조준한 곳에 가로로 긴 불벽을 세운다 | (Test) Raises a long wall of fire where you aim. |
| `spell.shield.name` | 주문 이름 (시험용) | 방호막 | Shield |
| `spell.shield.desc` | 주문 설명 (시험용) | (시험용) 응축한 마나로 막을 펼쳐 받는 피해를 줄인다 | (Test) Condensed mana forms a barrier that reduces damage taken. |
| `spell.fireball_scroll.name` | 주문 이름 | 화염구 두루마리 | Fireball Scroll |
| `spell.fireball_scroll.desc` | 주문 설명 (임시) | 화염구를 두루마리에 그려 두고 마나를 막아 멈춰 둔다 | A fireball drawn on a scroll, its mana held back until use. |
| `spell.flame_shot_scroll.name` | 주문 이름 (시험용) | 화염 투사 두루마리 | Flame Shot Scroll |
| `spell.flame_shot_scroll.desc` | 주문 설명 (시험용) | (시험용) 화염 투사를 두루마리에 그려 멈춰 둔다 | (Test) A flame shot held on a scroll. |
| `spell.shield_scroll.name` | 주문 이름 (시험용) | 방호막 두루마리 | Shield Scroll |
| `spell.shield_scroll.desc` | 주문 설명 (시험용) | (시험용) 방호막을 두루마리에 그려 멈춰 둔다 | (Test) A shield held on a scroll. |

**Cast** (새로)

| Key | Comment | ko | en |
|---|---|---|---|
| `cast.result.cast` | 판정: 시전. {0} 주문 이름, {1} 일치율 % | {0} {1}% | {0} {1}% |
| `cast.result.cast_exact` | 판정: 정확 시전 (90% 이상). {0} 주문 이름, {1} 일치율 % | {0} {1}% (정확) | {0} {1}% (precise) |
| `cast.result.cancel` | 판정: 취소. {0} 이유 | 취소: {0} | Canceled: {0} |
| `cast.why.empty` | 취소 이유 | 그린 것이 없다 | Nothing was drawn |
| `cast.why.small` | 취소 이유 | 그림이 너무 작다 | The drawing is too small |
| `cast.why.none` | 취소 이유 | 비교할 주문 그림이 없다 | No spell drawings to compare |
| `cast.why.nomatch` | 취소 이유 | 맞는 주문이 없다 | No matching spell |
| `cast.why.parts` | 취소 이유. {0} 주문 이름, {1} 모자란 부분 목록, {2} 부분 기준 % | 부분 일치율이 모자라다: {0}의 {1} (부분 기준 {2}%) | Part match too low: {1} in {0} (part threshold {2}%) |
| `cast.why.unlearned` | 취소 이유. {0} 주문 이름, {1} 요소 목록 | 습득하지 않은 요소가 있다: {0}의 {1} | Unlearned element: {1} in {0} |
| `cast.why.ambiguous` | 취소 이유. {0} 주문 이름 목록 | 주문이 하나로 정해지지 않는다 ({0}) | Can't tell which spell ({0}) |
| `cast.why.canceled` | 취소 이유 (Esc·우클릭) | 직접 취소 | Canceled by you |
| `cast.part` | 부분 하나. {0} 요소 이름, {1} 일치율 % | {0} {1}% | {0} {1}% |
| `cast.extra` | 남는 선. {0} % | 남는 선 {0}% | Stray lines {0}% |
| `cast.parts` | 판정 자세히: 부분 일치율. {0} 부분 목록 | 부분 일치율: {0} | Part match: {0} |
| `cast.nearest` | 판정 자세히: 맞는 주문이 없을 때. {0} 주문 이름, {1} 일치율 % | 가장 가까운 주문: {0} {1}% | Closest spell: {0} {1}% |
| `cast.hint` | 그리기 칸 안내. {0} 대기 초 | 좌클릭으로 그리기. 버튼을 떼도 {0}초 안에 이어 그릴 수 있다. Space 바로 판정, 우클릭·Esc 취소 | Left-click to draw. You can keep drawing within {0}s after letting go. Space judges now, right-click or Esc cancels. |

**Codex** (새로)

| Key | Comment | ko | en |
|---|---|---|---|
| `codex.name` | 코덱스 이름. {0} 플레이어 이름. 번역 안 함: 모든 언어 칸에 같은 값 | {0}'s Codex | {0}'s Codex |
| `codex.tab.scrolls` | 코덱스 탭 (툴팁 제목도) | 주문서 | Scrolls |
| `codex.tab.scrolls.tip` | 코덱스 탭 툴팁 | 얻은 주문서를 읽는다. 장을 끝까지 넘기면 요소 주문서는 그 요소를 익히고, 주문 주문서는 주문 도감에 오른다. | Read the scrolls you have found. Turn to the last page to learn an element or add a spell to your codex. |
| `codex.tab.practice` | 코덱스 탭 (툴팁 제목도) | 연습 | Practice |
| `codex.tab.practice.tip` | 코덱스 탭 툴팁 | 주문을 그려 본다. 도감에 있는 그림을 생략 없이 처음 완성하면 주문 도감에 오른다. | Practice drawing spells. Completing a codex drawing in full for the first time adds it to your codex. |
| `codex.leave` | 코덱스 버튼: 책상에서 일어나기 | 일어나기 | Stand up |
| `codex.empty.scrolls` | 주문서 탭이 비었을 때 | 가진 주문서가 없어 | You have no scrolls |
| `codex.scroll.element` | 목록: 요소 주문서. {0} 요소 이름 | 요소 주문서: {0} | Element scroll: {0} |
| `codex.scroll.spell` | 목록: 주문 주문서. {0} 주문 이름 | 주문서: {0} | Spell scroll: {0} |
| `codex.page` | 주문서 장 번호. {0} 지금 장, {1} 장 수 | {0} / {1} | {0} / {1} |
| `codex.prev` | 주문서 이전 장 버튼 | 이전 장 | Previous |
| `codex.next` | 주문서 다음 장 버튼 | 다음 장 | Next |
| `codex.elements` | 필요한 요소. {0} 요소 목록 | 필요한 요소: {0} | Elements: {0} |
| `codex.element.learned` | 요소 주문서 마지막 장 | 이 요소를 익혔다. 이 요소가 든 주문을 쓸 수 있다. | You learned this element. You can now use spells that contain it. |
| `codex.practice.free` | 연습: 따라 그릴 그림 없이 | 자유 그리기 | Free drawing |
| `codex.toast.registered` | 토스트: 도감 등록. {0} 주문 이름 | 주문 도감 갱신: {0} | Spell codex updated: {0} |
| `codex.toast.learned` | 토스트: 요소 습득. {0} 요소 이름 | 요소 습득: {0} | Element learned: {0} |

`codex.name`은 CSV에서 13개 언어 칸에 다 `{0}'s Codex`를 넣음 (누락 검사에 안 걸리게, 번역 안 함)

**Hub (추가)**

| Key | Comment | ko | en |
|---|---|---|---|
| `hub.codex` | 여관 방 책상 위 코덱스 (지점 이름) | 코덱스 | Codex |

**Menu (추가)**

| Key | Comment | ko | en |
|---|---|---|---|
| `menu.spells.elements` | 주문 도감 필요한 요소. {0} 요소 목록 | 필요한 요소: {0} | Elements: {0} |
| `menu.spells.kind.combat` | 주문 도감 종류 | 전투 주문 | Combat spell |
| `menu.spells.kind.scroll` | 주문 도감 종류 | 두루마리 | Scroll |

**UI (추가)**

| Key | Comment | ko | en |
|---|---|---|---|
| `common.list_separator` | 목록 이름 사이 구분자 (끝 공백 포함, 언어마다 다를 수 있음) | `, ` | `, ` |

**Tutorial (추가)**

| Key | Comment | ko | en |
|---|---|---|---|
| `tutorial.draw.title` | 튜토리얼 탭 제목 | 주문 그리기 | Drawing Spells |
| `tutorial.draw.body` | 튜토리얼 탭 설명 (임시) | 코덱스의 연습 장에서 좌클릭으로 그린다. 버튼을 떼도 3초 안에 이어 그릴 수 있고, 3초 동안 안 그리면 판정한다. Space는 바로 판정, 우클릭·Esc는 취소. | Draw with left-click on the practice page of the book on your desk. You can keep drawing within 3 seconds after letting go; after 3 seconds without drawing it is judged. Space judges right away, right-click or Esc cancels. |

2. 탭 7개를 키트 CSV 형식으로 `Assets/Localization/CSV/`에 씀 (`Spell.csv`·`Cast.csv`·`Codex.csv` 새로, `Hub.csv`·`Menu.csv`·`UI.csv`·`Tutorial.csv`)
3. `LocalizationEditorSettings.CreateStringTableCollection`으로 `Spell`·`Cast`·`Codex` 만들기 (`Assets/Localization/Tables`), 세 컬렉션의 언어 표마다 `SetPreloadTableFlag(table, true)`
4. `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV`
5. `Check Missing Translations`·`Check Missing Glyphs` (빠진 글자 0). `common.list_separator` 값의 끝 공백이 남았는지 en·ko 표에서 확인

---

## CLAUDE.md 변경

`### UI 4축` 표 끝에 한 줄 (새 접미사):

```markdown
| **Drawer** | UGUI `Graphic` 상속, 받은 점을 이어 선 메시로 그림 (로직 없음) | StrokeDrawer |
```

---

## 주의사항

- plan 코드는 작성 때 레포 밖에서 확인함: 조각을 스크립트 사본에 적용해 Unity 6000.3.9f1 DLL로 컴파일 (경고·오류 0), 인식기 테스트 21개를 UnityEngine 대역으로 .NET에서 돌려 통과, 프로토타입과 일치율·부분 일치율이 소수 셋째 자리까지 같음
- 그림 데이터·그린 획 모두 y 위가 + (UI 로컬 좌표). 프로토타입 값은 y를 뒤집어 넣음. 뒤집기는 거리를 안 바꾸고 회전 각도 목록이 ±대칭이라 판정이 프로토타입과 같음 (테스트 기대값을 Node로 프로토타입에 돌려 확인함. 회전 테스트는 각도 부호를 뒤집어 씀)
- 인식기 정렬은 JS 안정 정렬과 같게 같은 값이면 원래 순서 (`Order`·`Position`). `List.Sort`만 쓰면 같은 값 순서가 바뀜
- 생략 가능 수 0 (프로토타입 기본) → 생략본 없음. 생략은 테스트에서만 1로 확인. 레벨이 생기면 `RunEntry.OmitLimit`
- 최소 크기 60은 그리기 칸 단위 (책 그리기 칸 500 중 60)
- 연습 대기 3초는 게임 시간: 메뉴 창(M·J)이 떠 있으면 멈추고, Space(`Player/Cast`)도 `Player` 묶음이 꺼져서 안 들어옴. 메뉴 창 Blocker가 책 클릭도 막음
- 책상에선 Esc가 일시정지 메뉴 대신 그리기 취소 → 일어나기. 일시정지 메뉴는 일어난 뒤 Esc. 일어나기·앉기 연출(0.5초) 중 Esc는 무시
- 책상에선 `PlayerController`가 꺼져 있어서 `Player/Head`를 옮겨도 됨. 일어날 때 Head 로컬 위치·회전을 정확히 되돌려서 위아래 각도(`_pitch`)와 맞음
- 코덱스는 방에선 책상 위에 늘 펼쳐 둠 (들고 다니던 코덱스를 방에 들어오며 책상에 놓은 것). 입력은 앉아 있을 때만 (CanvasGroup Interactable·Blocks Raycasts). 물품에서 빼고 넣는 건 물품 가방이 생길 때
- 상호작용 지점은 책상이 아니라 코덱스 (`Codex`의 BoxCollider + `InteractPointView` `codex`). World Space 캔버스는 콜라이더가 없어서 판 콜라이더를 붙임
- 코덱스 이름 Codex/`codex.name`은 `{0}'s Codex`를 모든 언어 칸에 같게 넣음 (번역 안 함, {0} 플레이어 이름). 메뉴 창 주문 도감 탭 머리글도 같은 키
- 그리기 칸 `Guide`·`Ink`는 칸과 같은 RectTransform (꽉 채움, 오프셋 0, 피벗 가운데)이어야 그린 좌표와 선이 맞음. `StrokeDrawer`은 Raycast Target 끔 (칸 Image가 포인터를 받음)
- 주문서는 다 읽으면 목록에서 빠짐 (두 번 안 읽힘). 이미 익힌 요소 주문서·이미 등록한 주문 주문서는 토스트 없이 빠짐
- 연구(퍼즐 + 시간)는 퍼즐이 정해질 때까지 안 만듦. 새 게임은 주문서가 없어서 그림을 직접 그려 찾는 길만 있음 (디버그 판은 주문서로 확인)
- 연습에서 생략 없이 처음 완성하면 도감 등록 (두루마리 그림도 등록만. 두루마리 제작은 안 함)
- 결과 글의 주문·요소 이름은 판정 때 언어로 들어감 (언어를 바꾸면 다음 판정부터)
- 디버그 판(Outpost 씬에서 바로 Play): 요소 4개(불·응축·직선·차단) + 요소 주문서 3장(물·확산·벽) + 주문 주문서 5장(물 투사·화염 확산·화염 벽·방호막·방호막 두루마리). 새 게임은 주문서 없음 (드랍·임무 보상·상점은 6단계 이후)
- 목록 항목은 `TutorialListItem` 프리팹을 책·도감에 같이 씀
- 새 테이블 3개(Spell·Cast·Codex)는 Preload. 결과 글·토스트가 `GetLocalizedString`으로 바로 읽음
- `Spell` 테이블을 만든 뒤 SpellSO 빌더를 돌림 (키 id가 있어야 LocalizedString 칸을 채움). 빌더에서 에셋은 쓰기 직전에 읽음
- 입력 파일은 JSON에 액션·바인딩만 직접 넣음 (`ToJson`으로 다시 쓰면 파일 형식이 바뀔 수 있어서)

---

## 진행 추적

- [x] 묶음 Logic — StrokeEntry·SpellPartEntry·SpellSO·SpellShapeEntry·SpellCandidateEntry·SpellJudgeSpec·SpellRecognitionCalculator·StrokeDrawer·SpellDrawView·SpellDrawController·CodexView·CodexController·DeskSequence·SpellCodexUIBinder 새로, RunEntry·DataManager·GameManager·FlowStateBase·UIManager·TutorialSO·HubView·HubState·GameMenuView 수정. 컴파일 에러·경고 0
- [x] 묶음 Tests — `Mage.Tests.EditMode` asmdef, SpellRecognitionCalculatorTests 21개
- [x] 묶음 Input — `Player/Cast` (Space)
- [x] 묶음 Editor-Localization — 시트 Spell·Cast·Codex 탭, Hub·Menu·UI·Tutorial 줄, CSV 7개, 테이블 3개 만들기·Preload·Import, 누락 검사 (Preload는 이 Localization 버전에서 컬렉션 단위 `SetPreloadTableFlag(true, false)`)
- [x] 묶음 Data — SpellSO 9개 (빌더), TutorialSO Draw
- [x] 묶음 Editor-Scene — Boot(도감 탭·DataManager 목록), Outpost(코덱스 지점·DeskSeat·Codex·HubView), 저장 뒤 다시 열어 확인 (시간 막대 Timer는 Filled가 스프라이트 없이는 안 줄어서 로딩 바와 같은 기본 UISprite)
- [x] 묶음 Docs — CLAUDE.md 이름 표 Drawer
- [x] 묶음 Verify — 사용자 확인 목록 전달
- [x] 묶음 2차 수정 (2026-11-22·23, 1차 확인 중 요청) — 아래 '2차 수정'. F 상호작용, 책상 시점, 요소 → 룬, 코덱스 다시 짬, 주문서 기능 지움. 컴파일 에러·경고 0 (마지막 수정 셋은 에디터가 꺼져 있어 컴파일·씬 확인 못 함)
- [ ] 묶음 Verify 2 — 바뀐 확인 목록 사용자 확인

---

## 2차 수정 (2026-11-22·23)

1차 확인에서 F가 책 먼 쪽만 잡혀 확인이 7번에서 막힘 → 사용자 요청으로 고치고 코덱스를 다시 짬 (동작은 그림 모형으로 맞춤). 위 C#·에셋·씬·다국어 절 중 요소·주문서·코덱스·책상 시점 부분은 이 절이 대신함. 인식기·그리기 칸·메뉴 창 도감 탭은 이름만 바뀜

- **F 상호작용**: 아래를 보면 머리 레이가 3~5cm 앞 자기 CharacterController(머리가 캡슐 안)에 먼저 맞았음. `InteractDetector`가 `RaycastNonAlloc`으로 맞은 것 중 자기 몸(플레이어 아래 콜라이더)을 빼고 가장 가까운 것을 잡음
- **책상 시점**: `DeskSeat` 로컬 (-1.4, 1.22, 1.6), 회전 (90, 0, 0) = 책 가운데 0.415m 위에서 정면으로 내려다봄 (화면 위 = 책 위). 16:9·16:10에서 책 전체가 화면에 들어오고 책상이 화면을 채움. 머리 이동 0.7초
- **요소 → 룬**: `ElementSO`→`RuneSO`, `ElementCardView`→`RuneCardView`, `Data/Elements`→`Runes`, `ElementCard`→`RuneCard`, 멤버(`Runes`·`FindRune`·`LearnedRunes`·`Rune`·`PartRuneIds`·`MissingRuneIds`·생성 화면 칸), 테이블·시트 탭·CSV `Element`→`Rune`, 키 `element.*`→`rune.*`·`creation.element_*`→`creation.rune_*`·`tip.elements.*`→`tip.runes.*`·`menu.spells.elements`→`menu.spells.runes`·`codex.elements`→`codex.runes`, 화면 글 '요소'→'룬'. 스크립트·에셋은 meta째 옮겨 GUID 그대로, 키는 `RenameKey`라 id 그대로, 저장 칸은 `FormerlySerializedAs` 뒤 다시 저장
- **룬 그림**: `RuneSO._strokes` (프로토타입 spell-core.js 요소 그림, y 뒤집음: △ ○ ｜ Z 물결 V ─). 판정 비교 그림 = 다중 룬 마법 9개 뒤에 룬 7개 (세모만 그리면 '불 룬 주문'). 룬 주문은 등록 없음
- **주문서 기능 지움**: 탭·읽기 장·`RunEntry.ElementScrolls`·`SpellScrolls`·키 7개. 다중 룬 마법은 주문 연습에서 생략 없이 처음 완성하면 등록. 디버그 판은 룬 7개·마법 9개를 다 가짐
- **코덱스** (`CodexView`·`CodexController` 다시 씀, `CodexPageView`·`PageFlipSequence`·`CodexListHeader.prefab` 새로):
  - 덮인 책: 오른쪽 절반에 표지 (마법진 = 원 셋 + 육망성 + 둘레 룬 그림, `StrokeDrawer`. 가운데 코덱스 이름). F 대상은 표지 판 콜라이더. 앉으면 위 줄 오른쪽에 일어나기
  - 표지를 누르면 표지가 책등으로 접히고 왼쪽 절반이 책등에서 펴짐 (0.44초) → 룬 주문 첫 펼침, 위 줄 왼쪽에 탭 3개 (툴팁)
  - 룬 주문·다중 룬 마법: 장마다 하나 (묶음·이름·그림·필요한 룬·설명·쪽 번호). 다중 룬 마법은 룬 수 순서, 묶음마다 왼쪽 장에서 시작 (홀수면 빈 장). 왼쪽 장 왼쪽 끝 띠 = 이전, 오른쪽 장 오른쪽 끝 띠 = 다음 (넘길 장이 있을 때만, 없으면 흐림). 장 넘김 0.22초
  - 다중 룬 마법 왼쪽 장 맨 위: 지금 묶음 드롭다운 (마법이 있는 묶음만) → 고르면 그 묶음 첫 펼침까지 한 장씩 넘김
  - 주문 연습: 왼쪽 목록 (묶음 제목 + 항목, 빈 묶음 없음, 스크롤). 항목 = 흐린 그림, 묶음 제목 = 처음 화면 (자유 그리기 + 안내 글)
  - 일어나기·Esc: 그리는 중엔 그리기 취소, 아니면 (펼쳤으면 덮는 연출 뒤) 머리가 돌아감. 연출 중 입력 무시
- `SpellDrawController.Begin`은 주문 목록 대신 이름 함수 (판정 번호 → 이름), `ShowGuide`는 그림 데이터를 받고 그림이 있으면 안내 글을 숨김
- **다국어**: 시트 Element 탭 → Rune, Codex 탭 다시 씀 (20줄), Creation·Tip·Cast·Menu·Tutorial 줄 고침. `codex.dropdown`은 ▾가 Noto에 없어서 ▼. 테이블 이름을 바꾸면 Shared Data 주소만 옛 이름으로 남아 `SetAddress`로 맞춤
- **테스트** 38개: 룬 그림 7개·룬과 섞여도 마법 9개·세모만이면 불 룬 추가 (기대값은 에디터에서 계산해 확인)
- **마지막 수정** (2026-11-23, 에디터가 꺼져 있어 씬 파일·코드만 고침): 표지가 펼친 책보다 먼저 그려져 여는 모습이 가려짐 → Codex 자식 순서 OpenBook → Cover. 표지 Button 비활성 색(기본 알파 0.5) 때문에 걸어 다닐 때 표지가 반투명 → 기본·선택·비활성 0.88, 강조 1.0, 표지 색 (0.36, 0.227, 0.136). `ForceClose`는 `StopAllCoroutines`로 안쪽 접기·펴기까지 멈춤

---

## 확인 목록 (사용자, 에디터)

- 에디터를 켜면 컴파일 → 콘솔 에러·경고 0 (마지막 수정은 에디터가 꺼져 있어 컴파일 안 봄)
- Test Runner (Window > General > Test Runner) EditMode → `Mage.Tests.EditMode` 38개 통과
- Outpost 씬에서 바로 Play (디버그 판) → 여관 방 책상 위 오른쪽 절반에 덮인 책: 갈색 표지, 금색 마법진, 가운데 "디버그 마법사's Codex". 반투명 아님
- 책상 앞에서 표지 어디를 봐도 "[F] 코덱스" (책상 왼쪽 빈 자리는 안 잡힘). 문·NPC의 F는 그대로
- F → 머리가 0.7초 동안 책 바로 위로 가며 고개가 정면으로 내려다봄. 책상이 화면을 채우고 책 전체가 보임. 커서 보임, 파티·금화·조준점 사라짐, 위 줄 오른쪽에 일어나기만
- 표지에 마우스를 올리면 살짝 밝아짐. 표지를 누르면 표지가 책등 쪽으로 접혀 오른쪽 장이 드러나고 왼쪽 절반이 책등에서 펴짐. 위 줄 왼쪽에 탭 3개, 마우스를 올리면 툴팁
- 룬 주문 탭: 1쪽 불 룬 주문 … 7쪽 벽 룬 주문, 8쪽 빈 장. 첫 펼침에선 왼쪽 끝 띠(‹)가 흐리고 안 눌림. 오른쪽 끝 띠(›) → 장 넘김 연출로 다음 펼침. 마지막 펼침에선 › 흐림
- 다중 룬 마법 탭: 왼쪽 장 맨 위 "2룬 마법 ▼". 1~5쪽 2룬 마법 (화염 투사·물 투사·화염 확산·화염 벽·방호막), 6쪽 빈 장, 7~9쪽 3룬 마법 (화염구·화염 투사 두루마리·방호막 두루마리), 10쪽 빈 장, 11쪽 4룬 마법 (화염구 두루마리), 12쪽 빈 장. 장마다 이름·그림·"필요한 룬: …"·설명
- 띠로 넘기면 맨 위 묶음 제목이 그 펼침 묶음으로 바뀜. 묶음 제목 → 목록 (2룬·3룬·4룬 마법), 4룬 마법을 고르면 장 넘김이 여러 번 이어져 11쪽. 목록 밖을 누르면 닫힘
- 주문 연습 탭: 넘김 띠 없음. 왼쪽 목록 룬 주문 7 / 2룬 마법 5 / 3룬 마법 3 / 4룬 마법 1 (휠·끌기로 스크롤). 오른쪽 "자유 그리기" + 안내 글
- 항목 → 오른쪽 제목이 그 이름, 안내 글 대신 흐린 그림. 묶음 제목 → 왼쪽 그대로, 오른쪽 "자유 그리기" + 안내 글
- 좌클릭으로 그리면 선이 바로 따라옴. 떼고 다시 눌러 이어 그림. 위 막대가 3초 동안 줄고 다 줄면 판정. Space → 바로 판정. 그리는 중 우클릭·Esc → "취소: 직접 취소"
- 세모만 → "불 룬 주문 …%". 화염 투사(세모 + 위 꼭짓점에서 아래로 긴 선) → "화염 투사 …%"·"부분 일치율: 불 …, 직선 …, 남는 선 …". 작은 그림 → "취소: 그림이 너무 작다". 아무 낙서 → "취소: 맞는 주문이 없다"·"가장 가까운 주문: … xx%"
- 그리는 중이 아닐 때 Esc 또는 일어나기 → 왼쪽 절반이 접히고 표지가 덮인 뒤 머리가 원래 자리로, 조작·화면 글자 돌아옴, 커서 잠김. 다시 F → 덮인 책부터. 일어난 뒤 Esc → 일시정지 메뉴. 장 넘기는 중 Esc·일어나기는 무시
- 책상에서 J → 메뉴 창 주문 도감 탭: "필요한 룬: …". 닫으면 책상 그대로·커서 보임. 그리는 중 M → 막대가 멈추고, 닫으면 이어서
- 새 게임(생성부터): 생성 화면 "습득할 룬"·룬 카드, 모자라면 "룬을 4개 골라 줘". 방 책상: 룬 주문 탭은 고른 4개만, 다중 룬 마법 탭은 "아직 등록한 다중 룬 마법이 없어"·묶음 제목 없음, 연습 목록은 룬 주문만
- 새 게임에서 고른 룬으로 된 마법(예: 불·직선 → 화염 투사)을 생략 없이 그리면 "주문 도감 갱신: 화염 투사" → 다중 룬 마법 탭·연습 목록에 2룬 마법이 생김. 안 고른 룬이 든 마법 → "취소: 습득하지 않은 룬이 있다: …"
- 튜토리얼 탭: 처음 판정한 뒤 '주문 그리기' (설명에 '주문 연습 탭')
- 로딩창 팁 "룬 (임시)"
- 영어로 바꾸면 Rune Spells / Multi-Rune Magic / Spell Practice, "2-Rune Magic", "Fire Rune Spell", "Runes: …" (코덱스 이름·쪽 번호·‹ › ▼는 그대로)
- 콘솔 에러·경고 없음

---

## 커밋 메시지

```
Add — 5단계 코덱스: 룬 주문·다중 룬 마법·주문 연습, 주문 인식기, 요소 → 룬

- SpellRecognitionCalculator: 프로토타입 spell-core.js 인식기 이식 ($P 48점·회전 5번, 부분 검사(ICP)·남는 선, 생략본, 덜 그림·미습득·애매함 판정). EditMode 테스트 38개 (기대값은 프로토타입·에디터 계산으로 확인)
- 요소 → 룬: RuneSO(+룬 그림 획)·RuneCardView·Data/Runes·RuneCard, Rune 테이블·시트 탭, 키 rune.* 등 (GUID·키 id 그대로라 연결 유지), 화면 글 '요소' → '룬'
- SpellSO 9개 = 다중 룬 마법 (프로토타입 임시·시험용 그림, y 위가 +), 판 데이터에 등록한 마법·생략 가능 수(0). 디버그 판은 룬 7개·마법 9개를 다 가짐
- 코덱스: 책상 위 덮인 표지(마법진·'{플레이어}'s Codex', F 대상), 앉으면 머리가 책 바로 위에서 내려다봄. 표지를 누르면 왼쪽으로 펼침. 탭 룬 주문·다중 룬 마법(장마다 하나, 장 바깥 끝 띠로 넘김, 묶음 드롭다운 → 장 넘김 연출)·주문 연습(왼쪽 목록·흐린 그림, 좌클릭 그리기·3초 대기·Space 바로·우클릭·Esc 취소, 다중 룬 마법은 생략 없이 처음 완성하면 등록). 일어나기·Esc로 덮고 돌아옴
- StrokeDrawer(UGUI 선 메시, 새 접미사 Drawer), PageFlipSequence(장 넘김·표지 여닫기), SpellDrawController(그리기 칸·결과 글), 메뉴 창 주문 도감 탭(목록·종류·필요한 룬·그림·설명)
- InteractDetector: 아래를 볼 때 자기 CharacterController에 먼저 맞던 것 → 자기 몸 콜라이더를 빼고 가장 가까운 지점
- Esc를 상태가 먼저 받는 FlowStateBase.HandleCancel, 입력 Player/Cast(Space), 튜토리얼 '주문 그리기'
- 다국어: Spell·Cast·Codex 테이블 새로 (Preload), Element → Rune, Hub·Menu·UI·Tutorial·Creation·Tip·Cast 줄 추가·수정
- CLAUDE.md: 이름 표에 Drawer, '룬 조합'·Rune 테이블 예

Why: 마법 습득·연습을 늘 들고 다니는 코덱스로 여관 방 책상에서 하고, 숲 시전 전에 인식기를 Unity로 옮겨 그리기를 연습할 곳이 필요함. 요소는 룬으로 이름을 고정함
Alternatives: 화면을 덮는 책 UI(책상 위 코덱스로 정함), LineRenderer로 획 그리기(Overlay 캔버스인 도감 탭에 못 그림), 인식기를 Unity식으로 새로 짜기(프로토타입과 판정이 달라질 수 있어 그대로 옮김), 주문서 읽기 탭(뺌), 오른쪽 1~8룬 책갈피(왼쪽 장 맨 위 드롭다운으로), 장 전체를 눌러 넘기기(장 바깥 끝 띠로), 연구(퍼즐 + 시간)는 퍼즐이 정해질 때까지 뺌
```
