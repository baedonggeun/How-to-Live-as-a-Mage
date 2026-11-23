# Plan: 4단계 여관·길드·정문 — NPC 대화(자막+선택지), 영입 창, 임무 게시판, 금화·식량, 지하 실내, 정문 경비병

> 작업 기록. 2026-11-20 plan 문서 없이 바로 구현, 2026-11-22 사용자 검증·커밋(`c007df3`) 뒤 정리. 코드는 커밋된 그대로 (새 파일은 전문, 고친 파일은 `2232e3a` → `c007df3` diff)

## 결정 (사용자, 2026-11-20)

1. NPC 대화: 화면 아래 자막 + 선택지 버튼
2. 상호작용 키를 기본값 F로 통일 (E였음). 건물 출입도 문에서 상호작용
3. 금화 시작 100, 방값 70 (처음 방 잡을 때 한 번), 원정 식량 1인 1일치 금화 3, 식량은 여관 주인에게서 삼 (전부 임시)
4. 여관 방에 책상·침대. 책상 기능(주문서)은 5단계. 침대 하루 휴식은 뺌 ("의미 없는 기능") → 침대 상호작용 없음, 날짜 개념 없음
5. 다음 목표: 여관 주인 대사가 길드를 알려 주고 임무 일지 갱신
6. 영입: 후보 4명, 레벨 1 고정, 직업 4종 중 무작위(중복 가능), 무료. 직업 전투 수치 임시 (짐꾼 1, 나머지 3)
   - 레인저: 단궁, 원거리 딜러·정찰 / 짐꾼: 곤봉, 짐 나르기, 전투력 낮음 / 스피어 가드: 창·방패, 딜탱 / 듀얼리스트: 망토·레이피어, 회피 딜탱
7. 게시판: 첫 의뢰 하나만
8. 1주 지나기·주간 영입 갱신·정산·저장은 나중
9. 대사·이름은 전부 임시 문구

구현 기본값 (사용자 확인 전 정함): 첫 의뢰 기간 3일 (방값 내고 남은 30 = 3일 × 하루 9), 첫 출발 때 경비병이 파티·의뢰에 더해 식량도 봄

구현 때 정함: 실내는 Outpost 씬 지하 y -40 (여관 홀·방·길드), 문은 F → 화면 페이드 순간이동(`DoorView`), 대화는 `UIManager.ShowDialogue`(열리면 일시정지), 영입·게시판 창은 Boot `UICanvas`, 식량 단위는 '인분'(하루치 = 인원만큼), 경비병은 첫 출발(`HasDeparted`) 전만 검사, 숲 귀환은 정문 밖 `ReturnPoint`·의뢰 비움, 이름은 `Name` 테이블 8개

---

## 현행 분석 (구현 전, `2232e3a`)

- `HubState`(3-3): E 지점 3개로 임시 흐름. `Huts/RecruitHall`(id `recruit`, (-10, 0, 8)) = 파티가 비었으면 고정 용병 2명(`RunEntry.StubMercenaries`), 광장 `Board`(id `board`, (6, 0, -4)) = 임시 의뢰 이름(`RunEntry.StubQuest`, UI/`quest.forest_01.title`) + 토스트, `Signpost`(id `depart`, (3, 0, -16)) = 용병 2명·의뢰가 있으면 숲, 모자라면 토스트(Hub/`hub.party_required`·`hub.quest_required`)
- 첫 목표: `OnOpeningFinished`에서 `HubView._firstObjective`(InnRoom)를 넣고 동승 용병 자막 뒤 "임무 일지 갱신" 토스트. 목표가 바뀌는 곳 없음
- `RunEntry`: `PlayerName`, `Party`(`MercenaryEntry`: 전투 수치만), `QuestTitle`(LocalizedString), `LearnedElements`, `HasArrived`, `CurrentObjective`. `CreateDebug`가 임시 용병 2명·의뢰를 넣음
- 상호작용: `Player/Interact` = `<Keyboard>/e`. `InteractDetector`가 바라보는 `InteractPointView`를 알리고 안내 "[키] 이름"(키 이름은 바인딩에서 읽음)
- `UIManager`: 토스트·자막(`ShowSubtitle`, 게임 시간 4초)·설정 창·확인창·일시정지 메뉴·메뉴 창(M·J)·툴팁. 일시정지는 `GameManager.SetMenuPaused`(`Time.timeScale = 0`, `Player` 묶음 끔, 커서 풂). Esc 순서 확인창 → 설정 창 → 메뉴 창 → 일시정지 메뉴. `IsWindowOpen`으로 메뉴 창과 다른 창이 같이 안 뜸
- 공용 UI 프리팹: `WindowFrame`(Blocker + Panel: VerticalLayoutGroup + ContentSizeFitter), `Button`(Label = LocalizedText), `LocalizedText`
- Outpost 씬: 광장 오두막 `Huts/Inn`(10, 0, 10, yaw 45. 3-3에서 Storage → Inn), `Huts/RecruitHall`(-10, 0, 8, yaw -45), `Gate`, `PlayerSpawn`. 실내 없음. 숲에서 E → `HubState` → `PlayerSpawn`(정문 안쪽)
- `DataManager`: 요소·팁·튜토리얼 SO 목록을 Boot에서 받고 시작 때 빈 칸·id 겹침 검사
- 다국어 테이블 9개 (UI·Creation·Element·Tip·Hub·Opening·Tutorial·Menu·Objective). E가 박힌 문구 3개: Tip/`tip.return.body`, Tutorial/`tutorial.interact.body`, UI/`field.debug_info`

---

## C# 변경

### `Assets/Scripts/Party/JobSO.cs` (새 파일)

**추가 내용:** 용병 직업 SO. id·이름·설명(영입 창 툴팁)·전투 수치(1~5, 임시). 전투 수치 범위 상수는 `MercenaryEntry`에서 옮김

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>용병 직업 하나 (레인저·짐꾼·스피어 가드·듀얼리스트). 영입 후보가 이 중에서 무작위로 뽑힘</summary>
    [CreateAssetMenu(fileName = "Job", menuName = "Mage/Job")]
    public sealed class JobSO : ScriptableObject
    {
        /// <summary>전투 수치 최솟값</summary>
        public const int MinCombat = 1;

        /// <summary>전투 수치 최댓값</summary>
        public const int MaxCombat = 5;

        /// <summary>바뀌지 않는 id (ranger, porter, spear_guard, duelist)</summary>
        [SerializeField] string _id;

        /// <summary>직업 이름 (Job 테이블 job.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>무기와 역할 설명 (Job 테이블 job.{id}.desc). 영입 창 툴팁에 보임</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>전투 수치 (1~5, 임시). 숲 단계에서 동료 체력·공격에 씀</summary>
        [SerializeField, Range(MinCombat, MaxCombat)] int _combat = 3;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>직업 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>무기와 역할 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>전투 수치 (1~5)</summary>
        public int Combat => _combat;
    }
}
```

---

### `Assets/Scripts/Party/MercenaryEntry.cs`

**변경 내용:** 전투 수치만 있던 용병 → 이름(Name 테이블 LocalizedString)·직업·레벨. 전투 수치는 직업 값

```diff
@@ -1,26 +1,34 @@
 using System;
+using UnityEngine.Localization;
 
 namespace Mage
 {
-    /// <summary>파티 용병 한 명. 지금은 전투 수치만 (이름 방식은 4단계 영입 설계 때 정함)</summary>
+    /// <summary>용병 한 명 (영입 후보·파티원): 이름(임시 이름표), 직업, 레벨. 전투 수치는 직업이 정함 (임시)</summary>
     public sealed class MercenaryEntry
     {
-        /// <summary>전투 수치 최솟값</summary>
-        public const int MinCombat = 1;
+        /// <summary>이름 (Name 테이블)</summary>
+        public LocalizedString Name { get; }
 
-        /// <summary>전투 수치 최댓값</summary>
-        public const int MaxCombat = 5;
+        /// <summary>직업</summary>
+        public JobSO Job { get; }
 
-        /// <summary>전투 수치 (1~5). 3단계에서 동료 체력·공격력 계산에 씀</summary>
-        public int Combat { get; }
+        /// <summary>레벨 (1 이상)</summary>
+        public int Level { get; }
 
-        #region MercenaryEntry - 전투 수치 범위를 검사하고 만듦
-        /// <param name="combat">전투 수치 (1~5)</param>
-        public MercenaryEntry(int combat)
+        /// <summary>전투 수치 (1~5, 직업 값). 숲 단계에서 동료 체력·공격력 계산에 씀</summary>
+        public int Combat => Job.Combat;
+
+        #region MercenaryEntry - 이름·직업·레벨을 검사하고 만듦
+        /// <param name="name">이름</param>
+        /// <param name="job">직업</param>
+        /// <param name="level">레벨 (1 이상)</param>
+        public MercenaryEntry(LocalizedString name, JobSO job, int level)
         {
-            if (combat < MinCombat || combat > MaxCombat)
-                throw new ArgumentOutOfRangeException(nameof(combat), combat, $"전투는 {MinCombat}~{MaxCombat}");
-            Combat = combat;
+            if (job == null) throw new ArgumentNullException(nameof(job));
+            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "레벨은 1 이상");
+            Name = name ?? throw new ArgumentNullException(nameof(name));
+            Job = job;
+            Level = level;
         }
         #endregion
     }
```

---

### `Assets/Scripts/Party/RecruitPool.cs` (새 파일)

**추가 내용:** 영입 후보 추첨 (static). 직업은 목록에서 무작위(중복 가능), 이름은 Name 테이블 8개에서 겹치지 않게 (앞에서부터 섞어 뽑기). 후보 4명·레벨 1 (임시)

```csharp
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>영입 후보 추첨: 직업은 목록에서 무작위(중복 가능), 이름은 이름표에서 겹치지 않게. 지금은 후보 4명·레벨 1 고정 (임시)</summary>
    public static class RecruitPool
    {
        /// <summary>한 번에 뽑는 후보 수 (임시)</summary>
        public const int CandidateCount = 4;

        /// <summary>후보 레벨 (임시, 고정)</summary>
        public const int StartLevel = 1;

        /// <summary>이름표에 든 이름 수 (Name 테이블 name.mercenary.01 ~ 08)</summary>
        const int NameCount = 8;

        /// <summary>이름 키 형식 ({0} 두 자리 번호)</summary>
        const string NameKeyFormat = "name.mercenary.{0:00}";

        /// <summary>임시 이름표 테이블</summary>
        static readonly TableReference NameTable = "Name";

        #region Draw - 후보를 뽑음. 직업이 하나도 없으면 에러 로그 후 빈 목록
        /// <param name="jobs">뽑을 직업 (DataManager 목록, 빈 칸은 건너뜀)</param>
        public static List<MercenaryEntry> Draw(IReadOnlyList<JobSO> jobs)
        {
            // 뽑은 후보
            var result = new List<MercenaryEntry>(CandidateCount);
            // 빈 칸을 뺀 직업
            var validJobs = new List<JobSO>();
            if (jobs != null)
                foreach (JobSO job in jobs)
                    if (job != null) validJobs.Add(job);
            if (validJobs.Count == 0)
            {
                Debug.LogError("[Recruit] 직업 목록 비어 있음. 영입 후보 없음");
                return result;
            }

            // 이름 번호 (1 ~ NameCount). 앞에서부터 하나씩 섞어 뽑아 겹치지 않게
            var names = new List<int>(NameCount);
            for (int i = 1; i <= NameCount; i++) names.Add(i);
            // 뽑을 후보 수 (이름보다 많이 못 뽑음)
            int count = Mathf.Min(CandidateCount, NameCount);
            for (int i = 0; i < count; i++)
            {
                // 이번 이름 자리 (i 뒤에서 무작위로 골라 i와 바꿈)
                int pick = Random.Range(i, names.Count);
                (names[i], names[pick]) = (names[pick], names[i]);
                // 이번 후보 이름
                var name = new LocalizedString(NameTable, string.Format(CultureInfo.InvariantCulture, NameKeyFormat, names[i]));
                result.Add(new MercenaryEntry(name, validJobs[Random.Range(0, validJobs.Count)], StartLevel));
            }
            return result;
        }
        #endregion
    }
}
```

**호출 관계:** `HubState.OpenRecruit` → 처음 한 번 `RecruitPool.Draw(DataManager.Instance.Jobs)`

---

### `Assets/Scripts/Quest/QuestSO.cs` (새 파일)

**추가 내용:** 의뢰 SO. id·이름·설명·기간(일). 식량이 (플레이어 + 용병) × 기간 인분 듦

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>의뢰 하나 (길드 임무 게시판에 붙음). 지금은 첫 의뢰 하나</summary>
    [CreateAssetMenu(fileName = "Quest", menuName = "Mage/Quest")]
    public sealed class QuestSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (forest_01 …)</summary>
        [SerializeField] string _id;

        /// <summary>의뢰 이름 (Quest 테이블 quest.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>의뢰 설명 (Quest 테이블 quest.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>원정 기간 (일). 식량이 (플레이어 + 용병) × 기간 인분만큼 듦</summary>
        [SerializeField, Min(1)] int _days = 3;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>의뢰 이름</summary>
        public LocalizedString Title => _title;

        /// <summary>의뢰 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>원정 기간 (일)</summary>
        public int Days => _days;
    }
}
```

---

### `Assets/Scripts/Core/RunEntry.cs`

**변경 내용:** 임시 영입·의뢰 스텁(`StubMercenaries`·`StubQuest`·`QuestTitle`) 지움. 영입 후보·뽑았는지, 받은 의뢰(QuestSO), 금화(시작 100)·식량(인분)·방·첫 출발·숲 귀환 표시 추가. 디버그 판은 용병·의뢰 없이 시작

```diff
@@ -1,17 +1,13 @@
 using System;
 using System.Collections.Generic;
-using UnityEngine.Localization;
 
 namespace Mage
 {
     /// <summary>이번 판 데이터. 씬을 넘어 GameManager가 들고 다님</summary>
     public sealed class RunEntry
     {
-        /// <summary>임시 의뢰 이름이 든 String Table</summary>
-        const string StubQuestTable = "UI";
-
-        /// <summary>임시 의뢰 이름 키 (4단계에 QuestSO로 바뀜)</summary>
-        const string StubQuestKey = "quest.forest_01.title";
+        /// <summary>새 판 시작 금화 (임시)</summary>
+        public const int StartGold = 100;
 
         /// <summary>플레이어가 정한 마법사 이름. 플레이어 입력이라 번역 대상 아님</summary>
         public string PlayerName { get; set; } = "";
@@ -19,8 +15,29 @@ public sealed class RunEntry
         /// <summary>파티에 든 용병들</summary>
         public List<MercenaryEntry> Party { get; } = new();
 
-        /// <summary>지금 받은 의뢰 이름. 받기 전엔 null</summary>
-        public LocalizedString QuestTitle { get; set; }
+        /// <summary>길드 영입 후보 (처음 영입 창을 열 때 뽑음. 주마다 새로 뽑는 건 나중)</summary>
+        public List<MercenaryEntry> RecruitCandidates { get; } = new();
+
+        /// <summary>영입 후보를 뽑았는지</summary>
+        public bool CandidatesDrawn { get; set; }
+
+        /// <summary>지금 받은 의뢰. 받기 전·다녀온 뒤엔 null</summary>
+        public QuestSO Quest { get; set; }
+
+        /// <summary>금화</summary>
+        public int Gold { get; set; } = StartGold;
+
+        /// <summary>원정 식량 (인분. 한 사람 하루치가 1인분)</summary>
+        public int Rations { get; set; }
+
+        /// <summary>여관 방을 잡았는지 (방값은 처음 한 번)</summary>
+        public bool HasRoom { get; set; }
+
+        /// <summary>정문으로 한 번이라도 나갔는지. 그 전(첫 의뢰)엔 경비병이 파티·의뢰·식량을 봄</summary>
+        public bool HasDeparted { get; set; }
+
+        /// <summary>숲에서 막 돌아오는 중인지 (전초기지가 정문 밖에서 시작하고 한 번 쓰면 끔)</summary>
+        public bool ReturningFromField { get; set; }
 
         /// <summary>캐릭터 생성에서 고른 요소 (습득 요소). 숲의 주문 판정이 씀</summary>
         public List<ElementSO> LearnedElements { get; } = new();
@@ -31,26 +48,17 @@ public sealed class RunEntry
         /// <summary>지금 목표 (임무 일지). 없으면 null. 판마다 새로</summary>
         public ObjectiveSO CurrentObjective { get; set; }
 
-        #region CreateDebug - 에디터에서 중간 씬부터 Play할 때 쓰는 판 데이터 (받은 요소는 전부 습득)
+        #region CreateDebug - 에디터에서 중간 씬부터 Play할 때 쓰는 판 데이터 (도착한 판, 받은 요소는 전부 습득)
         /// <param name="learned">습득할 요소 (빈 칸은 건너뜀)</param>
         public static RunEntry CreateDebug(IEnumerable<ElementSO> learned)
         {
             if (learned == null) throw new ArgumentNullException(nameof(learned));
             // 디버그 판 데이터. 빌드에선 늘 Title부터 시작해서 이 이름이 화면에 안 나옴
-            var run = new RunEntry { PlayerName = "디버그 마법사", QuestTitle = StubQuest(), HasArrived = true };
-            run.Party.AddRange(StubMercenaries());
+            var run = new RunEntry { PlayerName = "디버그 마법사", HasArrived = true };
             foreach (var element in learned)
                 if (element != null) run.LearnedElements.Add(element);
             return run;
         }
         #endregion
-
-        #region StubMercenaries - 임시 영입용 고정 용병 2명 (4단계에서 후보 추첨으로 바뀜)
-        public static MercenaryEntry[] StubMercenaries() => new[] { new MercenaryEntry(3), new MercenaryEntry(2) };
-        #endregion
-
-        #region StubQuest - 임시 의뢰 이름 (4단계에서 게시판 의뢰로 바뀜)
-        public static LocalizedString StubQuest() => new LocalizedString(StubQuestTable, StubQuestKey);
-        #endregion
     }
 }
```

---

### `Assets/Scripts/Managers/DataManager.cs`

**변경 내용:** 직업·의뢰 SO 목록 칸(Boot에서 연결)과 시작 때 검사 (비었으면 에러, 빈 칸·빈 id·id 겹침·이름 키 빔)

```diff
@@ -25,11 +25,25 @@ public sealed class DataManager : MonoSingleton<DataManager>
         /// <summary>튜토리얼 전부 (연결 순서, 읽기 전용)</summary>
         public IReadOnlyList<TutorialSO> Tutorials => _tutorials;
 
-        #region InitSingleton - 팁 목록 검사, 튜토리얼 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
+        /// <summary>용병 직업 전부 (Boot에서 연결. 영입 후보가 여기서 뽑힘)</summary>
+        [SerializeField] JobSO[] _jobs;
+
+        /// <summary>용병 직업 전부 (연결 순서, 읽기 전용)</summary>
+        public IReadOnlyList<JobSO> Jobs => _jobs;
+
+        /// <summary>의뢰 전부 (Boot에서 연결. 첫 칸이 첫 의뢰)</summary>
+        [SerializeField] QuestSO[] _quests;
+
+        /// <summary>의뢰 전부 (연결 순서, 읽기 전용)</summary>
+        public IReadOnlyList<QuestSO> Quests => _quests;
+
+        #region InitSingleton - 팁·튜토리얼·직업·의뢰 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
         protected override void InitSingleton()
         {
             ValidateTips();
             ValidateTutorials();
+            ValidateJobs();
+            ValidateQuests();
             if (_elements == null || _elements.Length == 0)
             {
                 Debug.LogError("[Data] 요소 목록 비어 있음");
@@ -86,6 +100,62 @@ public TutorialSO FindTutorial(string id)
         }
         #endregion
 
+        #region ValidateJobs - 직업 목록 검사: 비었으면 에러(영입 후보가 안 나옴), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔
+        void ValidateJobs()
+        {
+            if (_jobs == null || _jobs.Length == 0)
+            {
+                Debug.LogError("[Data] 직업 목록 비어 있음. 영입 후보가 안 나옴");
+                _jobs = Array.Empty<JobSO>();
+                return;
+            }
+
+            // 지금까지 본 id (대소문자 구분)
+            var ids = new HashSet<string>(StringComparer.Ordinal);
+            for (int i = 0; i < _jobs.Length; i++)
+            {
+                // 검사할 직업
+                JobSO job = _jobs[i];
+                if (job == null)
+                    Debug.LogError($"[Data] 직업 {i}번 칸 비어 있음");
+                else if (string.IsNullOrWhiteSpace(job.Id))
+                    Debug.LogError($"[Data] {job.name}의 id 비어 있음");
+                else if (!ids.Add(job.Id))
+                    Debug.LogError($"[Data] 직업 id 겹침: {job.Id} ({job.name})");
+                else if (job.Name.IsEmpty || job.Description.IsEmpty)
+                    Debug.LogError($"[Data] {job.name}의 이름·설명 키 비어 있음");
+            }
+        }
+        #endregion
+
+        #region ValidateQuests - 의뢰 목록 검사: 비었으면 에러(게시판이 빔), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔
+        void ValidateQuests()
+        {
+            if (_quests == null || _quests.Length == 0)
+            {
+                Debug.LogError("[Data] 의뢰 목록 비어 있음. 게시판이 빔");
+                _quests = Array.Empty<QuestSO>();
+                return;
+            }
+
+            // 지금까지 본 id (대소문자 구분)
+            var ids = new HashSet<string>(StringComparer.Ordinal);
+            for (int i = 0; i < _quests.Length; i++)
+            {
+                // 검사할 의뢰
+                QuestSO quest = _quests[i];
+                if (quest == null)
+                    Debug.LogError($"[Data] 의뢰 {i}번 칸 비어 있음");
+                else if (string.IsNullOrWhiteSpace(quest.Id))
+                    Debug.LogError($"[Data] {quest.name}의 id 비어 있음");
+                else if (!ids.Add(quest.Id))
+                    Debug.LogError($"[Data] 의뢰 id 겹침: {quest.Id} ({quest.name})");
+                else if (quest.Title.IsEmpty || quest.Description.IsEmpty)
+                    Debug.LogError($"[Data] {quest.name}의 이름·설명 키 비어 있음");
+            }
+        }
+        #endregion
+
         #region ValidateTutorials - 튜토리얼 목록 검사: 비었으면 경고, 빈 칸, 빈 id, id 겹침, 제목·설명 키 빔
         void ValidateTutorials()
         {
```

---

### `Assets/Scripts/Dialogue/DialogueChoiceEntry.cs` (새 파일)

**추가 내용:** 대화 선택지 하나 (POCO): 버튼 글자 테이블·키·자리 값, 누르면 할 일. 다음 대사·닫기는 할 일이 정함

```csharp
using System;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>대화 선택지 하나: 버튼 글자(테이블·키·자리 값)와 누르면 할 일</summary>
    public sealed class DialogueChoiceEntry
    {
        /// <summary>버튼 글자가 든 String Table</summary>
        public TableReference Table { get; }

        /// <summary>버튼 글자 키</summary>
        public string Key { get; }

        /// <summary>버튼 글자의 {0}, {1}… 자리에 넣을 값 (없으면 빈 배열)</summary>
        public object[] Args { get; }

        /// <summary>누르면 할 일 (다음 대사를 띄우거나 대화를 닫는 것까지 여기서 정함)</summary>
        public Action OnPick { get; }

        #region DialogueChoiceEntry - 키·할 일 검사하고 만듦
        /// <param name="table">버튼 글자가 든 String Table</param>
        /// <param name="key">버튼 글자 키</param>
        /// <param name="onPick">누르면 할 일</param>
        /// <param name="args">버튼 글자 자리 값</param>
        public DialogueChoiceEntry(TableReference table, string key, Action onPick, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("빈 선택지 키", nameof(key));
            Table = table;
            Key = key;
            OnPick = onPick ?? throw new ArgumentNullException(nameof(onPick));
            Args = args ?? Array.Empty<object>();
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/DialogueView.cs` (새 파일)

**추가 내용:** NPC 대화 참조 (Boot `UICanvas/Dialogue`): 켜고 끄는 `Body`, 말한 사람·대사 글자, 선택지 자리·프리팹

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>NPC 대화 참조 (Boot 씬 UICanvas/Dialogue. 화면 아래 말한 사람·대사, 그 위에 선택지)</summary>
    public sealed class DialogueView : MonoBehaviour
    {
        /// <summary>대화 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>말한 사람 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _speaker;

        /// <summary>대사 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _line;

        /// <summary>선택지 버튼을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _choiceList;

        /// <summary>선택지 버튼 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] DialogueChoiceView _choicePrefab;

        /// <summary>대화 전체</summary>
        public GameObject Root => _root;

        /// <summary>말한 사람 글자</summary>
        public LocalizeStringEvent Speaker => _speaker;

        /// <summary>대사 글자</summary>
        public LocalizeStringEvent Line => _line;

        /// <summary>선택지 버튼 자리</summary>
        public RectTransform ChoiceList => _choiceList;

        /// <summary>선택지 버튼 프리팹</summary>
        public DialogueChoiceView ChoicePrefab => _choicePrefab;
    }
}
```

---

### `Assets/Scripts/UI/Views/DialogueChoiceView.cs` (새 파일)

**추가 내용:** 선택지 버튼 참조 (`DialogueChoice` 프리팹 루트)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>대화 선택지 버튼 참조 (DialogueChoice 프리팹 루트)</summary>
    public sealed class DialogueChoiceView : MonoBehaviour
    {
        /// <summary>선택지 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>선택지 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _label;

        /// <summary>선택지 버튼</summary>
        public Button Button => _button;

        /// <summary>선택지 글자</summary>
        public LocalizeStringEvent Label => _label;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/DialogueController.cs` (새 파일)

**추가 내용:** 대화 띄우기: 말한 사람·대사 키·자리 값을 넣고 선택지 버튼을 다시 만듦 (떠 있으면 내용만 바꿈). 고르면 선택지 할 일만 부름. 일시정지는 UIManager

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>NPC 대화: 화면 아래 말한 사람·대사, 그 위에 선택지 버튼. 고르면 선택지의 할 일만 부름 (다음 대사·닫기는 그 할 일이 정함). 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class DialogueController : MonoBehaviour
    {
        /// <summary>같은 오브젝트의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] DialogueView _view;

        /// <summary>지금 만든 선택지 버튼</summary>
        readonly List<DialogueChoiceView> _choices = new();

        /// <summary>대화가 떠 있는지</summary>
        public bool IsOpen => _view != null && _view.Root.activeSelf;

        #region Show - 말한 사람·대사 키를 넣고 선택지 버튼을 다시 만들어 띄움 (떠 있으면 내용만 바꿈)
        /// <param name="table">말한 사람·대사가 든 String Table</param>
        /// <param name="speakerKey">말한 사람 키</param>
        /// <param name="lineKey">대사 키</param>
        /// <param name="choices">선택지 (하나 이상)</param>
        /// <param name="lineArgs">대사의 {0}, {1}… 자리에 넣을 값</param>
        public void Show(TableReference table, string speakerKey, string lineKey, IReadOnlyList<DialogueChoiceEntry> choices, params object[] lineArgs)
        {
            if (_view == null)
            {
                Debug.LogError("[Dialogue] DialogueView 연결 안 됨", this);
                return;
            }
            if (choices == null || choices.Count == 0)
            {
                Debug.LogError($"[Dialogue] 선택지 없는 대화: {lineKey}", this);
                return;
            }
            _view.Speaker.StringReference.SetReference(table, speakerKey);
            // 대사 글자의 String Reference
            var line = _view.Line.StringReference;
            line.SetReference(table, lineKey);
            line.Arguments = lineArgs;
            ClearChoices();
            foreach (DialogueChoiceEntry choice in choices)
            {
                // 이 선택지 버튼
                DialogueChoiceView button = Instantiate(_view.ChoicePrefab, _view.ChoiceList);
                // 버튼 글자의 String Reference
                var label = button.Label.StringReference;
                label.SetReference(choice.Table, choice.Key);
                label.Arguments = choice.Args;
                // 누를 때 쓸 선택지 (반복 변수를 붙잡지 않게)
                DialogueChoiceEntry picked = choice;
                button.Button.onClick.AddListener(() => picked.OnPick());
                button.Label.RefreshString();
                _choices.Add(button);
            }
            _view.Root.SetActive(true);
            _view.Speaker.RefreshString();
            _view.Line.RefreshString();
        }
        #endregion

        #region Hide - 대화를 끄고 선택지 버튼을 지움
        public void Hide()
        {
            ClearChoices();
            if (_view != null) _view.Root.SetActive(false);
        }
        #endregion

        #region ClearChoices - 만든 선택지 버튼을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        void ClearChoices()
        {
            foreach (DialogueChoiceView choice in _choices)
                if (choice != null) Destroy(choice.gameObject);
            _choices.Clear();
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/RecruitView.cs` (새 파일)

**추가 내용:** 영입 창 참조 (Boot `UICanvas/Recruit` 루트): 후보·파티 줄 자리, 줄 프리팹, 닫기 버튼

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>영입 창 참조 (Boot 씬 UICanvas/Recruit 루트. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class RecruitView : MonoBehaviour
    {
        /// <summary>후보 줄을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _candidateList;

        /// <summary>파티 줄을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _partyList;

        /// <summary>용병 한 줄 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] RecruitRowView _rowPrefab;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>후보 줄 자리</summary>
        public RectTransform CandidateList => _candidateList;

        /// <summary>파티 줄 자리</summary>
        public RectTransform PartyList => _partyList;

        /// <summary>용병 한 줄 프리팹</summary>
        public RecruitRowView RowPrefab => _rowPrefab;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
```

---

### `Assets/Scripts/UI/Views/RecruitRowView.cs` (새 파일)

**추가 내용:** 용병 한 줄 참조 (`RecruitRow` 프리팹 루트): 이름·직업(툴팁)·레벨·전투 글자, 버튼·버튼 글자

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>영입 창 용병 한 줄 참조 (RecruitRow 프리팹 루트): 이름·직업·레벨·전투·버튼</summary>
    public sealed class RecruitRowView : MonoBehaviour
    {
        /// <summary>이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>직업 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _jobText;

        /// <summary>직업 글자에 마우스를 올리면 무기·역할 툴팁 (인스펙터에서만 연결)</summary>
        [SerializeField] TooltipHandler _jobTooltip;

        /// <summary>레벨 글자 (Hub/hub.recruit.level, {0} 레벨. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _levelText;

        /// <summary>전투 글자 (Hub/hub.recruit.combat, {0} 전투 수치. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _combatText;

        /// <summary>영입·내보내기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>버튼 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _buttonLabel;

        /// <summary>이름 글자</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>직업 글자</summary>
        public LocalizeStringEvent JobText => _jobText;

        /// <summary>직업 툴팁</summary>
        public TooltipHandler JobTooltip => _jobTooltip;

        /// <summary>레벨 글자</summary>
        public LocalizeStringEvent LevelText => _levelText;

        /// <summary>전투 글자</summary>
        public LocalizeStringEvent CombatText => _combatText;

        /// <summary>영입·내보내기 버튼</summary>
        public Button Button => _button;

        /// <summary>버튼 글자</summary>
        public LocalizeStringEvent ButtonLabel => _buttonLabel;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/RecruitController.cs` (새 파일)

**추가 내용:** 영입 창: 상태가 넘긴 후보·파티 목록으로 줄을 만들고, 영입(파티가 차면 못 누름)·내보내기를 누르면 상태의 할 일을 부른 뒤 목록을 다시 만듦. 직업 글자에 무기·역할 툴팁

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>영입 창: 후보·파티 목록을 줄로 만들고, 영입·내보내기를 누르면 상태가 준 할 일을 부른 뒤 목록을 다시 만듦. 목록 데이터는 상태가 넘김. 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class RecruitController : MonoBehaviour
    {
        /// <summary>영입 버튼 글자 키</summary>
        const string HireKey = "hub.recruit.hire";

        /// <summary>내보내기 버튼 글자 키</summary>
        const string DismissKey = "hub.recruit.dismiss";

        /// <summary>레벨 글자 키 ({0} 레벨)</summary>
        const string LevelKey = "hub.recruit.level";

        /// <summary>전투 글자 키 ({0} 전투 수치)</summary>
        const string CombatKey = "hub.recruit.combat";

        /// <summary>영입 창 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] RecruitView _view;

        /// <summary>지금 만든 줄</summary>
        readonly List<RecruitRowView> _rows = new();

        /// <summary>영입 후보 (상태가 넘긴 판 데이터 목록)</summary>
        IReadOnlyList<MercenaryEntry> _candidates;

        /// <summary>지금 파티 (상태가 넘긴 판 데이터 목록)</summary>
        IReadOnlyList<MercenaryEntry> _party;

        /// <summary>파티에 넣을 수 있는 용병 수</summary>
        int _partyMax;

        /// <summary>영입을 누르면 할 일 (상태가 후보 → 파티로 옮김)</summary>
        Action<MercenaryEntry> _onHire;

        /// <summary>내보내기를 누르면 할 일 (상태가 파티 → 후보로 옮김)</summary>
        Action<MercenaryEntry> _onDismiss;

        /// <summary>같은 창의 참조</summary>
        public RecruitView View => _view;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Show - 목록과 할 일을 받아 창을 켜고 줄을 만듦
        /// <param name="candidates">영입 후보</param>
        /// <param name="party">지금 파티</param>
        /// <param name="partyMax">파티에 넣을 수 있는 용병 수</param>
        /// <param name="onHire">영입을 누르면 할 일</param>
        /// <param name="onDismiss">내보내기를 누르면 할 일</param>
        public void Show(IReadOnlyList<MercenaryEntry> candidates, IReadOnlyList<MercenaryEntry> party, int partyMax,
            Action<MercenaryEntry> onHire, Action<MercenaryEntry> onDismiss)
        {
            if (_view == null)
            {
                Debug.LogError("[Recruit] RecruitView 연결 안 됨", this);
                return;
            }
            _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _partyMax = partyMax;
            _onHire = onHire ?? throw new ArgumentNullException(nameof(onHire));
            _onDismiss = onDismiss ?? throw new ArgumentNullException(nameof(onDismiss));
            gameObject.SetActive(true);
            Rebuild();
        }
        #endregion

        #region Hide - 창을 끄고 줄·할 일을 비움
        public void Hide()
        {
            ClearRows();
            _onHire = null;
            _onDismiss = null;
            gameObject.SetActive(false);
        }
        #endregion

        #region Rebuild - 줄을 지우고 후보(영입, 파티가 차면 못 누름)·파티(내보내기) 순서대로 다시 만듦
        void Rebuild()
        {
            ClearRows();
            // 파티에 자리가 남았는지
            bool hasRoom = _party.Count < _partyMax;
            foreach (MercenaryEntry candidate in _candidates)
            {
                // 누를 때 쓸 후보 (반복 변수를 붙잡지 않게)
                MercenaryEntry picked = candidate;
                AddRow(_view.CandidateList, candidate, HireKey, hasRoom, () => _onHire?.Invoke(picked));
            }
            foreach (MercenaryEntry member in _party)
            {
                // 누를 때 쓸 파티원
                MercenaryEntry picked = member;
                AddRow(_view.PartyList, member, DismissKey, true, () => _onDismiss?.Invoke(picked));
            }
        }
        #endregion

        #region AddRow - 용병 한 줄: 이름·직업(툴팁)·레벨·전투 글자와 버튼. 누르면 할 일 뒤 목록을 다시 만듦
        /// <param name="list">줄을 넣을 자리</param>
        /// <param name="entry">보일 용병</param>
        /// <param name="buttonKey">버튼 글자 키</param>
        /// <param name="interactable">버튼을 누를 수 있는지</param>
        /// <param name="onClick">버튼을 누르면 할 일</param>
        void AddRow(RectTransform list, MercenaryEntry entry, string buttonKey, bool interactable, Action onClick)
        {
            // 이 용병의 줄
            RecruitRowView row = Instantiate(_view.RowPrefab, list);
            row.NameText.StringReference.SetReference(entry.Name.TableReference, entry.Name.TableEntryReference);
            row.JobText.StringReference.SetReference(entry.Job.Name.TableReference, entry.Job.Name.TableEntryReference);
            row.JobTooltip.Set(entry.Job.Name, entry.Job.Description);
            SetArgsText(row.LevelText, LevelKey, entry.Level);
            SetArgsText(row.CombatText, CombatKey, entry.Combat);
            row.ButtonLabel.StringReference.SetReference(Table, buttonKey);
            row.Button.interactable = interactable;
            row.Button.onClick.AddListener(() =>
            {
                onClick();
                if (IsOpen) Rebuild();
            });
            _rows.Add(row);
        }
        #endregion

        #region SetArgsText - 이 창 테이블의 키와 {0} 값을 넣고 글자 갱신
        /// <param name="text">넣을 글자</param>
        /// <param name="key">문구 키</param>
        /// <param name="value">{0} 자리 값</param>
        static void SetArgsText(UnityEngine.Localization.Components.LocalizeStringEvent text, string key, int value)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.SetReference(Table, key);
            reference.Arguments = new object[] { value };
            text.RefreshString();
        }
        #endregion

        #region ClearRows - 만든 줄을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        void ClearRows()
        {
            foreach (RecruitRowView row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
        #endregion
    }
}
```

**호출 관계:** `HubState.OpenRecruit` → `UIManager.ShowRecruit(후보, 파티, 2, Hire, Dismiss)` → `RecruitController.Show`. 줄 버튼 → `HubState.Hire`/`Dismiss` → `Rebuild`

---

### `Assets/Scripts/UI/Views/QuestBoardView.cs` (새 파일)

**추가 내용:** 임무 게시판 창 참조 (Boot `UICanvas/QuestBoard` 루트): 의뢰 이름·설명·기간 글자, 받기 버튼·글자, 닫기 버튼

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>임무 게시판 창 참조 (Boot 씬 UICanvas/QuestBoard 루트. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class QuestBoardView : MonoBehaviour
    {
        /// <summary>의뢰 이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questTitle;

        /// <summary>의뢰 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questDescription;

        /// <summary>기간 글자 (Hub/hub.board.days, {0} 일 수. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questDays;

        /// <summary>받기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _takeButton;

        /// <summary>받기 버튼 글자 (받기·받음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _takeLabel;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>의뢰 이름 글자</summary>
        public LocalizeStringEvent QuestTitle => _questTitle;

        /// <summary>의뢰 설명 글자</summary>
        public LocalizeStringEvent QuestDescription => _questDescription;

        /// <summary>기간 글자</summary>
        public LocalizeStringEvent QuestDays => _questDays;

        /// <summary>받기 버튼</summary>
        public Button TakeButton => _takeButton;

        /// <summary>받기 버튼 글자</summary>
        public LocalizeStringEvent TakeLabel => _takeLabel;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/QuestBoardController.cs` (새 파일)

**추가 내용:** 게시판 창: 상태가 넘긴 의뢰의 이름·설명·기간을 넣음. 받기 → 상태 할 일 + 버튼 '받음'(못 누름)

```csharp
using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>임무 게시판 창: 의뢰 이름·설명·기간과 받기 버튼. 받으면 상태가 준 할 일을 부르고 버튼을 '받음'으로. 의뢰 데이터는 상태가 넘김. 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class QuestBoardController : MonoBehaviour
    {
        /// <summary>받기 버튼 글자 키</summary>
        const string TakeKey = "hub.board.take";

        /// <summary>받은 뒤 버튼 글자 키</summary>
        const string TakenKey = "hub.board.taken";

        /// <summary>기간 글자 키 ({0} 일 수)</summary>
        const string DaysKey = "hub.board.days";

        /// <summary>게시판 창 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] QuestBoardView _view;

        /// <summary>받기를 누르면 할 일 (상태가 판 데이터에 의뢰를 넣음). 창이 닫혀 있으면 null</summary>
        Action _onTake;

        /// <summary>같은 창의 참조</summary>
        public QuestBoardView View => _view;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Awake - 연결 확인, 받기 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Board] QuestBoardView 연결 안 됨", this);
                return;
            }
            _view.TakeButton.onClick.AddListener(OnTakeClicked);
        }
        #endregion

        #region Show - 의뢰 이름·설명·기간을 넣고 켬. 이미 받았으면 버튼을 '받음'으로
        /// <param name="quest">보일 의뢰</param>
        /// <param name="taken">이미 받았는지</param>
        /// <param name="onTake">받기를 누르면 할 일</param>
        public void Show(QuestSO quest, bool taken, Action onTake)
        {
            if (_view == null)
            {
                Debug.LogError("[Board] QuestBoardView 연결 안 됨", this);
                return;
            }
            if (quest == null) throw new ArgumentNullException(nameof(quest));
            _onTake = onTake ?? throw new ArgumentNullException(nameof(onTake));
            _view.QuestTitle.StringReference.SetReference(quest.Title.TableReference, quest.Title.TableEntryReference);
            _view.QuestDescription.StringReference.SetReference(quest.Description.TableReference, quest.Description.TableEntryReference);
            // 기간 글자의 String Reference
            var days = _view.QuestDays.StringReference;
            days.SetReference(Table, DaysKey);
            days.Arguments = new object[] { quest.Days };
            gameObject.SetActive(true);
            _view.QuestDays.RefreshString();
            SetTaken(taken);
        }
        #endregion

        #region Hide - 창을 끄고 할 일을 비움
        public void Hide()
        {
            _onTake = null;
            gameObject.SetActive(false);
        }
        #endregion

        #region OnTakeClicked - 받기: 할 일을 부르고 버튼을 '받음'으로
        void OnTakeClicked()
        {
            _onTake?.Invoke();
            SetTaken(true);
        }
        #endregion

        #region SetTaken - 받았으면 버튼을 못 누르게 하고 글자를 '받음'으로
        /// <param name="taken">받았는지</param>
        void SetTaken(bool taken)
        {
            _view.TakeButton.interactable = !taken;
            _view.TakeLabel.StringReference.SetReference(Table, taken ? TakenKey : TakeKey);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Sequences/FadeSequence.cs` (새 파일)

**추가 내용:** 화면을 0.25초 검게 덮고 → 할 일 → 0.25초 걷고 → 끝 알림. 실제 시간이라 일시정지 중에도 돎. 덮는 동안 클릭 막음, 도는 중이면 새로 안 틂

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>화면을 잠깐 검게 덮었다가 걷는 연출 (Boot 씬 UICanvas/Fade). 다 덮였을 때 할 일을 부르고, 걷힌 뒤 끝 알림. 실제 시간이라 일시정지 중에도 돎</summary>
    public sealed class FadeSequence : MonoBehaviour
    {
        /// <summary>검은 화면 투명도 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>덮거나 걷는 데 걸리는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _halfSeconds = 0.25f;

        /// <summary>지금 도는 연출. 없으면 null</summary>
        Coroutine _routine;

        #region Awake - 연결 확인, 처음엔 투명·클릭 통과
        void Awake()
        {
            if (_group == null)
            {
                Debug.LogError("[Fade] CanvasGroup 연결 안 됨", this);
                return;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }
        #endregion

        #region Play - 덮기 → onBlack → 걷기 → onDone. 연결이 없거나 이미 도는 중이면 안 틀고 false
        /// <param name="onBlack">다 덮였을 때 할 일 (없으면 null)</param>
        /// <param name="onDone">다 걷힌 뒤 할 일 (없으면 null)</param>
        public bool Play(Action onBlack, Action onDone)
        {
            if (_group == null || _routine != null) return false;
            _routine = StartCoroutine(Run(onBlack, onDone));
            return true;
        }
        #endregion

        #region Run - 덮는 동안 클릭을 막고, 덮인 뒤 할 일, 걷은 뒤 끝 알림
        /// <param name="onBlack">다 덮였을 때 할 일</param>
        /// <param name="onDone">다 걷힌 뒤 할 일</param>
        IEnumerator Run(Action onBlack, Action onDone)
        {
            _group.blocksRaycasts = true;
            yield return FadeTo(1f);
            onBlack?.Invoke();
            yield return FadeTo(0f);
            _group.blocksRaycasts = false;
            _routine = null;
            onDone?.Invoke();
        }
        #endregion

        #region FadeTo - 투명도를 목표까지 정해진 시간 동안 바꿈 (실제 시간)
        /// <param name="target">목표 투명도 (0~1)</param>
        IEnumerator FadeTo(float target)
        {
            // 시작 투명도
            float start = _group.alpha;
            // 지난 시간 (초)
            float elapsed = 0f;
            while (elapsed < _halfSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, elapsed / _halfSeconds);
                yield return null;
            }
            _group.alpha = target;
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Managers/UIManager.cs`

**변경 내용:** 페이드·대화·영입 창·게시판 창 칸과 열기/닫기. 대화·영입·게시판 중 하나라도 떠 있으면 `SetMenuPaused(true)`, 다 닫히면 되돌림 (창을 바꿔 열 땐 열기 → 닫기 순서). Esc 순서에 대화 → 영입 → 게시판 추가, `IsWindowOpen`에 세 창 추가. `FadeThrough`

```diff
@@ -1,5 +1,6 @@
 using System;
 using System.Collections;
+using System.Collections.Generic;
 using UnityEngine;
 using UnityEngine.InputSystem;
 using UnityEngine.Localization;
@@ -7,7 +8,7 @@
 
 namespace Mage
 {
-    /// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·자막·설정 창·확인창·일시정지 메뉴·메뉴 창(지도·임무 일지·튜토리얼·주문 도감)·툴팁</summary>
+    /// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·자막·설정 창·확인창·일시정지 메뉴·메뉴 창(지도·임무 일지·튜토리얼·주문 도감)·툴팁·NPC 대화·영입 창·게시판 창·화면 페이드</summary>
     public sealed class UIManager : MonoSingleton<UIManager>
     {
         /// <summary>토스트 표시 시간 (초)</summary>
@@ -52,6 +53,18 @@ public sealed class UIManager : MonoSingleton<UIManager>
         /// <summary>툴팁 (Boot 씬 UICanvas 맨 위 자식)</summary>
         [SerializeField] TooltipController _tooltip;
 
+        /// <summary>화면 페이드 (Boot 씬 UICanvas 맨 아래 자식. 문 드나들 때)</summary>
+        [SerializeField] FadeSequence _fade;
+
+        /// <summary>NPC 대화 (Boot 씬 UICanvas 자식. 자막 위에 그려짐)</summary>
+        [SerializeField] DialogueController _dialogue;
+
+        /// <summary>영입 창 (Boot 씬 UICanvas 자식)</summary>
+        [SerializeField] RecruitController _recruit;
+
+        /// <summary>임무 게시판 창 (Boot 씬 UICanvas 자식)</summary>
+        [SerializeField] QuestBoardController _questBoard;
+
         /// <summary>자막을 숨기는 코루틴. 새 자막이 오면 끊고 다시 시작</summary>
         Coroutine _hideSubtitle;
 
@@ -129,7 +142,7 @@ IEnumerator HideToastAfter(float seconds)
         }
         #endregion
 
-        #region InitWindows - 설정 창·확인창·일시정지 메뉴를 끄고 버튼을 한 번만 연결, Esc 구독, 메뉴 창·자막·툴팁 끄기, M·J 구독
+        #region InitWindows - 설정 창·확인창·일시정지 메뉴를 끄고 버튼을 한 번만 연결, Esc 구독, 메뉴 창·자막·툴팁·대화·영입·게시판 끄기, M·J 구독
         void InitWindows()
         {
             if (_pauseMenu == null)
@@ -173,6 +186,27 @@ void InitWindows()
             if (_tooltip == null) Debug.LogError("[UI] TooltipController 연결 안 됨");
             else _tooltip.Hide();
 
+            if (_fade == null) Debug.LogError("[UI] FadeSequence 연결 안 됨");
+
+            if (_dialogue == null) Debug.LogError("[UI] DialogueController 연결 안 됨");
+            else _dialogue.Hide();
+
+            if (_recruit == null)
+                Debug.LogError("[UI] RecruitController 연결 안 됨");
+            else
+            {
+                _recruit.gameObject.SetActive(false);
+                if (_recruit.View != null) _recruit.View.CloseButton.onClick.AddListener(HideRecruit);
+            }
+
+            if (_questBoard == null)
+                Debug.LogError("[UI] QuestBoardController 연결 안 됨");
+            else
+            {
+                _questBoard.gameObject.SetActive(false);
+                if (_questBoard.View != null) _questBoard.View.CloseButton.onClick.AddListener(HideQuestBoard);
+            }
+
             _map = FindAction(MapActionPath);
             if (_map != null) _map.performed += OnMap;
             _journal = FindAction(JournalActionPath);
@@ -294,7 +328,7 @@ void ReturnToTitle()
         }
         #endregion
 
-        #region OnCancel - Esc: 확인창 → 설정 창 → 메뉴 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸고 플레이 씬이면 일시정지 메뉴를 엶
+        #region OnCancel - Esc: 확인창 → 설정 창 → 대화 → 영입 창 → 게시판 창 → 메뉴 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸고 플레이 씬이면 일시정지 메뉴를 엶
         /// <param name="context">입력 정보 (안 씀)</param>
         void OnCancel(InputAction.CallbackContext context)
         {
@@ -302,6 +336,12 @@ void OnCancel(InputAction.CallbackContext context)
                 HideConfirm();
             else if (_settings != null && _settings.gameObject.activeSelf)
                 HideSettings();
+            else if (_dialogue != null && _dialogue.IsOpen)
+                HideDialogue();
+            else if (_recruit != null && _recruit.IsOpen)
+                HideRecruit();
+            else if (_questBoard != null && _questBoard.IsOpen)
+                HideQuestBoard();
             else if (_gameMenu != null && _gameMenu.IsOpen)
                 HideGameMenu();
             else if (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
@@ -358,11 +398,118 @@ void HideGameMenu()
         }
         #endregion
 
-        #region IsWindowOpen - 일시정지 메뉴·설정 창·확인창 중 하나라도 떠 있는지 (메뉴 창과 같이 안 뜨게)
+        #region IsWindowOpen - 일시정지 메뉴·설정 창·확인창·대화·영입 창·게시판 창 중 하나라도 떠 있는지 (메뉴 창과 같이 안 뜨게)
         bool IsWindowOpen() =>
             (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
             || (_settings != null && _settings.gameObject.activeSelf)
-            || (_confirm != null && _confirm.gameObject.activeSelf);
+            || (_confirm != null && _confirm.gameObject.activeSelf)
+            || IsHubWindowOpen();
+        #endregion
+
+        #region IsHubWindowOpen - 대화·영입 창·게시판 창 중 하나라도 떠 있는지
+        bool IsHubWindowOpen() =>
+            (_dialogue != null && _dialogue.IsOpen)
+            || (_recruit != null && _recruit.IsOpen)
+            || (_questBoard != null && _questBoard.IsOpen);
+        #endregion
+
+        #region RefreshMenuPause - 대화·영입·게시판 창이 하나라도 떠 있으면 게임 멈춤, 다 닫히면 되돌림 (창을 바꿔 열 때 안 풀리게 열기 → 닫기 순서로 부름)
+        void RefreshMenuPause()
+        {
+            // 일시정지 메뉴·메뉴 창은 스스로 멈추고 풀어서 그대로 둠 (대화류와 같이 안 뜸)
+            bool otherMenuOpen = (_pauseMenu != null && _pauseMenu.gameObject.activeSelf) || (_gameMenu != null && _gameMenu.IsOpen);
+            if (otherMenuOpen) return;
+            GameManager.Instance.SetMenuPaused(IsHubWindowOpen());
+        }
+        #endregion
+
+        #region ShowDialogue - NPC 대화를 띄우고 게임 멈춤 (떠 있으면 내용만 바꿈)
+        /// <param name="table">말한 사람·대사가 든 String Table</param>
+        /// <param name="speakerKey">말한 사람 키</param>
+        /// <param name="lineKey">대사 키</param>
+        /// <param name="choices">선택지 (하나 이상)</param>
+        /// <param name="lineArgs">대사의 {0}, {1}… 자리에 넣을 값</param>
+        public void ShowDialogue(TableReference table, string speakerKey, string lineKey, IReadOnlyList<DialogueChoiceEntry> choices, params object[] lineArgs)
+        {
+            if (_dialogue == null)
+            {
+                Debug.LogError("[UI] DialogueController 연결 안 됨");
+                return;
+            }
+            _dialogue.Show(table, speakerKey, lineKey, choices, lineArgs);
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region HideDialogue - NPC 대화를 닫음. 다른 창이 없으면 게임을 다시 돌림
+        public void HideDialogue()
+        {
+            if (_dialogue != null) _dialogue.Hide();
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region ShowRecruit - 영입 창을 띄우고 게임 멈춤 (목록·할 일은 상태가 넘김)
+        /// <param name="candidates">영입 후보</param>
+        /// <param name="party">지금 파티</param>
+        /// <param name="partyMax">파티에 넣을 수 있는 용병 수</param>
+        /// <param name="onHire">영입을 누르면 할 일</param>
+        /// <param name="onDismiss">내보내기를 누르면 할 일</param>
+        public void ShowRecruit(IReadOnlyList<MercenaryEntry> candidates, IReadOnlyList<MercenaryEntry> party, int partyMax,
+            Action<MercenaryEntry> onHire, Action<MercenaryEntry> onDismiss)
+        {
+            if (_recruit == null)
+            {
+                Debug.LogError("[UI] RecruitController 연결 안 됨");
+                return;
+            }
+            _recruit.Show(candidates, party, partyMax, onHire, onDismiss);
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region HideRecruit - 영입 창을 닫음 (닫기 버튼·Esc). 다른 창이 없으면 게임을 다시 돌림
+        public void HideRecruit()
+        {
+            if (_recruit != null) _recruit.Hide();
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region ShowQuestBoard - 게시판 창을 띄우고 게임 멈춤 (의뢰·할 일은 상태가 넘김)
+        /// <param name="quest">보일 의뢰</param>
+        /// <param name="taken">이미 받았는지</param>
+        /// <param name="onTake">받기를 누르면 할 일</param>
+        public void ShowQuestBoard(QuestSO quest, bool taken, Action onTake)
+        {
+            if (_questBoard == null)
+            {
+                Debug.LogError("[UI] QuestBoardController 연결 안 됨");
+                return;
+            }
+            _questBoard.Show(quest, taken, onTake);
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region HideQuestBoard - 게시판 창을 닫음 (닫기 버튼·Esc). 다른 창이 없으면 게임을 다시 돌림
+        public void HideQuestBoard()
+        {
+            if (_questBoard != null) _questBoard.Hide();
+            RefreshMenuPause();
+        }
+        #endregion
+
+        #region FadeThrough - 화면을 잠깐 검게 덮고 다 덮였을 때 onBlack, 걷힌 뒤 onDone. 이미 덮는 중이면 안 하고 false (연결이 없으면 바로 둘 다 부름)
+        /// <param name="onBlack">다 덮였을 때 할 일</param>
+        /// <param name="onDone">다 걷힌 뒤 할 일</param>
+        public bool FadeThrough(Action onBlack, Action onDone)
+        {
+            if (_fade != null) return _fade.Play(onBlack, onDone);
+            onBlack?.Invoke();
+            onDone?.Invoke();
+            return true;
+        }
         #endregion
 
         #region ShowSubtitle - 말한 사람·대사 자막을 게임 시간 4초 보이고, 끝나면 onHidden (앞 자막은 알림 없이 끊음)
```

---

### `Assets/Scripts/World/DoorView.cs` (새 파일)

**추가 내용:** 드나드는 문 표지 (같은 오브젝트의 `InteractPointView`와 같이 씀, 로직 없음): 도착 지점

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>드나드는 문 표지 (같은 오브젝트의 InteractPointView와 같이 씀, 로직 없음). 쓰면 화면이 잠깐 어두워졌다가 도착 지점으로 옮김</summary>
    public sealed class DoorView : MonoBehaviour
    {
        /// <summary>문을 쓰면 서는 곳 (위치·좌우 방향. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _destination;

        /// <summary>문을 쓰면 서는 곳</summary>
        public Transform Destination => _destination;
    }
}
```

---

### `Assets/Scripts/World/InteractPointView.cs`

**변경 내용:** 주석만 (E → 상호작용 키, 전초기지 id 목록)

```diff
@@ -3,13 +3,13 @@
 
 namespace Mage
 {
-    /// <summary>E로 쓸 수 있는 곳 표지 (씬 오브젝트, 로직 없음). 무엇을 할지는 상태가 Id로 정함. 콜라이더가 같은 오브젝트나 자식에 있어야 바라볼 때 잡힘</summary>
+    /// <summary>상호작용 키(기본 F)로 쓸 수 있는 곳 표지 (씬 오브젝트, 로직 없음). 무엇을 할지는 상태가 Id로 정함 (문이면 같은 오브젝트의 DoorView). 콜라이더가 같은 오브젝트나 자식에 있어야 바라볼 때 잡힘</summary>
     public sealed class InteractPointView : MonoBehaviour
     {
-        /// <summary>상태가 구분할 id (전초기지: recruit, board, depart). 인스펙터에서만 정함</summary>
+        /// <summary>상태가 구분할 id (전초기지: innkeeper, clerk, board, guard, room_door, 문들). 인스펙터에서만 정함</summary>
         [SerializeField] string _id;
 
-        /// <summary>안내에 보일 이름 ("[E] 출발"의 출발). 인스펙터에서만 정함</summary>
+        /// <summary>안내에 보일 이름 ("[F] 여관"의 여관). 인스펙터에서만 정함</summary>
         [SerializeField] LocalizedString _label;
 
         /// <summary>상태가 구분할 id</summary>
```

---

### `Assets/Scripts/Player/InteractDetector.cs`

**변경 내용:** 주석만 (E → 상호작용 키, 기본 F)

```diff
@@ -4,10 +4,10 @@
 
 namespace Mage
 {
-    /// <summary>눈 앞 일정 거리 안에서 바라보는 상호작용 지점을 찾고, E(Player/Interact)를 누르면 알림. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
+    /// <summary>눈 앞 일정 거리 안에서 바라보는 상호작용 지점을 찾고, 상호작용 키(Player/Interact, 기본 F)를 누르면 알림. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
     public sealed class InteractDetector : MonoBehaviour
     {
-        /// <summary>상호작용 입력 액션 경로 (전역 액션, E)</summary>
+        /// <summary>상호작용 입력 액션 경로 (전역 액션, 기본 F)</summary>
         const string InteractActionPath = "Player/Interact";
 
         /// <summary>키 이름을 읽을 바인딩 그룹 (키보드·마우스)</summary>
@@ -31,15 +31,15 @@ public sealed class InteractDetector : MonoBehaviour
         /// <summary>바라보는 지점이 바뀜 (없어지면 null)</summary>
         public event Action<InteractPointView> TargetChanged;
 
-        /// <summary>지점을 바라보며 E를 누름</summary>
+        /// <summary>지점을 바라보며 상호작용 키를 누름</summary>
         public event Action<InteractPointView> Interacted;
 
-        /// <summary>E 키 이름 (안내 글용, 키보드 기준. 액션이 없으면 빈 문자열)</summary>
+        /// <summary>상호작용 키 이름 (안내 글용, 키보드 기준. 액션이 없으면 빈 문자열)</summary>
         public string KeyName => _interact != null
             ? _interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardMouseGroup))
             : string.Empty;
 
-        #region Awake - 연결 확인, E 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
+        #region Awake - 연결 확인, 상호작용 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
         void Awake()
         {
             if (_eye == null) Debug.LogError("[Interact] 눈 연결 안 됨");
@@ -49,14 +49,14 @@ void Awake()
         }
         #endregion
 
-        #region OnEnable - E 구독
+        #region OnEnable - 상호작용 구독
         void OnEnable()
         {
             if (_interact != null) _interact.performed += OnInteract;
         }
         #endregion
 
-        #region OnDisable - E 구독 해제, 바라보던 지점 비움 (알림)
+        #region OnDisable - 상호작용 구독 해제, 바라보던 지점 비움 (알림)
         void OnDisable()
         {
             if (_interact != null) _interact.performed -= OnInteract;
@@ -86,7 +86,7 @@ void SetTarget(InteractPointView target)
         }
         #endregion
 
-        #region OnInteract - 바라보는 지점이 있으면 E 알림
+        #region OnInteract - 바라보는 지점이 있으면 상호작용 알림
         /// <param name="context">입력 정보 (안 씀)</param>
         void OnInteract(InputAction.CallbackContext context)
         {
```

---

### `Assets/Scripts/UI/Views/HubView.cs`

**변경 내용:** 목표 2개(길드·출발), 금화·식량 글자, 숲에서 돌아오면 서는 곳

```diff
@@ -40,6 +40,18 @@ public sealed class HubView : MonoBehaviour
         /// <summary>첫 목표 (여관에서 방 잡기. 인스펙터에서만 연결)</summary>
         [SerializeField] ObjectiveSO _firstObjective;
 
+        /// <summary>방을 잡은 뒤 목표 (길드에서 용병 구하고 의뢰 받기. 인스펙터에서만 연결)</summary>
+        [SerializeField] ObjectiveSO _guildObjective;
+
+        /// <summary>파티·의뢰를 갖춘 뒤 목표 (식량을 챙겨 정문으로. 인스펙터에서만 연결)</summary>
+        [SerializeField] ObjectiveSO _departObjective;
+
+        /// <summary>금화·식량 글자 (Hub/hub.money, {0} 금화·{1} 식량 인분. 인스펙터에서만 연결)</summary>
+        [SerializeField] LocalizeStringEvent _moneyText;
+
+        /// <summary>숲에서 돌아오면 서는 곳 (정문 밖, 정문을 봄. 인스펙터에서만 연결)</summary>
+        [SerializeField] Transform _returnPoint;
+
         /// <summary>파티 수 글자</summary>
         public LocalizeStringEvent PartyText => _partyText;
 
@@ -72,5 +84,17 @@ public sealed class HubView : MonoBehaviour
 
         /// <summary>첫 목표</summary>
         public ObjectiveSO FirstObjective => _firstObjective;
+
+        /// <summary>방을 잡은 뒤 목표</summary>
+        public ObjectiveSO GuildObjective => _guildObjective;
+
+        /// <summary>파티·의뢰를 갖춘 뒤 목표</summary>
+        public ObjectiveSO DepartObjective => _departObjective;
+
+        /// <summary>금화·식량 글자</summary>
+        public LocalizeStringEvent MoneyText => _moneyText;
+
+        /// <summary>숲에서 돌아오면 서는 곳</summary>
+        public Transform ReturnPoint => _returnPoint;
     }
 }
```

---

### `Assets/Scripts/UI/Views/GameMenuView.cs`

**변경 내용:** 임무 일지 받은 의뢰 글자 칸

```diff
@@ -43,6 +43,9 @@ public sealed class GameMenuView : MonoBehaviour
         /// <summary>임무 일지: 목표가 없을 때 글 (인스펙터에서만 연결)</summary>
         [SerializeField] GameObject _journalEmpty;
 
+        /// <summary>임무 일지: 받은 의뢰 글자 (Menu/menu.journal.quest, {0} 이름·{1} 기간. 인스펙터에서만 연결)</summary>
+        [SerializeField] LocalizeStringEvent _questText;
+
         /// <summary>튜토리얼: "모두 보기 (스포일러가 있을 수 있습니다)" 토글 (인스펙터에서만 연결)</summary>
         [SerializeField] Toggle _spoilerToggle;
 
@@ -100,6 +103,9 @@ public sealed class GameMenuView : MonoBehaviour
         /// <summary>임무 일지: 목표가 없을 때 글</summary>
         public GameObject JournalEmpty => _journalEmpty;
 
+        /// <summary>임무 일지: 받은 의뢰 글자</summary>
+        public LocalizeStringEvent QuestText => _questText;
+
         /// <summary>튜토리얼: 모두 보기 토글</summary>
         public Toggle SpoilerToggle => _spoilerToggle;
 
```

---

### `Assets/Scripts/UI/Binders/JournalUIBinder.cs`

**변경 내용:** 탭이 켜질 때 받은 의뢰 줄 "의뢰: 이름 (N일)" (의뢰가 없으면 숨김)

```diff
@@ -2,13 +2,13 @@
 
 namespace Mage
 {
-    /// <summary>임무 일지 탭 ← 이번 판 현재 목표. 탭이 켜질 때마다 채움 (목표가 없으면 빈 글)</summary>
+    /// <summary>임무 일지 탭 ← 이번 판 현재 목표·받은 의뢰. 탭이 켜질 때마다 채움 (목표가 없으면 빈 글, 의뢰가 없으면 의뢰 줄 숨김)</summary>
     public sealed class JournalUIBinder : MonoBehaviour
     {
         /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
         [SerializeField] GameMenuView _view;
 
-        #region OnEnable - 현재 목표의 이름·설명 키를 넣음. 목표가 없으면 빈 글만
+        #region OnEnable - 받은 의뢰 줄을 채우고, 현재 목표의 이름·설명 키를 넣음. 목표가 없으면 빈 글만
         void OnEnable()
         {
             if (_view == null)
@@ -16,6 +16,14 @@ void OnEnable()
                 Debug.LogError("[Menu] JournalUIBinder에 GameMenuView 연결 안 됨", this);
                 return;
             }
+            // 이번 판 받은 의뢰 (판이나 의뢰가 없으면 null)
+            QuestSO quest = GameManager.Instance.Run?.Quest;
+            _view.QuestText.gameObject.SetActive(quest != null);
+            if (quest != null)
+            {
+                _view.QuestText.StringReference.Arguments = new object[] { quest.Title.GetLocalizedString(), quest.Days };
+                _view.QuestText.RefreshString();
+            }
             // 이번 판 현재 목표 (판이나 목표가 없으면 null)
             ObjectiveSO objective = GameManager.Instance.Run?.CurrentObjective;
             // 목표가 있는지
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 임시 E 지점 3개(`Recruit`·`TakeQuest`·`Depart`)를 시설 흐름으로 바꿈.
- 지점 id로 나눔: 문(`DoorView`)이면 페이드 순간이동(방 문은 방을 잡아야 열림), `innkeeper`·`clerk`·`board`·`guard`
- 여관 주인: 방 잡기(금화 70, 한 번) → 길드를 알려 주는 대사 + 목표 갱신, 식량 하루치(인원 × 3)
- 접수원: 처음이면 후보 추첨 → 영입 창. 게시판: 첫 의뢰 하나 → 받기
- 경비병: 첫 출발 전엔 용병 2명·의뢰·식량(기간 × 인원)이 모자라면 한마디로 막음. 되면 출발(식량 뺌, `HasDeparted`) → 숲
- 목표: 방 → 길드(파티·의뢰) → 정문 → (첫 출발 뒤) 없음. 바뀔 때 "임무 일지 갱신" 토스트
- 숲에서 돌아오면 정문 밖 `ReturnPoint`에서 시작, 의뢰 비움, 경비병 자막 → 목표 알림. 화면에 금화·식량

```diff
@@ -1,36 +1,107 @@
 using System;
+using System.Collections.Generic;
 using UnityEngine;
+using UnityEngine.Localization;
 using UnityEngine.Localization.Tables;
 
 namespace Mage
 {
-    /// <summary>전초기지. 첫 도착이면 역마차 연출부터. 1인칭으로 돌아다니며 E 지점에서 임시 영입·의뢰 받기·출발 → 숲</summary>
+    /// <summary>전초기지. 첫 도착이면 역마차 연출부터. 1인칭으로 돌아다니며 상호작용 키(F)로 여관(방·식량)·길드(영입·게시판)·정문(경비병)을 씀 → 숲</summary>
     public sealed class HubState : FlowStateBase
     {
         /// <summary>이 상태의 씬 이름</summary>
         public const string Scene = "Outpost";
 
-        /// <summary>출발에 필요한 용병 수</summary>
+        /// <summary>파티에 넣을 수 있는 용병 수 (플레이어 + 2명). 첫 출발에도 이만큼 필요</summary>
         public const int RequiredMercenaries = 2;
 
-        /// <summary>영입소 지점 id</summary>
-        const string RecruitId = "recruit";
+        /// <summary>여관 방값 (금화, 처음 방 잡을 때 한 번. 임시)</summary>
+        public const int RoomPrice = 70;
 
-        /// <summary>게시판 지점 id</summary>
+        /// <summary>원정 식량 1인분 값 (금화, 한 사람 하루치. 임시)</summary>
+        public const int RationPrice = 3;
+
+        /// <summary>여관 주인 지점 id</summary>
+        const string InnkeeperId = "innkeeper";
+
+        /// <summary>길드 접수원 지점 id</summary>
+        const string ClerkId = "clerk";
+
+        /// <summary>임무 게시판 지점 id</summary>
         const string BoardId = "board";
 
-        /// <summary>출발 표지판 지점 id</summary>
-        const string DepartId = "depart";
+        /// <summary>정문 경비병 지점 id</summary>
+        const string GuardId = "guard";
+
+        /// <summary>여관 방 문 지점 id (방을 잡아야 열림)</summary>
+        const string RoomDoorId = "room_door";
 
-        /// <summary>파티가 모자랄 때 토스트 키 ({0} 필요한 인원)</summary>
+        /// <summary>경비병: 용병이 모자랄 때 대사 키 ({0} 필요한 인원)</summary>
         const string PartyRequiredKey = "hub.party_required";
 
         /// <summary>의뢰를 받았을 때 토스트 키 ({0} 의뢰 이름)</summary>
         const string QuestTakenKey = "hub.quest_taken";
 
-        /// <summary>의뢰 없이 출발할 때 토스트 키</summary>
+        /// <summary>경비병: 의뢰 없이 나가려 할 때 대사 키</summary>
         const string QuestRequiredKey = "hub.quest_required";
 
+        /// <summary>방을 안 잡고 방 문을 쓸 때 토스트 키</summary>
+        const string RoomLockedKey = "hub.room_locked";
+
+        /// <summary>여관 주인 이름 키 (지점 이름·말한 사람)</summary>
+        const string InnkeeperKey = "hub.innkeeper";
+
+        /// <summary>길드 접수원 이름 키</summary>
+        const string ClerkKey = "hub.clerk";
+
+        /// <summary>경비병 이름 키</summary>
+        const string GuardKey = "hub.guard";
+
+        /// <summary>여관 주인: 처음 말 걸 때 대사 키</summary>
+        const string InnGreetKey = "hub.innkeeper.greet";
+
+        /// <summary>여관 주인: 방을 잡은 뒤 대사 키 (길드를 알려 줌)</summary>
+        const string InnRentedKey = "hub.innkeeper.rented";
+
+        /// <summary>여관 주인: 식량을 판 뒤 대사 키 ({0} 지금 식량 인분)</summary>
+        const string InnFoodKey = "hub.innkeeper.food_bought";
+
+        /// <summary>여관 주인: 금화가 모자랄 때 대사 키</summary>
+        const string InnNoGoldKey = "hub.innkeeper.no_gold";
+
+        /// <summary>길드 접수원: 말 걸 때 대사 키</summary>
+        const string ClerkGreetKey = "hub.clerk.greet";
+
+        /// <summary>경비병: 식량이 모자랄 때 대사 키 ({0} 필요한 인분)</summary>
+        const string GuardNoFoodKey = "hub.guard.no_food";
+
+        /// <summary>경비병: 나갈 수 있을 때 대사 키</summary>
+        const string GuardReadyKey = "hub.guard.ready";
+
+        /// <summary>경비병: 숲에서 돌아왔을 때 자막 키</summary>
+        const string GuardWelcomeKey = "hub.guard.welcome";
+
+        /// <summary>선택지: 방 잡기 키 ({0} 방값)</summary>
+        const string ChoiceRentKey = "hub.choice.rent";
+
+        /// <summary>선택지: 식량 하루치 사기 키 ({0} 값)</summary>
+        const string ChoiceFoodKey = "hub.choice.food";
+
+        /// <summary>선택지: 용병 구하기 키</summary>
+        const string ChoiceRecruitKey = "hub.choice.recruit";
+
+        /// <summary>선택지: 출발 키</summary>
+        const string ChoiceDepartKey = "hub.depart";
+
+        /// <summary>선택지: 아직 (안 나감) 키</summary>
+        const string ChoiceStayKey = "hub.choice.stay";
+
+        /// <summary>선택지: 알았어 키</summary>
+        const string ChoiceOkKey = "hub.choice.ok";
+
+        /// <summary>선택지: 그만 (대화 끝) 키</summary>
+        const string ChoiceByeKey = "hub.choice.bye";
+
         /// <summary>이동 튜토리얼 id</summary>
         const string MoveTutorialId = "move";
 
@@ -40,7 +111,7 @@ public sealed class HubState : FlowStateBase
         /// <summary>달리기 튜토리얼 id</summary>
         const string SprintTutorialId = "sprint";
 
-        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 E를 쓰면 배움)</summary>
+        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 상호작용 키를 쓰면 배움)</summary>
         const string InteractTutorialId = "interact";
 
         /// <summary>목표가 바뀌었을 때 토스트 키 ({0} 목표 이름, Menu 테이블)</summary>
@@ -67,32 +138,47 @@ public sealed class HubState : FlowStateBase
         /// <summary>이번 진입이 첫 도착이라 역마차 연출을 트는지 (OnSceneReady에서 정함)</summary>
         bool _arriving;
 
-        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 아니면 시작 위치. 파티 수 표시, 안내 글 숨김
+        /// <summary>이번 진입이 숲에서 돌아온 것인지 (정문 밖에서 시작. OnSceneReady에서 정함)</summary>
+        bool _returning;
+
+        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 숲에서 돌아왔으면 정문 밖, 아니면 시작 위치. 파티 수·금화 표시, 안내 글 숨김
         protected override void OnSceneReady()
         {
             _view = FindView<HubView>();
             if (_view == null) return;
             _arriving = !Game.Run.HasArrived;
-            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _view.Spawn);
+            _returning = !_arriving && Game.Run.ReturningFromField;
+            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _returning ? _view.ReturnPoint : _view.Spawn);
             _view.Player.gameObject.SetActive(!_arriving);
             _view.Hud.SetActive(!_arriving);
             _view.InteractPrompt.gameObject.SetActive(false);
             RefreshParty();
+            RefreshMoney();
         }
         #endregion
 
-        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작
+        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작. 숲에서 돌아왔으면 의뢰를 비우고 경비병 자막 → 끝나면 목표 알림
         public override void Reveal()
         {
             if (_view == null) return;
             Cursor.lockState = CursorLockMode.Locked;
-            if (!_arriving)
+            if (_arriving)
             {
-                StartControl();
+                _view.Opening.Finished += OnOpeningFinished;
+                _view.Opening.Play();
                 return;
             }
-            _view.Opening.Finished += OnOpeningFinished;
-            _view.Opening.Play();
+            StartControl();
+            if (!_returning)
+            {
+                UpdateObjective(false);
+                return;
+            }
+            // 숲에서 돌아옴: 받은 의뢰는 다녀온 걸로 비움 (정산은 나중)
+            Game.Run.ReturningFromField = false;
+            Game.Run.Quest = null;
+            UpdateObjective(false);
+            UIManager.Instance.ShowSubtitle(new LocalizedString(Table, GuardKey), new LocalizedString(Table, GuardWelcomeKey), NotifyObjective);
         }
         #endregion
 
@@ -105,7 +191,7 @@ void OnOpeningFinished()
             _view.Player.gameObject.SetActive(true);
             _view.Hud.SetActive(true);
             StartControl();
-            Game.Run.CurrentObjective = _view.FirstObjective;
+            UpdateObjective(false);
             UIManager.Instance.ShowSubtitle(_view.ArrivalSpeaker, _view.ArrivalLine, NotifyObjective);
         }
         #endregion
@@ -120,13 +206,12 @@ void NotifyObjective()
         }
         #endregion
 
-        #region StartControl - 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 E는 구독 전이라 안 들어감), 안 배운 조작 안내 알림
+        #region StartControl - 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감), 안 배운 조작 안내 알림
         void StartControl()
         {
             _view.Detector.TargetChanged += OnTargetChanged;
             _view.Detector.Interacted += OnInteracted;
-            _view.Player.enabled = true;
-            _view.Detector.enabled = true;
+            SetControl(true);
             // 안 배운 조작 안내만 차례로 12시에 (배운 건 TutorialManager가 거름)
             TutorialManager.Instance.Notify(MoveTutorialId);
             TutorialManager.Instance.Notify(LookTutorialId);
@@ -134,6 +219,16 @@ void StartControl()
         }
         #endregion
 
+        #region SetControl - 조작·지점 찾기 켜고 끄기 (문을 드나드는 페이드 동안 끔. 씬이 내려갔으면 무시)
+        /// <param name="on">켤지</param>
+        void SetControl(bool on)
+        {
+            if (_view == null) return;
+            _view.Player.enabled = on;
+            _view.Detector.enabled = on;
+        }
+        #endregion
+
         #region Exit - 연출 중이면 알림 없이 멈춤, 알림 구독 해제, 조작·지점 찾기 끔, 커서 풀기, 조작 안내·자막 정리
         public override void Exit()
         {
@@ -143,8 +238,7 @@ public override void Exit()
                 _view.Opening.Cancel();
                 _view.Detector.TargetChanged -= OnTargetChanged;
                 _view.Detector.Interacted -= OnInteracted;
-                _view.Player.enabled = false;
-                _view.Detector.enabled = false;
+                SetControl(false);
             }
             TutorialManager.Instance.Clear();
             UIManager.Instance.HideSubtitle();
@@ -152,7 +246,7 @@ public override void Exit()
         }
         #endregion
 
-        #region OnTargetChanged - 바라보는 지점이 있으면 "[E] 이름", 없으면 숨김
+        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김
         /// <param name="target">바라보는 지점 (없으면 null)</param>
         void OnTargetChanged(InteractPointView target)
         {
@@ -169,52 +263,264 @@ void OnTargetChanged(InteractPointView target)
         }
         #endregion
 
-        #region OnInteracted - 처음 쓰면 상호작용 배움. 지점 id로 나눔: 영입·의뢰 받기·출발. 모르는 id는 경고
-        /// <param name="target">E를 누를 때 바라본 지점</param>
+        #region OnInteracted - 처음 쓰면 상호작용 배움. 문이면 드나들고, 아니면 지점 id로 나눔: 여관 주인·접수원·게시판·경비병. 모르는 id는 경고
+        /// <param name="target">상호작용 키를 누를 때 바라본 지점</param>
         void OnInteracted(InteractPointView target)
         {
             TutorialManager.Instance.MarkLearned(InteractTutorialId);
-            if (string.Equals(target.Id, RecruitId, StringComparison.Ordinal)) Recruit();
-            else if (string.Equals(target.Id, BoardId, StringComparison.Ordinal)) TakeQuest();
-            else if (string.Equals(target.Id, DepartId, StringComparison.Ordinal)) Depart();
-            else Debug.LogWarning($"[Hub] 모르는 지점 id: {target.Id} ({target.name})");
+            if (target.TryGetComponent(out DoorView door))
+            {
+                UseDoor(target, door);
+                return;
+            }
+            switch (target.Id)
+            {
+                case InnkeeperId: TalkInnkeeper(InnGreetKey); break;
+                case ClerkId: TalkClerk(); break;
+                case BoardId: OpenBoard(); break;
+                case GuardId: TalkGuard(); break;
+                default: Debug.LogWarning($"[Hub] 모르는 지점 id: {target.Id} ({target.name})"); break;
+            }
         }
         #endregion
 
-        #region Recruit - 임시 영입: 파티가 비었을 때만 고정 용병을 넣음 (4번에서 후보 선택으로 바뀜)
-        void Recruit()
+        #region UseDoor - 방 문은 방을 잡아야 열림. 화면이 덮인 사이 도착 지점으로 옮기고, 다 걷히면 조작을 돌려줌
+        /// <param name="target">쓴 문 지점</param>
+        /// <param name="door">문 도착 지점</param>
+        void UseDoor(InteractPointView target, DoorView door)
         {
-            if (Game.Run.Party.Count >= RequiredMercenaries) return;
-            Game.Run.Party.AddRange(RunEntry.StubMercenaries());
-            RefreshParty();
+            if (door.Destination == null)
+            {
+                Debug.LogError($"[Hub] 문 도착 지점 없음: {target.name}", target);
+                return;
+            }
+            if (string.Equals(target.Id, RoomDoorId, StringComparison.Ordinal) && !Game.Run.HasRoom)
+            {
+                UIManager.Instance.ShowToast(Table, RoomLockedKey);
+                return;
+            }
+            if (!UIManager.Instance.FadeThrough(() => _view.Player.PlaceAt(door.Destination), () => SetControl(true))) return;
+            SetControl(false);
         }
         #endregion
 
-        #region TakeQuest - 임시 의뢰 받기 + 토스트 (4번에서 게시판 창으로 바뀜)
-        void TakeQuest()
+        #region TalkInnkeeper - 여관 주인 대화: 방 잡기(안 잡았을 때)·식량 하루치 사기·그만
+        /// <param name="lineKey">이번 대사 키</param>
+        /// <param name="lineArgs">대사 자리 값</param>
+        void TalkInnkeeper(string lineKey, params object[] lineArgs)
         {
-            Game.Run.QuestTitle = RunEntry.StubQuest();
-            UIManager.Instance.ShowToast(Table, QuestTakenKey, Game.Run.QuestTitle.GetLocalizedString());
+            // 이번 선택지
+            var choices = new List<DialogueChoiceEntry>();
+            if (!Game.Run.HasRoom) choices.Add(new DialogueChoiceEntry(Table, ChoiceRentKey, RentRoom, RoomPrice));
+            choices.Add(new DialogueChoiceEntry(Table, ChoiceFoodKey, BuyFood, DayFoodPrice()));
+            choices.Add(new DialogueChoiceEntry(Table, ChoiceByeKey, UIManager.Instance.HideDialogue));
+            UIManager.Instance.ShowDialogue(Table, InnkeeperKey, lineKey, choices, lineArgs);
         }
         #endregion
 
-        #region Depart - 파티가 모자라거나 의뢰가 없으면 토스트, 아니면 숲으로
-        void Depart()
+        #region RentRoom - 금화가 되면 방값을 내고 방을 잡음 (한 번). 주인이 길드를 알려 주고 목표 갱신
+        void RentRoom()
+        {
+            if (Game.Run.Gold < RoomPrice)
+            {
+                TalkInnkeeper(InnNoGoldKey);
+                return;
+            }
+            Game.Run.Gold -= RoomPrice;
+            Game.Run.HasRoom = true;
+            RefreshMoney();
+            TalkInnkeeper(InnRentedKey);
+            UpdateObjective(true);
+        }
+        #endregion
+
+        #region BuyFood - 금화가 되면 지금 인원의 하루치 식량을 삼
+        void BuyFood()
         {
-            if (Game.Run.Party.Count < RequiredMercenaries)
+            // 하루치 값 (금화)
+            int price = DayFoodPrice();
+            if (Game.Run.Gold < price)
             {
-                UIManager.Instance.ShowToast(Table, PartyRequiredKey, RequiredMercenaries);
+                TalkInnkeeper(InnNoGoldKey);
                 return;
             }
-            if (Game.Run.QuestTitle == null)
+            Game.Run.Gold -= price;
+            Game.Run.Rations += PartySize();
+            RefreshMoney();
+            TalkInnkeeper(InnFoodKey, Game.Run.Rations);
+        }
+        #endregion
+
+        #region TalkClerk - 길드 접수원 대화: 용병 구하기·그만
+        void TalkClerk()
+        {
+            // 이번 선택지
+            var choices = new[]
+            {
+                new DialogueChoiceEntry(Table, ChoiceRecruitKey, OpenRecruit),
+                new DialogueChoiceEntry(Table, ChoiceByeKey, UIManager.Instance.HideDialogue),
+            };
+            UIManager.Instance.ShowDialogue(Table, ClerkKey, ClerkGreetKey, choices);
+        }
+        #endregion
+
+        #region OpenRecruit - 처음이면 후보를 뽑고 영입 창을 엶 (대화는 창을 연 뒤 닫아서 게임이 안 풀림)
+        void OpenRecruit()
+        {
+            // 이번 판 데이터
+            RunEntry run = Game.Run;
+            if (!run.CandidatesDrawn)
+            {
+                run.RecruitCandidates.AddRange(RecruitPool.Draw(DataManager.Instance.Jobs));
+                run.CandidatesDrawn = true;
+            }
+            UIManager.Instance.ShowRecruit(run.RecruitCandidates, run.Party, RequiredMercenaries, Hire, Dismiss);
+            UIManager.Instance.HideDialogue();
+        }
+        #endregion
+
+        #region Hire - 후보를 파티로 옮김 (자리가 있을 때)
+        /// <param name="entry">영입할 후보</param>
+        void Hire(MercenaryEntry entry)
+        {
+            // 이번 판 데이터
+            RunEntry run = Game.Run;
+            if (run.Party.Count >= RequiredMercenaries || !run.RecruitCandidates.Remove(entry)) return;
+            run.Party.Add(entry);
+            RefreshParty();
+            UpdateObjective(true);
+        }
+        #endregion
+
+        #region Dismiss - 파티원을 후보로 되돌림
+        /// <param name="entry">내보낼 파티원</param>
+        void Dismiss(MercenaryEntry entry)
+        {
+            if (!Game.Run.Party.Remove(entry)) return;
+            Game.Run.RecruitCandidates.Add(entry);
+            RefreshParty();
+            UpdateObjective(true);
+        }
+        #endregion
+
+        #region OpenBoard - 게시판 창: 첫 의뢰 (지금은 하나뿐)
+        void OpenBoard()
+        {
+            // 게시판 의뢰 목록
+            IReadOnlyList<QuestSO> quests = DataManager.Instance.Quests;
+            if (quests.Count == 0 || quests[0] == null)
             {
-                UIManager.Instance.ShowToast(Table, QuestRequiredKey);
+                Debug.LogWarning("[Hub] 게시판에 붙일 의뢰 없음");
                 return;
             }
+            // 붙어 있는 의뢰
+            QuestSO quest = quests[0];
+            UIManager.Instance.ShowQuestBoard(quest, Game.Run.Quest == quest, () => TakeQuest(quest));
+        }
+        #endregion
+
+        #region TakeQuest - 의뢰를 받음. 목표가 바뀌면 목표 알림, 아니면 "의뢰를 받았어" 토스트
+        /// <param name="quest">받을 의뢰</param>
+        void TakeQuest(QuestSO quest)
+        {
+            Game.Run.Quest = quest;
+            if (!UpdateObjective(true)) UIManager.Instance.ShowToast(Table, QuestTakenKey, quest.Title.GetLocalizedString());
+        }
+        #endregion
+
+        #region TalkGuard - 정문 경비병: 첫 출발 전엔 파티·의뢰·식량이 모자라면 말로 막음. 됐거나 첫 출발 뒤면 출발·아직
+        void TalkGuard()
+        {
+            // 이번 판 데이터
+            RunEntry run = Game.Run;
+            if (!run.HasDeparted)
+            {
+                if (run.Party.Count < RequiredMercenaries)
+                {
+                    GuardLine(PartyRequiredKey, RequiredMercenaries);
+                    return;
+                }
+                if (run.Quest == null)
+                {
+                    GuardLine(QuestRequiredKey);
+                    return;
+                }
+                if (run.Rations < NeededRations())
+                {
+                    GuardLine(GuardNoFoodKey, NeededRations());
+                    return;
+                }
+            }
+            // 나갈 때 선택지
+            var choices = new[]
+            {
+                new DialogueChoiceEntry(Table, ChoiceDepartKey, Depart),
+                new DialogueChoiceEntry(Table, ChoiceStayKey, UIManager.Instance.HideDialogue),
+            };
+            UIManager.Instance.ShowDialogue(Table, GuardKey, GuardReadyKey, choices);
+        }
+        #endregion
+
+        #region GuardLine - 경비병 한마디 + 알았어
+        /// <param name="lineKey">대사 키</param>
+        /// <param name="lineArgs">대사 자리 값</param>
+        void GuardLine(string lineKey, params object[] lineArgs)
+        {
+            // 하나뿐인 선택지
+            var choices = new[] { new DialogueChoiceEntry(Table, ChoiceOkKey, UIManager.Instance.HideDialogue) };
+            UIManager.Instance.ShowDialogue(Table, GuardKey, lineKey, choices, lineArgs);
+        }
+        #endregion
+
+        #region Depart - 원정 식량을 빼고(모자라면 있는 만큼) 출발 표시, 대화를 닫고 숲으로
+        void Depart()
+        {
+            // 이번 판 데이터
+            RunEntry run = Game.Run;
+            run.Rations -= Mathf.Min(run.Rations, NeededRations());
+            run.HasDeparted = true;
+            UpdateObjective(false);
+            UIManager.Instance.HideDialogue();
             Game.ChangeState(new FieldState());
         }
         #endregion
 
+        #region PartySize - 원정 인원 (플레이어 + 파티 용병)
+        int PartySize() => 1 + Game.Run.Party.Count;
+        #endregion
+
+        #region DayFoodPrice - 지금 인원의 하루치 식량 값 (금화)
+        int DayFoodPrice() => RationPrice * PartySize();
+        #endregion
+
+        #region NeededRations - 받은 의뢰에 필요한 식량 (인원 × 기간 인분. 의뢰가 없으면 0)
+        int NeededRations() => Game.Run.Quest != null ? Game.Run.Quest.Days * PartySize() : 0;
+        #endregion
+
+        #region NextObjective - 판 데이터로 지금 목표를 정함: 방 → 길드(파티·의뢰) → 정문 → (첫 출발 뒤) 없음
+        ObjectiveSO NextObjective()
+        {
+            // 이번 판 데이터
+            RunEntry run = Game.Run;
+            if (!run.HasRoom) return _view.FirstObjective;
+            if (run.Party.Count < RequiredMercenaries || run.Quest == null) return _view.GuildObjective;
+            return run.HasDeparted ? null : _view.DepartObjective;
+        }
+        #endregion
+
+        #region UpdateObjective - 목표가 바뀌었으면 판 데이터에 넣고 (notify면) 알림. 바뀌었으면 true
+        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
+        bool UpdateObjective(bool notify)
+        {
+            // 판 데이터로 정한 목표
+            ObjectiveSO next = NextObjective();
+            if (next == Game.Run.CurrentObjective) return false;
+            Game.Run.CurrentObjective = next;
+            if (notify) NotifyObjective();
+            return true;
+        }
+        #endregion
+
         #region RefreshParty - 파티 수 표시 갱신 ({0}/{1})
         void RefreshParty()
         {
@@ -222,5 +528,13 @@ void RefreshParty()
             _view.PartyText.RefreshString();
         }
         #endregion
+
+        #region RefreshMoney - 금화·식량 표시 갱신 ({0} 금화, {1} 식량 인분)
+        void RefreshMoney()
+        {
+            _view.MoneyText.StringReference.Arguments = new object[] { Game.Run.Gold, Game.Run.Rations };
+            _view.MoneyText.RefreshString();
+        }
+        #endregion
     }
 }
```

**호출/구독 관계:**
- `InteractDetector.Interacted` → `OnInteracted` → `UseDoor`(→ `UIManager.FadeThrough`) / `TalkInnkeeper` / `TalkClerk` / `OpenBoard` / `TalkGuard`
- 대화 선택지 → `RentRoom`·`BuyFood`·`OpenRecruit`·`Depart`·`UIManager.HideDialogue`. 같은 사람 다음 대사는 같은 `Talk…`를 다른 대사 키로 다시 부름
- `FieldState.OnInteract` → `ReturningFromField = true` → `HubState.OnSceneReady`(`ReturnPoint`) → `Reveal`(의뢰 비움, 자막 → `NotifyObjective`)

---

### `Assets/Scripts/Flow/FieldState.cs`

**변경 내용:** 상호작용 키로 돌아갈 때 숲 귀환 표시, 받은 의뢰 이름은 `QuestSO`에서 읽음. 주석 E → 상호작용 키

```diff
@@ -3,7 +3,7 @@
 
 namespace Mage
 {
-    /// <summary>숲. 1단계는 판 데이터 표시만 하고 E로 전초기지에 돌아감</summary>
+    /// <summary>숲. 1단계는 판 데이터 표시만 하고 상호작용 키(F)로 전초기지에 돌아감 (정문 밖)</summary>
     public sealed class FieldState : FlowStateBase
     {
         /// <summary>이 상태의 씬 이름 (지역이 늘면 의뢰 데이터에서 받음)</summary>
@@ -18,7 +18,7 @@ public sealed class FieldState : FlowStateBase
         /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬. 문구를 보는 동안 게임은 멈춰 있음)</summary>
         public override bool WaitsForContinue => true;
 
-        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬. 메뉴가 떠 있는 동안 E 귀환도 막힘)</summary>
+        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬. 메뉴가 떠 있는 동안 귀환도 막힘)</summary>
         public override bool UsesPauseMenu => true;
 
         /// <summary>귀환 입력 액션. Exit에서 구독 해제</summary>
@@ -33,7 +33,7 @@ protected override void OnSceneReady()
         }
         #endregion
 
-        #region Reveal - 커서 잠금, E 입력 구독 (로딩창을 넘길 때 누른 E는 구독 전이라 귀환으로 안 감)
+        #region Reveal - 커서 잠금, 상호작용 입력 구독 (로딩창을 넘길 때 누른 키는 구독 전이라 귀환으로 안 감)
         public override void Reveal()
         {
             Cursor.lockState = CursorLockMode.Locked;
@@ -42,7 +42,7 @@ public override void Reveal()
         }
         #endregion
 
-        #region Exit - E 구독 해제, 커서 풀기
+        #region Exit - 상호작용 구독 해제, 커서 풀기
         public override void Exit()
         {
             if (_interact != null) _interact.performed -= OnInteract;
@@ -55,15 +55,19 @@ public override void Exit()
         void ShowRun(FieldDebugView view)
         {
             // 의뢰 이름. 디버그 표시라 들어올 때 한 번만 읽음 (숲에서 언어를 바꾸면 다음 진입 때 바뀜)
-            string quest = Game.Run.QuestTitle != null ? Game.Run.QuestTitle.GetLocalizedString() : string.Empty;
+            string quest = Game.Run.Quest != null ? Game.Run.Quest.Title.GetLocalizedString() : string.Empty;
             view.InfoText.StringReference.Arguments = new object[] { Game.Run.PlayerName, Game.Run.Party.Count, quest };
             view.InfoText.RefreshString();
         }
         #endregion
 
-        #region OnInteract - E를 누르면 전초기지로
+        #region OnInteract - 상호작용 키를 누르면 전초기지로 (정문 밖에서 시작하게 표시)
         /// <param name="context">입력 정보 (안 씀)</param>
-        void OnInteract(InputAction.CallbackContext context) => Game.ChangeState(new HubState());
+        void OnInteract(InputAction.CallbackContext context)
+        {
+            Game.Run.ReturningFromField = true;
+            Game.ChangeState(new HubState());
+        }
         #endregion
     }
 }
```

---

## 입력 변경

### `Assets/InputSystem_Actions.inputactions`

`Player/Interact` 바인딩 `<Keyboard>/e` → `<Keyboard>/f` (JSON 한 줄만 고침)

---

## 에셋 변경

### `Assets/Data/Jobs/` (새로, JobSO)

| 파일 | `_id` | `_name`·`_description` (Job 테이블) | `_combat` |
|---|---|---|---|
| `Ranger.asset` | ranger | `job.ranger.name`·`.desc` | 3 |
| `Porter.asset` | porter | `job.porter.name`·`.desc` | 1 |
| `SpearGuard.asset` | spear_guard | `job.spear_guard.name`·`.desc` | 3 |
| `Duelist.asset` | duelist | `job.duelist.name`·`.desc` | 3 |

### `Assets/Data/Quests/Forest01.asset` (새로, QuestSO)

`_id` = `forest_01`, `_title` = Quest/`quest.forest_01.title`, `_description` = Quest/`quest.forest_01.desc`, `_days` = 3

### `Assets/Data/Objectives/` (새로, ObjectiveSO)

| 파일 | `_id` | `_title`·`_description` (Objective 테이블) |
|---|---|---|
| `Guild.asset` | guild | `objective.guild.title`·`.desc` |
| `Depart.asset` | depart | `objective.depart.title`·`.desc` |

### `Assets/Art/Placeholder/Materials/Npc.mat`

`_Color` 부동소수 반올림만 (Unity 재저장, 값 같음)

---

## Prefab 변경

공통: 미리보기 씬에서 만들어 `PrefabUtility.SaveAsPrefabAsset`. 글자는 LocalizedText 프리팹, 런타임에 키를 넣는 글자는 비워 둠

### `Assets/Prefabs/UI/DialogueChoice.prefab` (새로)

1. `Button.prefab` 변형(Variant). LayoutElement Preferred (640, 56)
2. Add Component → `DialogueChoiceView` (`_button` → 루트 Button, `_label` → `Label`의 LocalizeStringEvent)

### `Assets/Prefabs/UI/RecruitRow.prefab` (새로)

1. 루트 `RecruitRow`: 크기 (620, 60), Image (1, 1, 1, 0.06) Raycast Target 끔, HorizontalLayoutGroup (Padding 12·6·2·2, Spacing 10, 가운데 왼쪽, Control Child Size 켬, Force Expand 끔), LayoutElement Preferred Height 60
2. 자식 순서·LayoutElement Preferred (폭, 높이): `Name` (120, 56), `Job` (160, 56), `Level` (70, 56), `Combat` (90, 56) — 전부 글자 26, 줄바꿈 끔, 세로 가운데. `Job`만 Raycast Target 켬 + `TooltipHandler` (`_title`·`_body` 비움, 코드가 넣음)
3. `Button`: `Button.prefab` 인스턴스, LayoutElement Preferred (130, 52)
4. Add Component → `RecruitRowView` (`_nameText`·`_jobText`·`_levelText`·`_combatText` → 각 자식 LocalizeStringEvent, `_jobTooltip` → `Job`의 TooltipHandler, `_button` → `Button`, `_buttonLabel` → `Button/Label`)

### `Assets/Prefabs/World/NpcFigure.prefab` (새로)

서 있는 NPC 도형 (걷기 없음, 상호작용 콜라이더 있음. 3-3 `Npc.prefab`은 걷는 용병용이라 콜라이더 없음)

1. 루트 `NpcFigure`: CapsuleCollider (반지름 0.35, 높이 1.8, 중심 (0, 0.9, 0))
2. `Visual/Body`: Capsule, 위치 (0, 0.9, 0), 스케일 (0.7, 0.9, 0.7), `Npc.mat`, 콜라이더 없음
3. `Visual/HeadBall`: Sphere, 위치 (0, 1.6, 0), 스케일 0.3, `Npc.mat`, 콜라이더 없음

---

## 씬 변경

### `Assets/Scenes/Boot.unity`

`UICanvas` 자식 순서: `Fade`, `Subtitle`, `TutorialPrompt`, `Dialogue`, `GameMenu`, `Recruit`, `QuestBoard`, `Pause`, `Settings`, `Confirm`, `Toast`, `Tooltip` (뒤가 위. 페이드는 맨 아래라 대화·창이 덮이지 않음, 자막 위에 대화)

1. `Fade` 새로: 꽉 채움, Image 검정 (Raycast Target 켬), CanvasGroup (Alpha 0, Interactable·Blocks Raycasts 끔), `FadeSequence` (`_group` → 이 CanvasGroup, `_halfSeconds` 0.25)
2. `Dialogue` 새로: 꽉 채움. Add Component → `DialogueView`, `DialogueController` (`_view` → 같은 오브젝트)
   - `Body` (꽉 채움, 꺼 둠) 아래:
     - `Panel`: 앵커·피벗 (0.5, 0), 위치 (0, 200), 크기 (1400, 150), Image (0.06, 0.06, 0.08, 0.92) Raycast Target 끔, VerticalLayoutGroup (Padding 16·16·14·14, Spacing 6, 가운데, Control Child Size 켬)
       - `Speaker`: 글자 26, 색 (1, 0.85, 0.5), 가운데, 줄바꿈 끔
       - `Line`: 글자 32, 가운데
     - `Choices`: 앵커·피벗 (0.5, 0), 위치 (0, 370), 크기 (640, 0), VerticalLayoutGroup (Spacing 8, 아래 가운데, Control Child Size 켬), ContentSizeFitter (Vertical Preferred)
   - DialogueView 칸: `_root` → `Body`, `_speaker`·`_line` → Panel 자식, `_choiceList` → `Choices`, `_choicePrefab` → `DialogueChoice.prefab`
3. `Recruit` 새로: `WindowFrame.prefab` 인스턴스 (꺼 둠). 루트에 Add Component → `RecruitView`, `RecruitController` (`_view` → 같은 오브젝트)
   - `Panel` 아래 순서: `Title` (Hub/`hub.recruit`, 글자 40, 가운데, 줄바꿈 끔), `Columns`, `CloseButton` (`Button.prefab`, UI/`common.close`, Preferred Width 240)
   - `Columns`: HorizontalLayoutGroup (Spacing 40, 위 가운데, Control Child Size 켬). 자식 `CandidateColumn`·`PartyColumn`: VerticalLayoutGroup (Spacing 10)
     - 각 열 `Header` (Hub/`hub.recruit.candidates`·`hub.recruit.party`, 글자 30), `List` (VerticalLayoutGroup Spacing 6, Control Child Size 켬, LayoutElement Min Height 270·Preferred Width 640)
   - RecruitView 칸: `_candidateList`·`_partyList` → 각 `List`, `_rowPrefab` → `RecruitRow.prefab`, `_closeButton` → `CloseButton`
4. `QuestBoard` 새로: `WindowFrame.prefab` 인스턴스 (꺼 둠). 루트에 Add Component → `QuestBoardView`, `QuestBoardController`
   - `Panel` 아래 순서: `Title` (Hub/`hub.board.title`, 글자 40, 가운데), `QuestTitle` (글자 34), `QuestDescription` (글자 26, 줄바꿈), `QuestDays` (글자 26) — 셋 다 LayoutElement Preferred Width 900, `Buttons` (HorizontalLayoutGroup Spacing 16, 가운데): `TakeButton` (Hub/`hub.board.take`), `CloseButton` (UI/`common.close`), 둘 다 Preferred Width 240
   - QuestBoardView 칸: 글자 3개, `_takeButton`·`_takeLabel` → `TakeButton`·그 `Label`, `_closeButton`
5. `GameMenu/Panel/Pages/JournalPage/QuestText` 새로: Menu/`menu.journal.quest`, 앵커·피벗 (0.5, 1), 위치 (0, -470), 크기 (1200, 50), 글자 28, 줄바꿈 끔. `GameMenuView._questText` 연결
6. `Managers`:
   - `UIManager`: `_fade` → Fade, `_dialogue` → Dialogue의 DialogueController, `_recruit` → Recruit의 RecruitController, `_questBoard` → QuestBoard의 QuestBoardController
   - `DataManager`: `_jobs` = Ranger, Porter, SpearGuard, Duelist / `_quests` = Forest01
7. 저장 → 다시 열어 UIManager 새 칸 4개, DataManager 목록 2개, View 칸, 자식 순서 확인

### `Assets/Scenes/Outpost.unity`

실내는 지하 `Interiors` (0, -40, 0) 아래 셋. 벽 `Road.mat`, 바닥·가구·문 `Wood.mat`, 천장 `Roof.mat`, 도형은 Cube (BoxCollider). 조명은 Point (색 (1, 0.9, 0.75)). 문은 Cube + `InteractPointView` + `DoorView`

1. 광장:
   - `Huts/RecruitHall` 이름을 `Guild`로. Add Component → `InteractPointView` (`guild_door`, Hub/`hub.guild`), `DoorView` (→ `Interiors/GuildInterior/Entry`). 옛 `recruit` 지점은 지움
   - `Huts/Inn`: Add Component → `InteractPointView` (`inn_door`, Hub/`hub.inn`), `DoorView` (→ `Interiors/InnInterior/Entry`)
   - 광장 `Board`를 길드 안으로 옮김 (아래), `Signpost`(depart) 지움
   - `Guard` 새로: `NpcFigure.prefab`, 위치 (2.2, 0, -19.4), yaw 180 (정문 안쪽에서 바깥을 봄). Add Component → `InteractPointView` (`guard`, Hub/`hub.guard`)
   - `ReturnPoint` 새로: (2.2, 0, -23.5), yaw 0 (정문 밖, 정문을 봄)
   - `DoorPoints` 새로 (원점): `InnOutside` (7.384, 0, 7.384), `GuildOutside` (-7.384, 0, 5.384) (각 문 앞, yaw 0)
2. `Interiors/InnInterior` (0, 0, 0): 여관 홀 10 × 8, 천장 3.3
   - `Floor` (0, -0.1, 0) (10, 0.2, 8) / `Ceiling` (0, 3.3, 0) (10, 0.2, 8) / 벽 `WallNorth`·`WallSouth` (0, 1.6, ±4) (10, 3.2, 0.2), `WallEast`·`WallWest` (±5, 1.6, 0) (0.2, 3.2, 8)
   - `Counter` (0, 0.55, 1.8) (3, 1.1, 0.8), `Innkeeper` (`NpcFigure`, (0, 0, 2.8), yaw 180, `InteractPointView` `innkeeper`·Hub/`hub.innkeeper`)
   - `Light` (0, 2.8, 0) 세기 3 범위 12
   - `Entry` (0, 0, -2.8) / `ExitDoor` (0, 1.1, -3.85) (1.2, 2.2, 0.1), `inn_exit`·Hub/`hub.exit` → `DoorPoints/InnOutside`
   - `RoomDoor` (4.85, 1.1, -1.5) (0.1, 2.2, 1.2), `room_door`·Hub/`hub.room_door` → `Interiors/RoomInterior/Entry` / `RoomReturn` (3.6, 0, -1.5)
3. `Interiors/RoomInterior` (20, 0, 0): 방 4.5 × 4.5, 천장 2.9
   - `Floor` (4.5, 0.2, 4.5) / `Ceiling` (0, 2.9, 0) / 벽 (±2.25, 1.4) 높이 2.8
   - `Bed` (1.4, 0.3, 1) (1.1, 0.6, 2), `Desk` (-1.4, 0.4, 1.6) (1.2, 0.8, 0.6) — 둘 다 상호작용 없음 (책상은 5단계)
   - `Light` (0, 2.4, 0) 세기 2 범위 6
   - `Entry` (0, 0, -1.4) / `ExitDoor` (0, 1.1, -2.15) (1, 2.2, 0.1), `room_exit`·Hub/`hub.exit` → `Interiors/InnInterior/RoomReturn`
4. `Interiors/GuildInterior` (-30, 0, 0): 길드 12 × 10, 천장 3.6
   - `Floor` (12, 0.2, 10) / `Ceiling` (0, 3.6, 0) / 벽 `WallNorth`·`WallSouth` (0, 1.75, ±5) (12, 3.5, 0.2), `WallEast`·`WallWest` (±6, 1.75, 0) (0.2, 3.5, 10)
   - `ClerkDesk` (0, 0.55, 2.8) (3, 1.1, 0.8), `Clerk` (`NpcFigure`, (0, 0, 3.8), yaw 180, `clerk`·Hub/`hub.clerk`)
   - `Board` (`Board.prefab`, 광장에서 옮김, (-5.6, 0, 0), yaw 90, `board`·Hub/`hub.board`)
   - `Table` (3.5, 0.4, -1.5) (2.2, 0.8, 1), `Bench` (3.5, 0.225, -2.5) (2.2, 0.45, 0.5), 동승했던 용병 `MercenaryA`·`MercenaryB` (`NpcFigure`, (2.9 / 4.1, 0.45, -2.5), 스케일 y 0.45 = 앉은 모습, 상호작용 없음)
   - `LightEast`·`LightWest` (±3, 3, 0) 세기 2.5 범위 10
   - `Entry` (0, 0, -3.8) / `ExitDoor` (0, 1.1, -4.85) (1.2, 2.2, 0.1), `guild_exit`·Hub/`hub.exit` → `DoorPoints/GuildOutside`
5. `Canvas/Hud/MoneyText` 새로: Hub/`hub.money`, 앵커·피벗 (0, 1), 위치 (40, -100), 크기 (600, 50), 글자 44 (파티 수 글자 아래)
6. HubView: `_guildObjective` = `Guild.asset`, `_departObjective` = `Depart.asset`, `_moneyText` → `Canvas/Hud/MoneyText`, `_returnPoint` → `ReturnPoint`
7. 저장 → 다시 열어 문 5개의 `DoorView` 도착 지점, 지점 id·이름 키, HubView 새 칸 4개 확인

---

## 다국어 변경

1. 시트 `Hub` 탭: 5줄 고침 + 33줄 추가

**고침**

| Key | Comment | ko | en |
|---|---|---|---|
| `hub.recruit` | 영입 창 제목 | 용병 영입 | Hire Mercenaries |
| `hub.depart` | 경비병 대화 선택지: 숲으로 나감 | 출발 | Depart |
| `hub.party_required` | 경비병: 첫 출발 때 용병이 모자랄 때. {0} 필요한 인원 | 혼자 숲에 들어간다면 위험할 텐데. 길드에서 용병 {0}명부터 구해 와 | Going into the forest alone would be dangerous. Hire {0} mercenaries at the guild first. |
| `hub.board` | 길드 임무 게시판 이름 | 게시판 | Quest Board |
| `hub.quest_required` | 경비병: 첫 출발 때 의뢰가 없을 때 | 일거리도 없이 나가려고? 길드 게시판에서 의뢰부터 받아 | Heading out without a job? Take a quest from the guild board first. |

**추가**

| Key | Comment | ko | en |
|---|---|---|---|
| `hub.inn` | 여관 문 이름 | 여관 | Inn |
| `hub.guild` | 길드 문 이름 | 길드 | Guild |
| `hub.room_door` | 여관 방 문 이름 | 내 방 | My Room |
| `hub.exit` | 실내에서 나가는 문 이름 | 나가기 | Exit |
| `hub.innkeeper` | 여관 주인 이름 (지점·말한 사람, 임시) | 여관 주인 | Innkeeper |
| `hub.clerk` | 길드 접수원 이름 (지점·말한 사람, 임시) | 길드 접수원 | Guild Clerk |
| `hub.guard` | 정문 경비병 이름 (지점·말한 사람, 임시) | 경비병 | Gate Guard |
| `hub.money` | 화면 금화·식량. {0} 금화, {1} 식량 인분 | 금화 {0} · 식량 {1}인분 | Gold {0} · Rations {1} |
| `hub.room_locked` | 방을 안 잡고 방 문을 쓸 때 토스트 | 방을 먼저 잡아야 해 | Get a room first |
| `hub.innkeeper.greet` | 여관 주인 첫 대사 (임시) | 어서 와. 방이 필요하면 말해. 원정 식량도 팔아 | Welcome. Let me know if you need a room. I sell rations for expeditions too. |
| `hub.innkeeper.rented` | 방을 잡은 뒤 여관 주인 대사. 길드를 알려 줌 (임시) | 방은 안쪽 문이야. 용병이랑 일거리는 길드에서 구해. 광장 왼쪽 안쪽이야 | Your room is through the inner door. Mercenaries and work are at the guild, at the far left of the square. |
| `hub.innkeeper.food_bought` | 식량을 판 뒤 여관 주인 대사. {0} 지금 식량 인분 | 식량 챙겨 줬어. 지금 {0}인분이야 | Here are your rations. You now have {0}. |
| `hub.innkeeper.no_gold` | 금화가 모자랄 때 여관 주인 대사 | 금화가 모자라는데 | You don't have enough gold. |
| `hub.clerk.greet` | 길드 접수원 대사 (임시) | 길드에 온 걸 환영해. 용병이 필요하면 말해. 의뢰는 저 게시판에 붙어 있어 | Welcome to the guild. Tell me if you need mercenaries. Quests are posted on that board. |
| `hub.guard.no_food` | 경비병: 첫 출발 때 식량이 모자랄 때. {0} 필요한 인분 | 먹을 건 챙겼어? 여관 주인한테 식량 {0}인분은 사 가 | Did you pack food? Buy at least {0} rations from the innkeeper. |
| `hub.guard.ready` | 경비병: 나갈 수 있을 때 | 나갈 거야? 조심해 | Heading out? Be careful. |
| `hub.guard.welcome` | 숲에서 돌아왔을 때 경비병 자막 (임시) | 무사히 돌아왔군 | Glad you made it back. |
| `hub.choice.rent` | 여관 선택지: 방 잡기. {0} 방값 | 방 잡기 (금화 {0}) | Get a room ({0} gold) |
| `hub.choice.food` | 여관 선택지: 지금 인원 하루치 식량 사기. {0} 값 | 식량 하루치 사기 (금화 {0}) | Buy a day of rations ({0} gold) |
| `hub.choice.recruit` | 길드 선택지: 영입 창 열기 | 용병 구하기 | Hire mercenaries |
| `hub.choice.stay` | 경비병 선택지: 안 나감 | 아직 | Not yet |
| `hub.choice.ok` | 대화 선택지: 알았어 | 알았어 | Got it |
| `hub.choice.bye` | 대화 선택지: 대화 끝 | 그만 | Goodbye |
| `hub.recruit.hire` | 영입 창 후보 줄 버튼 | 영입 | Hire |
| `hub.recruit.dismiss` | 영입 창 파티 줄 버튼 | 내보내기 | Dismiss |
| `hub.recruit.level` | 영입 창 레벨. {0} 레벨 | Lv {0} | Lv {0} |
| `hub.recruit.combat` | 영입 창 전투 수치. {0} 전투 | 전투 {0} | Combat {0} |
| `hub.recruit.candidates` | 영입 창 후보 목록 제목 | 후보 | Candidates |
| `hub.recruit.party` | 영입 창 파티 목록 제목 | 파티 | Party |
| `hub.board.title` | 임무 게시판 창 제목 | 임무 게시판 | Quest Board |
| `hub.board.days` | 게시판 의뢰 기간. {0} 일 수 | 기간 {0}일 | {0} days |
| `hub.board.take` | 게시판 받기 버튼 | 받기 | Accept |
| `hub.board.taken` | 게시판 받은 뒤 버튼 | 받음 | Accepted |

2. 새 탭 3개 (머리글 15칸)

**Job**

| Key | Comment | ko | en |
|---|---|---|---|
| `job.ranger.name` | 직업 이름 | 레인저 | Ranger |
| `job.ranger.desc` | 직업 설명: 무기와 역할 (영입 창 툴팁) | 단궁. 원거리 딜러, 정찰 | Short bow. Ranged damage and scouting. |
| `job.porter.name` | 직업 이름 | 짐꾼 | Porter |
| `job.porter.desc` | 직업 설명: 무기와 역할 (영입 창 툴팁) | 곤봉. 짐을 나르고 싸움은 약함 | Club. Carries supplies, weak in combat. |
| `job.spear_guard.name` | 직업 이름 | 스피어 가드 | Spear Guard |
| `job.spear_guard.desc` | 직업 설명: 무기와 역할 (영입 창 툴팁) | 창과 방패. 딜탱 | Spear and shield. Deals damage and tanks. |
| `job.duelist.name` | 직업 이름 | 듀얼리스트 | Duelist |
| `job.duelist.desc` | 직업 설명: 무기와 역할 (영입 창 툴팁) | 망토와 레이피어. 회피로 버티는 딜탱 | Cloak and rapier. Deals damage and tanks by dodging. |

**Quest**

| Key | Comment | ko | en |
|---|---|---|---|
| `quest.forest_01.title` | 첫 의뢰 이름 (UI 탭에서 옮김) | 숲 외곽 정리 (임시) | Clear the Forest Edge (temp) |
| `quest.forest_01.desc` | 첫 의뢰 설명 (임시) | 숲 외곽에 나타난 고블린을 몰아내 줘. | Drive off the goblins that appeared at the forest edge. |

**Name** — `name.mercenary.01`~`08`, Comment "용병 임시 이름": 브란 Bran, 엘다 Elda, 코르빈 Corvin, 마렌 Maren, 토비아스 Tobias, 리셀 Lisel, 가레스 Gareth, 이나 Ina

3. 다른 탭:
   - `Menu`: `menu.journal.quest` 추가 (임무 일지 받은 의뢰 줄. {0} 의뢰 이름, {1} 기간(일) / 의뢰: {0} ({1}일) / Quest: {0} ({1} days))
   - `Objective`: `objective.guild.title`·`.desc`, `objective.depart.title`·`.desc` 추가 (문구는 CSV)
   - `UI`: `quest.forest_01.title` 지움 (Quest로 옮김), `field.debug_info`의 `[E]` → `[F]`
   - `Tip`: `tip.return.body` E → F / `Tutorial`: `tutorial.interact.body` E → F
4. 탭 8개를 키트 CSV 형식으로 `Assets/Localization/CSV/`에 씀 (Hub·Job·Quest·Name·Menu·Objective·UI·Tip·Tutorial)
5. `LocalizationEditorSettings.CreateStringTableCollection`으로 `Job`·`Quest`·`Name` 만들기 (`Assets/Localization/Tables`), 언어 표마다 `SetPreloadTableFlag(table, true)`
6. `Import Tables from CSV`
7. UI 컬렉션에서 `quest.forest_01.title` 제거: `RemoveEntry` + 언어 표마다 `RemoveEntry(id)` + SetDirty, `SaveAssets`
8. `Check Missing Translations`·`Check Missing Glyphs`

---

## 주의사항

- 실내는 같은 씬 지하 (y -40): 여관 홀 (0, -40, 0), 방 (20, -40, 0), 길드 (-30, -40, 0). 문은 페이드 순간이동이라 씬을 안 불러옴. 오두막은 겉모습 그대로
- 문 = `InteractPointView` + `DoorView` 같은 오브젝트. 방 문(`room_door`)만 방을 잡아야 열림, 아니면 토스트
- `FadeSequence`는 실제 시간 (일시정지 중에도 돎). 덮는 동안 조작·지점 찾기 끔, 다 걷힌 뒤 켬. 도는 중에 또 쓰면 무시
- 대화·영입·게시판 창이 열리면 `SetMenuPaused(true)` (시간 멈춤·`Player` 묶음 끔·커서 풂). 창을 바꿔 열 땐 열기 → 닫기 순서 (`OpenRecruit`: `ShowRecruit` 뒤 `HideDialogue`)라 사이에 안 풀림. 메뉴 창·일시정지 메뉴와 같이 안 뜸
- 선택지 버튼은 대화마다 새로 만들고 지움. 누른 버튼의 onClick 안에서 지워도 `Destroy`가 프레임 끝이라 괜찮음
- 식량 단위는 인분 (한 사람 하루치). 하루치 = 인원(플레이어 + 파티)만큼, 값 = 3 × 인원. 첫 의뢰 필요량 = 기간 3 × 인원 (3명이면 9인분 = 금화 27)
- 경비병 검사는 첫 출발 전만 (`HasDeparted`). 출발하면 필요한 만큼 뺌 (모자라면 있는 만큼)
- 숲 귀환: `FieldState`가 `ReturningFromField`를 켜고 `HubState`가 `ReturnPoint`에서 시작, 의뢰 비움 (정산 없음), 경비병 자막 뒤 목표 알림. 첫 출발 뒤엔 목표 없음
- 영입 후보는 처음 영입 창을 열 때 한 번 뽑음 (`CandidatesDrawn`). 내보낸 용병은 후보로 돌아감. 주간 갱신 없음
- 길드 벤치의 `MercenaryA`·`MercenaryB`는 동승 용병 자리 (`NpcFigure`, 상호작용 없음). 마차 용병(3-3 `Npc`)과 다른 오브젝트
- 침대·책상은 상호작용 없음 (책상은 5단계)
- 디버그 판(중간 씬 Play)은 용병·의뢰 없음 (스텁 지움) → 길드에서 영입·의뢰부터
- Job·Quest·Name 테이블은 Preload (Boot 상주 창이 써서 로딩창 글자 기다림에 안 걸림)
- `Npc.mat` diff는 Unity 재저장 부동소수 반올림뿐

---

## 진행 추적

- [x] 묶음 Logic — JobSO·RecruitPool·QuestSO·DialogueChoiceEntry·DialogueView·DialogueChoiceView·DialogueController·RecruitView·RecruitRowView·RecruitController·QuestBoardView·QuestBoardController·FadeSequence·DoorView 새로, MercenaryEntry·RunEntry·DataManager·UIManager·HubView·GameMenuView·JournalUIBinder·HubState·FieldState·InteractPointView·InteractDetector 수정. 컴파일 에러·경고 0
- [x] 묶음 Input — `Player/Interact` E → F
- [x] 묶음 Editor-Localization — 시트 Hub 고침·추가, Job·Quest·Name 탭, Menu·Objective·UI·Tip·Tutorial 줄, CSV 9개, 테이블 3개 만들기·Preload·Import, UI의 `quest.forest_01.title` 제거, 누락 검사
- [x] 묶음 Prefab·Data — DialogueChoice·RecruitRow·NpcFigure 프리팹, JobSO 4개·QuestSO 1개·ObjectiveSO 2개
- [x] 묶음 Editor-Scene — Boot(Fade·Dialogue·Recruit·QuestBoard·QuestText·매니저 연결·DataManager 목록), Outpost(문 2개·경비병·귀환 지점·문 앞 지점·지하 실내 3곳·금화 글자·HubView), 저장 뒤 다시 열어 확인
- [x] 묶음 Verify — 사용자 확인 (2026-11-22)

---

## 확인 목록 (사용자, 에디터 Play)

- 새 게임 → 연출 뒤 화면 왼쪽 위 "금화 100 · 식량 0인분", 안내 "[F] …"
- 여관 문 F → 화면이 잠깐 어두워졌다 여관 홀 → 여관 주인 F → 자막 + 선택지 (방 잡기 (금화 70) / 식량 하루치 사기 / 그만), 대화 중 게임 멈춤·커서 보임
- 방 안 잡고 방 문 F → "방을 먼저 잡아야 해". 방 잡기 → 금화 30, 주인이 길드를 알려 줌, "임무 일지 갱신: 길드에서 용병 구하고 의뢰 받기". 방 잡기 선택지는 다시 안 나옴
- 방 문 → 방 (책상·침대, F 안내 없음) → 나가기 → 여관 홀 방 문 앞
- 길드 문 → 길드 안, 벤치에 용병 2명. 접수원 → 용병 구하기 → 영입 창 (후보 4명: 이름·직업·Lv 1·전투, 직업에 마우스를 올리면 무기·역할 툴팁). 영입 2명 → 파티 칸, 셋째는 못 누름. 내보내기 → 후보로. 창을 닫고 다시 열어도 같은 후보
- 게시판 → 의뢰 이름·설명·기간 3일 → 받기 → 버튼 '받음', "임무 일지 갱신: 식량을 챙겨 정문으로" (용병 2명을 먼저 구했을 때. 아니면 의뢰를 받았다는 토스트). J → 임무 일지 "의뢰: 숲 외곽 정리 (임시) (3일)"
- 경비병: 파티가 모자라면 / 의뢰가 없으면 / 식량이 모자라면 각각 다른 한마디 + 알았어. 식량 하루치(3명 금화 9)를 3번 사면 9인분 → 경비병 출발 → 숲 (식량 0)
- 숲에서 F → 정문 밖에서 시작, 자막 "무사히 돌아왔군", 임무 일지 목표 없음·의뢰 줄 없음. 그 뒤 경비병은 파티·의뢰 없이도 출발·아직
- 금화가 모자랄 때 방·식량 → "금화가 모자라는데"
- 대화·영입·게시판 창에서 Esc → 닫힘 (일시정지 메뉴 안 열림), 다 닫히면 게임 다시 돎
- 상호작용은 F (E로는 안 됨). 로딩창 팁·튜토리얼 탭·숲 디버그 글자도 F
- 영어로 바꾸면 대사·선택지·창·직업·이름도 영어
- 콘솔 에러·경고 없음

---

## 커밋 메시지

```
Add — 4단계 여관·길드·정문: NPC 대화(자막+선택지), 영입 창, 임무 게시판, 금화·식량, 지하 실내, 정문 경비병

- 상호작용 키 E → F. 건물 문은 F로 들어가고 화면이 잠깐 어두워졌다 실내로 (FadeSequence, DoorView)
- 여관: 주인에게 방 잡기(금화 70, 처음 한 번)·식량 하루치(1인 3), 방에 책상·침대 (기능 없음)
- 길드: 접수원 대화 → 영입 창(후보 4명, 레벨 1, 직업 4종 랜덤, 무료, 영입·내보내기), 게시판 → 첫 의뢰(3일), 동승했던 다른 파티 용병 2명이 앉아 있음
- 정문 경비병: 첫 출발 전엔 파티·의뢰·식량이 모자라면 말로 막음, 출발하면 식량을 빼고 숲으로. 숲에서 돌아오면 정문 밖에서 시작
- 목표: 방 → 길드(파티·의뢰) → 식량 챙겨 정문, 임무 일지에 받은 의뢰 줄
- JobSO·QuestSO·RecruitPool, RunEntry 금화·식량·방·의뢰 (임시 영입·의뢰 스텁 지움)
- 다국어: Job·Quest·Name 테이블 새로 (Preload), UI의 quest.forest_01.title을 Quest로 옮김, E가 박힌 문구 3개 F로

Why: 도착부터 첫 의뢰 출발까지를 튜토리얼처럼 느끼지 않게 시설과 NPC 대화로 이끄는 흐름(기획 13의 4·6·7·9)이 필요함
Alternatives: 자막만 대화(방값·영입 확인에 선택지가 필요해서 뺌), 걸어 들어가는 실내(오두막을 키워야 해서 뺌), 침대 하루 휴식(의미 없어서 뺌)
```
