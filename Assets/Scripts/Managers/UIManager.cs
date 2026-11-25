using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·자막·설정 창·확인창·일시정지 메뉴·메뉴 창(지도·임무 일지·튜토리얼·주문 도감)·툴팁·버튼 클릭음·NPC 대화·영입 창·게시판 창·화면 페이드</summary>
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>토스트 표시 시간 (초)</summary>
        const float ToastSeconds = 2f;

        /// <summary>창 닫기·일시정지 메뉴 입력 액션 경로 (전역 액션, Esc)</summary>
        const string CancelActionPath = "UI/Cancel";

        /// <summary>일시정지 메뉴 '타이틀로' 확인창 문구 키 (UI 테이블)</summary>
        const string ToTitleConfirmKey = "pause.to_title_confirm";

        /// <summary>자막을 보이는 시간 (초, 게임 시간이라 일시정지 중엔 멈춤)</summary>
        const float SubtitleSeconds = 4f;

        /// <summary>메뉴 창 지도 탭 입력 액션 경로 (전역 액션, M)</summary>
        const string MapActionPath = "UI/Map";

        /// <summary>메뉴 창 임무 일지 탭 입력 액션 경로 (전역 액션, J)</summary>
        const string JournalActionPath = "UI/Journal";

        /// <summary>테이블을 안 넘긴 토스트·확인창이 쓰는 테이블 (공용 UI 문구)</summary>
        static readonly TableReference UiTable = "UI";

        /// <summary>화면 아래 잠깐 뜨는 알림 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] ToastView _toast;

        /// <summary>설정 창 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] SettingsView _settings;

        /// <summary>확인창 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] ConfirmView _confirm;

        /// <summary>일시정지 메뉴 (Boot 씬 UICanvas 자식. 설정 창·확인창보다 아래에 그려짐)</summary>
        [SerializeField] PauseMenuView _pauseMenu;

        /// <summary>메뉴 창 (Boot 씬 UICanvas 자식. 일시정지 메뉴보다 아래에 그려짐)</summary>
        [SerializeField] GameMenuController _gameMenu;

        /// <summary>NPC 대사 자막 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] SubtitleView _subtitle;

        /// <summary>툴팁 (Boot 씬 UICanvas 맨 위 자식)</summary>
        [SerializeField] TooltipController _tooltip;

        /// <summary>화면 페이드 (Boot 씬 UICanvas 맨 아래 자식. 문 드나들 때)</summary>
        [SerializeField] FadeSequence _fade;

        /// <summary>NPC 대화 (Boot 씬 UICanvas 자식. 자막 위에 그려짐)</summary>
        [SerializeField] DialogueController _dialogue;

        /// <summary>영입 창 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] RecruitController _recruit;

        /// <summary>임무 게시판 창 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] QuestBoardController _questBoard;

        /// <summary>메뉴 창 지도 탭 지도 (Boot 씬 UICanvas/GameMenu/…/MapPage/Map. 출처는 상태가 SetMapSource로 넘김)</summary>
        [SerializeField] MapUIBinder _menuMap;

        /// <summary>메뉴 창 임무 일지 탭 (Boot 씬 UICanvas/GameMenu 안. 출처는 상태가 SetJournalSource로 넘김)</summary>
        [SerializeField] JournalUIBinder _menuJournal;

        /// <summary>메뉴 창 주문 도감 탭 (Boot 씬 UICanvas/GameMenu 안. 출처는 상태가 SetCodexSource로 넘김)</summary>
        [SerializeField] SpellCodexUIBinder _menuSpells;

        /// <summary>메뉴 창 튜토리얼 탭 (Boot 씬 UICanvas/GameMenu 안. Start에서 튜토리얼 목록·배웠는지를 넘김)</summary>
        [SerializeField] TutorialListUIBinder _menuTutorials;

        /// <summary>설정 창 음량·음소거 (Boot 씬 설정 창. Start에서 기기 설정·음량 바꾸기를 넘김)</summary>
        [SerializeField] SettingsUIBinder _settingsBinder;

        /// <summary>설명창 (Boot 씬 UICanvas 자식. 게시판 창 위)</summary>
        [SerializeField] TutorialWindowController _tutorialWindow;

        /// <summary>자막을 숨기는 코루틴. 새 자막이 오면 끊고 다시 시작</summary>
        Coroutine _hideSubtitle;

        /// <summary>지금 자막이 끝나면 할 일. 없으면 null</summary>
        Action _onSubtitleHidden;

        /// <summary>지도 탭 입력 액션. OnDestroy에서 구독 해제</summary>
        InputAction _map;

        /// <summary>임무 일지 탭 입력 액션. OnDestroy에서 구독 해제</summary>
        InputAction _journal;

        /// <summary>토스트를 숨기는 코루틴. 새 토스트가 오면 끊고 다시 시작</summary>
        Coroutine _hideToast;

        /// <summary>확인창에서 확인을 누르면 할 일. 창이 닫혀 있으면 null</summary>
        Action _onConfirm;

        /// <summary>창 닫기 입력 액션. OnDestroy에서 구독 해제</summary>
        InputAction _cancel;

        #region InitSingleton - 공용 창 준비, 토스트 연결 확인, 처음엔 숨김
        protected override void InitSingleton()
        {
            InitWindows();
            if (_toast == null)
            {
                Debug.LogError("[UI] ToastView 연결 안 됨");
                return;
            }
            _toast.Root.SetActive(false);
        }
        #endregion

        #region ShowToast - UI 테이블 문구로 토스트 (테이블을 안 넘기는 호출)
        /// <param name="key">UI 테이블 키</param>
        /// <param name="args">문구의 {0}, {1}… 자리에 넣을 값</param>
        public void ShowToast(string key, params object[] args) => ShowToast(UiTable, key, args);
        #endregion

        #region ShowToast - 지정한 테이블의 문구를 잠깐 띄움. 이미 떠 있으면 문구를 바꾸고 시간을 다시 셈
        /// <param name="table">문구가 든 String Table</param>
        /// <param name="key">문구 키</param>
        /// <param name="args">문구의 {0}, {1}… 자리에 넣을 값</param>
        public void ShowToast(TableReference table, string key, params object[] args)
        {
            if (table.ReferenceType == TableReference.Type.Empty || string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("[UI] 빈 테이블·키 토스트 무시");
                return;
            }
            if (_toast == null)
            {
                Debug.LogError("[UI] ToastView 연결 안 됨");
                return;
            }
            _toast.Root.SetActive(true);
            // 토스트 글자의 String Reference (테이블·키를 부를 때마다 넣음)
            var text = _toast.Text.StringReference;
            text.SetReference(table, key);
            text.Arguments = args;
            _toast.Text.RefreshString();
            if (_hideToast != null) StopCoroutine(_hideToast);
            _hideToast = StartCoroutine(HideToastAfter(ToastSeconds));
        }
        #endregion

        #region HideToastAfter - 정해진 시간 뒤 토스트 숨김 (일시정지 중에도 흐르게 실제 시간 기준)
        /// <param name="seconds">기다릴 시간 (초)</param>
        IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _toast.Root.SetActive(false);
            _hideToast = null;
        }
        #endregion

        #region InitWindows - 설정 창·확인창·일시정지 메뉴를 끄고 버튼을 한 번만 연결, Esc 구독, 메뉴 창·자막·툴팁·대화·영입·게시판 끄기, 메뉴 탭·설정 창 Binder 연결 확인, 클릭음·툴팁 알림 구독, M·J 구독
        void InitWindows()
        {
            if (_pauseMenu == null)
                Debug.LogError("[UI] PauseMenuView 연결 안 됨");
            else
            {
                _pauseMenu.gameObject.SetActive(false);
                _pauseMenu.ResumeButton.onClick.AddListener(HidePauseMenu);
                _pauseMenu.SettingsButton.onClick.AddListener(ShowSettings);
                _pauseMenu.ToTitleButton.onClick.AddListener(OnToTitle);
            }

            if (_settings == null)
                Debug.LogError("[UI] SettingsView 연결 안 됨");
            else
            {
                _settings.gameObject.SetActive(false);
                _settings.CloseButton.onClick.AddListener(HideSettings);
            }

            if (_confirm == null)
                Debug.LogError("[UI] ConfirmView 연결 안 됨");
            else
            {
                _confirm.gameObject.SetActive(false);
                _confirm.ConfirmButton.onClick.AddListener(OnConfirmClicked);
                _confirm.CancelButton.onClick.AddListener(HideConfirm);
            }

            if (_gameMenu == null)
                Debug.LogError("[UI] GameMenuController 연결 안 됨");
            else
            {
                _gameMenu.gameObject.SetActive(false);
                _gameMenu.CloseClicked += HideGameMenu;
            }

            if (_subtitle == null) Debug.LogError("[UI] SubtitleView 연결 안 됨");
            else _subtitle.Root.SetActive(false);

            if (_tooltip == null) Debug.LogError("[UI] TooltipController 연결 안 됨");
            else _tooltip.Hide();

            if (_fade == null) Debug.LogError("[UI] FadeSequence 연결 안 됨");

            if (_dialogue == null) Debug.LogError("[UI] DialogueController 연결 안 됨");
            else _dialogue.Hide();

            if (_recruit == null)
                Debug.LogError("[UI] RecruitController 연결 안 됨");
            else
            {
                _recruit.gameObject.SetActive(false);
                _recruit.CloseClicked += HideRecruit;
            }

            if (_questBoard == null)
                Debug.LogError("[UI] QuestBoardController 연결 안 됨");
            else
            {
                _questBoard.gameObject.SetActive(false);
                _questBoard.CloseClicked += HideQuestBoard;
            }

            if (_menuMap == null) Debug.LogError("[UI] 메뉴 지도 MapUIBinder 연결 안 됨");
            if (_menuJournal == null) Debug.LogError("[UI] 메뉴 임무 일지 JournalUIBinder 연결 안 됨");
            if (_menuSpells == null) Debug.LogError("[UI] 메뉴 주문 도감 SpellCodexUIBinder 연결 안 됨");
            if (_menuTutorials == null) Debug.LogError("[UI] 메뉴 튜토리얼 TutorialListUIBinder 연결 안 됨");
            if (_settingsBinder == null) Debug.LogError("[UI] 설정 창 SettingsUIBinder 연결 안 됨");

            if (_tutorialWindow == null)
                Debug.LogError("[UI] TutorialWindowController 연결 안 됨");
            else
            {
                _tutorialWindow.gameObject.SetActive(false);
                _tutorialWindow.CloseClicked += HideTutorialWindow;
            }

            ClickSoundHandler.Clicked += OnButtonClicked;
            TooltipHandler.ShowRequested += ShowTooltip;
            TooltipHandler.HideRequested += HideTooltip;

            _map = FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMap;
            _journal = FindAction(JournalActionPath);
            if (_journal != null) _journal.performed += OnJournal;

            _cancel = InputSystem.actions.FindAction(CancelActionPath);
            if (_cancel == null)
                Debug.LogError($"[UI] {CancelActionPath} 액션 없음. Esc로 창이 안 닫힘");
            else
                _cancel.performed += OnCancel;
        }
        #endregion

        #region Start - 메뉴 튜토리얼 탭에 튜토리얼 목록·배웠는지, 설정 창에 기기 설정·음량 바꾸기를 넘김 (매니저끼리 Awake 순서가 없어서 Start에서. 빠진 매니저는 Instance가 에러를 남김)
        void Start()
        {
            // 튜토리얼 목록을 들고 있는 곳 (Boot 상주)
            DataManager data = DataManager.Instance;
            // 배운 튜토리얼을 아는 곳 (Boot 상주)
            TutorialManager tutorials = TutorialManager.Instance;
            if (_menuTutorials != null && data != null && tutorials != null) _menuTutorials.Init(data.Tutorials, tutorials.IsLearned);
            // 기기 설정을 들고 있는 곳 (Boot 상주)
            SettingsManager settings = SettingsManager.Instance;
            // 음량을 믹서에 넣는 곳 (Boot 상주)
            AudioManager audio = AudioManager.Instance;
            if (_settingsBinder != null && settings != null && audio != null) _settingsBinder.Init(settings.Device, audio.SetVolume);
        }
        #endregion

        #region OnDestroy - Esc·M·J·클릭음·툴팁 알림 구독 해제
        protected override void OnDestroy()
        {
            if (_cancel != null) _cancel.performed -= OnCancel;
            if (_map != null) _map.performed -= OnMap;
            if (_journal != null) _journal.performed -= OnJournal;
            ClickSoundHandler.Clicked -= OnButtonClicked;
            TooltipHandler.ShowRequested -= ShowTooltip;
            TooltipHandler.HideRequested -= HideTooltip;
            base.OnDestroy();
        }
        #endregion

        #region ShowSettings - 설정 창 켜기
        public void ShowSettings()
        {
            if (_settings == null)
            {
                Debug.LogError("[UI] SettingsView 연결 안 됨");
                return;
            }
            _settings.gameObject.SetActive(true);
        }
        #endregion

        #region HideSettings - 설정 창 끄고 바뀐 설정을 파일에 저장 (닫기 버튼·Esc 둘 다 여기로)
        public void HideSettings()
        {
            if (_settings != null) _settings.gameObject.SetActive(false);
            // 설정 파일 담당 (Boot 상주)
            var settingsManager = SettingsManager.Instance;
            if (settingsManager != null) settingsManager.Save();
        }
        #endregion

        #region ShowConfirm - UI 테이블 문구로 확인창 (테이블을 안 넘기는 호출)
        /// <param name="messageKey">UI 테이블 문구 키</param>
        /// <param name="onConfirm">확인을 눌렀을 때 할 일</param>
        public void ShowConfirm(string messageKey, Action onConfirm) => ShowConfirm(UiTable, messageKey, onConfirm);
        #endregion

        #region ShowConfirm - 지정한 테이블의 문구로 확인창 켜기. 확인을 누르면 onConfirm, 취소·Esc면 그냥 닫힘
        /// <param name="table">문구가 든 String Table</param>
        /// <param name="messageKey">문구 키</param>
        /// <param name="onConfirm">확인을 눌렀을 때 할 일</param>
        public void ShowConfirm(TableReference table, string messageKey, Action onConfirm)
        {
            if (onConfirm == null) throw new ArgumentNullException(nameof(onConfirm));
            if (table.ReferenceType == TableReference.Type.Empty || string.IsNullOrWhiteSpace(messageKey))
            {
                Debug.LogWarning("[UI] 빈 테이블·키 확인창 무시");
                return;
            }
            if (_confirm == null)
            {
                Debug.LogError("[UI] ConfirmView 연결 안 됨");
                return;
            }
            _confirm.MessageText.StringReference.SetReference(table, messageKey);
            _onConfirm = onConfirm;
            _confirm.gameObject.SetActive(true);
            _confirm.MessageText.RefreshString();
        }
        #endregion

        #region OnConfirmClicked - 할 일을 꺼내 두고 창을 닫은 뒤 실행 (할 일이 다른 창을 열어도 안 꼬이게)
        void OnConfirmClicked()
        {
            // 이번 확인으로 할 일
            Action action = _onConfirm;
            HideConfirm();
            action?.Invoke();
        }
        #endregion

        #region HideConfirm - 할 일을 비우고 확인창 끄기
        void HideConfirm()
        {
            _onConfirm = null;
            if (_confirm != null) _confirm.gameObject.SetActive(false);
        }
        #endregion

        #region ShowPauseMenu - 일시정지 메뉴 켜고 게임 멈춤
        void ShowPauseMenu()
        {
            _pauseMenu.gameObject.SetActive(true);
            GameManager.Instance.SetMenuPaused(true);
        }
        #endregion

        #region HidePauseMenu - 일시정지 메뉴 끄고 게임 다시 돌림 (계속하기·Esc)
        void HidePauseMenu()
        {
            if (_pauseMenu != null) _pauseMenu.gameObject.SetActive(false);
            GameManager.Instance.SetMenuPaused(false);
        }
        #endregion

        #region OnToTitle - '타이틀로' 확인창 (확인하면 메뉴를 닫고 타이틀로)
        void OnToTitle() => ShowConfirm(ToTitleConfirmKey, ReturnToTitle);
        #endregion

        #region ReturnToTitle - 일시정지 메뉴를 닫아 게임 시간·입력·커서를 되돌린 뒤 타이틀로
        void ReturnToTitle()
        {
            HidePauseMenu();
            GameManager.Instance.ReturnToTitle();
        }
        #endregion

        #region OnCancel - Esc: 확인창 → 설정 창 → 대화 → 영입 창 → 게시판 창 → 메뉴 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸으면 상태가 먼저 받고(책상에서 일어나기 등), 상태가 안 받았고 플레이 씬이면 일시정지 메뉴를 엶
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancel(InputAction.CallbackContext context)
        {
            if (_confirm != null && _confirm.gameObject.activeSelf)
                HideConfirm();
            else if (_settings != null && _settings.gameObject.activeSelf)
                HideSettings();
            else if (_tutorialWindow != null && _tutorialWindow.IsOpen)
                HideTutorialWindow();
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

        #region IsWindowOpen - 일시정지 메뉴·설정 창·확인창·대화·영입 창·게시판 창 중 하나라도 떠 있는지 (메뉴 창과 같이 안 뜨게)
        bool IsWindowOpen() =>
            (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
            || (_settings != null && _settings.gameObject.activeSelf)
            || (_confirm != null && _confirm.gameObject.activeSelf)
            || IsHubWindowOpen();
        #endregion

        #region IsHubWindowOpen - 대화·영입 창·게시판 창·설명창 중 하나라도 떠 있는지
        bool IsHubWindowOpen() =>
            (_dialogue != null && _dialogue.IsOpen)
            || (_recruit != null && _recruit.IsOpen)
            || (_questBoard != null && _questBoard.IsOpen)
            || (_tutorialWindow != null && _tutorialWindow.IsOpen);
        #endregion

        #region RefreshMenuPause - 대화·영입·게시판 창이 하나라도 떠 있으면 게임 멈춤, 다 닫히면 되돌림 (창을 바꿔 열 때 안 풀리게 열기 → 닫기 순서로 부름)
        void RefreshMenuPause()
        {
            // 일시정지 메뉴·메뉴 창은 스스로 멈추고 풀어서 그대로 둠 (대화류와 같이 안 뜸)
            bool otherMenuOpen = (_pauseMenu != null && _pauseMenu.gameObject.activeSelf) || (_gameMenu != null && _gameMenu.IsOpen);
            if (otherMenuOpen) return;
            GameManager.Instance.SetMenuPaused(IsHubWindowOpen());
        }
        #endregion

        #region ShowDialogue - NPC 대화를 띄우고 게임 멈춤 (떠 있으면 내용만 바꿈)
        /// <param name="table">말한 사람·대사가 든 String Table</param>
        /// <param name="speakerKey">말한 사람 키</param>
        /// <param name="lineKey">대사 키</param>
        /// <param name="choices">선택지 (하나 이상)</param>
        /// <param name="lineArgs">대사의 {0}, {1}… 자리에 넣을 값</param>
        public void ShowDialogue(TableReference table, string speakerKey, string lineKey, IReadOnlyList<DialogueChoiceEntry> choices, params object[] lineArgs)
        {
            if (_dialogue == null)
            {
                Debug.LogError("[UI] DialogueController 연결 안 됨");
                return;
            }
            _dialogue.Show(table, speakerKey, lineKey, choices, lineArgs);
            RefreshMenuPause();
        }
        #endregion

        #region HideDialogue - NPC 대화를 닫음. 다른 창이 없으면 게임을 다시 돌림
        public void HideDialogue()
        {
            if (_dialogue != null) _dialogue.Hide();
            RefreshMenuPause();
        }
        #endregion

        #region ShowRecruit - 영입 창을 띄우고 게임 멈춤 (목록·할 일은 상태가 넘김)
        /// <param name="candidates">영입 후보</param>
        /// <param name="party">지금 파티</param>
        /// <param name="partyMax">파티에 넣을 수 있는 용병 수</param>
        /// <param name="onHire">영입을 누르면 할 일</param>
        /// <param name="onDismiss">내보내기를 누르면 할 일</param>
        public void ShowRecruit(IReadOnlyList<MercenaryEntry> candidates, IReadOnlyList<MercenaryEntry> party, int partyMax,
            Action<MercenaryEntry> onHire, Action<MercenaryEntry> onDismiss)
        {
            if (_recruit == null)
            {
                Debug.LogError("[UI] RecruitController 연결 안 됨");
                return;
            }
            _recruit.Show(candidates, party, partyMax, onHire, onDismiss);
            RefreshMenuPause();
        }
        #endregion

        #region HideRecruit - 영입 창을 닫음 (닫기 버튼·Esc). 다른 창이 없으면 게임을 다시 돌림
        public void HideRecruit()
        {
            if (_recruit != null) _recruit.Hide();
            RefreshMenuPause();
        }
        #endregion

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

        #region HideQuestBoard - 게시판 창을 닫음 (닫기 버튼·Esc). 다른 창이 없으면 게임을 다시 돌림
        public void HideQuestBoard()
        {
            if (_questBoard != null) _questBoard.Hide();
            RefreshMenuPause();
        }
        #endregion

        #region ShowTutorialWindow - 설명창을 띄우고 게임 멈춤. 닫히면 onClosed (닫기 버튼·Esc). 창이 없으면 바로 onClosed
        /// <param name="tutorial">보일 튜토리얼 (장이 있는 것)</param>
        /// <param name="onClosed">닫히면 할 일</param>
        public void ShowTutorialWindow(TutorialSO tutorial, Action onClosed)
        {
            if (_tutorialWindow == null)
            {
                Debug.LogError("[UI] TutorialWindowController 연결 안 됨");
                onClosed?.Invoke();
                return;
            }
            _tutorialWindow.Show(tutorial, onClosed);
            RefreshMenuPause();
        }
        #endregion

        #region HideTutorialWindow - 설명창을 닫음 (할 일을 부름). 다른 창이 없으면 게임을 다시 돌림
        public void HideTutorialWindow()
        {
            if (_tutorialWindow != null) _tutorialWindow.Hide();
            RefreshMenuPause();
        }
        #endregion

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

        #region SetJournalSource - 메뉴 창 임무 일지 탭에 보일 것을 만드는 출처를 넘김 (전초기지·숲이 들어올 때)
        /// <param name="source">지금 목표·받은 의뢰·날 수를 만드는 함수</param>
        public void SetJournalSource(Func<JournalSpec> source)
        {
            if (_menuJournal != null) _menuJournal.Bind(source);
        }
        #endregion

        #region ClearJournalSource - 메뉴 창 임무 일지 탭 출처를 놓음 (나갈 때. 다음에 열면 빈 일지)
        public void ClearJournalSource()
        {
            if (_menuJournal != null) _menuJournal.Unbind();
        }
        #endregion

        #region SetCodexSource - 메뉴 창 주문 도감 탭에 코덱스 캡슐·플레이어 이름을 넘김 (전초기지·숲이 들어올 때)
        /// <param name="codex">코덱스 캡슐</param>
        /// <param name="playerName">플레이어 이름</param>
        public void SetCodexSource(CodexService codex, string playerName)
        {
            if (_menuSpells != null) _menuSpells.Bind(codex, playerName);
        }
        #endregion

        #region ClearCodexSource - 메뉴 창 주문 도감 탭 출처를 놓음 (나갈 때. 다음에 열면 빈 도감)
        public void ClearCodexSource()
        {
            if (_menuSpells != null) _menuSpells.Unbind();
        }
        #endregion

        #region FadeThrough - 화면을 잠깐 검게 덮고 다 덮였을 때 onBlack, 걷힌 뒤 onDone. 이미 덮는 중이면 안 하고 false (연결이 없으면 바로 둘 다 부름)
        /// <param name="onBlack">다 덮였을 때 할 일</param>
        /// <param name="onDone">다 걷힌 뒤 할 일</param>
        public bool FadeThrough(Action onBlack, Action onDone)
        {
            if (_fade != null) return _fade.Play(onBlack, onDone);
            onBlack?.Invoke();
            onDone?.Invoke();
            return true;
        }
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

        #region ShowTooltip - 툴팁 띄우기 (TooltipHandler 알림)
        /// <param name="title">제목</param>
        /// <param name="body">설명 (비면 제목만)</param>
        void ShowTooltip(LocalizedString title, LocalizedString body)
        {
            if (_tooltip == null)
            {
                Debug.LogError("[UI] TooltipController 연결 안 됨");
                return;
            }
            _tooltip.Show(title, body);
        }
        #endregion

        #region HideTooltip - 툴팁 숨기기 (TooltipHandler 알림)
        void HideTooltip()
        {
            if (_tooltip != null) _tooltip.Hide();
        }
        #endregion

        #region OnButtonClicked - 버튼 클릭음 알림 (ClickSoundHandler): 클릭음을 2D로 냄 (소리 담당이 없으면 무음)
        /// <param name="sound">낼 클릭음</param>
        void OnButtonClicked(SoundDataSO sound)
        {
            // 소리를 낼 곳 (Boot 상주)
            AudioManager audio = AudioManager.Instance;
            if (audio != null) audio.Play(sound);
        }
        #endregion
    }
}
