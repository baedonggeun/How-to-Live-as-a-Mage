using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·자막·설정 창·확인창·일시정지 메뉴·메뉴 창(지도·임무 일지·튜토리얼·주문 도감)·툴팁</summary>
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

        #region InitWindows - 설정 창·확인창·일시정지 메뉴를 끄고 버튼을 한 번만 연결, Esc 구독, 메뉴 창·자막·툴팁 끄기, M·J 구독
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

            _cancel = InputSystem.actions.FindAction(CancelActionPath);
            if (_cancel == null)
                Debug.LogError($"[UI] {CancelActionPath} 액션 없음. Esc로 창이 안 닫힘");
            else
                _cancel.performed += OnCancel;
        }
        #endregion

        #region OnDestroy - Esc·M·J 구독 해제
        protected override void OnDestroy()
        {
            if (_cancel != null) _cancel.performed -= OnCancel;
            if (_map != null) _map.performed -= OnMap;
            if (_journal != null) _journal.performed -= OnJournal;
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
    }
}
