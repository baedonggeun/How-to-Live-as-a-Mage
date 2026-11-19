# Plan: 로딩창 (팁·바·아무 키로 넘기기), 켤 때 엔진·회사 로고(넘기기), 타이틀 '클릭하여 계속' 연출, 화면 공개(Reveal) 분리

## 현행 분석

- 흐름: `GameManager.Start` → `ShowCover` → `LocalizationSettings.InitializationOperation` 대기 → `ChangeState(start)` → `SwitchState`: `ShowCover` → 이전 상태 `Exit` → `TransitionManager.LoadScene`(내용 씬 언로드 → `Resources.UnloadUnusedAssets` → Additive 로드 → 활성 씬) → `next.Begin`(`OnSceneReady`) → `finally`에서 `HideCover`. 씬이 켜진 뒤 같은 프레임에 덮개가 걷힘
- 덮개: Boot `Managers/TransitionManager/CoverCanvas`(Screen Space Overlay, 정렬 100, CanvasScaler 1920×1080·Match 0.5, GraphicRaycaster)의 `Cover`(전체 화면 검은 Image, Raycast Target, 씬에선 꺼짐). TransitionManager `_cover`(GameObject)를 SetActive로 켜고 끔. 공용 창 `UICanvas`는 정렬 50
- 글자: `Assets/Localization/Prefabs/LocalizedText.prefab` = TextMeshPro(자리 글자 `LocalizedText`, 폰트 NotoSansKR SDF, 크기 36, 200×50) + LocalizeStringEvent + LocaleFontUIBinder(`FontSets/Body`). Title 글자 5개(UI 테이블 `title.*`)
- 테이블 읽기: Localization Settings PreloadBehavior = PreloadSelectedLocale인데 Preload 걸린 테이블이 없음 → 씬 글자는 켜질 때 `GetTableEntryAsync`로 비동기로 읽고, 그동안 자리 글자가 보임. 읽기 작업은 `LocalizedString.CurrentLoadingOperationHandle`. 성공 콜백(RefreshString → TMP 글자)은 작업이 Done이 되는 순간 같이 불림
- 폰트: LocaleFontUIBinder.OnEnable이 `SelectedLocaleAsync`가 끝났으면 바로 바꿈. 다국어 초기화가 씬 로드 전에 끝나서 글자가 켜질 때 늘 바로 바뀜
- 시작 언어: 명령줄 `-language=` → PlayerPrefs `selected-locale` → en
- 상태가 `OnSceneReady`에서 하는 일: Title 버튼·배경음, 생성 카드 생성·`SetReference`·`RefreshString`·배경음, 오프닝 버튼, 전초기지 버튼·`RefreshString`, 숲 `RefreshString`·커서 잠금·`Player/Interact`(E, Button, Hold 없음) 구독 → 전초기지로
- 에디터 Play: PlayModeRouter가 빌드 목록 씬이면 Boot부터 띄우고 시작 상태는 SessionState `Mage.EditorStartScene`
- 데이터: DataManager `_elements`(ElementSO) 검사 패턴. SoundDataSO 에셋 0개, 트윈 패키지 없음
- Unity 스플래시: Player Settings에서 켜짐 (Unity 로고, 빌드에서만 나옴). 엔진이 그려서 플레이어가 못 넘김 (SplashScreen API로 끊는 공식 예제는 옛 Input `Input.anyKeyDown`, 빌드에서만 확인됨). Unity 6는 Personal도 끌 수 있음
- 시간·백그라운드: `Time.timeScale`·`Application.runInBackground`를 바꾸는 코드 없음. Player Settings Run In Background 꺼짐 → 빌드는 창이 뒤로 가면 멈춤 (로딩도). 이동·시점 같은 조작 스크립트 아직 없음
- Input System 설정 에셋 없음 → 기본값 (Process Events In Dynamic Update라 timeScale과 상관없이 입력 받음)
- MCP `editor_playmode_set_run_in_background`: Player Settings Run In Background를 덮어쓰고 나중에 되돌림 (빌드엔 안 들어감)
- Title 씬: Main Camera(Solid Color) + Canvas(Screen Space Overlay, CanvasScaler 1920×1080, TitleView). Canvas 자식 순서 Background(전체 화면 어두운 단색 Image) → Logo(Image + 자식 LogoText `title.game_name`) → Menu(아래 가운데, 버튼 4개). 공개되면 바로 메뉴
- 다국어 원본: Google 시트 'How to Live as a Mage Localization' (탭 = 테이블) → CSV → Import. 키트 Setup은 UI 테이블만 관리 → 새 테이블은 `LocalizationEditorSettings.CreateStringTableCollection`

---

## C# 변경

### `Assets/Scripts/Tip/TipSO.cs` (신규)

**추가 내용:**
- 로딩창 팁 하나. 제목·본문 LocalizedString, 그림 Sprite
- `Assets/Scripts/Tip/` 폴더 새로 생김

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>로딩창 팁 하나 (조작·기능 설명). 씬 전환 때 무작위로 하나 보임</summary>
    [CreateAssetMenu(fileName = "Tip", menuName = "Mage/Tip")]
    public sealed class TipSO : ScriptableObject
    {
        /// <summary>팁 제목 (Tip 테이블 tip.{이름}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>팁 본문 (Tip 테이블 tip.{이름}.body)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>팁 그림 (조작 그림 등). 비면 안 보임</summary>
        [SerializeField] Sprite _picture;

        /// <summary>팁 제목</summary>
        public LocalizedString Title => _title;

        /// <summary>팁 본문</summary>
        public LocalizedString Body => _body;

        /// <summary>팁 그림. 비면 null</summary>
        public Sprite Picture => _picture;
    }
}
```

**호출/구독 관계:**
- DataManager `_tips`에 연결. TransitionManager.PickTip이 고르고 ShowLoading이 로딩창에 넣음

---

### `Assets/Scripts/UI/Views/LoadingView.cs` (신규)

**추가 내용:** 로딩창 참조 (View). Boot `CoverCanvas/Cover`에 붙음

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>로딩창 참조 (Boot CoverCanvas/Cover에 붙음)</summary>
    public sealed class LoadingView : MonoBehaviour
    {
        /// <summary>로딩창 전체. 투명도로 보이고 숨고, 보일 때만 클릭을 막음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>로딩 바 채움 (Image Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _progressFill;

        /// <summary>팁 묶음 (그림·제목·본문). 팁이 없으면 꺼짐 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tipRoot;

        /// <summary>팁 묶음 투명도 (Tip의 Canvas Group). 팁 글자를 다 읽기 전엔 0 → 덮개가 이미 보일 때(첫 진입) 자리 글자가 안 비침 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _tipGroup;

        /// <summary>팁 그림. 팁에 그림이 없으면 꺼짐 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _tipPicture;

        /// <summary>팁 제목 글자. 테이블·키는 TransitionManager가 넣음 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tipTitle;

        /// <summary>팁 본문 글자. 테이블·키는 TransitionManager가 넣음 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tipBody;

        /// <summary>'아무 키나 눌러 계속' 깜빡임. 로딩이 끝나면 켜짐 (인스펙터에서만 연결)</summary>
        [SerializeField] BlinkSequence _prompt;

        /// <summary>로딩이 끝났을 때 낼 소리. 비면 무음 (인스펙터에서만 연결)</summary>
        [SerializeField] SoundDataSO _readySound;

        /// <summary>로딩창 전체</summary>
        public CanvasGroup Group => _group;

        /// <summary>로딩 바 채움</summary>
        public Image ProgressFill => _progressFill;

        /// <summary>팁 묶음</summary>
        public GameObject TipRoot => _tipRoot;

        /// <summary>팁 묶음 투명도</summary>
        public CanvasGroup TipGroup => _tipGroup;

        /// <summary>팁 그림</summary>
        public Image TipPicture => _tipPicture;

        /// <summary>팁 제목 글자</summary>
        public LocalizeStringEvent TipTitle => _tipTitle;

        /// <summary>팁 본문 글자</summary>
        public LocalizeStringEvent TipBody => _tipBody;

        /// <summary>'아무 키나 눌러 계속' 깜빡임</summary>
        public BlinkSequence Prompt => _prompt;

        /// <summary>로딩이 끝났을 때 낼 소리. 비면 null</summary>
        public SoundDataSO ReadySound => _readySound;
    }
}
```

**호출/구독 관계:** TransitionManager `_loading`만 씀

---

### `Assets/Scripts/UI/Sequences/BlinkSequence.cs` (신규)

**추가 내용:**
- 켜져 있는 동안 CanvasGroup 투명도를 1 ↔ 최소값으로 오르내림, 꺼지면 0
- `Assets/Scripts/UI/Sequences/` 폴더 새로 생김

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>켜져 있는 동안 CanvasGroup 투명도를 깜빡임 ('아무 키나 눌러 계속' 같은 안내). 꺼지면 안 보이게 둠</summary>
    public sealed class BlinkSequence : MonoBehaviour
    {
        /// <summary>깜빡일 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>밝음 → 어두움 → 밝음 한 번 걸리는 시간 (초)</summary>
        [SerializeField, Min(0.1f)] float _period = 1.2f;

        /// <summary>가장 어두울 때 투명도</summary>
        [SerializeField, Range(0f, 1f)] float _minAlpha = 0.2f;

        /// <summary>켜진 시각 (실제 시간). 켤 때마다 밝게 시작하려고</summary>
        float _startTime;

        #region Awake - 묶음 연결 확인 (컴포넌트가 꺼져 있어도 오브젝트가 켜지면 불림)
        void Awake()
        {
            if (_group == null) Debug.LogError($"[Blink] {name}에 CanvasGroup 연결 안 됨", this);
        }
        #endregion

        #region OnEnable - 밝게 시작
        void OnEnable()
        {
            _startTime = Time.unscaledTime;
            if (_group != null) _group.alpha = 1f;
        }
        #endregion

        #region Update - 시간에 따라 투명도를 오르내림 (일시정지 중에도 깜빡이게 실제 시간)
        void Update()
        {
            if (_group == null) return;
            // 0(밝음) → 1(어두움) → 0을 오가는 값. 한 번 오가는 데 _period초
            float t = Mathf.PingPong((Time.unscaledTime - _startTime) * 2f / _period, 1f);
            _group.alpha = Mathf.Lerp(1f, _minAlpha, t);
        }
        #endregion

        #region OnDisable - 안 보이게
        void OnDisable()
        {
            if (_group != null) _group.alpha = 0f;
        }
        #endregion
    }
}
```

**호출/구독 관계:** 로딩창 Prompt는 TransitionManager가 `enabled`로 켜고 끔 (WaitForContinue에서 켬, Open·HideCover에서 끔). Title `ClickPrompt`는 켜진 채 시작하고 TitleIntroSequence가 끔

---

### `Assets/Scripts/UI/Sequences/LogoSequence.cs` (신규)

**추가 내용:** 로고 그림 CanvasGroup들을 차례로(엔진 → 회사) 나타남 → 머묾 → 사라짐. 바탕 Image는 안 건드림. Skip이 오면 남은 로고 전부 바로 끝

```csharp
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>게임을 켤 때 로고 연출: 로고마다 나타남 → 머묾 → 사라짐을 차례로 (엔진 → 회사. Boot CoverCanvas/Logo에 붙음). 검은 바탕은 그대로 둠. Skip이 오면 남은 로고 전부 끝</summary>
    public sealed class LogoSequence : MonoBehaviour
    {
        /// <summary>차례로 보일 로고 그림 묶음 (엔진 → 회사). 투명도로 나타나고 사라짐 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup[] _logos;

        /// <summary>로고 하나가 나타나는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeInSeconds = 0.6f;

        /// <summary>로고 하나가 다 나타난 채 머무는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _holdSeconds = 1.2f;

        /// <summary>로고 하나가 사라지는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeOutSeconds = 0.6f;

        /// <summary>넘기기 요청이 왔는지. 켜지면 남은 나타남·머묾·사라짐이 바로 끝남</summary>
        bool _skipped;

        #region Play - 로고마다 나타남 → 머묾 → 사라짐. 끝나면 로고는 다 안 보이고 바탕만 남음. Skip이 오면 다음 프레임에 끝남
        public IEnumerator Play()
        {
            if (_logos == null || _logos.Length == 0)
            {
                Debug.LogError("[Logo] 로고 CanvasGroup 연결 안 됨", this);
                yield break;
            }
            _skipped = false;
            foreach (CanvasGroup logo in _logos)
            {
                if (logo == null)
                {
                    Debug.LogError("[Logo] 로고 목록에 빈 칸 있음", this);
                    continue;
                }
                yield return Fade(logo, 0f, 1f, _fadeInSeconds);
                // elapsed: 다 나타난 뒤 지난 시간 (초). 넘기면 바로 끝
                for (float elapsed = 0f; elapsed < _holdSeconds && !_skipped; elapsed += Time.unscaledDeltaTime)
                {
                    yield return null;
                }
                yield return Fade(logo, 1f, 0f, _fadeOutSeconds);
            }
        }
        #endregion

        #region Skip - 남은 로고 연출을 끊음 (로고 화면은 TransitionManager가 끔)
        public void Skip() => _skipped = true;
        #endregion

        #region Fade - 로고 투명도를 시간에 맞춰 바꿈 (실제 시간 기준). 넘기면 바로 안 보이게 하고 끝
        /// <param name="logo">바꿀 로고</param>
        /// <param name="from">시작 투명도</param>
        /// <param name="to">끝 투명도</param>
        /// <param name="seconds">걸리는 시간 (초). 0이면 바로 끝 값</param>
        IEnumerator Fade(CanvasGroup logo, float from, float to, float seconds)
        {
            // elapsed: 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < seconds && !_skipped; elapsed += Time.unscaledDeltaTime)
            {
                logo.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }
            logo.alpha = _skipped ? 0f : to;
        }
        #endregion
    }
}
```

**호출/구독 관계:** TransitionManager.PlayLogo가 `Play()`를 코루틴으로 돌리고, 그동안 넘기기 버튼이 오면 `Skip()`

---

### `Assets/Scripts/UI/Sequences/TitleIntroSequence.cs` (신규)

**추가 내용:** Title 시작 연출. '클릭하여 계속' 화면에서 시작 → Play가 오면 안내를 끄고 화면 묶음(`Pan`)을 위로 밀어 화면이 아래로 내려가 보이게 → 제목·메뉴 묶음(`Main`)이 나타난 뒤 누를 수 있게

```csharp
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>타이틀 시작 연출: '클릭하여 계속' 화면 → 화면이 아래로 내려감 → 제목·메뉴가 나타남 (Title Canvas에 붙음)</summary>
    public sealed class TitleIntroSequence : MonoBehaviour
    {
        /// <summary>위로 밀릴 화면 묶음 (Sky·ClickPrompt, 두 화면 높이). 위로 밀려서 화면이 아래로 내려가 보임 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _pan;

        /// <summary>'클릭하여 계속' 깜빡임. Play가 오면 끔 (인스펙터에서만 연결)</summary>
        [SerializeField] BlinkSequence _prompt;

        /// <summary>제목·메뉴 묶음. 화면이 다 내려간 뒤 나타나고 그때부터 누를 수 있음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _main;

        /// <summary>화면이 내려가는 거리 (CanvasScaler 기준 해상도 단위. 1080 = 한 화면)</summary>
        [SerializeField, Min(0f)] float _panDistance = 1080f;

        /// <summary>화면이 내려가는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _panSeconds = 1.2f;

        /// <summary>제목·메뉴가 나타나는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeInSeconds = 0.6f;

        /// <summary>화면 묶음의 처음 위치 ('클릭하여 계속' 화면)</summary>
        Vector2 _panStart;

        #region Awake - 연결 확인, 처음 상태로: 화면 위, 안내 깜빡임, 제목·메뉴 숨김·못 누름
        void Awake()
        {
            if (_pan == null || _prompt == null || _main == null)
            {
                Debug.LogError("[TitleIntro] 화면 묶음·안내·제목 메뉴 중 연결 안 된 것 있음", this);
                return;
            }
            _panStart = _pan.anchoredPosition;
            _prompt.enabled = true;
            _main.alpha = 0f;
            _main.interactable = false;
            _main.blocksRaycasts = false;
        }
        #endregion

        #region Play - 연출 시작 (TitleState가 '클릭하여 계속'을 받으면 한 번 부름)
        public void Play()
        {
            if (_pan == null || _prompt == null || _main == null) return;
            StartCoroutine(PlayRoutine());
        }
        #endregion

        #region PlayRoutine - 안내 끔 → 화면 내림(천천히 출발·도착) → 제목·메뉴 나타남 → 누를 수 있게 (실제 시간 기준)
        IEnumerator PlayRoutine()
        {
            _prompt.enabled = false;
            // 화면 묶음의 끝 위치. 위로 _panDistance만큼 밀림 = 화면이 아래로 내려감
            Vector2 panEnd = _panStart + Vector2.up * _panDistance;
            // elapsed: 내려가기 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < _panSeconds; elapsed += Time.unscaledDeltaTime)
            {
                _pan.anchoredPosition = Vector2.Lerp(_panStart, panEnd, Mathf.SmoothStep(0f, 1f, elapsed / _panSeconds));
                yield return null;
            }
            _pan.anchoredPosition = panEnd;
            // elapsed: 나타나기 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < _fadeInSeconds; elapsed += Time.unscaledDeltaTime)
            {
                _main.alpha = elapsed / _fadeInSeconds;
                yield return null;
            }
            _main.alpha = 1f;
            _main.interactable = true;
            _main.blocksRaycasts = true;
        }
        #endregion
    }
}
```

**호출/구독 관계:** TitleView `Intro`로 참조, TitleState.OnPressStart가 `Play()`

---

### `Assets/Scripts/Managers/TransitionManager.cs`

**변경 내용:** 파일 전체 교체
- `_cover`(GameObject) → `_loading`(LoadingView). 덮개는 늘 켜 두고 CanvasGroup 투명도·blocksRaycasts로 보이고 숨김
- `_logo`(LogoSequence) 추가: ShowLogo(로고가 도는 동안 넘기기 버튼을 들음. 로딩이 남았으면 로고가 끝날 때 로고 화면을 꺼서 밑의 로딩창을 보임) / WaitForLogo / LogoShown
- ShowCover(팁 없이) / ShowLoading(팁 고르고 글자를 다 읽은 뒤 보임) / HideCover(로고 화면도 끔)
- Open·HideCover: 덮개가 떠 있는 동안 `Time.timeScale` 0, (빌드만) `Application.runInBackground` 켬 → 걷힐 때 1·원래 값
- SetProgress, WaitForLocalizedTexts(+FindLoadingText), WaitForContinue(안내 뒤 0.3초부터), IsContinueControl(public static, TitleState도 씀), PickTip 추가. CanLoad는 그대로
- LoadScene: 바 끝 위치를 받아 씬을 읽는 동안 `AsyncOperation.progress`로 바를 채움. 로드 시작 실패(null) 확인

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>씬을 어떻게 옮길지 담당: 로딩창(덮개·바·팁·넘기기 안내), 켤 때 로고, 내용 씬 언로드·Additive 로드, 글자 준비 대기, 덮개 동안 게임 멈춤. 언제 옮길지는 GameManager가 정함</summary>
    public sealed class TransitionManager : MonoSingleton<TransitionManager>
    {
        /// <summary>글자가 다 채워지길 기다리는 최대 시간 (초). 넘으면 에러를 남기고 그냥 진행 (로딩창에 갇히지 않게)</summary>
        const float TextTimeoutSeconds = 10f;

        /// <summary>넘기기 안내가 뜬 뒤 입력을 안 세는 시간 (초). 로딩 중 연타하던 키로 바로 넘어가지 않게</summary>
        const float ContinueGraceSeconds = 0.3f;

        /// <summary>씬 읽기가 끝났을 때 AsyncOperation.progress 값 (Unity가 정함). 그 뒤 1까지는 활성화</summary>
        const float SceneReadProgress = 0.9f;

        /// <summary>로딩창 (CoverCanvas/Cover). 늘 켜 두고 투명도로 숨김 → 숨긴 동안에도 팁 글자를 읽음</summary>
        [SerializeField] LoadingView _loading;

        /// <summary>켤 때 로고 화면 (CoverCanvas/Logo, 로딩창보다 위)</summary>
        [SerializeField] LogoSequence _logo;

        /// <summary>게임을 켤 때 로고를 틀었는지 (Title부터 시작). 첫 진입 로딩창에 팁을 깔지 정할 때 씀</summary>
        public bool LogoShown { get; private set; }

        /// <summary>지금 떠 있는 내용 씬. Boot만 있을 땐 비어 있음</summary>
        Scene _contentScene;

        /// <summary>로고 연출 코루틴. 안 틀었거나 끝났으면 null</summary>
        Coroutine _logoRoutine;

        /// <summary>로딩을 끝내고 로고가 끝나길 기다리는 중인지 (WaitForLogo 안). 그러면 로고가 끝나도 로고 화면(검은 바탕)을 그대로 둠 → 로딩창이 한 프레임 비치지 않고 바로 공개</summary>
        bool _waitingForLogo;

        /// <summary>바로 전에 보인 팁 위치 (DataManager 팁 목록 순서). 아직 없으면 -1</summary>
        int _lastTipIndex = -1;

        /// <summary>프로젝트 설정 Run In Background 값. 덮개가 걷히면 이 값으로 되돌림 (빌드만)</summary>
        bool _runInBackgroundDefault;

        #region InitSingleton - 로딩창·로고 연결 확인, Run In Background 원래 값 기억, 처음엔 둘 다 숨김
        protected override void InitSingleton()
        {
            if (_loading == null) Debug.LogError("[Transition] 로딩창 연결 안 됨");
            if (_logo == null) Debug.LogError("[Transition] 로고 연결 안 됨");
            // HideCover가 이 값으로 되돌려서 먼저 기억
            _runInBackgroundDefault = Application.runInBackground;
            HideCover();
        }
        #endregion

        #region ShowLogo - 로고 화면을 켜고 연출 시작 (게임을 켤 때 한 번)
        public void ShowLogo()
        {
            if (_logo == null) return;
            LogoShown = true;
            _logo.gameObject.SetActive(true);
            _logoRoutine = StartCoroutine(PlayLogo());
        }
        #endregion

        #region PlayLogo - 로고 연출을 끝까지 틂. 그동안 넘기기 버튼이 오면 남은 로고를 넘김. 끝났을 때 로딩이 남았으면 로고 화면을 꺼서 밑의 로딩창을 보임
        IEnumerator PlayLogo()
        {
            // 로고가 도는 동안만 듣는 넘기기 구독 (로딩 중에도 넘김). 블록을 나가면 해제
            using (InputSystem.onAnyButtonPress.Where(IsContinueControl).CallOnce(_ => _logo.Skip()))
            {
                yield return _logo.Play();
            }
            // 로딩이 다 끝나 기다리는 중이면 로고 화면을 둔 채 HideCover가 같이 끔
            if (!_waitingForLogo) _logo.gameObject.SetActive(false);
            _logoRoutine = null;
        }
        #endregion

        #region WaitForLogo - 로고 연출이 끝날(또는 넘길) 때까지 기다림. 안 틀었거나 끝났으면 바로 끝
        public IEnumerator WaitForLogo()
        {
            _waitingForLogo = true;
            while (_logoRoutine != null) yield return null;
            _waitingForLogo = false;
        }
        #endregion

        #region ShowCover - 팁 없이 로딩창 켜기 (게임을 켤 때 로고 밑, 첫 진입, 키 입력을 안 기다리는 전환)
        public void ShowCover()
        {
            if (_loading == null) return;
            _loading.TipRoot.SetActive(false);
            Open();
        }
        #endregion

        #region ShowLoading - 팁을 하나 골라 글자를 다 읽은 뒤 로딩창 켜기 (읽는 동안 팁은 투명, 이전 화면 그대로)
        public IEnumerator ShowLoading()
        {
            if (_loading == null) yield break;
            // 이번에 보일 팁. 목록이 비었거나 빈 칸이면 null
            TipSO tip = PickTip();
            if (tip != null)
            {
                // 팁 묶음은 HideCover가 꺼 둠. 꺼진 채 키를 넣고 켜야 이전 팁 키를 다시 안 읽음
                _loading.TipTitle.StringReference.SetReference(tip.Title.TableReference, tip.Title.TableEntryReference);
                _loading.TipBody.StringReference.SetReference(tip.Body.TableReference, tip.Body.TableEntryReference);
                _loading.TipPicture.sprite = tip.Picture;
                _loading.TipPicture.gameObject.SetActive(tip.Picture != null);
                // 덮개가 이미 보일 때(첫 진입) 자리 글자가 안 비치게 다 읽을 때까지 투명
                _loading.TipGroup.alpha = 0f;
                _loading.TipRoot.SetActive(true);
                yield return WaitForLocalizedTexts();
                _loading.TipGroup.alpha = 1f;
            }
            Open();
        }
        #endregion

        #region Open - 로딩창 보이기·클릭 막기, 바 비우기, 넘기기 안내 끄기, 게임 시간 멈춤, (빌드) 창이 뒤로 가도 계속 돎
        void Open()
        {
            SetProgress(0f);
            _loading.Prompt.enabled = false;
            _loading.Group.alpha = 1f;
            _loading.Group.blocksRaycasts = true;
            // 덮개 밑에서 물리·애니메이션·적이 안 돌게. 로딩창 연출은 실제 시간이라 그대로 돎
            Time.timeScale = 0f;
            // Alt+Tab 해도 로딩이 이어지게. 에디터는 MCP·사용자 설정과 안 싸우게 빌드에서만
            if (!Application.isEditor) Application.runInBackground = true;
        }
        #endregion

        #region HideCover - 게임 시간·Run In Background 되돌림, 로딩창(투명도 0·클릭 통과)과 로고 화면 숨김, 팁·넘기기 안내 끄기
        public void HideCover()
        {
            Time.timeScale = 1f;
            if (!Application.isEditor) Application.runInBackground = _runInBackgroundDefault;
            if (_logo != null) _logo.gameObject.SetActive(false);
            if (_loading == null) return;
            _loading.Group.alpha = 0f;
            _loading.Group.blocksRaycasts = false;
            _loading.Prompt.enabled = false;
            _loading.TipRoot.SetActive(false);
        }
        #endregion

        #region SetProgress - 로딩 바 채우기 (0~1, 범위 밖은 자름)
        /// <param name="progress">채울 비율</param>
        public void SetProgress(float progress)
        {
            if (_loading != null) _loading.ProgressFill.fillAmount = Mathf.Clamp01(progress);
        }
        #endregion

        #region WaitForLocalizedTexts - 켜진 다국어 글자가 다 채워질 때까지 기다림 (프레임마다 다시 찾아 늦게 켜진 글자도 잡음). 시간을 넘기면 에러 남기고 끝냄
        public IEnumerator WaitForLocalizedTexts()
        {
            // 이 시각이 지나면 그만 기다림 (실제 시간)
            float deadline = Time.realtimeSinceStartup + TextTimeoutSeconds;
            // 아직 테이블을 읽는 글자. 없으면 null
            LocalizeStringEvent pending;
            while ((pending = FindLoadingText()) != null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogError($"[Transition] {TextTimeoutSeconds}초 안에 글자를 못 채움: {pending.name}. 그냥 진행", pending);
                    yield break;
                }
                yield return null;
            }
        }
        #endregion

        #region FindLoadingText - 켜진 글자 중 테이블을 아직 읽는 것 하나. 없으면 null
        static LocalizeStringEvent FindLoadingText()
        {
            foreach (var text in Object.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None))
            {
                // 이 글자의 테이블 읽기 작업. 키가 비었거나 꺼져 있으면 빈 핸들
                var load = text.StringReference.CurrentLoadingOperationHandle;
                if (load.IsValid() && !load.IsDone) return text;
            }
            return null;
        }
        #endregion

        #region WaitForContinue - 끝 소리를 내고 넘기기 안내를 깜빡이며, 0.3초 뒤부터 넘기기 버튼(IsContinueControl)을 새로 누를 때까지 기다림
        public IEnumerator WaitForContinue()
        {
            if (_loading == null) yield break;
            // 비어 있으면 AudioManager가 무시함
            AudioManager.Instance.Play(_loading.ReadySound);
            _loading.Prompt.enabled = true;
            // 로딩 중 연타하던 키가 안내가 뜨자마자 들어오지 않게 잠깐 안 셈 (덮개 동안 timeScale 0이라 실제 시간)
            yield return new WaitForSecondsRealtime(ContinueGraceSeconds);
            // 버튼이 눌렸는지 (아래 구독 콜백이 켬)
            bool pressed = false;
            // 다음에 눌리는 넘기기 버튼 하나만 듣는 구독. 블록을 나가면 해제
            using (InputSystem.onAnyButtonPress.Where(IsContinueControl).CallOnce(_ => pressed = true))
            {
                while (!pressed) yield return null;
            }
        }
        #endregion

        #region IsContinueControl - 넘기기로 셀 버튼인지. 마우스·패드 버튼은 다 셈. 키보드는 수정자 키(Shift·Ctrl·Alt·Win), 수정자를 누른 채 누른 키(Alt+Tab, Shift+Tab = Steam 오버레이), 스크린샷 키(F12 = Steam, PrintScreen)를 뺌
        /// <param name="control">새로 눌린 버튼</param>
        public static bool IsContinueControl(InputControl control)
        {
            // 키보드 키가 아니면(마우스·패드 버튼) 셈
            if (control is not KeyControl key) return true;
            if (key.keyCode is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt or Key.LeftMeta or Key.RightMeta
                or Key.F12 or Key.PrintScreen) return false;
            // 이 키가 붙은 키보드. 키보드가 아니면 수정자를 못 봐서 그냥 셈
            if (key.device is not Keyboard keyboard) return true;
            return !(keyboard.shiftKey.isPressed || keyboard.ctrlKey.isPressed || keyboard.altKey.isPressed
                || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed);
        }
        #endregion

        #region PickTip - 팁 목록에서 무작위 하나. 둘 이상이면 바로 전 팁은 뺌. 목록이 비면 null
        TipSO PickTip()
        {
            // 팁 목록 (DataManager 연결 순서)
            IReadOnlyList<TipSO> tips = DataManager.Instance.Tips;
            if (tips.Count == 0) return null;
            // 바로 전 팁을 뺄지 (팁이 하나뿐이면 못 뺌)
            bool skipLast = tips.Count > 1 && _lastTipIndex >= 0 && _lastTipIndex < tips.Count;
            // 고른 위치. 바로 전 팁을 빼면 한 칸 적게 뽑고, 그 자리부터는 한 칸 뒤로 밂
            int index = Random.Range(0, skipLast ? tips.Count - 1 : tips.Count);
            if (skipLast && index >= _lastTipIndex) index++;
            _lastTipIndex = index;
            return tips[index];
        }
        #endregion

        #region CanLoad - 씬을 읽을 수 있는지 확인 (빌드 목록 기준)
        /// <param name="sceneName">확인할 씬 이름</param>
        public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
        #endregion

        #region LoadScene - 지금 내용 씬을 내리고 새 씬을 Additive로 올린 뒤 활성 씬으로 지정. 올리는 동안 바를 0 → progressEnd로 채움
        /// <param name="sceneName">올릴 씬 이름</param>
        /// <param name="progressEnd">씬이 다 올라왔을 때 바 위치 (0~1)</param>
        public IEnumerator LoadScene(string sceneName, float progressEnd)
        {
            // 먼저 내려서 두 씬이 동시에 메모리에 있는 순간을 없앰 (덮개가 덮인 상태. 그동안 바는 0)
            if (_contentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(_contentScene);
                // 언로드는 에셋을 안 지워서 직접 정리 (Resources 폴더와 상관없는 API)
                yield return Resources.UnloadUnusedAssets();
            }

            // 씬 올리기 작업. progress 0~0.9가 읽기, 0.9 → 1이 활성화(Awake·OnEnable, 한 프레임)
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[Transition] {sceneName} 씬 로드 시작 실패");
                yield break;
            }
            while (!load.isDone)
            {
                SetProgress(load.progress / SceneReadProgress * progressEnd);
                yield return null;
            }
            SetProgress(progressEnd);
            _contentScene = SceneManager.GetSceneByName(sceneName);
            // 조명·스카이박스·런타임 생성 오브젝트가 내용 씬을 따르게
            SceneManager.SetActiveScene(_contentScene);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- GameManager.Start: ShowLogo(Title부터일 때), ShowCover
- GameManager.SwitchState: ShowLoading(키 입력 대기 상태로 갈 때·로고 밑 첫 진입) / ShowCover(나머지) → LoadScene(씬 로드 몫) → SetProgress → WaitForLocalizedTexts → WaitForLogo(첫 진입) → WaitForContinue(키 입력 대기 상태) → HideCover
- TitleState.Reveal: IsContinueControl
- PickTip → DataManager.Tips. PlayLogo·WaitForContinue → `InputSystem.onAnyButtonPress` + IsContinueControl. PlayLogo → LogoSequence.Play·Skip. WaitForContinue → AudioManager.Play

---

### `Assets/Scripts/Managers/GameManager.cs`

**변경 내용:**

1. `EditorStartSceneKey` 상수 아래에 필드 추가 (Boot `Managers/GameManager` 인스펙터에서 조절)

```csharp
        /// <summary>로딩 바에서 씬 로드 단계가 차지하는 몫 (0~1). 콘솔 [Flow] 로딩 시간 로그로 잰 비율에 맞춤</summary>
        [SerializeField, Range(0f, 1f)] float _sceneLoadShare = 0.8f;

        /// <summary>로딩 바에서 씬에 정보 적용 단계가 차지하는 몫 (0~1). 다국어 적용은 나머지</summary>
        [SerializeField, Range(0f, 1f)] float _sceneReadyShare = 0.1f;
```

2. Start 전체 교체 — 시작 상태를 먼저 고르고, Title부터면 로고를 켠 뒤 다국어 대기

```csharp
        #region Start - 매니저 확인 → 시작 상태 고르기 → (타이틀부터면) 로고 → 다국어 준비 대기 → 시작 상태로 진입 (매니저끼리 Awake 순서가 없어서 Start에서)
        IEnumerator Start()
        {
            // 하나라도 빠지면 Boot 씬 구성이 잘못된 것 (빠진 쪽은 Instance가 에러를 남김)
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && SettingsManager.Instance != null
                && AudioManager.Instance != null && TutorialManager.Instance != null;
            if (!allReady) yield break;

            // 시작할 흐름 상태 (빌드: Title, 에디터: 편집 중이던 씬)
            FlowStateBase start = CreateStartState();
            if (start == null) yield break;

            // 게임을 켤 때(타이틀부터)만 엔진·회사 로고. 로고가 도는 동안 다국어 준비·첫 씬 로드를 진행 (먼저 끝나거나 넘기면 밑의 로딩창이 보임)
            if (start is TitleState) TransitionManager.Instance.ShowLogo();
            // 로고 밑 검은 덮개. 에디터에서 중간 씬부터 켜면 이게 화면을 덮음
            TransitionManager.Instance.ShowCover();
            // 다국어 초기화 작업
            var localization = LocalizationSettings.InitializationOperation;
            yield return localization;
            if (localization.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError("[Flow] 다국어 초기화 실패. 화면 글자가 안 나올 수 있음");

            // 타이틀·생성 화면이 아니면 중간부터 시작한 것 → 디버그 데이터
            Run = start is TitleState or CharacterCreationState ? new RunEntry() : RunEntry.CreateDebug(DataManager.Instance.Elements);
            ChangeState(start);
        }
        #endregion
```

3. SwitchState 전체 교체

```csharp
        #region SwitchState - 덮개(팁 로딩창 또는 검은 덮개) → 이전 상태 Exit → 씬 교체 → 새 상태 Begin → 글자 대기 → 로고·키 입력 대기 → 덮개 해제 → 새 상태 Reveal
        /// <param name="next">들어갈 상태</param>
        IEnumerator SwitchState(FlowStateBase next)
        {
            // 덮개·씬 교체 담당
            TransitionManager transition = TransitionManager.Instance;
            // 게임을 켠 뒤 첫 진입인지. 키 입력을 안 기다림 (Title부터면 로고가 덮음)
            bool firstEntry = _state == null;
            // 넘기기 버튼을 기다릴지. 들어갈 상태가 정함 (첫 진입은 안 기다림)
            bool waitForContinue = !firstEntry && next.WaitsForContinue;
            // 팁 로딩창을 띄울지: 넘기기 버튼을 기다릴 때, 또는 로고 밑 첫 진입 (로고가 로딩보다 먼저 끝나거나 넘기면 보임)
            bool showTip = waitForContinue || (firstEntry && transition.LogoShown);
            _isSwitching = true;
            try
            {
                if (showTip) yield return transition.ShowLoading();
                else transition.ShowCover();
                Debug.Log($"[Flow] {_state?.GetType().Name ?? "시작"} → {next.GetType().Name}");
                _state?.Exit();
                _state = next;
                // 단계 시작 시각 (실제 시간). 단계마다 걸린 시간을 로그로 남겨 바 몫을 맞출 때 씀
                float stepStart = Time.realtimeSinceStartup;
                yield return transition.LoadScene(next.SceneName, _sceneLoadShare);
                // 씬 로드에 걸린 시간 (초)
                float loadSeconds = Time.realtimeSinceStartup - stepStart;
                stepStart = Time.realtimeSinceStartup;
                // (나중) 상태별 준비 작업 자리: 던전 생성·스폰. 덮개가 덮인 채로
                next.Begin(this);
                transition.SetProgress(Mathf.Min(1f, _sceneLoadShare + _sceneReadyShare));
                // 씬에 정보 적용에 걸린 시간 (초)
                float readySeconds = Time.realtimeSinceStartup - stepStart;
                stepStart = Time.realtimeSinceStartup;
                // 글자 테이블을 다 읽어야 걷음. 폰트는 글자가 켜질 때 키트가 바로 바꿔서 따로 안 기다림
                yield return transition.WaitForLocalizedTexts();
                transition.SetProgress(1f);
                Debug.Log($"[Flow] {next.SceneName} 로딩 시간(초): 씬 로드 {loadSeconds:F2}, 정보 적용 {readySeconds:F2}, 다국어 {Time.realtimeSinceStartup - stepStart:F2}");
                // 첫 진입은 로고가 끝나거나 넘길 때까지, 키 입력 대기 상태는 넘기기 버튼을 누를 때까지 덮어 둠. 나머지는 바로 걷음
                if (firstEntry) yield return transition.WaitForLogo();
                if (waitForContinue) yield return transition.WaitForContinue();
            }
            finally
            {
                // 중간에 예외가 나도 덮개·잠금·게임 시간이 남지 않게 (게임 시간은 HideCover가 되돌림)
                transition.HideCover();
                _isSwitching = false;
            }
            next.Reveal();
        }
        #endregion
```

CreateStartState·ChangeState는 그대로.

**호출/구독 관계:**
- ChangeState(버튼·입력에서) → SwitchState. 전환 중(`_isSwitching`)엔 다른 ChangeState 거절 (그대로)
- 팁·키 입력: `next.WaitsForContinue`(전초기지·숲), 첫 진입은 `TransitionManager.LogoShown`(로고 밑 팁 로딩창)
- Reveal은 덮개가 걷힌 뒤 한 번. 예외로 끝나면 안 불림

---

### `Assets/Scripts/Managers/DataManager.cs`

**변경 내용:**

1. `Elements` 프로퍼티 아래에 추가

```csharp
        /// <summary>로딩창 팁 전부 (Boot에서 연결)</summary>
        [SerializeField] TipSO[] _tips;

        /// <summary>로딩창 팁 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<TipSO> Tips => _tips;
```

2. InitSingleton — region 제목 바꾸고 첫 줄에 `ValidateTips();`. 나머지는 그대로

```csharp
        // before
        #region InitSingleton - 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
        protected override void InitSingleton()
        {
            if (_elements == null || _elements.Length == 0)

        // after
        #region InitSingleton - 팁 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
        protected override void InitSingleton()
        {
            ValidateTips();
            if (_elements == null || _elements.Length == 0)
```

3. InitSingleton region 뒤에 추가

```csharp
        #region ValidateTips - 팁 목록 검사: 비었으면 경고(로딩창에 팁 없이 나옴), 빈 칸, 제목·본문 키 빔
        void ValidateTips()
        {
            if (_tips == null || _tips.Length == 0)
            {
                Debug.LogWarning("[Data] 팁 목록 비어 있음. 로딩창에 팁 없이 나옴");
                _tips = Array.Empty<TipSO>();
                return;
            }

            for (int i = 0; i < _tips.Length; i++)
            {
                // 검사할 팁
                TipSO tip = _tips[i];
                if (tip == null)
                    Debug.LogError($"[Data] 팁 {i}번 칸 비어 있음");
                else if (tip.Title.IsEmpty || tip.Body.IsEmpty)
                    Debug.LogError($"[Data] {tip.name}의 제목·본문 키 비어 있음");
            }
        }
        #endregion
```

**호출/구독 관계:** `Tips` → TransitionManager.PickTip

---

### `Assets/Scripts/Flow/FlowStateBase.cs`

**변경 내용:**

1. OnSceneReady region 제목만

```csharp
        // before
        #region OnSceneReady - 상태별로 View를 찾아 버튼·입력을 연결하고 표시
        // after
        #region OnSceneReady - 상태별로 View를 찾아 버튼을 연결하고 글자를 채움 (덮개 밑. 배경음·조작 입력은 Reveal)
```

2. OnSceneReady region 뒤, Exit 앞에 추가

```csharp
        #region Reveal - 덮개가 걷힌 뒤 GameManager가 부름. 화면이 보일 때 시작할 것 (배경음, 커서 잠금, 조작 입력)
        public virtual void Reveal() { }
        #endregion
```

3. `SceneName` 아래에 추가

```csharp
        /// <summary>이 상태로 들어갈 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다릴지. 기본은 안 기다림 (검은 덮개만, 준비되면 바로 걷음). 플레이 씬(전초기지·숲)만 켬</summary>
        public virtual bool WaitsForContinue => false;
```

**호출/구독 관계:** Reveal은 GameManager.SwitchState 끝 (HideCover 뒤). WaitsForContinue는 SwitchState 시작에서 읽음

---

### `Assets/Scripts/UI/Views/TitleView.cs`

**변경 내용:** 시작 연출 참조 추가

1. `_bgm` 필드 아래에 추가

```csharp
        /// <summary>시작 연출 ('클릭하여 계속' → 화면 내림 → 제목·메뉴). 인스펙터에서만 연결</summary>
        [SerializeField] TitleIntroSequence _intro;
```

2. `Bgm` 프로퍼티 아래에 추가

```csharp
        /// <summary>시작 연출</summary>
        public TitleIntroSequence Intro => _intro;
```

**호출/구독 관계:** TitleState.OnPressStart가 `Intro.Play()`

---

### `Assets/Scripts/Flow/TitleState.cs`

**변경 내용:** View를 필드로 들고, 배경음과 '클릭하여 계속' 입력 대기를 Reveal로. 누르면 시작 연출

1. using 추가: `UnityEngine.InputSystem`, `UnityEngine.InputSystem.Utilities`(Where·CallOnce)

2. `SceneName` 아래에 필드 추가

```csharp
        /// <summary>이 씬의 화면 참조</summary>
        TitleView _view;

        /// <summary>'클릭하여 계속' 입력 구독. 누르면 CallOnce가 풀고, 누르기 전에 나가면 Exit가 풂. 없으면 null</summary>
        System.IDisposable _pressSubscription;
```

3. OnSceneReady 전체 교체 + Reveal·OnPressStart·Exit 추가

```csharp
        #region OnSceneReady - 버튼 연결, 이어하기 끔 (제목·메뉴는 TitleIntroSequence가 숨긴 채 시작)
        protected override void OnSceneReady()
        {
            _view = FindView<TitleView>();
            if (_view == null) return;
            _view.NewGameButton.onClick.AddListener(OnNewGame);
            // 저장이 없어서 이어할 판이 없음 (저장은 프로토타입 범위 밖)
            _view.ContinueButton.interactable = false;
            _view.SettingsButton.onClick.AddListener(OnSettings);
            _view.QuitButton.onClick.AddListener(OnQuit);
        }
        #endregion

        #region Reveal - 배경음 재생, '클릭하여 계속' 입력 대기 (넘기기 버튼 규칙은 로딩창과 같음)
        public override void Reveal()
        {
            if (_view == null) return;
            // 비어 있으면 AudioManager가 무시함
            AudioManager.Instance.PlayBgm(_view.Bgm);
            _pressSubscription = InputSystem.onAnyButtonPress
                .Where(TransitionManager.IsContinueControl)
                .CallOnce(_ => OnPressStart());
        }
        #endregion

        #region OnPressStart - 시작 연출 (화면 내림 → 제목·메뉴). 구독은 CallOnce가 이미 풂
        void OnPressStart()
        {
            _pressSubscription = null;
            if (_view.Intro == null)
            {
                Debug.LogError("[Flow] TitleView에 Intro 연결 안 됨");
                return;
            }
            _view.Intro.Play();
        }
        #endregion

        #region Exit - 누르기 전에 나가면 '클릭하여 계속' 구독 해제
        public override void Exit()
        {
            _pressSubscription?.Dispose();
            _pressSubscription = null;
        }
        #endregion
```

---

### `Assets/Scripts/Flow/CharacterCreationState.cs`

**변경 내용:** 배경음을 Reveal로

1. OnSceneReady — region 제목 바꾸고 마지막 두 줄 삭제

```csharp
        // 제목 before
        #region OnSceneReady - 이름 입력 규칙, 확정 버튼, 요소 카드, 고른 수 표시, 배경음
        // 제목 after
        #region OnSceneReady - 이름 입력 규칙, 확정 버튼, 요소 카드, 고른 수 표시
```

```csharp
            // 삭제
            // 비어 있으면 AudioManager가 무시함 (타이틀 곡이 이어짐)
            AudioManager.Instance.PlayBgm(_view.Bgm);
```

2. OnSceneReady region 뒤에 추가

```csharp
        #region Reveal - 배경음 재생
        public override void Reveal()
        {
            // 비어 있으면 AudioManager가 무시함 (타이틀 곡이 이어짐)
            if (_view != null) AudioManager.Instance.PlayBgm(_view.Bgm);
        }
        #endregion
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** `SceneName` 아래에 추가. 들어올 때 팁을 띄우고 넘기기 버튼을 기다림

```csharp
        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;
```

---

### `Assets/Scripts/Flow/FieldState.cs`

**변경 내용:** 커서 잠금·E 구독을 Reveal로, 들어올 때 넘기기 버튼을 기다림. Exit는 그대로

1. `SceneName` 아래에 추가

```csharp
        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬. 문구를 보는 동안 게임은 멈춰 있음)</summary>
        public override bool WaitsForContinue => true;
```

2. OnSceneReady 전체 교체 + Reveal 추가

```csharp
        #region OnSceneReady - 판 데이터 표시
        protected override void OnSceneReady()
        {
            // 이 씬의 화면 참조
            var view = FindView<FieldDebugView>();
            if (view != null) ShowRun(view);
        }
        #endregion

        #region Reveal - 커서 잠금, E 입력 구독 (로딩창을 넘길 때 누른 E는 구독 전이라 귀환으로 안 감)
        public override void Reveal()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _interact = InputSystem.actions.FindAction(InteractActionPath, throwIfNotFound: true);
            _interact.performed += OnInteract;
        }
        #endregion
```

---

## 데이터·다국어 변경

### Tip 테이블 컬렉션

1. Window > Asset Management > Localization Tables > New Table Collection → String Table Collection, 이름 `Tip`, 경로 `Assets/Localization/Tables`, 언어 13개 전부 체크 → Create
   - 스크립트로는 `LocalizationEditorSettings.CreateStringTableCollection("Tip", "Assets/Localization/Tables", LocalizationEditorSettings.GetLocales())`
2. `Tip.asset`, `Tip Shared Data.asset`, `Tip_{언어}.asset` 13개 생김. Addressables 그룹 파일 14개(`Localization-Assets-Shared`, `Localization-String-Tables-*`)도 바뀜

### 시트 'How to Live as a Mage Localization'

1. UI 탭 맨 아래에 행 2개 추가 (나머지 언어 칸은 비움)

| Key | Comment | ko | en |
|---|---|---|---|
| loading.continue | 로딩이 끝나면 깜빡이는 넘기기 안내 | 아무 키나 눌러 계속 | Press any key to continue |
| title.click_to_continue | 초기 화면 첫 안내. 누르면 화면이 내려가고 제목·메뉴가 나옴 | 클릭하여 계속 | Click to continue |

2. 새 탭 `Tip`. 머리글 15칸 `Key, Comment, ko, en, ja, zh-Hans, zh-Hant, ru, de, fr, es, pt-BR, it, pl, tr`. 행 6개 (나머지 언어 칸은 비움)

| Key | Comment | ko | en |
|---|---|---|---|
| tip.settings.title | 로딩창 팁: 설정 제목 | 설정 (임시) | Settings (temp) |
| tip.settings.body | 로딩창 팁: 설정 본문 | 타이틀의 설정에서 언어와 음량을 바꿀 수 있어 | You can change the language and volume in Settings on the title screen |
| tip.elements.title | 로딩창 팁: 요소 제목 | 요소 (임시) | Elements (temp) |
| tip.elements.body | 로딩창 팁: 요소 본문 | 캐릭터를 만들 때 고른 요소로 주문을 그려 | You draw spells with the elements you pick when creating your character |
| tip.return.title | 로딩창 팁: 귀환 제목 | 귀환 (임시) | Return (temp) |
| tip.return.body | 로딩창 팁: 귀환 본문 | 숲에서 E를 누르면 전초기지로 돌아가 | Press E in the forest to return to the outpost |

3. UI 탭·Tip 탭을 받아 `Assets/Localization/CSV/UI.csv`·`Assets/Localization/CSV/Tip.csv`에 덮어씀 (키트 CSV 형식: BOM UTF-8, CRLF, 마지막 줄바꿈 없음, 머리글만 따옴표 없음, 뒤쪽 빈 칸 채움)
4. Tools > PaulowniaSky > Localization Kit > Import Tables from CSV → Check Missing Translations → Check Missing Glyphs

### 팁 SO (`Assets/Data/Tips/`)

1. `Assets/Data/Tips/` 폴더 → 우클릭 Create > Mage > Tip으로 3개

| 파일 | Title | Body | Picture |
|---|---|---|---|
| `Settings.asset` | Tip / `tip.settings.title` | Tip / `tip.settings.body` | 비움 |
| `Elements.asset` | Tip / `tip.elements.title` | Tip / `tip.elements.body` | 비움 |
| `Return.asset` | Tip / `tip.return.title` | Tip / `tip.return.body` | 비움 |

---

## Scene 변경

### `Assets/Scenes/Boot.unity`

크기·위치는 CanvasScaler 기준 해상도(1920×1080) 단위.

1. Boot 씬 열기
2. `Managers/TransitionManager/CoverCanvas/Cover`
   - 오브젝트 켬 (Inspector 맨 위 체크)
   - Add Component → `Canvas Group`: Alpha 0, Interactable 끔, Blocks Raycasts 끔
   - Add Component → `LoadingView` (연결은 7번)
3. Cover 아래 `Tip` (우클릭 Create Empty)
   - Rect Transform: Anchor middle-center, Pos (0, 40), Size 1200×600
   - Add Component → `Canvas Group`: Alpha 1, Interactable 끔, Blocks Raycasts 끔 (팁 글자를 다 읽기 전엔 ShowLoading이 0으로 둠)
   - Add Component → `Vertical Layout Group`: Child Alignment Middle Center, Spacing 24, Control Child Size Width·Height 켬, Use Child Scale 끔, Child Force Expand Width 켬·Height 끔
   - 자식 3개 (이 순서):
     - `Picture` (UI > Image): Preserve Aspect 켬, Raycast Target 끔, Source Image 비움. Add Component → `Layout Element`: Preferred Height 320
     - `Title`: `Assets/Localization/Prefabs/LocalizedText.prefab`을 끌어다 놓고 이름 바꿈. TextMeshPro: Font Size 44, Alignment 가운데·가운데, Raycast Target 끔. Localize String Event 키 비움
     - `Body`: LocalizedText를 끌어다 놓고 이름 바꿈. TextMeshPro: Font Size 30, Alignment 가운데·위, Wrapping 켬, Raycast Target 끔. Localize String Event 키 비움
   - `Tip` 오브젝트 끔
4. Cover 아래 `ProgressBar` (UI > Image)
   - Rect Transform: Anchor bottom-center, Pivot (0.5, 0), Pos (0, 80), Size 640×12
   - Image: Source Image `UISprite`(내장), Color #3A3A3A, Raycast Target 끔
   - 자식 `Fill` (UI > Image): Anchor stretch-stretch, Left·Right·Top·Bottom 0. Image: Source Image `UISprite`, Color #D9D9D9, Image Type Filled, Fill Method Horizontal, Fill Origin Left, Fill Amount 0, Raycast Target 끔
5. Cover 아래 `Prompt`: LocalizedText를 끌어다 놓고 이름 바꿈
   - Rect Transform: Anchor bottom-center, Pivot (0.5, 0), Pos (0, 120), Size 900×50
   - TextMeshPro: Font Size 28, Alignment 가운데·가운데, Raycast Target 끔
   - Localize String Event > String Reference: `UI` / `loading.continue`
   - Add Component → `Canvas Group`: Alpha 0, Interactable 끔, Blocks Raycasts 끔
   - Add Component → `BlinkSequence`: Group → 이 오브젝트 Canvas Group, Period 1.2, Min Alpha 0.2. **컴포넌트 체크 끔**
6. CoverCanvas 아래 `Logo` (UI > Image). Hierarchy에서 Cover **다음** 형제로 둠 (위에 그려짐)
   - Rect Transform: Anchor stretch-stretch, Left·Right·Top·Bottom 0
   - Image: Color #000000, Raycast Target 켬
   - 자식 2개 (UI > Image, 이 순서). 둘 다 Anchor middle-center, Raycast Target 끔, Add Component → `Canvas Group`: Alpha 0, Interactable·Blocks Raycasts 끔
     - `EngineLogo`: Size 480×200. Source Image `UISprite`(내장), Image Type Sliced, Color #BFBFBF (엔진 로고 자리표시. 나중에 Made with Unity 로고)
     - `CompanyLogo`: Size 360×360. Source Image `Knob`(내장), Preserve Aspect 켬, Color #BFBFBF (회사 로고 자리표시)
   - `Logo`에 Add Component → `LogoSequence`: Logos 크기 2 → `EngineLogo`, `CompanyLogo` Canvas Group (이 순서), Fade In 0.6, Hold 1.2, Fade Out 0.6
   - `Logo` 오브젝트 끔
7. Cover의 `LoadingView` 연결
   - Group → Cover Canvas Group
   - Progress Fill → `ProgressBar/Fill` Image
   - Tip Root → `Tip`
   - Tip Group → `Tip` Canvas Group
   - Tip Picture → `Tip/Picture` Image
   - Tip Title → `Tip/Title` Localize String Event
   - Tip Body → `Tip/Body` Localize String Event
   - Prompt → `Prompt` BlinkSequence
   - Ready Sound → 비움
8. `Managers/TransitionManager`: Loading → `Cover`(LoadingView), Logo → `Logo`(LogoSequence)
9. `Managers/DataManager`: Tips 크기 3 → `Assets/Data/Tips/Settings`, `Elements`, `Return`
10. `Managers/GameManager`: Scene Load Share 0.8, Scene Ready Share 0.1 확인 (기본값. 검증 때 로딩 시간 로그 보고 맞춤)
11. 씬 저장 → 다시 열어 확인: 7~10 연결·값, Cover 켜짐·Canvas Group Alpha 0, `Tip`·`Logo` 꺼짐, `Tip` Canvas Group Alpha 1, `EngineLogo`·`CompanyLogo` Canvas Group Alpha 0, Prompt BlinkSequence 꺼짐·Canvas Group Alpha 0

### `Assets/Scenes/Title.unity`

크기·위치는 CanvasScaler 기준 해상도(1920×1080) 단위.

1. Title 씬 열기
2. Canvas 아래 `Pan` (우클릭 Create Empty). Hierarchy에서 `Background` **다음** 형제로 둠 (Background 위, 제목·메뉴 밑에 그려짐)
   - Rect Transform: Anchor top-stretch, Pivot (0.5, 1), Left·Right 0, Pos Y 0, Height 2160 (두 화면. 아래 반은 비워서 밑의 Background가 보임)
   - 자식 `Sky` (UI > Image): Anchor top-stretch, Pivot (0.5, 1), Left·Right 0, Pos Y 0, Height 1080. Color #2B3A55 (하늘 자리표시), Raycast Target 끔
   - 자식 `ClickPrompt`: `Assets/Localization/Prefabs/LocalizedText.prefab`을 끌어다 놓고 이름 바꿈. Hierarchy에서 `Sky` 다음
     - Rect Transform: Anchor top-center, Pivot (0.5, 0.5), Pos (0, -880), Size 900×60
     - TextMeshPro: Font Size 32, Alignment 가운데·가운데, Raycast Target 끔
     - Localize String Event > String Reference: `UI` / `title.click_to_continue`
     - Add Component → `Canvas Group`: Alpha 1, Interactable 끔, Blocks Raycasts 끔
     - Add Component → `BlinkSequence`: Group → 이 오브젝트 Canvas Group, Period 1.2, Min Alpha 0.2. **컴포넌트 켜 둠**
3. Canvas 아래 `Main` (우클릭 Create Empty). Hierarchy에서 `Pan` 다음 (맨 위에 그려짐)
   - Rect Transform: Anchor stretch-stretch, Left·Right·Top·Bottom 0
   - Add Component → `Canvas Group`: Alpha 0, Interactable 끔, Blocks Raycasts 끔
   - 기존 `Logo`·`Menu`를 `Main` 아래로 끌어 옮김 (Main이 Canvas와 같은 크기라 Rect Transform 값 그대로)
4. `Canvas`에 Add Component → `TitleIntroSequence`: Pan → `Pan`, Prompt → `ClickPrompt` BlinkSequence, Main → `Main` Canvas Group, Pan Distance 1080, Pan Seconds 1.2, Fade In Seconds 0.6
5. `Canvas`의 `TitleView`: Intro → `Canvas`의 TitleIntroSequence
6. 씬 저장 → 다시 열어 확인: Canvas 자식 순서 Background → Pan → Main, `Logo`·`Menu`가 Main 아래, Main Canvas Group Alpha 0·Interactable·Blocks Raycasts 끔, ClickPrompt BlinkSequence 켜짐, 4·5 연결

### Player Settings

1. Edit > Project Settings > Player > Splash Image > **Show Splash Screen 끔** (엔진 로고는 Boot `Logo/EngineLogo`가 대신. Unity 6는 Personal도 끌 수 있음)
2. `ProjectSettings/ProjectSettings.asset`에서 `m_ShowUnitySplashScreen: 0` 확인

---

## 주의사항

- `Where`·`CallOnce`는 `UnityEngine.InputSystem.Utilities` 확장 메서드, `KeyControl`은 `UnityEngine.InputSystem.Controls`. using이 빠지면 CS1061·CS0246 (TransitionManager·TitleState)
- Cover는 켜 둔 채 Canvas Group 투명도로 숨김. 꺼 두면 숨긴 동안 팁 글자를 못 읽어서 ShowLoading이 기다릴 게 없고, 로딩창이 뜬 뒤 팁 글자가 늦게 채워짐
- 팁 키는 `Tip`이 꺼진 상태에서 넣고 켬 (HideCover가 끔). 켜진 채 넣으면 이전 팁 키를 한 번 더 읽음. 글자를 다 읽기 전엔 `Tip` Canvas Group 0 → 첫 진입처럼 덮개가 이미 보일 때 자리 글자가 안 비침
- Prompt BlinkSequence는 컴포넌트 꺼짐 + Canvas Group Alpha 0으로 저장. 켜진 채 저장되면 로딩창이 뜨자마자 깜빡임
- `Logo`는 CoverCanvas 안에서 Cover 다음 형제여야 위에 그려짐. 씬엔 꺼진 채 저장 (InitSingleton·HideCover도 끔). 로고 그림은 자식 `EngineLogo`·`CompanyLogo`(자리표시), LogoSequence가 Logos 순서대로 틂
- 끝 소리는 비워 둬도 됨 (`AudioManager.Play(null)` 무시). 클립 없는 SoundDataSO를 넣으면 재생 때마다 경고
- `WaitForLocalizedTexts`는 켜진 LocalizeStringEvent 전부(Boot 것 포함)를 봄. 설정 창·확인창·토스트는 평소 꺼져 있어서 안 걸림
- 읽기 성공 콜백은 작업이 Done이 되는 순간 같이 불려서, IsDone이면 글자가 이미 들어가 있음. 실패는 콜백이 다음 프레임이지만 글자를 안 바꿔서 기다릴 필요 없음
- 넘기기 버튼(로딩창·로고·타이틀 공통 `IsContinueControl`): `onAnyButtonPress`라 구독한 뒤 새로 누른 버튼만 셈 (누르고 있던 키, 마우스 이동, 스틱 방향은 안 셈). 수정자 키(Shift·Ctrl·Alt·Win), 수정자를 누른 채 누른 키(Alt+Tab, Shift+Tab = Steam 오버레이), F12(Steam 스크린샷 기본 키)·PrintScreen은 안 셈. 로딩창은 안내가 뜨고 0.3초 뒤부터 셈 (연타하던 키로 바로 안 넘어감). 창 포커스를 되찾는 클릭은 셈 (게임은 걷힌 뒤 시작이라 피해 없음)
- 넘긴 직후 새 씬에 선택된 UI가 있으면 Enter·Space가 Submit으로도 들어갈 수 있음. 지금 씬들엔 처음 선택된 UI 없음
- 클릭으로 넘길 땐 누른 순간 로딩창이 클릭을 막고 있어서 뗄 때 새 씬 버튼이 안 눌림
- FieldState: E 구독이 Reveal에 있어서 로딩창에서 E로 넘겨도 귀환 안 됨. Interact는 Hold 없는 Button. Reveal 전에 Exit가 와도 `_interact` null 체크가 있음
- 첫 진입(`_state == null`): 키 입력을 안 기다림. Title부터면 로고 밑에 팁 로딩창을 깔고 로고가 끝나거나 넘길 때까지 덮음. 에디터에서 중간 씬부터 켜면 로고를 안 틀어서 검은 덮개(팁 없음)만, `WaitForLogo`가 바로 끝남. 첫 ChangeState가 거절되면(`CanLoad` 실패) 다음 시도도 첫 진입
- 켤 때 흐름: 엔진 로고 → 회사 로고 (로고마다 약 2.4초, 다 보면 약 4.8초). 넘기기 버튼 한 번이면 남은 로고 전부 끝 (로딩 중에도). 로고가 로딩보다 먼저 끝나거나 넘기면 로고 화면을 꺼서 밑의 팁 로딩창(바)이 보이고, 로딩이 끝나면 키 입력 없이 Title. 로딩이 먼저 끝났으면 로고 화면을 둔 채 바로 Title (`_waitingForLogo`, 로딩창이 한 프레임 안 비침). 에디터에서 Title부터 Play해도 매번 로고가 나옴
- 다국어 초기화 전에 로고를 넘기면 그동안은 검은 덮개 + 바(0)만. 팁은 다국어 준비 뒤 ShowLoading이 붙임. 회전 아이콘은 안 둠 (검은 화면 대기를 로딩창이 대신함)
- Unity 스플래시는 끔 (Player Settings). 켜 두면 빌드에서 Boot 로고 앞에 못 넘기는 Unity 로고가 한 번 더 나옴
- 덮개가 떠 있는 동안 `Time.timeScale` 0 (Open에서 0, HideCover에서 1). 물리·애니메이션·deltaTime·WaitForSeconds는 멈추고, Sequence(Logo·Blink·TitleIntro)와 로딩 대기는 실제 시간이라 그대로 돎. 덮개 밑에서 물리가 자리 잡지 않아서 걷힌 뒤 첫 프레임부터 움직임. 나중에 조작 스크립트(마우스 시점처럼 deltaTime 안 쓰는 입력)·적 AI는 Reveal 뒤에 켬. Input System 처리를 Fixed Update로 바꾸면 로딩창에서 입력을 못 받음
- Run In Background: 빌드에서만 덮개가 떠 있는 동안 켜고 걷힐 때 원래 값(꺼짐)으로. 에디터는 `Application.isEditor`로 건너뜀 (MCP `editor_playmode_set_run_in_background`·사용자 설정과 안 싸우게) → 에디터 검증 안 되고 빌드에서 확인
- 키 입력 대기는 들어갈 상태의 `WaitsForContinue`로 정함: 전초기지·숲만 켬. Title·생성·오프닝은 팁·안내 없이 검은 덮개(바만) → 준비되면 바로 걷음. 로드가 짧으면 덮개가 잠깐 깜빡임 (페이드 넣을 때 다듬음)
- 로딩 바: 씬 로드 몫 `_sceneLoadShare`(0.8)·정보 적용 몫 `_sceneReadyShare`(0.1), 다국어는 나머지. 씬 로드 단계 안은 `AsyncOperation.progress`(0~0.9)를 몫에 맞춰 채움. 언로드·UnloadUnusedAssets 동안은 0, 0.9 → 1(활성화)은 한 프레임이라 그동안 바·깜빡임 멈춤. 전환마다 `[Flow] {씬} 로딩 시간(초)` 로그. 에디터 시간은 빌드와 달라서 빌드를 만들면 다시 잼
- 타이틀: 공개되면 '클릭하여 계속'만 깜빡임 (제목·메뉴는 `Main` Canvas Group 0·못 누름). 넘기기 버튼 → 안내 꺼짐 → `Pan`이 1.2초 동안 1080 올라감(화면이 내려가 보임) → `Main`이 0.6초 동안 나타난 뒤에만 누를 수 있음. 로고를 넘기려고 연타하면 '클릭하여 계속'도 바로 넘어갈 수 있음 (메뉴로 빨리 가려는 거라 둠). 나중에 세로로 긴 그림 한 장을 `Pan`에 넣으면 됨
- Title로 돌아오는 길은 아직 없음. 생기면 그때도 '클릭하여 계속'부터 나옴 (바로 메뉴가 나을지는 그때 정함)
- 시트에 쓰려면 Google Sheets 커넥터가 켜져 있어야 함. CSV·테이블 창은 직접 고치지 않음
- Boot TransitionManager YAML에 예전 `_cover` 값이 남아 있다가 씬을 다시 저장하면 지워짐
- 순서: 다국어(키 생성) → 팁 SO(키 연결) → 씬(Prompt·ClickPrompt 키 연결)
- MCP Play 검증: Play 직후엔 editor_status가 playing일 때 부름. 에디터가 백그라운드면 run-in-background·auto-tick을 잠깐 켬. MCP 키 입력이 Input System까지 안 가면 Game 뷰에서 직접 누름. 로고 넘기기는 script_eval로 `Object.FindAnyObjectByType<LogoSequence>().Skip()`도 됨

---

## 진행 추적

- [x] 묶음 Logic — 신규 TipSO·LoadingView·LogoSequence·BlinkSequence·TitleIntroSequence, 수정 TransitionManager·GameManager·DataManager·FlowStateBase·TitleView·TitleState·CharacterCreationState·HubState·FieldState. 컴파일 에러·경고 0
- [x] 묶음 다국어 — Tip 테이블 컬렉션, 시트 UI 행 2개·Tip 탭, CSV 2개 덮어씀 → Import. Check Missing Glyphs ko·en 0. Missing Translations는 새 키 8개가 ko·en 말고 11개 언어 빔
- [x] 묶음 데이터 — `Assets/Data/Tips/` 팁 SO 3개, 키 연결
- [x] 묶음 Scene — Boot 로딩창·로고 2개·연결, Title 시작 연출(Pan·Main·연결), Player Settings Show Splash Screen 끔. 저장 뒤 다시 열어 확인
- [ ] 묶음 검증 — 프레임 기록기(script_eval로 `Canvas.willRenderCanvases`에 붙여 로고·로딩창 투명도, 바, 팁·안내, 화면 글자·폰트, `Time.timeScale`, Title `Pan` 위치·`Main` 투명도가 바뀐 프레임 기록)
  - [x] ko로 Title부터 Play, 안 누름: 엔진 로고 → 회사 로고(각 약 2.4초) → 로딩이 끝나 있으면 로딩창 없이 바로 Title. 로고 동안 `Time.timeScale` 0, 공개 뒤 1. 콘솔에 `[Flow] Title 로딩 시간` 로그 — 로딩은 로고 시작 약 1.4초 뒤 끝남, 로고가 꺼진 프레임에 덮개도 같이 걷힘. ⚠ 엔진 로고 나타남(0.6초)은 Play 시작 직후 긴 프레임(약 0.44초)에 묻혀 기록 첫 프레임부터 다 보임 (회사 로고 나타남·사라짐은 0.6초대로)
  - [x] Title 공개: 첫 프레임부터 '클릭하여 계속' 최종 글자 + NotoSansKR, 깜빡임. 제목·메뉴 안 보이고 메뉴 자리를 클릭해도 안 눌림. 배경음은 공개 뒤. 공개 뒤 2초 동안 글자·폰트 안 바뀜. 콘솔 에러 0 — 메뉴 자리 클릭은 연출만 시작되고 새 게임 안 눌림 (연출이 끝난 뒤 같은 자리 클릭은 눌림). 배경음은 Title BGM 에셋이 비어 소리 확인 못 함 (호출 위치만 Reveal)
  - [x] Shift·Ctrl·Alt·F12·PrintScreen만 눌러선 안 넘어감. 클릭(또는 아무 키) → 안내 꺼짐 → 화면이 1.2초 동안 내려감 → 제목·메뉴가 0.6초 동안 나타난 뒤에만 '새 게임' 눌림 — Alt+Tab·Shift+Tab도 안 넘어감. 실측 내려감 1.18초, 나타남 0.59초, 그 다음 프레임에 누를 수 있게 됨
  - [x] Play 직후(로고 시작 1초 안) 넘기기 (키 또는 script_eval로 `LogoSequence.Skip`): 로고 꺼짐 → 검은 덮개 + 바 → 팁(자리 글자 안 비침, 첫 프레임부터 최종 글자) → 로딩이 끝나면 키 입력 없이 Title. 로딩이 이미 끝났으면 바로 Title — 로고 중 Shift는 안 넘김, Space로 넘김 (en으로 확인)
  - [x] 새 게임(`onClick.Invoke`) → 팁·안내 없이 검은 덮개 → 키 입력 없이 생성 화면, 요소 카드 글자 첫 프레임부터 최종
  - [x] 생성 → 오프닝(바로 걷힘) → 전초기지: 로딩창 첫 프레임부터 팁 글자 최종 + KR 폰트. 바가 씬 로드 동안 조금씩 차서 0.8 → 0.9 → 1 (같은 프레임에 끝난 단계는 마지막 값만). 바가 1이 된 뒤에만 안내 깜빡임. 로딩 중 연타해도 안내가 뜨고 0.3초 지나야 넘어감. Shift+Tab·Alt만으론 안 넘어감. 아무 키 → 걷힘 → 전초기지 글자 첫 프레임부터 최종 — 에디터에선 씬 로드가 1~2프레임이라 0 → 0.8로 한 번에 참 (중간 값은 빌드에서 확인). 연타 9번(안내 +0.15초까지) 뒤 안내 +0.5초에도 대기 중
  - [x] 전초기지 ↔ 숲을 이어 해도 같은 팁이 연속으로 안 나옴. 콘솔 로딩 시간 로그로 단계 비율을 보고 Boot GameManager 몫을 맞춘 뒤 저장 — 10번 전환에 연속 같은 팁 없음. 에디터 실측(전초기지·숲) 씬 로드 0.10~0.21초, 정보 적용 0.00초, 다국어 0.02~0.04초 = 약 87 : 0 : 13 → 0.8·0.1 유지 (차이는 정보 적용 0.1이 한 번에 차는 것뿐, 던전 생성이 들어오거나 빌드에서 다시 잼)
  - [x] en(PlayerPrefs `selected-locale` en)으로 같은 확인('Click to continue'), 폰트 NotoSans. 끝나면 ko로 되돌림 — 팁 'Elements (temp)' 첫 프레임부터 최종
  - [x] 전초기지 → 숲: 숲 로딩창을 E로 넘겨도 경고·귀환 없음. 넘긴 뒤 E → 전초기지
  - [x] 마우스 클릭으로 넘겨도 새 씬 버튼이 같이 안 눌림 — '영입' 버튼 자리 클릭으로 넘김 → 파티 0 그대로. 덮개 없이 같은 자리 클릭은 눌림(파티 2)
  - [x] 에디터에서 Outpost를 열고 Play: 로고·팁·넘기기 없이 바로 공개
  - [x] run-in-background·auto-tick 원래대로 (둘 다 끔)
  - [ ] 빌드: 첫 화면이 Unity 로고가 아니라 Boot 엔진 로고. 전초기지 → 숲 로딩 중 Alt+Tab 몇 초 → 돌아오면 안내가 깜빡이는 중 (로딩이 이어짐)

---

## 커밋 메시지

```
Add — 로딩창(팁·바·아무 키로 넘기기), 켤 때 엔진·회사 로고(넘기기), 타이틀 '클릭하여 계속' 연출, 화면 공개(Reveal) 분리

- TransitionManager: 덮개를 Canvas Group 로딩창(LoadingView)으로. 팁 무작위(바로 전 팁 제외) → 팁 글자를 다 읽은 뒤 보임. 바는 씬 로드 몫(읽는 동안 AsyncOperation.progress로 참)·정보 적용 몫·다국어 나머지. 켜진 LocalizeStringEvent가 다 채워질 때까지 기다림(10초 넘으면 에러 후 진행). 끝 소리 + '아무 키나 눌러 계속' 깜빡임 → 0.3초 뒤부터 InputSystem.onAnyButtonPress. 넘기기 버튼에서 수정자·수정자 조합(Alt+Tab, Shift+Tab)·F12·PrintScreen 뺌. 덮개 동안 Time.timeScale 0, 빌드는 Run In Background 켬. 켤 때 로고는 언제든 넘기고, 로딩이 남았으면 밑의 로딩창을 보임
- GameManager: 첫 화면이 Title이면 로고부터, 로고 밑에 팁 로딩창을 깔고 다국어 초기화·Title 로드. 키 입력은 들어갈 상태가 WaitsForContinue일 때만(전초기지·숲). 바 몫은 인스펙터, 단계별 로딩 시간 로그. 걷은 뒤 Reveal
- FlowStateBase.Reveal·WaitsForContinue: Title·생성 배경음, 숲 커서 잠금·E 구독을 덮개가 걷힌 뒤로. 전초기지·숲만 키 입력 대기
- 타이틀: '클릭하여 계속'(깜빡임) → 넘기기 버튼 → 화면이 1.2초 동안 내려감 → 제목·메뉴가 0.6초 동안 나타난 뒤 누를 수 있음 (TitleIntroSequence, Title Pan·Main)
- TipSO(제목·본문·그림), DataManager 팁 목록·검사, 임시 팁 3개. Tip 테이블(시트 Tip 탭), UI loading.continue·title.click_to_continue
- LoadingView, LogoSequence(로고마다 나타남 0.6·머묾 1.2·사라짐 0.6초, Skip), BlinkSequence. Boot CoverCanvas에 로딩창(팁·바·안내)·Logo 화면(엔진·회사 로고 자리표시). Player Settings Show Splash Screen 끔
- 검증: 컴파일·콘솔 에러 0. ko·en Play: 로고(넘기기 포함) 뒤 로딩창 또는 바로 Title, Title 첫 프레임부터 최종 글자·폰트, '클릭하여 계속' → 내려감 → 메뉴, 로딩창 첫 프레임부터 팁 최종, 바 단계, 키 입력 뒤에만 걷힘, 수정자·F12로 안 넘어감, 숲 로딩창 E로 넘겨도 귀환 없음, 중간 씬 Play는 바로 공개. 빌드 Alt+Tab 중 로딩 이어짐

Why: 덮개가 씬 로드 직후 같은 프레임에 걷혀서, 비동기로 읽히는 글자 테이블이 채워지는 과정(자리 글자 LocalizedText → 최종)이 보였음. 켤 때는 다국어가 준비되기 전이라 로고로 가리고, 전환마다 팁을 읽고 넘기게 함. 다른 게임 사례 검토 뒤: 로고는 대부분 넘기니 로고 시간에 로딩을 숨기는 데 기대지 않고 남은 로딩만 로딩창으로, 메뉴 사이 전환마다 키 입력은 피곤해서 플레이 씬만, 문구를 보는 사이 게임이 돌지 않게, 연타·Alt+Tab·Steam 키로 넘어가지 않게, 바가 멈췄다 덜컥 차지 않게, Alt+Tab 하면 로딩이 멈추지 않게. 타이틀은 첫 화면에서 한 번 누르고 들어가게
Alternatives: 테이블 Preload 플래그(새 테이블마다 걸어야 하고 공개 시점 대기는 어차피 필요), LocalizeStringEvent WaitForCompletion(글자마다 켜야 하고 읽는 동안 프레임 멈춤), 정해진 프레임 수 대기(근거 없음), 최소 표시 시간(빠른 씬에서 기다림만 늘고 팁을 읽었는지 모름), Unity 스플래시 중 다국어 초기화(빌드에서만 확인되고 보장 애매), Unity 스플래시를 SplashScreen API로 넘기기(공식 예제가 옛 Input, 빌드에서만 확인), 로딩이 끝난 뒤에만 로고 넘기기(로딩이 길면 로고에 갇힘), 검은 화면 + 회전 아이콘(팁 로딩창이 있어서 따로 안 만듦), Run In Background 늘 켬(게임 중 Alt+Tab 멈춤이 사라짐), 타이틀 카메라 이동(지금 Title이 Canvas 그림뿐이라 3D 배경부터 필요)
```
