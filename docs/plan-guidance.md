# Plan: 3-3 안내 바탕 — 12시 조작 안내·NPC 자막·동승 용병, 메뉴 UI(지도·임무 일지·튜토리얼·주문 도감), BG3식 툴팁

## 현행 분석

- Boot `UICanvas`(Screen Space Overlay, 정렬 50) 자식 순서: `Pause`, `Settings`, `Confirm`, `Toast`. 뒤 자식이 위에 그려짐. `CoverCanvas`(정렬 100)가 로딩창·로고
- `UIManager`: 토스트(`ShowToast`, 실제 시간 2초), 설정 창·확인창·일시정지 메뉴, Esc(`UI/Cancel`) 순서 = 확인창 → 설정 창 → 일시정지 메뉴 → (플레이 씬이면) 일시정지 메뉴 열기. 일시정지는 `GameManager.SetMenuPaused(true)`: `Time.timeScale = 0`, `Player` 묶음 끔, 커서 풀기. 열 수 있는지는 `GameManager.CanPause`(전환 중 아님 + 상태가 `UsesPauseMenu`)
- 입력(`Assets/InputSystem_Actions.inputactions`, 전역 `InputSystem.actions`): `Player`(Move·Look·Attack·Interact·Crouch·Jump·Previous·Next·Sprint), `UI`(Navigate·Submit·Cancel·Point·Click…). M·J 액션 없음
- `TutorialManager`: Boot `Managers`에 빈 `MonoSingleton`만 있음
- `SettingsManager`: `settings_device.json`(`DeviceSettingsEntry`)을 .tmp → 바꿔치기 + .bak로 저장. 새 필드는 파일에 없으면 기본값이라 버전 안 올림
- `DataManager`: 요소·팁 SO 목록을 Boot에서 연결, 시작 때 빈 칸·id 겹침 검사
- `HubState`(3-2): 첫 도착이면 `OnSceneReady`에서 플레이어·`Canvas/Hud` 끄고 `Reveal`에서 `OpeningSequence.Play` → `OnOpeningFinished`에서 켜고 `StartControl`. 숲에서 돌아오거나 디버그 판이면 바로 `StartControl`
- Outpost `Opening`: `PlayableDirector`(`Assets/Timelines/Opening.playable`, 27초, Wrap None) 트랙 Cameras·Wagon·Overlay·Passenger. Activation 트랙은 Post-playback으로 끝 상태를 정함
- `Assets/Prefabs/World/Stagecoach.prefab`: 오른쪽 의자(x 0.65)에 `Passenger`(꺼 둠), `SeatHead`(yaw 270, 마차 안쪽을 봄). 왼쪽 의자 x -0.65는 비어 있음
- 생성 화면 `ElementCard.prefab`(220×300): 그림(120×120)·이름·설명 글자 + 토글. `CharacterCreationState.CreateCards`가 이름·설명 키를 넣음
- 공용 UI 프리팹: `WindowFrame`(Blocker + Panel: VerticalLayoutGroup + ContentSizeFitter), `Button`(Label = LocalizedText), `Toggle`(Control·Checkmark·Label)
- 여관 자리: Outpost `Huts/Storage`(10, 0, 10), 문이 광장 쪽(문 앞 약 (8.2, 8.2))

---

## C# 변경

### `Assets/Scripts/Tutorial/TutorialSO.cs` (새 파일)

**추가 내용:** 튜토리얼 하나. 12시 안내 문구, 튜토리얼 탭 제목·설명, 배웠다고 칠 행동, 띄우기 전 걸어야 할 시간

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>튜토리얼에서 배우는 행동 (조작 안내가 사라지는 조건)</summary>
    public enum TutorialAction
    {
        /// <summary>이동 입력 (WASD)</summary>
        Move,

        /// <summary>시점 입력 (마우스)</summary>
        Look,

        /// <summary>움직이며 달리기 입력 (Shift)</summary>
        Sprint,

        /// <summary>상호작용 (E). 12시 안내 없이 게임 코드가 처음 쓸 때 배움으로 알림</summary>
        Interact,
    }

    /// <summary>튜토리얼 하나: 화면 12시 조작 안내 문구와 튜토리얼 탭에 남는 제목·설명. 배우면 이 PC에 기록</summary>
    [CreateAssetMenu(fileName = "Tutorial", menuName = "Mage/Tutorial")]
    public sealed class TutorialSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (게임 코드가 알릴 때 씀: move, look, sprint, interact). 배운 기록 파일에 저장됨</summary>
        [SerializeField] string _id;

        /// <summary>화면 12시에 띄울 짧은 안내 (Tutorial 테이블 tutorial.{id}.prompt). 비면 안 띄움 (배움만 기록)</summary>
        [SerializeField] LocalizedString _prompt;

        /// <summary>튜토리얼 탭 제목 (tutorial.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>튜토리얼 탭 설명 (tutorial.{id}.body)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>배웠다고 치는 행동</summary>
        [SerializeField] TutorialAction _action;

        /// <summary>안내를 띄우기 전에 달리지 않고 걸어야 하는 시간 (초). 0이면 바로 띄움</summary>
        [SerializeField, Min(0f)] float _walkSecondsBeforeShow;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>화면 12시 안내. 비면 안 띄움</summary>
        public LocalizedString Prompt => _prompt;

        /// <summary>튜토리얼 탭 제목</summary>
        public LocalizedString Title => _title;

        /// <summary>튜토리얼 탭 설명</summary>
        public LocalizedString Body => _body;

        /// <summary>배웠다고 치는 행동</summary>
        public TutorialAction Action => _action;

        /// <summary>안내를 띄우기 전에 걸어야 하는 시간 (초)</summary>
        public float WalkSecondsBeforeShow => _walkSecondsBeforeShow;
    }
}
```

---

### `Assets/Scripts/UI/Views/TutorialPromptView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>화면 12시 조작 안내 참조 (Boot 씬 UICanvas/TutorialPrompt)</summary>
    public sealed class TutorialPromptView : MonoBehaviour
    {
        /// <summary>안내 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>안내 글자 (키는 TutorialManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _text;

        /// <summary>안내 전체</summary>
        public GameObject Root => _root;

        /// <summary>안내 글자</summary>
        public LocalizeStringEvent Text => _text;
    }
}
```

---

### `Assets/Scripts/Managers/TutorialManager.cs` (전체 교체)

**변경 내용:** 빈 매니저를 채움. 상황 알림 → 안 배운 것만 대기열 → 12시에 하나씩 → 입력을 직접 보고 실제로 하면 배움 저장 (안내가 뜨기 전에 이미 하면 안 띄우고 배움) → 0.5초 쉬고 다음

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>튜토리얼 진행 담당: 게임 코드가 상황(튜토리얼 id)을 알리면 안 배운 조작 안내만 화면 12시에 하나씩 띄우고, 입력을 보고 실제로 하면 배움으로 이 PC에 저장. 다른 코드가 튜토리얼 중인지 묻지 않게 알림만 받음</summary>
    public sealed class TutorialManager : MonoSingleton<TutorialManager>
    {
        /// <summary>이동 입력 액션 경로 (전역 액션)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>움직이는 중으로 볼 이동 입력 크기 (0~1)</summary>
        const float MoveInputThreshold = 0.5f;

        /// <summary>화면 12시 안내 (Boot 씬 UICanvas/TutorialPrompt. 인스펙터에서만 연결)</summary>
        [SerializeField] TutorialPromptView _prompt;

        /// <summary>이동을 배웠다고 칠 이동 입력 시간 (초). 걷기 5.6m/s면 약 2m</summary>
        [SerializeField, Min(0f)] float _moveSeconds = 0.36f;

        /// <summary>둘러보기를 배웠다고 칠 마우스 이동량 합. PlayerController 기본 감도 0.1이면 약 90°</summary>
        [SerializeField, Min(0f)] float _lookAmount = 900f;

        /// <summary>달리기를 배웠다고 칠, 움직이며 달린 시간 (초)</summary>
        [SerializeField, Min(0f)] float _sprintSeconds = 1f;

        /// <summary>안내 하나가 끝나고 다음 안내를 띄우기까지 쉬는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _gapSeconds = 0.5f;

        /// <summary>띄울 차례를 기다리는 튜토리얼 (알린 순서)</summary>
        readonly Queue<TutorialSO> _queue = new();

        /// <summary>지금 다루는 튜토리얼. 없으면 null</summary>
        TutorialSO _current;

        /// <summary>지금 안내가 화면에 떠 있는지 (걷기를 기다리는 동안은 false)</summary>
        bool _shown;

        /// <summary>지금 튜토리얼의 진행량 (안내가 뜨기 전 것도 셈. 행동마다 단위가 다름: 초 또는 마우스 이동량)</summary>
        float _progress;

        /// <summary>지금 안내를 띄우기 전에 달리지 않고 걸은 시간 (초)</summary>
        float _walked;

        /// <summary>다음 안내까지 남은 쉬는 시간 (초)</summary>
        float _gap;

        /// <summary>이동 입력. 없으면 null</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        #region InitSingleton - 안내 연결 확인·숨김, 입력 액션 찾기
        protected override void InitSingleton()
        {
            if (_prompt == null) Debug.LogError("[Tutorial] TutorialPromptView 연결 안 됨");
            else _prompt.Root.SetActive(false);
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Tutorial] {path} 액션 없음");
            return action;
        }
        #endregion

        #region Notify - 상황 알림: 모르는 id·12시 안내가 없는 튜토리얼은 경고, 배웠거나 이미 다루는 중이면 무시. 아니면 대기열에 넣고 쉬는 중이 아니면 바로 시작
        /// <param name="id">튜토리얼 id (TutorialSO.Id)</param>
        public void Notify(string id)
        {
            // 알린 튜토리얼 (없으면 null)
            TutorialSO tutorial = DataManager.Instance.FindTutorial(id);
            if (tutorial == null)
            {
                Debug.LogWarning($"[Tutorial] 모르는 튜토리얼 id: {id}");
                return;
            }
            if (tutorial.Prompt.IsEmpty)
            {
                Debug.LogWarning($"[Tutorial] {id}는 12시 안내가 없음. MarkLearned로 알려야 함");
                return;
            }
            if (IsLearned(id) || _current == tutorial || _queue.Contains(tutorial)) return;
            _queue.Enqueue(tutorial);
            if (_current == null && _gap <= 0f) StartNext();
        }
        #endregion

        #region MarkLearned - 안내 없이 배움으로 기록 (상호작용처럼 처음 써 본 걸 게임 코드가 알림). 새로 배웠으면 파일 저장
        /// <param name="id">튜토리얼 id</param>
        public void MarkLearned(string id)
        {
            // 기기 설정 담당 (배운 기록이 여기 있음)
            var settings = SettingsManager.Instance;
            if (settings != null && settings.Device.AddLearnedTutorial(id)) settings.Save();
        }
        #endregion

        #region IsLearned - 이 PC에서 배운 튜토리얼인지
        /// <param name="id">튜토리얼 id</param>
        public bool IsLearned(string id) => SettingsManager.Instance.Device.HasLearnedTutorial(id);
        #endregion

        #region Clear - 떠 있는 안내와 대기열을 비움 (상태를 나갈 때. 다음에 다시 알리면 다시 나옴)
        public void Clear()
        {
            _queue.Clear();
            _current = null;
            _shown = false;
            _gap = 0f;
            if (_prompt != null) _prompt.Root.SetActive(false);
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지·메뉴) 안 셈. 쉬는 중이면 기다림. 행동 진행량은 안내가 뜨기 전에도 셈(다 차면 안내 없이 배움). 안 떠 있으면 걷기를 기다렸다 띄움
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (_current == null)
            {
                if (_gap <= 0f) return;
                _gap -= dt;
                if (_gap <= 0f) StartNext();
                return;
            }
            // 안내가 뜨기 전에 한 것도 셈 (처음부터 달리던 플레이어에겐 달리기 안내를 안 띄움)
            _progress += Measure(_current.Action, dt);
            if (_progress >= Goal(_current.Action))
            {
                Complete();
                return;
            }
            if (_shown) return;
            if (IsMoving() && !IsSprinting()) _walked += dt;
            if (_walked >= _current.WalkSecondsBeforeShow) Show();
        }
        #endregion

        #region StartNext - 대기열에서 안 배운 다음 튜토리얼을 꺼내 다룸 (걸어야 할 시간이 0이면 바로 띄움)
        void StartNext()
        {
            _current = null;
            while (_queue.Count > 0)
            {
                // 다음 차례
                TutorialSO next = _queue.Dequeue();
                if (IsLearned(next.Id)) continue;
                _current = next;
                _shown = false;
                _progress = 0f;
                _walked = 0f;
                if (next.WalkSecondsBeforeShow <= 0f) Show();
                return;
            }
        }
        #endregion

        #region Show - 지금 튜토리얼의 안내를 12시에 띄움
        void Show()
        {
            _shown = true;
            if (_prompt == null) return;
            _prompt.Text.StringReference.SetReference(_current.Prompt.TableReference, _current.Prompt.TableEntryReference);
            _prompt.Root.SetActive(true);
        }
        #endregion

        #region Complete - 배움 저장, 안내 숨김, 쉬는 시간 뒤 다음 안내
        void Complete()
        {
            MarkLearned(_current.Id);
            _current = null;
            _shown = false;
            if (_prompt != null) _prompt.Root.SetActive(false);
            _gap = _gapSeconds;
            if (_gap <= 0f) StartNext();
        }
        #endregion

        #region Measure - 이번 프레임 행동 진행량 (이동·달리기: 초, 둘러보기: 마우스 이동량)
        /// <param name="action">배울 행동</param>
        /// <param name="dt">이번 프레임 시간 (초)</param>
        float Measure(TutorialAction action, float dt) => action switch
        {
            TutorialAction.Move => IsMoving() ? dt : 0f,
            TutorialAction.Look => _look != null ? _look.ReadValue<Vector2>().magnitude : 0f,
            TutorialAction.Sprint => IsMoving() && IsSprinting() ? dt : 0f,
            _ => 0f,
        };
        #endregion

        #region Goal - 배웠다고 칠 진행량
        /// <param name="action">배울 행동</param>
        float Goal(TutorialAction action) => action switch
        {
            TutorialAction.Move => _moveSeconds,
            TutorialAction.Look => _lookAmount,
            TutorialAction.Sprint => _sprintSeconds,
            _ => float.MaxValue,
        };
        #endregion

        #region IsMoving - 이동 입력이 들어오는지
        bool IsMoving() => _move != null && _move.ReadValue<Vector2>().magnitude >= MoveInputThreshold;
        #endregion

        #region IsSprinting - 달리기 입력이 눌려 있는지
        bool IsSprinting() => _sprint != null && _sprint.IsPressed();
        #endregion
    }
}
```

**호출/구독 관계:**
- `HubState.StartControl` → `Notify("move")`·`Notify("look")`·`Notify("sprint")` (배운 건 걸러짐). `HubState.OnInteracted` → `MarkLearned("interact")`. `HubState.Exit` → `Clear()`
- `TutorialListUIBinder` → `IsLearned`
- 저장은 `SettingsManager.Device.AddLearnedTutorial` + `SettingsManager.Save`

---

### `Assets/Scripts/Settings/DeviceSettingsEntry.cs`

**변경 내용:** 배운 튜토리얼 id 목록 추가 (이 PC). 버전은 그대로 (파일에 없으면 빈 목록)

```csharp
using System;
using System.Collections.Generic;   // ← 추가
using System.Linq;                  // ← 추가
using UnityEngine;
```

클래스 설명: `/// <summary>기기 설정 (이 PC에만 맞는 값: 음량, 창이 비활성이면 음소거, 배운 튜토리얼). …(뒤는 그대로)`

`_muteWhenUnfocused` 아래에 추가:

```csharp
        /// <summary>이 PC에서 배운 튜토리얼 id (TutorialSO.Id). 배운 조작 안내는 다시 안 띄움</summary>
        [SerializeField] List<string> _learnedTutorials = new();
```

`MuteWhenUnfocused` 프로퍼티 아래에 추가:

```csharp
        #region HasLearnedTutorial - 배운 튜토리얼인지 (대소문자 구분)
        /// <param name="id">튜토리얼 id</param>
        public bool HasLearnedTutorial(string id) => !string.IsNullOrEmpty(id) && _learnedTutorials.Contains(id);
        #endregion

        #region AddLearnedTutorial - 배운 튜토리얼 추가. 새로 넣었으면 true (빈 id·이미 있는 id는 false)
        /// <param name="id">튜토리얼 id</param>
        public bool AddLearnedTutorial(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || _learnedTutorials.Contains(id)) return false;
            _learnedTutorials.Add(id);
            return true;
        }
        #endregion
```

`Validate` 전체 교체 (배운 목록에서 빈 id·겹친 id 뺌):

```csharp
        #region Validate - 파일에서 읽은 값을 쓸 수 있게 고침 (음량 0~1·NaN, 배운 튜토리얼 빈 id·겹침, 모르는 버전은 지금 버전으로). 고친 게 있으면 true
        public bool Validate()
        {
            // 고친 전체 음량
            float master = ToValid(_masterVolume);
            // 고친 배경음 음량
            float bgm = ToValid(_bgmVolume);
            // 고친 효과음 음량
            float sfx = ToValid(_sfxVolume);
            // 고친 배운 튜토리얼 목록 (빈 id·겹친 id 뺌)
            List<string> learned = (_learnedTutorials ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
            // 하나라도 고쳤는지. NaN은 어떤 값과도 != 라서 고친 걸로 잡힘
            bool changed = _version != CurrentVersion
                || master != _masterVolume || bgm != _bgmVolume || sfx != _sfxVolume
                || _learnedTutorials == null || learned.Count != _learnedTutorials.Count;
            _version = CurrentVersion;
            _masterVolume = master;
            _bgmVolume = bgm;
            _sfxVolume = sfx;
            _learnedTutorials = learned;
            return changed;
        }
        #endregion
```

---

### `Assets/Scripts/Managers/SettingsManager.cs`

**변경 내용:** `Device` 설명만 (배운 튜토리얼도 기기 설정에 있음)

```csharp
        /// <summary>지금 기기 설정 (음량, 배운 튜토리얼). 바꾼 값은 Save를 불러야 파일에 남음</summary>
        public DeviceSettingsEntry Device => _device;
```

---

### `Assets/Scripts/Managers/DataManager.cs`

**변경 내용:** 튜토리얼 목록·검사·id로 찾기

`Tips` 프로퍼티 아래에 추가:

```csharp
        /// <summary>튜토리얼 전부 (Boot에서 연결. 이 순서대로 튜토리얼 탭 목록이 나옴)</summary>
        [SerializeField] TutorialSO[] _tutorials;

        /// <summary>튜토리얼 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<TutorialSO> Tutorials => _tutorials;
```

`InitSingleton` 첫 줄 `ValidateTips();` 바로 아래에 `ValidateTutorials();` 추가 (요소 검사의 `return`보다 앞). region 설명: `InitSingleton - 팁 목록 검사, 튜토리얼 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침`

`ValidateTips` 아래에 추가:

```csharp
        #region FindTutorial - id로 튜토리얼 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">튜토리얼 id</param>
        public TutorialSO FindTutorial(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (TutorialSO tutorial in _tutorials)
                if (tutorial != null && string.Equals(tutorial.Id, id, StringComparison.Ordinal)) return tutorial;
            return null;
        }
        #endregion

        #region ValidateTutorials - 튜토리얼 목록 검사: 비었으면 경고, 빈 칸, 빈 id, id 겹침, 제목·설명 키 빔
        void ValidateTutorials()
        {
            if (_tutorials == null || _tutorials.Length == 0)
            {
                Debug.LogWarning("[Data] 튜토리얼 목록 비어 있음. 조작 안내·튜토리얼 탭이 안 나옴");
                _tutorials = Array.Empty<TutorialSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _tutorials.Length; i++)
            {
                // 검사할 튜토리얼
                TutorialSO tutorial = _tutorials[i];
                if (tutorial == null)
                    Debug.LogError($"[Data] 튜토리얼 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(tutorial.Id))
                    Debug.LogError($"[Data] {tutorial.name}의 id 비어 있음");
                else if (!ids.Add(tutorial.Id))
                    Debug.LogError($"[Data] 튜토리얼 id 겹침: {tutorial.Id} ({tutorial.name})");
                else if (tutorial.Title.IsEmpty || tutorial.Body.IsEmpty)
                    Debug.LogError($"[Data] {tutorial.name}의 제목·설명 키 비어 있음");
            }
        }
        #endregion
```

---

### `Assets/Scripts/UI/Views/SubtitleView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>NPC 대사 자막 참조 (Boot 씬 UICanvas/Subtitle, 화면 아래 토스트 위)</summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        /// <summary>자막 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>말한 사람 글자 (키는 UIManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _speaker;

        /// <summary>대사 글자 (키는 UIManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _line;

        /// <summary>자막 전체</summary>
        public GameObject Root => _root;

        /// <summary>말한 사람 글자</summary>
        public LocalizeStringEvent Speaker => _speaker;

        /// <summary>대사 글자</summary>
        public LocalizeStringEvent Line => _line;
    }
}
```

---

### `Assets/Scripts/Quest/ObjectiveSO.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>목표 하나 (임무 일지 탭에 보임). 대사·물건이 줌</summary>
    [CreateAssetMenu(fileName = "Objective", menuName = "Mage/Objective")]
    public sealed class ObjectiveSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (inn_room …)</summary>
        [SerializeField] string _id;

        /// <summary>목표 이름 (Objective 테이블 objective.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>목표 설명 (Objective 테이블 objective.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>목표 이름</summary>
        public LocalizedString Title => _title;

        /// <summary>목표 설명</summary>
        public LocalizedString Description => _description;
    }
}
```

---

### `Assets/Scripts/Core/RunEntry.cs`

**변경 내용:** `HasArrived` 아래에 현재 목표 추가

```csharp
        /// <summary>지금 목표 (임무 일지). 없으면 null. 판마다 새로</summary>
        public ObjectiveSO CurrentObjective { get; set; }
```

---

### `Assets/Scripts/UI/Views/GameMenuView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>메뉴 창 참조 (Boot 씬 UICanvas/GameMenu 루트. 지도·임무 일지·튜토리얼·주문 도감 탭. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class GameMenuView : MonoBehaviour
    {
        /// <summary>지도 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _mapTab;

        /// <summary>임무 일지 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _journalTab;

        /// <summary>튜토리얼 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _tutorialTab;

        /// <summary>주문 도감 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _spellsTab;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>지도 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _mapPage;

        /// <summary>임무 일지 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _journalPage;

        /// <summary>튜토리얼 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialPage;

        /// <summary>주문 도감 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _spellsPage;

        /// <summary>임무 일지: 목표 이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _objectiveTitle;

        /// <summary>임무 일지: 목표 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _objectiveDescription;

        /// <summary>임무 일지: 목표가 없을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _journalEmpty;

        /// <summary>튜토리얼: "모두 보기 (스포일러가 있을 수 있습니다)" 토글 (인스펙터에서만 연결)</summary>
        [SerializeField] Toggle _spoilerToggle;

        /// <summary>튜토리얼: 목록 항목을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _tutorialList;

        /// <summary>튜토리얼: 목록 항목 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] TutorialListItemView _tutorialItemPrefab;

        /// <summary>튜토리얼: 고른 항목 설명 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialDetail;

        /// <summary>튜토리얼: 고른 항목 제목 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tutorialTitle;

        /// <summary>튜토리얼: 고른 항목 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tutorialBody;

        /// <summary>튜토리얼: 보일 항목이 없을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialEmpty;

        /// <summary>지도 탭 버튼</summary>
        public Button MapTab => _mapTab;

        /// <summary>임무 일지 탭 버튼</summary>
        public Button JournalTab => _journalTab;

        /// <summary>튜토리얼 탭 버튼</summary>
        public Button TutorialTab => _tutorialTab;

        /// <summary>주문 도감 탭 버튼</summary>
        public Button SpellsTab => _spellsTab;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;

        /// <summary>지도 페이지</summary>
        public GameObject MapPage => _mapPage;

        /// <summary>임무 일지 페이지</summary>
        public GameObject JournalPage => _journalPage;

        /// <summary>튜토리얼 페이지</summary>
        public GameObject TutorialPage => _tutorialPage;

        /// <summary>주문 도감 페이지</summary>
        public GameObject SpellsPage => _spellsPage;

        /// <summary>임무 일지: 목표 이름 글자</summary>
        public LocalizeStringEvent ObjectiveTitle => _objectiveTitle;

        /// <summary>임무 일지: 목표 설명 글자</summary>
        public LocalizeStringEvent ObjectiveDescription => _objectiveDescription;

        /// <summary>임무 일지: 목표가 없을 때 글</summary>
        public GameObject JournalEmpty => _journalEmpty;

        /// <summary>튜토리얼: 모두 보기 토글</summary>
        public Toggle SpoilerToggle => _spoilerToggle;

        /// <summary>튜토리얼: 목록 자리</summary>
        public RectTransform TutorialList => _tutorialList;

        /// <summary>튜토리얼: 목록 항목 프리팹</summary>
        public TutorialListItemView TutorialItemPrefab => _tutorialItemPrefab;

        /// <summary>튜토리얼: 고른 항목 설명 묶음</summary>
        public GameObject TutorialDetail => _tutorialDetail;

        /// <summary>튜토리얼: 고른 항목 제목 글자</summary>
        public LocalizeStringEvent TutorialTitle => _tutorialTitle;

        /// <summary>튜토리얼: 고른 항목 설명 글자</summary>
        public LocalizeStringEvent TutorialBody => _tutorialBody;

        /// <summary>튜토리얼: 보일 항목이 없을 때 글</summary>
        public GameObject TutorialEmpty => _tutorialEmpty;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/GameMenuController.cs` (새 파일, 새 폴더)

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>메뉴 창 탭</summary>
    public enum GameMenuTab
    {
        /// <summary>지도 (M)</summary>
        Map,

        /// <summary>임무 일지 (J)</summary>
        Journal,

        /// <summary>튜토리얼</summary>
        Tutorial,

        /// <summary>주문 도감</summary>
        Spells,
    }

    /// <summary>메뉴 창 탭 바꾸기: 탭 버튼을 누르면 그 페이지만 켜고 그 탭 버튼은 못 누르게. 여닫기·일시정지는 UIManager</summary>
    public sealed class GameMenuController : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>같은 창의 참조</summary>
        public GameMenuView View => _view;

        /// <summary>지금 보이는 탭</summary>
        public GameMenuTab CurrentTab { get; private set; }

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Awake - 연결 확인, 탭 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] GameMenuView 연결 안 됨", this);
                return;
            }
            _view.MapTab.onClick.AddListener(() => ShowTab(GameMenuTab.Map));
            _view.JournalTab.onClick.AddListener(() => ShowTab(GameMenuTab.Journal));
            _view.TutorialTab.onClick.AddListener(() => ShowTab(GameMenuTab.Tutorial));
            _view.SpellsTab.onClick.AddListener(() => ShowTab(GameMenuTab.Spells));
        }
        #endregion

        #region ShowTab - 고른 탭 페이지만 켜고, 그 탭 버튼은 못 누르게 (지금 탭 표시)
        /// <param name="tab">보일 탭</param>
        public void ShowTab(GameMenuTab tab)
        {
            CurrentTab = tab;
            if (_view == null) return;
            SetTab(_view.MapTab, _view.MapPage, tab == GameMenuTab.Map);
            SetTab(_view.JournalTab, _view.JournalPage, tab == GameMenuTab.Journal);
            SetTab(_view.TutorialTab, _view.TutorialPage, tab == GameMenuTab.Tutorial);
            SetTab(_view.SpellsTab, _view.SpellsPage, tab == GameMenuTab.Spells);
        }
        #endregion

        #region SetTab - 탭 하나 켜고 끄기 (켜진 탭 버튼은 못 누름)
        /// <param name="button">탭 버튼</param>
        /// <param name="page">탭 페이지</param>
        /// <param name="on">이 탭을 보일지</param>
        static void SetTab(Button button, GameObject page, bool on)
        {
            button.interactable = !on;
            page.SetActive(on);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `UIManager.ToggleGameMenu` → 창을 켠 뒤 `ShowTab`. 페이지가 켜질 때 페이지의 UIBinder `OnEnable`이 내용을 채움

---

### `Assets/Scripts/UI/Binders/JournalUIBinder.cs` (새 파일)

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>임무 일지 탭 ← 이번 판 현재 목표. 탭이 켜질 때마다 채움 (목표가 없으면 빈 글)</summary>
    public sealed class JournalUIBinder : MonoBehaviour
    {
        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        #region OnEnable - 현재 목표의 이름·설명 키를 넣음. 목표가 없으면 빈 글만
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] JournalUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            // 이번 판 현재 목표 (판이나 목표가 없으면 null)
            ObjectiveSO objective = GameManager.Instance.Run?.CurrentObjective;
            // 목표가 있는지
            bool has = objective != null;
            _view.ObjectiveTitle.gameObject.SetActive(has);
            _view.ObjectiveDescription.gameObject.SetActive(has);
            _view.JournalEmpty.SetActive(!has);
            if (!has) return;
            _view.ObjectiveTitle.StringReference.SetReference(objective.Title.TableReference, objective.Title.TableEntryReference);
            _view.ObjectiveDescription.StringReference.SetReference(objective.Description.TableReference, objective.Description.TableEntryReference);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/TutorialListItemView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>튜토리얼 탭 목록 항목 참조 (TutorialListItem 프리팹 루트)</summary>
    public sealed class TutorialListItemView : MonoBehaviour
    {
        /// <summary>항목 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>항목 제목 글자 (키는 UIBinder가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>항목 버튼</summary>
        public Button Button => _button;

        /// <summary>항목 제목 글자</summary>
        public LocalizeStringEvent Title => _title;
    }
}
```

---

### `Assets/Scripts/UI/Binders/TutorialListUIBinder.cs` (새 파일)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>튜토리얼 탭 ← 배운 튜토리얼 (이 PC). 탭이 켜질 때 목록을 다시 만들고, 스포일러 토글을 켜면 안 배운 것까지 다 보임. 항목을 누르면 제목·설명</summary>
    public sealed class TutorialListUIBinder : MonoBehaviour
    {
        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>지금 만든 목록 항목</summary>
        readonly List<TutorialListItemView> _items = new();

        #region OnEnable - 토글 구독, 목록 만들기
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] TutorialListUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            _view.SpoilerToggle.onValueChanged.AddListener(OnSpoilerChanged);
            Rebuild();
        }
        #endregion

        #region OnDisable - 토글 구독 해제
        void OnDisable()
        {
            if (_view == null) return;
            _view.SpoilerToggle.onValueChanged.RemoveListener(OnSpoilerChanged);
        }
        #endregion

        #region OnSpoilerChanged - 모두 보기를 켜고 끄면 목록을 다시 만듦
        /// <param name="isOn">켜졌는지 (안 씀, 토글 값을 다시 읽음)</param>
        void OnSpoilerChanged(bool isOn) => Rebuild();
        #endregion

        #region Rebuild - 목록 항목을 지우고 DataManager 순서대로 다시 만듦 (토글이 꺼져 있으면 배운 것만). 첫 항목 설명을 보임
        void Rebuild()
        {
            foreach (TutorialListItemView item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();

            // 안 배운 것까지 다 보일지
            bool showAll = _view.SpoilerToggle.isOn;
            // 목록 첫 항목 (설명을 처음 보일 것. 없으면 null)
            TutorialSO first = null;
            foreach (TutorialSO tutorial in DataManager.Instance.Tutorials)
            {
                if (tutorial == null) continue;
                if (!showAll && !TutorialManager.Instance.IsLearned(tutorial.Id)) continue;
                // 이 튜토리얼 항목
                TutorialListItemView item = Instantiate(_view.TutorialItemPrefab, _view.TutorialList);
                item.Title.StringReference.SetReference(tutorial.Title.TableReference, tutorial.Title.TableEntryReference);
                // 버튼이 누를 때 쓸 튜토리얼 (반복 변수를 붙잡지 않게)
                TutorialSO picked = tutorial;
                item.Button.onClick.AddListener(() => ShowDetail(picked));
                _items.Add(item);
                if (first == null) first = tutorial;
            }
            _view.TutorialEmpty.SetActive(first == null);
            _view.TutorialDetail.SetActive(first != null);
            if (first != null) ShowDetail(first);
        }
        #endregion

        #region ShowDetail - 고른 튜토리얼의 제목·설명 키를 넣음
        /// <param name="tutorial">고른 튜토리얼</param>
        void ShowDetail(TutorialSO tutorial)
        {
            _view.TutorialTitle.StringReference.SetReference(tutorial.Title.TableReference, tutorial.Title.TableEntryReference);
            _view.TutorialBody.StringReference.SetReference(tutorial.Body.TableReference, tutorial.Body.TableEntryReference);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Views/TooltipView.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>툴팁 참조 (Boot 씬 UICanvas/Tooltip, 맨 위)</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        /// <summary>마우스를 따라가는 패널 (앵커 가운데, 피벗 왼쪽 위. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _panel;

        /// <summary>제목 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _body;

        /// <summary>마우스를 따라가는 패널</summary>
        public RectTransform Panel => _panel;

        /// <summary>제목 글자</summary>
        public LocalizeStringEvent Title => _title;

        /// <summary>설명 글자</summary>
        public LocalizeStringEvent Body => _body;
    }
}
```

---

### `Assets/Scripts/UI/Controllers/TooltipController.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>툴팁 띄우기·숨기기·마우스 따라가기. 기본은 마우스 오른쪽 아래, 화면을 넘으면 마우스 반대쪽으로</summary>
    public sealed class TooltipController : MonoBehaviour
    {
        /// <summary>같은 오브젝트의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] TooltipView _view;

        /// <summary>툴팁이 놓인 캔버스 (UICanvas. 좌표 바꿀 때 씀. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _canvas;

        /// <summary>마우스에서 띄울 거리 (캔버스 단위, x 오른쪽, y 위)</summary>
        [SerializeField] Vector2 _offset = new(16f, -16f);

        #region Show - 제목·설명 키를 넣고 켬 (설명이 비면 제목만). 패널 크기를 바로 맞춰 마우스 옆에 놓음
        /// <param name="title">제목</param>
        /// <param name="body">설명 (없으면 null 또는 빈 값)</param>
        public void Show(LocalizedString title, LocalizedString body)
        {
            if (_view == null || _canvas == null)
            {
                Debug.LogError("[Tooltip] TooltipView·캔버스 연결 안 됨", this);
                return;
            }
            _view.Title.StringReference.SetReference(title.TableReference, title.TableEntryReference);
            // 설명이 있는지
            bool hasBody = body != null && !body.IsEmpty;
            _view.Body.gameObject.SetActive(hasBody);
            if (hasBody) _view.Body.StringReference.SetReference(body.TableReference, body.TableEntryReference);
            gameObject.SetActive(true);
            // 새 글자 크기로 패널 크기를 바로 맞춤 (안 하면 첫 프레임 화면 끝 판단이 옛 크기로 됨)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Panel);
            Follow();
        }
        #endregion

        #region Hide - 끔
        public void Hide() => gameObject.SetActive(false);
        #endregion

        #region LateUpdate - 떠 있는 동안 마우스를 따라감
        void LateUpdate() => Follow();
        #endregion

        #region Follow - 마우스 위치를 캔버스 좌표로 바꿔 패널을 옮김. 오른쪽·아래가 넘치면 마우스 반대쪽으로
        void Follow()
        {
            // 마우스 (없으면 안 따라감)
            var mouse = Mouse.current;
            if (mouse == null || _view == null || _canvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, mouse.position.ReadValue(), null, out Vector2 local);
            // 패널 크기 (레이아웃이 정한 크기)
            Vector2 size = _view.Panel.rect.size;
            // 캔버스 가운데에서 가장자리까지 거리
            Vector2 half = _canvas.rect.size * 0.5f;
            // 패널 왼쪽 위 x
            float x = local.x + _offset.x;
            if (x + size.x > half.x) x = local.x - _offset.x - size.x;
            // 패널 왼쪽 위 y
            float y = local.y + _offset.y;
            if (y - size.y < -half.y) y = local.y - _offset.y + size.y;
            _view.Panel.anchoredPosition = new Vector2(x, y);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Handlers/TooltipHandler.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>UI 요소에 마우스를 올리면 툴팁, 떼거나 꺼지면 숨김. 내용은 인스펙터나 Set으로 (SO의 LocalizedString은 읽기만 함)</summary>
    public sealed class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>툴팁 제목 (비면 툴팁 안 띄움)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>툴팁 설명 (비면 제목만)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>이 요소가 툴팁을 띄운 상태인지 (남이 띄운 툴팁을 끄지 않게)</summary>
        bool _shown;

        #region Set - 내용 바꾸기 (상태가 데이터를 넣을 때)
        /// <param name="title">제목</param>
        /// <param name="body">설명</param>
        public void Set(LocalizedString title, LocalizedString body)
        {
            _title = title;
            _body = body;
        }
        #endregion

        #region OnPointerEnter - 마우스가 들어오면 툴팁 (제목이 비면 무시)
        /// <param name="eventData">포인터 정보 (안 씀)</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_title == null || _title.IsEmpty) return;
            _shown = true;
            UIManager.Instance.ShowTooltip(_title, _body);
        }
        #endregion

        #region OnPointerExit - 마우스가 나가면 숨김
        /// <param name="eventData">포인터 정보 (안 씀)</param>
        public void OnPointerExit(PointerEventData eventData) => HideIfShown();
        #endregion

        #region OnDisable - 꺼지거나 지워지면 숨김 (씬이 바뀌어도 툴팁이 남지 않게)
        void OnDisable() => HideIfShown();
        #endregion

        #region HideIfShown - 이 요소가 띄운 툴팁이면 숨김
        void HideIfShown()
        {
            if (!_shown) return;
            _shown = false;
            // 공용 UI 담당 (앱 종료 중이면 없을 수 있음)
            var ui = UIManager.Instance;
            if (ui != null) ui.HideTooltip();
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Managers/UIManager.cs`

**변경 내용:** 자막(게임 시간 4초, 끝나면 알림), 메뉴 창(M·J 여닫기, 일시정지, Esc), 툴팁 위임

`using`에 추가:

```csharp
using UnityEngine.Localization;
```

클래스 설명: `/// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·자막·설정 창·확인창·일시정지 메뉴·메뉴 창(지도·임무 일지·튜토리얼·주문 도감)·툴팁</summary>`

상수 추가 (`ToTitleConfirmKey` 아래):

```csharp
        /// <summary>자막을 보이는 시간 (초, 게임 시간이라 일시정지 중엔 멈춤)</summary>
        const float SubtitleSeconds = 4f;

        /// <summary>메뉴 창 지도 탭 입력 액션 경로 (전역 액션, M)</summary>
        const string MapActionPath = "UI/Map";

        /// <summary>메뉴 창 임무 일지 탭 입력 액션 경로 (전역 액션, J)</summary>
        const string JournalActionPath = "UI/Journal";
```

필드 추가 (`_pauseMenu` 아래):

```csharp
        /// <summary>메뉴 창 (Boot 씬 UICanvas 자식. 일시정지 메뉴보다 아래에 그려짐)</summary>
        [SerializeField] GameMenuController _gameMenu;

        /// <summary>NPC 대사 자막 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] SubtitleView _subtitle;

        /// <summary>툴팁 (Boot 씬 UICanvas 맨 위 자식)</summary>
        [SerializeField] TooltipController _tooltip;

        /// <summary>자막을 숨기는 코루틴. 새 자막이 오면 끊고 다시 시작</summary>
        Coroutine _hideSubtitle;

        /// <summary>지금 자막이 끝나면 할 일. 없으면 null</summary>
        Action _onSubtitleHidden;

        /// <summary>지도 탭 입력 액션. OnDestroy에서 구독 해제</summary>
        InputAction _map;

        /// <summary>임무 일지 탭 입력 액션. OnDestroy에서 구독 해제</summary>
        InputAction _journal;
```

`InitWindows`: 확인창 블록 아래, `_cancel = …` 위에 추가 (region 설명 끝에 ", 메뉴 창·자막·툴팁 끄기, M·J 구독" 붙임):

```csharp
            if (_gameMenu == null)
                Debug.LogError("[UI] GameMenuController 연결 안 됨");
            else
            {
                _gameMenu.gameObject.SetActive(false);
                if (_gameMenu.View != null) _gameMenu.View.CloseButton.onClick.AddListener(HideGameMenu);
            }

            if (_subtitle == null) Debug.LogError("[UI] SubtitleView 연결 안 됨");
            else _subtitle.Root.SetActive(false);

            if (_tooltip == null) Debug.LogError("[UI] TooltipController 연결 안 됨");
            else _tooltip.Hide();

            _map = FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMap;
            _journal = FindAction(JournalActionPath);
            if (_journal != null) _journal.performed += OnJournal;
```

`OnDestroy`: `base.OnDestroy();` 위에 추가 (region 설명 "Esc·M·J 구독 해제"):

```csharp
            if (_map != null) _map.performed -= OnMap;
            if (_journal != null) _journal.performed -= OnJournal;
```

`OnCancel` 교체 (메뉴 창을 일시정지 메뉴보다 먼저 닫음):

```csharp
        #region OnCancel - Esc: 확인창 → 설정 창 → 메뉴 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸고 플레이 씬이면 일시정지 메뉴를 엶
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancel(InputAction.CallbackContext context)
        {
            if (_confirm != null && _confirm.gameObject.activeSelf)
                HideConfirm();
            else if (_settings != null && _settings.gameObject.activeSelf)
                HideSettings();
            else if (_gameMenu != null && _gameMenu.IsOpen)
                HideGameMenu();
            else if (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
                HidePauseMenu();
            else if (_pauseMenu != null && GameManager.Instance.CanPause)
                ShowPauseMenu();
        }
        #endregion
```

새 메서드 (파일 끝 `OnCancel` 아래):

```csharp
        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[UI] {path} 액션 없음");
            return action;
        }
        #endregion

        #region OnMap - M: 메뉴 창 지도 탭
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnMap(InputAction.CallbackContext context) => ToggleGameMenu(GameMenuTab.Map);
        #endregion

        #region OnJournal - J: 메뉴 창 임무 일지 탭
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnJournal(InputAction.CallbackContext context) => ToggleGameMenu(GameMenuTab.Journal);
        #endregion

        #region ToggleGameMenu - 창이 떠 있으면 같은 탭은 닫고 다른 탭은 바꿈. 닫혀 있으면 (다른 창이 없고 플레이 씬일 때) 그 탭으로 열고 게임 멈춤
        /// <param name="tab">열 탭</param>
        void ToggleGameMenu(GameMenuTab tab)
        {
            if (_gameMenu == null) return;
            if (_gameMenu.IsOpen)
            {
                if (_gameMenu.CurrentTab == tab) HideGameMenu();
                else _gameMenu.ShowTab(tab);
                return;
            }
            if (IsWindowOpen() || !GameManager.Instance.CanPause) return;
            _gameMenu.gameObject.SetActive(true);
            _gameMenu.ShowTab(tab);
            GameManager.Instance.SetMenuPaused(true);
        }
        #endregion

        #region HideGameMenu - 메뉴 창 끄고 게임 다시 돌림 (닫기 버튼·같은 키·Esc)
        void HideGameMenu()
        {
            if (_gameMenu != null) _gameMenu.gameObject.SetActive(false);
            GameManager.Instance.SetMenuPaused(false);
        }
        #endregion

        #region IsWindowOpen - 일시정지 메뉴·설정 창·확인창 중 하나라도 떠 있는지 (메뉴 창과 같이 안 뜨게)
        bool IsWindowOpen() =>
            (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
            || (_settings != null && _settings.gameObject.activeSelf)
            || (_confirm != null && _confirm.gameObject.activeSelf);
        #endregion

        #region ShowSubtitle - 말한 사람·대사 자막을 게임 시간 4초 보이고, 끝나면 onHidden (앞 자막은 알림 없이 끊음)
        /// <param name="speaker">말한 사람</param>
        /// <param name="line">대사</param>
        /// <param name="onHidden">자막이 끝까지 보이고 사라진 뒤 할 일 (없으면 null)</param>
        public void ShowSubtitle(LocalizedString speaker, LocalizedString line, Action onHidden = null)
        {
            if (speaker == null || line == null || speaker.IsEmpty || line.IsEmpty)
            {
                Debug.LogWarning("[UI] 빈 자막 무시");
                onHidden?.Invoke();
                return;
            }
            if (_subtitle == null)
            {
                Debug.LogError("[UI] SubtitleView 연결 안 됨");
                onHidden?.Invoke();
                return;
            }
            HideSubtitle();
            _subtitle.Speaker.StringReference.SetReference(speaker.TableReference, speaker.TableEntryReference);
            _subtitle.Line.StringReference.SetReference(line.TableReference, line.TableEntryReference);
            _subtitle.Root.SetActive(true);
            _onSubtitleHidden = onHidden;
            _hideSubtitle = StartCoroutine(HideSubtitleAfter(SubtitleSeconds));
        }
        #endregion

        #region HideSubtitleAfter - 정해진 시간 뒤 자막을 숨기고 할 일 실행 (게임 시간 기준)
        /// <param name="seconds">기다릴 시간 (초)</param>
        IEnumerator HideSubtitleAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _subtitle.Root.SetActive(false);
            _hideSubtitle = null;
            // 끝나면 할 일 (꺼내고 비운 뒤 실행)
            Action onHidden = _onSubtitleHidden;
            _onSubtitleHidden = null;
            onHidden?.Invoke();
        }
        #endregion

        #region HideSubtitle - 자막을 바로 숨김. 끝나면 할 일은 버림 (상태를 나갈 때)
        public void HideSubtitle()
        {
            if (_hideSubtitle != null) StopCoroutine(_hideSubtitle);
            _hideSubtitle = null;
            _onSubtitleHidden = null;
            if (_subtitle != null) _subtitle.Root.SetActive(false);
        }
        #endregion

        #region ShowTooltip - 툴팁 띄우기 (TooltipHandler가 부름)
        /// <param name="title">제목</param>
        /// <param name="body">설명 (비면 제목만)</param>
        public void ShowTooltip(LocalizedString title, LocalizedString body)
        {
            if (_tooltip == null)
            {
                Debug.LogError("[UI] TooltipController 연결 안 됨");
                return;
            }
            _tooltip.Show(title, body);
        }
        #endregion

        #region HideTooltip - 툴팁 숨기기
        public void HideTooltip()
        {
            if (_tooltip != null) _tooltip.Hide();
        }
        #endregion
```

**호출/구독 관계:**
- `UI/Map`·`UI/Journal` performed → `OnMap`·`OnJournal` → `ToggleGameMenu`. 닫기 버튼 → `HideGameMenu`
- `HubState.OnOpeningFinished` → `ShowSubtitle(…, NotifyObjective)`, `HubState.Exit` → `HideSubtitle`
- `TooltipHandler` → `ShowTooltip`·`HideTooltip`

---

### `Assets/Scripts/World/NpcWalkController.cs` (새 파일)

```csharp
using UnityEngine;
using UnityEngine.Playables;

namespace Mage
{
    /// <summary>정해진 길을 따라 걷고 끝 점에 닿으면 사라짐. 연출이 정한 시각을 넘기거나 연출이 멈추면(끝남·건너뜀) 지금 자리에서 걷기 시작 (동승 용병이 마차에서 내려 여관으로 걸어감)</summary>
    public sealed class NpcWalkController : MonoBehaviour
    {
        /// <summary>걸어갈 점 (차례대로. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform[] _path;

        /// <summary>걷는 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _speed = 1.6f;

        /// <summary>몸을 돌리는 속도 (도/초)</summary>
        [SerializeField, Min(0f)] float _turnSpeed = 360f;

        /// <summary>걷기 시작을 맞출 연출 디렉터. 비면 켜지자마자 걸음 (인스펙터에서만 연결)</summary>
        [SerializeField] PlayableDirector _director;

        /// <summary>연출이 이 시각(초)을 넘기면 걷기 시작. 연출이 멈춰 있으면(끝남·건너뜀) 바로 (둘째 용병은 조금 늦게)</summary>
        [SerializeField, Min(0f)] float _startTime;

        /// <summary>다음에 갈 점 번호</summary>
        int _index;

        #region Awake - 연결 확인
        void Awake()
        {
            if (_path == null || _path.Length == 0) Debug.LogError("[Npc] 걸어갈 점 연결 안 됨", this);
        }
        #endregion

        #region Update - 게임 시간이 멈췄거나 연출이 시작 시각 전이면 안 움직임. 다음 점으로 걸으며 몸을 돌리고, 끝 점에 닿으면 끔
        void Update()
        {
            if (_path == null || _index >= _path.Length) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || IsWaiting()) return;
            // 다음 점 (높이는 지금 높이)
            Vector3 target = _path[_index].position;
            target.y = transform.position.y;
            // 다음 점까지 방향
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _turnSpeed * dt);
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * dt);
            if ((transform.position - target).sqrMagnitude > 0.0004f) return;
            _index++;
            if (_index >= _path.Length) gameObject.SetActive(false);
        }
        #endregion

        #region IsWaiting - 연출이 돌고 있고 아직 시작 시각 전인지 (일시정지 중엔 연출 시간도 멈춰서 계속 기다림)
        bool IsWaiting() => _director != null && _director.state == PlayState.Playing && _director.time < _startTime;
        #endregion
    }
}
```

**호출/구독 관계:**
- Timeline Mercenaries 트랙이 연출 시작 때 `Stagecoach/Mercenaries`를 켜면(끝난 뒤에도 켜 둠) 두 용병 루트는 착지 자리에서 기다림. 겉모습(`Visual`)은 MercsGetOff 애니메이션이 의자 → 일어섬 → 착지로 움직임. 연출 시간이 `_startTime`(A 25.9, B 26.9)을 넘기거나 연출이 멈추면 걷기 시작, 연출이 끝난 뒤에도 게임 시간으로 계속 걸음
- 애니메이션은 `Visual` 아래만 건드리고 루트는 안 건드려서 걷기와 안 부딪힘

---

### `Assets/Scripts/UI/Views/HubView.cs`

**변경 내용:** `_hud` 아래에 칸 3개, `Hud` 프로퍼티 아래에 프로퍼티 3개

```csharp
using UnityEngine.Localization;   // ← 추가

        /// <summary>도착 대사 말한 사람 (Hub/hub.arrival.speaker. 인스펙터에서만 정함)</summary>
        [SerializeField] LocalizedString _arrivalSpeaker;

        /// <summary>도착 대사 (Hub/hub.arrival.line. 인스펙터에서만 정함)</summary>
        [SerializeField] LocalizedString _arrivalLine;

        /// <summary>첫 목표 (여관에서 방 잡기. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _firstObjective;

        // ...

        /// <summary>도착 대사 말한 사람</summary>
        public LocalizedString ArrivalSpeaker => _arrivalSpeaker;

        /// <summary>도착 대사</summary>
        public LocalizedString ArrivalLine => _arrivalLine;

        /// <summary>첫 목표</summary>
        public ObjectiveSO FirstObjective => _firstObjective;
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 조작 안내 알림, 첫 E에 상호작용 배움, 도착 뒤 자막·첫 목표·알림, 나갈 때 안내·자막 정리

상수 추가 (`QuestRequiredKey` 아래):

```csharp
        /// <summary>이동 튜토리얼 id</summary>
        const string MoveTutorialId = "move";

        /// <summary>둘러보기 튜토리얼 id</summary>
        const string LookTutorialId = "look";

        /// <summary>달리기 튜토리얼 id</summary>
        const string SprintTutorialId = "sprint";

        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 E를 쓰면 배움)</summary>
        const string InteractTutorialId = "interact";

        /// <summary>목표가 바뀌었을 때 토스트 키 ({0} 목표 이름, Menu 테이블)</summary>
        const string ObjectiveUpdatedKey = "menu.journal.updated";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";
```

`OnOpeningFinished` 교체:

```csharp
        #region OnOpeningFinished - 연출이 끝남: 도착 표시, 플레이어·화면 글자 켜고 조작 시작. 첫 목표를 정하고 동승 용병 대사 자막 → 끝나면 임무 일지 갱신 알림
        void OnOpeningFinished()
        {
            _view.Opening.Finished -= OnOpeningFinished;
            _arriving = false;
            Game.Run.HasArrived = true;
            _view.Player.gameObject.SetActive(true);
            _view.Hud.SetActive(true);
            StartControl();
            Game.Run.CurrentObjective = _view.FirstObjective;
            UIManager.Instance.ShowSubtitle(_view.ArrivalSpeaker, _view.ArrivalLine, NotifyObjective);
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
```

`StartControl` 끝에 추가 (region 설명 끝에 ", 안 배운 조작 안내 알림" 붙임):

```csharp
            // 안 배운 조작 안내만 차례로 12시에 (배운 건 TutorialManager가 거름)
            TutorialManager.Instance.Notify(MoveTutorialId);
            TutorialManager.Instance.Notify(LookTutorialId);
            TutorialManager.Instance.Notify(SprintTutorialId);
```

`OnInteracted` 첫 줄에 추가 (region 설명 앞에 "처음 쓰면 상호작용 배움. " 붙임):

```csharp
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
```

`Exit`: `Cursor.lockState = CursorLockMode.None;` 위에 추가 (region 설명 끝에 ", 조작 안내·자막 정리" 붙임):

```csharp
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
```

**호출/구독 관계:**
- 첫 도착: `OnOpeningFinished` → `StartControl`(조작 안내) → 자막 4초 → `NotifyObjective`
- 숲에서 돌아오면 `Reveal` → `StartControl`만 (안 배운 안내가 있으면 다시 나옴)

---

### `Assets/Scripts/UI/Views/ElementCardView.cs`

**변경 내용:** 설명 글자 칸을 빼고 툴팁 칸 추가 (카드엔 이름·그림만)

```csharp
        // 삭제: _descriptionText 필드와 DescriptionText 프로퍼티

        /// <summary>마우스를 올리면 요소 이름·설명 툴팁 (내용은 상태가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] TooltipHandler _tooltip;

        /// <summary>요소 툴팁</summary>
        public TooltipHandler Tooltip => _tooltip;
```

---

### `Assets/Scripts/Flow/CharacterCreationState.cs`

**변경 내용:** `CreateCards`에서 설명 글자 대신 툴팁

```csharp
        #region CreateCards - 요소마다 카드를 만들어 이름·그림을 넣고 이름·설명은 툴팁으로, 켜고 끌 때 고른 수를 다시 셈
        // ...
                card.NameText.StringReference.SetReference(element.Name.TableReference, element.Name.TableEntryReference);
                card.Tooltip.Set(element.Name, element.Description);   // ← DescriptionText 줄을 이걸로 바꿈
```

---

## 입력 변경

### `Assets/InputSystem_Actions.inputactions`

`UI` 묶음에 액션 2개 (Button, 그룹 `Keyboard&Mouse`). 구현 때는 JSON 파일에 액션 2·바인딩 2만 직접 넣음 (기존 줄 그대로, 40줄 추가). 에디터 API로 하면 아래와 같음:

```csharp
// var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
// var ui = asset.FindActionMap("UI", throwIfNotFound: true);
// ui.AddAction("Map", InputActionType.Button).AddBinding("<Keyboard>/m", groups: "Keyboard&Mouse");
// ui.AddAction("Journal", InputActionType.Button).AddBinding("<Keyboard>/j", groups: "Keyboard&Mouse");
// File.WriteAllText(path, asset.ToJson()); AssetDatabase.ImportAsset(path);
```

---

## 에셋 변경

### `Assets/Art/Placeholder/Materials/Npc.mat` (새로)

`Universal Render Pipeline/Lit`, `_BaseColor` (0.60, 0.55, 0.45)

### `Assets/Data/Tutorials/` (새로, TutorialSO)

| 파일 | `_id` | `_prompt` | `_title`·`_body` | `_action` | `_walkSecondsBeforeShow` |
|---|---|---|---|---|---|
| `Move.asset` | move | Tutorial/`tutorial.move.prompt` | `tutorial.move.title`·`.body` | Move | 0 |
| `Look.asset` | look | `tutorial.look.prompt` | `tutorial.look.title`·`.body` | Look | 0 |
| `Sprint.asset` | sprint | `tutorial.sprint.prompt` | `tutorial.sprint.title`·`.body` | Sprint | 6 |
| `Interact.asset` | interact | (비움) | `tutorial.interact.title`·`.body` | Interact | 0 |

### `Assets/Data/Objectives/InnRoom.asset` (새로, ObjectiveSO)

`_id` = `inn_room`, `_title` = Objective/`objective.inn_room.title`, `_description` = Objective/`objective.inn_room.desc`

---

## Prefab 변경

공통: 미리보기 씬(`EditorSceneManager.NewPreviewScene`)에서 만들고 `PrefabUtility.SaveAsPrefabAsset`. 겉모습 도형은 콜라이더 삭제

### `Assets/Prefabs/World/Npc.prefab` (새로)

1. 루트 `Npc`: Add Component → `NpcWalkController` (`_speed` 1.6, `_turnSpeed` 360, `_path`·`_director`·`_startTime`은 씬에서). 콜라이더 없음
2. `Visual/Body`: Capsule, 위치 (0, 0.9, 0), 스케일 (0.7, 0.9, 0.7), `Npc.mat`
3. `Visual/HeadBall`: Sphere, 위치 (0, 1.6, 0), 스케일 0.3, `Npc.mat`
4. `Visual`은 위치 (0, 0, 0). 이 기본값(선 모습)이 마차 용병 애니메이션의 착지 모습

### `Assets/Prefabs/World/Stagecoach.prefab`

1. 루트 아래 `Mercenaries` 새로 (꺼 둠, 위치 (0, 0, 0)): Add Component → `Animator` (컨트롤러 없음, Culling Mode Always Animate). WagonRig와 같은 방식
2. `Mercenaries/MercenaryA`: `Npc.prefab` 인스턴스, 위치 (-1.9, 0, 0.4) (마차 왼쪽 착지 자리), 회전 (0, 0, 0)
3. `Mercenaries/MercenaryB`: `Npc.prefab` 인스턴스, 위치 (-1.9, 0, -0.9), 회전 (0, 0, 0)
4. 앉은 모습은 프리팹 값이 아니라 MercsGetOff 애니메이션이 만듦 (`Visual`을 의자 자리 (1.25, 1.55, 0)로 올리고 앉은 키). 프리팹 값은 착지한 선 모습

### `Assets/Prefabs/UI/TutorialListItem.prefab` (새로)

1. `Assets/Prefabs/UI/Button.prefab`의 프리팹 변형(Variant)으로 만듦
2. LayoutElement Preferred Height 56
3. Add Component → `TutorialListItemView` (`_button` → 루트 Button, `_title` → `Label`의 LocalizeStringEvent)

### `Assets/Prefabs/UI/ElementCard.prefab`

1. `ElementCardView._descriptionText`가 가리키던 설명 LocalizedText 오브젝트 삭제 (카드 크기 220×300은 그대로)
2. 루트 Add Component → `TooltipHandler` (`_title`·`_body` 비움, 상태가 넣음)
3. `ElementCardView._tooltip` → 루트 `TooltipHandler`

---

## 씬 변경

### `Assets/Scenes/Boot.unity`

`UICanvas` 자식 순서를 `Subtitle`, `TutorialPrompt`, `GameMenu`, `Pause`, `Settings`, `Confirm`, `Toast`, `Tooltip`으로. 새 UI 글자는 전부 LocalizedText 프리팹, Raycast Target 끔 (버튼·토글·목록 항목 제외). 런타임에 키를 넣는 글자는 글자를 비워 둠 (프리팹 기본 글자 "LocalizedText"가 안 보이게). Subtitle·TutorialPrompt 패널 Image도 Raycast Target 끔

1. `Subtitle` 새로: 앵커·피벗 (0.5, 0), 위치 (0, 240), 크기 (1400, 120) (토스트 y 120~220보다 위). Add Component → `SubtitleView` (`_root` → `Panel`, `_speaker` → `Panel/Speaker`, `_line` → `Panel/Line`)
   - `Panel`: 꽉 채움, Image (0, 0, 0, 0.5), VerticalLayoutGroup (Padding 12, Spacing 4, 가운데, Control Child Size 켬)
   - `Panel/Speaker`: 글자 26, 색 (1, 0.85, 0.5), 가운데 정렬
   - `Panel/Line`: 글자 34, 가운데 정렬, 줄바꿈 켬
2. `TutorialPrompt` 새로: 앵커·피벗 (0.5, 1), 위치 (0, -60), 크기 (720, 80). Add Component → `TutorialPromptView` (`_root` → `Panel`, `_text` → `Panel/Text`)
   - `Panel`: 꽉 채움, Image (0, 0, 0, 0.55)
   - `Panel/Text`: 꽉 채움, 글자 36, 가운데 정렬
3. `GameMenu` 새로: `Assets/Prefabs/UI/WindowFrame.prefab` 인스턴스 이름을 GameMenu로. 루트에 Add Component → `GameMenuView`, `GameMenuController` (`_view` → 같은 오브젝트 GameMenuView)
   - `Panel` 아래 `Tabs`: HorizontalLayoutGroup (Spacing 12, Control Child Size 켬). `Button.prefab` 5개, 버튼마다 LayoutElement Preferred Width 240 (기본 360이면 5개가 페이지 폭 1300을 넘음): `MapTab`(Menu/`menu.tab.map`), `JournalTab`(`menu.tab.journal`), `TutorialTab`(`menu.tab.tutorial`), `SpellsTab`(`menu.tab.spells`), `CloseButton`(UI/`common.close`)
   - `Panel` 아래 `Pages`: LayoutElement Preferred (1300, 640). 자식 페이지 4개 (꽉 채움):
     - `MapPage`: 가운데 글자 28 Menu/`menu.map.placeholder`
     - `JournalPage`: Add Component → `JournalUIBinder` (`_view` → GameMenuView). 자식 `ObjectiveTitle`(위 가운데 위치 (0, -60) 크기 (1200, 60), 글자 36), `ObjectiveDescription`(위치 (0, -140) 크기 (1200, 300), 글자 28, 왼쪽 위 정렬, 줄바꿈), `JournalEmpty`(가운데 글자 28 `menu.journal.empty`)
     - `TutorialPage`: Add Component → `TutorialListUIBinder` (`_view` → GameMenuView). 자식 `SpoilerToggle`(`Toggle.prefab`, 왼쪽 위 위치 (20, -20) 크기 (520, 48), 라벨 `menu.tutorial.spoiler`, `Control`의 Toggle isOn 끔 (프리팹 기본이 켜짐. 오브젝트는 켜 둠)), `List`(왼쪽 위치 (20, -90) 크기 (420, 530), VerticalLayoutGroup Spacing 6, Control Child Size 켬, Child Force Expand Height 끔), `Detail`(오른쪽 위치 (470, -90) 크기 (800, 530): `Title` 글자 34 위, `Body` 글자 26 줄바꿈 왼쪽 위 정렬), `Empty`(가운데 글자 28 `menu.tutorial.empty`)
     - `SpellsPage`: 가운데 글자 28 `menu.spells.placeholder`
   - GameMenuView 칸 연결: 탭 버튼 4·닫기 → 위 버튼, 페이지 4 → 위 페이지, `_objectiveTitle`·`_objectiveDescription`·`_journalEmpty` → JournalPage 자식, `_spoilerToggle` → SpoilerToggle의 Toggle(`Control`), `_tutorialList` → `List`, `_tutorialItemPrefab` → `TutorialListItem.prefab`, `_tutorialDetail` → `Detail`, `_tutorialTitle`·`_tutorialBody` → Detail 자식, `_tutorialEmpty` → `Empty`
   - GameMenu 꺼 둠
4. `Tooltip` 새로: 꽉 채움. Add Component → `TooltipView`, `TooltipController` (`_view` → 같은 오브젝트, `_canvas` → UICanvas RectTransform, `_offset` (16, -16))
   - `Panel`: 앵커 (0.5, 0.5), 피벗 (0, 1), 크기 (420, 0), Image (0.08, 0.08, 0.1, 0.92) Raycast Target 끔, VerticalLayoutGroup (Padding 14, Spacing 6, Control Child Size 켬, Child Force Expand Height 끔), ContentSizeFitter (Vertical Preferred)
   - `Panel/Title`: 글자 28. `Panel/Body`: 글자 24, 줄바꿈
   - TooltipView 칸: `_panel` → `Panel`, `_title`·`_body` → 자식
   - Tooltip 꺼 둠
5. `Managers`:
   - `UIManager`: `_gameMenu` → GameMenu의 GameMenuController, `_subtitle` → Subtitle의 SubtitleView, `_tooltip` → Tooltip의 TooltipController
   - `TutorialManager`: `_prompt` → TutorialPrompt의 TutorialPromptView
   - `DataManager`: `_tutorials` = Move, Look, Sprint, Interact
6. 저장 → 다시 열어 UIManager 새 칸 3개, TutorialManager `_prompt`, DataManager `_tutorials` 4칸, GameMenuView 칸 전부, SpoilerToggle isOn 꺼짐, 자식 순서 확인

### `Assets/Scenes/Outpost.unity`

1. `Huts/Storage` 이름을 `Inn`으로
2. `Opening/ArrivalPath` 새로 (빈 오브젝트) 자식 점 5개: `P0` (-1.3, 0, -21.5), `P1` (0, 0, -17), `P2` (2, 0, -6), `P3` (6.5, 0, 5), `P4` (7.7, 0, 7.7)
3. `Opening/WagonRig/Stagecoach` 인스턴스의 `Mercenaries/MercenaryA`·`MercenaryB` NpcWalkController (씬 오버라이드. 프리팹은 씬 오브젝트를 못 가리킴):
   - `MercenaryA`: `_path` = P0~P4, `_director` = `Opening`의 PlayableDirector, `_startTime` 25.9
   - `MercenaryB`: `_path` = P0~P4, `_director` = 같음, `_startTime` 26.9
4. `Opening.playable` (27초 → 30초. 마차가 서면 용병이 먼저 내리고 그 뒤 플레이어가 내림):
   - Cameras: 둘째 `SeatRight` 18~25.5 → **18~24.5**, `SeatForward` 클립 하나 더 **23~28.5** (맞은편 의자를 봄. 새 GUID `exposedName` + `director.SetReferenceValue`), `Dismount` 24~27 → **27~30**
   - Passenger: 0~24 → **0~27**
   - `Mercenaries` Activation 트랙 새로: Post-playback **Active**, 클립 0~30, 바인딩 `Opening/WagonRig/Stagecoach/Mercenaries`
   - `MercsGetOff` Animation 트랙 새로: 바인딩 `Mercenaries`의 Animator, 클립 0~26 (AnimationClip은 Timeline 하위 에셋: `AddObjectToAsset` 뒤 `AnimationTrack.CreateClip`). 키 (클립 시간, 키 사이 ClampedAuto):

     | 경로 (A·B 같음) | 0·24.5 (앉음) | 25.1 (일어섬) | 25.5 (벽 넘음) | 25.9 (착지) |
     |---|---|---|---|---|
     | `MercenaryA/Visual` 위치 | (1.25, 1.55, 0) | (1.25, 1.1, 0) | (0.6, 1.9, 0) | (0, 0, 0) |
     | `MercenaryA/Visual/Body` 위치 | (0, 0.45, 0) | (0, 0.9, 0) | — | — |
     | `MercenaryA/Visual/Body` 스케일 | (0.7, 0.45, 0.7) | (0.7, 0.9, 0.7) | — | — |
     | `MercenaryA/Visual/HeadBall` 위치 | (0, 0.8, 0) | (0, 1.6, 0) | — | — |

     `MercenaryB/…`도 같은 키. Vector3는 x·y·z 셋 다 키. `Mercenaries`·용병 루트는 안 움직임 (루트는 NpcWalkController가 움직임)
5. HubView: `_arrivalSpeaker` = Hub/`hub.arrival.speaker`, `_arrivalLine` = Hub/`hub.arrival.line` (SerializedObject로 `m_TableReference.m_TableCollectionName` = `GUID:{Shared Data GUID}`, `m_TableEntryReference.m_KeyId`), `_firstObjective` = `InnRoom.asset`
6. 저장 → 다시 열어 Timeline 길이 30초·트랙 6개(Cameras·Wagon·Overlay·Passenger·Mercenaries·MercsGetOff)·바인딩·카메라 샷 7개 참조, 두 용병 `_path` 5칸·`_director`·`_startTime`, HubView 새 칸 3개 확인

---

## 다국어 변경

1. 시트에 탭 3개 추가 (머리글 15칸), `Hub` 탭에 2줄 추가:

**Tutorial**

| Key | Comment | ko | en |
|---|---|---|---|
| `tutorial.move.prompt` | 화면 12시 이동 안내 (키 글자 임시) | WASD 이동 | WASD Move |
| `tutorial.move.title` | 튜토리얼 탭 제목 | 이동 | Movement |
| `tutorial.move.body` | 튜토리얼 탭 설명 (임시) | W·A·S·D로 걷는다. | Walk with W, A, S and D. |
| `tutorial.look.prompt` | 화면 12시 둘러보기 안내 | 마우스로 둘러보기 | Look around with the mouse |
| `tutorial.look.title` | 튜토리얼 탭 제목 | 시점 | Looking Around |
| `tutorial.look.body` | 튜토리얼 탭 설명 (임시) | 마우스를 움직여 둘러본다. | Move the mouse to look around. |
| `tutorial.sprint.prompt` | 화면 12시 달리기 안내 (키 글자 임시) | Shift 달리기 | Shift Sprint |
| `tutorial.sprint.title` | 튜토리얼 탭 제목 | 달리기 | Sprinting |
| `tutorial.sprint.body` | 튜토리얼 탭 설명 (임시) | 걷는 중에 Shift를 누르고 있으면 달린다. | Hold Shift while walking to sprint. |
| `tutorial.interact.title` | 튜토리얼 탭 제목 | 상호작용 | Interaction |
| `tutorial.interact.body` | 튜토리얼 탭 설명 (임시) | 가까이에서 화면 가운데를 대면 키와 이름이 보인다. E를 눌러 쓴다. | Aim at something nearby to see its key and name. Press E to use it. |

**Menu**

| Key | Comment | ko | en |
|---|---|---|---|
| `menu.tab.map` | 메뉴 창 탭 | 지도 | Map |
| `menu.tab.journal` | 메뉴 창 탭 | 임무 일지 | Journal |
| `menu.tab.tutorial` | 메뉴 창 탭 | 튜토리얼 | Tutorial |
| `menu.tab.spells` | 메뉴 창 탭 | 주문 도감 | Spell Codex |
| `menu.map.placeholder` | 지도 탭 자리 글 (임시) | 지도는 준비 중 (임시) | The map is coming (temp) |
| `menu.spells.placeholder` | 주문 도감 탭에 주문이 없을 때 | 아직 발견한 주문이 없어 | No spells discovered yet |
| `menu.journal.empty` | 임무 일지 탭에 목표가 없을 때 | 진행 중인 목표가 없어 | No current objective |
| `menu.journal.updated` | 목표가 바뀌었을 때 토스트. {0} 목표 이름 | 임무 일지 갱신: {0} | Journal updated: {0} |
| `menu.tutorial.empty` | 튜토리얼 탭에 보일 항목이 없을 때 | 아직 배운 것이 없어 | Nothing learned yet |
| `menu.tutorial.spoiler` | 튜토리얼 탭 토글. 켜면 안 배운 것까지 다 보임 | 모두 보기 (스포일러가 있을 수 있습니다) | Show all (may contain spoilers) |

**Objective**

| Key | Comment | ko | en |
|---|---|---|---|
| `objective.inn_room.title` | 첫 목표 이름 | 여관에서 방 잡기 | Get a room at the inn |
| `objective.inn_room.desc` | 첫 목표 설명 (임시) | 같이 온 용병들 말대로 숙소부터 잡자. 여관은 광장 오른쪽 안쪽에 있다. | Like the mercenaries said, find lodging first. The inn is at the far right of the square. |

**Hub (추가)**

| Key | Comment | ko | en |
|---|---|---|---|
| `hub.arrival.speaker` | 도착 대사 말한 사람 (다른 파티 용병, 임시) | 용병 | Mercenary |
| `hub.arrival.line` | 하차 뒤 동승 용병 대사 (임시) | 숙소부터 잡고 일을 알아보자고. | Let's get a room first, then look for work. |

2. 탭 4개를 키트 CSV 형식으로 `Assets/Localization/CSV/Tutorial.csv`·`Menu.csv`·`Objective.csv`·`Hub.csv`에 씀
3. `LocalizationEditorSettings.CreateStringTableCollection`으로 `Tutorial`·`Menu`·`Objective` 만들기 (`Assets/Localization/Tables`). 만든 뒤 세 컬렉션의 언어 표마다 `LocalizationEditorSettings.SetPreloadTableFlag(table, true)`
4. `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV`
5. `Check Missing Translations`·`Check Missing Glyphs`
6. 세 테이블 언어 표마다 `LocalizationEditorSettings.GetPreloadTableFlag`가 true인지 확인

---

## 주의사항

- 조작 안내 완료 기준은 입력으로 셈: 이동 입력 0.36초(걷기로 약 2m), 마우스 이동량 합 900(감도 0.1이면 약 90°), 움직이며 달리기 1초. 달리기 안내는 달리지 않고 6초 걸은 뒤에 뜸. 그 전에 움직이며 1초 달리면 안내 없이 배움. 게임 시간이 0이면(로딩·일시정지·메뉴) 안 셈
- 배운 기록은 `settings_device.json`의 `_learnedTutorials`. 다시 보려면 그 파일에서 지우거나 파일을 지움
- 메뉴 창은 `GameManager.SetMenuPaused`를 일시정지 메뉴와 같이 씀. 둘이 같이 안 뜨게 `IsWindowOpen`으로 막고, Esc는 메뉴 창을 먼저 닫음. `CanPause`가 false인 화면(타이틀·생성·로딩)에선 M·J 무시
- M·J는 `UI` 묶음이라 일시정지로 `Player` 묶음이 꺼져도 들어옴 (같은 키로 닫기)
- 툴팁 패널과 그 자식은 Raycast Target 끔 (켜면 마우스를 가려 깜빡임). 툴팁은 UICanvas 맨 위
- 자막은 게임 시간 4초 (일시정지 중엔 멈춤). `HideSubtitle`은 끝나면 할 일을 버림 → `HubState.Exit`가 불러서 나간 뒤 목표 알림이 안 옴
- 첫 목표는 연출이 끝날 때 바로 판 데이터에 넣고, 알림만 대사 뒤
- 동승 용병은 마차 프리팹 안 Npc 2개뿐 (따로 걷는 용병 없음). Timeline Mercenaries 트랙이 켜고 Post-playback Active라 연출이 끝나거나 건너뛰어도 켜진 채 걸음. 콜라이더 없음
- 앉음 → 일어섬 → 뛰어내림은 MercsGetOff가 `Visual`만 움직여서 함. Timeline 애니메이션은 연출 중 매 프레임 값을 덮어쓰고 끝나면(Wrap None) 원래 값으로 돌려서, 루트(걷기)와 나누고 `Visual` 기본값을 착지 모습으로 둠. 뛰어내릴 때 캡슐이 벽을 조금 뚫는 건 임시 도형이라 둠
- 연출 순서: 23초 마차 멈춤 → 23~24.5 맞은편 의자로 시점 돌림 → 24.5~25.1 일어섬 → 25.9 착지, A 걷기 시작(B는 26.9) → 27~28.5 플레이어 하차(오른쪽으로 90° 돌며) → 30 조작
- `ElementCardView._descriptionText`를 지워도 프리팹에 남은 옛 칸은 무시됨. 카드의 이름 글자는 그대로
- 입력 파일은 JSON에 액션·바인딩만 직접 넣음 (`ToJson`으로 다시 쓰면 파일 전체 형식이 바뀔 수 있어서 diff를 줄이려고)
- 새 String Table 3개라 Addressables 그룹 파일이 같이 바뀜
- 목표 문구는 `Objective` 테이블 (콘텐츠는 SO 종류마다, 키 앞머리 = 테이블 이름). `Quest` 테이블은 4단계 QuestSO 때 만들고 UI의 `quest.forest_01.title`도 그때 옮김
- 새 테이블 3개는 Preload 켬: Boot 상주 UI(12시 안내·메뉴 창·목표 토스트)만 써서 로딩창 `WaitForLocalizedTexts`가 안 기다림. 안 켜면 처음 뜰 때 한두 프레임 "LocalizedText"나 이전 토스트 글자가 보일 수 있음. 켜면 GameManager.Start가 기다리는 다국어 준비 때 같이 읽힘
- 빌더에서 에셋(SO·프리팹·머티리얼)은 씬을 연 뒤 쓰기 직전에 읽음. LocalizedString 칸은 SerializedObject로 넣고, 저장 뒤 다시 열어 확인
- 지금 E 지점 3개(영입소·게시판·출발 표지판)는 4단계까지 그대로

---

## 진행 추적

- [x] 묶음 Logic — TutorialSO·TutorialPromptView·SubtitleView·ObjectiveSO·GameMenuView·GameMenuController·JournalUIBinder·TutorialListItemView·TutorialListUIBinder·TooltipView·TooltipController·TooltipHandler·NpcWalkController 새로, TutorialManager 채움, DeviceSettingsEntry·SettingsManager(주석)·DataManager·RunEntry·UIManager·HubView·HubState·ElementCardView·CharacterCreationState 수정. 컴파일 에러·경고 0
- [x] 묶음 Input — `UI/Map`(M)·`UI/Journal`(J)
- [x] 묶음 Editor-Localization — 시트 Tutorial·Menu·Objective 탭, Hub 2줄, CSV 4개, 테이블 3개 만들기·Preload·Import, 누락 검사
- [x] 묶음 Prefab·Data — Npc.mat, Npc·TutorialListItem 프리팹 새로, Stagecoach 용병 2명(Npc)·Animator, ElementCard 툴팁, TutorialSO 4개, ObjectiveSO 1개
- [x] 묶음 Editor-Scene — Boot(Subtitle·TutorialPrompt·GameMenu·Tooltip·매니저 연결·DataManager 목록), Outpost(Inn 이름, 길·용병 연결, Timeline 30초·트랙 2개·클립 시간, HubView), 저장 뒤 다시 열어 확인
- [ ] 묶음 Verify — 사용자 확인 목록 전달

---

## 확인 목록 (사용자, 에디터 Play)

- 생성 화면: 요소 카드엔 이름·그림만. 마우스를 올리면 마우스 오른쪽 아래에 이름·설명 툴팁, 화면 오른쪽·아래 끝에선 반대쪽으로 넘어감. 마우스를 떼면 사라짐
- 연출: 맞은편 의자에 용병 2명이 앉아 있음 (좌석 시점·3인칭). 마차가 서면 시점이 맞은편으로 돌고, 용병들이 일어나 왼쪽으로 뛰어내려 바로 정문 쪽으로 걸어감 (같은 용병이 둘로 보이는 순간 없음) → 그 뒤 플레이어가 오른쪽으로 내리며 앞서가는 용병들이 보임
- 연출 중 건너뛰기 → 용병들은 마차 왼쪽에서 바로 걷기 시작
- 조작 시작: 화면 아래 자막 "용병 / 숙소부터 잡고 일을 알아보자고." 약 4초 → 토스트 "임무 일지 갱신: 여관에서 방 잡기"
- 화면 12시 "WASD 이동" → 움직이면 사라짐 → 잠깐 뒤 "마우스로 둘러보기" → 둘러보면 사라짐 → 달리지 않고 6초 걸으면 "Shift 달리기" → 1초 달리면 사라짐
- 배운 기록을 지우고(주의사항) 다시: 둘러보기 안내가 사라진 뒤 바로 움직이며 Shift를 1초 누르면 "Shift 달리기"가 안 뜸, 튜토리얼 탭엔 달리기가 있음
- 용병 2명이 광장을 지나 Inn(옛 Storage) 문 앞까지 걸어가 사라짐
- M → 메뉴 창 지도 탭(자리 글), 게임 멈춤·커서 보임. J → 임무 일지 탭 "여관에서 방 잡기"·설명. 같은 키 다시 → 닫힘. Esc → 닫힘. 탭 버튼으로 바꾸기
- 튜토리얼 탭: 배운 것(이동·시점·달리기, 영입소 등에서 E를 쓴 뒤엔 상호작용)만 목록. 누르면 오른쪽 제목·설명. "모두 보기" 토글 켜면 4개 다
- 주문 도감 탭: "아직 발견한 주문이 없어"
- 메뉴 창이 떠 있을 때 Esc로 일시정지 메뉴가 같이 안 열림. 일시정지 메뉴가 떠 있을 때 M·J 무시
- 숲에 갔다 오거나 새 게임을 해도 배운 조작 안내는 다시 안 나옴
- 영어로 바꾸면 안내·자막·메뉴·툴팁도 영어
- 콘솔 에러·경고 없음

---

## 커밋 메시지

```
Add — 3-3 안내 바탕: 12시 조작 안내·NPC 자막·동승 용병, 메뉴 UI(지도·임무 일지·튜토리얼·주문 도감), BG3식 툴팁

- TutorialManager: 상황 알림을 받아 안 배운 조작 안내만 화면 12시에 하나씩 띄우고, 입력을 보고 실제로 하면 배움으로 이 PC에 저장 (settings_device.json). TutorialSO 4개 (이동·시점·달리기·상호작용)
- 자막 (UIManager.ShowSubtitle). 동승한 다른 파티 용병 2명이 맞은편 의자에 앉아 있다가 마차가 서면 일어나 먼저 뛰어내려 여관(임시 Inn)으로 걸어가고, 그 뒤 플레이어가 내림 (NpcWalkController, Timeline Animation·Activation 트랙, 연출 27초 → 30초)
- 첫 목표 "여관에서 방 잡기" (ObjectiveSO, RunEntry.CurrentObjective), 대사 뒤 임무 일지 갱신 토스트
- 메뉴 창: 지도(M)·임무 일지(J)·튜토리얼·주문 도감 탭, 여는 동안 일시정지, 튜토리얼 탭 '모두 보기(스포일러)' 토글
- BG3식 툴팁 (TooltipHandler·TooltipController). 생성 화면 요소 카드는 이름·그림만, 설명은 툴팁으로
- 다국어: Tutorial·Menu·Objective 테이블 새로 (Preload), Hub에 도착 대사

Why: 도착부터 첫 의뢰까지 튜토리얼처럼 느끼지 않게 이끌 안내 바탕이 먼저 필요함. 4단계(여관·길드·정문)가 이 위에 올라감
Alternatives: 화면 아래 조작 안내 (12시가 한눈에 보여 놓치지 않음), 빛 기둥 출발 힌트 (경비병 대사로 대체), 지도와 임무 일지를 따로 (한 창의 탭으로), 판마다 학습 기록 (이 PC에 계속), 안내 완료를 플레이어 위치로 셈 (입력으로 세면 매니저가 씬을 몰라도 됨)
```
