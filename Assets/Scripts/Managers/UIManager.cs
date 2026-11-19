using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>씬을 넘어 쓰는 공용 UI 담당: 토스트·설정 창·확인창·일시정지 메뉴</summary>
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>토스트 표시 시간 (초)</summary>
        const float ToastSeconds = 2f;

        /// <summary>창 닫기·일시정지 메뉴 입력 액션 경로 (전역 액션, Esc)</summary>
        const string CancelActionPath = "UI/Cancel";

        /// <summary>일시정지 메뉴 '타이틀로' 확인창 문구 키 (UI 테이블)</summary>
        const string ToTitleConfirmKey = "pause.to_title_confirm";

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

        #region InitWindows - 설정 창·확인창·일시정지 메뉴를 끄고 버튼을 한 번만 연결, Esc 구독
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

            _cancel = InputSystem.actions.FindAction(CancelActionPath);
            if (_cancel == null)
                Debug.LogError($"[UI] {CancelActionPath} 액션 없음. Esc로 창이 안 닫힘");
            else
                _cancel.performed += OnCancel;
        }
        #endregion

        #region OnDestroy - Esc 구독 해제
        protected override void OnDestroy()
        {
            if (_cancel != null) _cancel.performed -= OnCancel;
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

        #region OnCancel - Esc: 확인창 → 설정 창 → 일시정지 메뉴 순으로 열린 것을 닫음. 아무것도 안 열렸고 플레이 씬이면 일시정지 메뉴를 엶
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancel(InputAction.CallbackContext context)
        {
            if (_confirm != null && _confirm.gameObject.activeSelf)
                HideConfirm();
            else if (_settings != null && _settings.gameObject.activeSelf)
                HideSettings();
            else if (_pauseMenu != null && _pauseMenu.gameObject.activeSelf)
                HidePauseMenu();
            else if (_pauseMenu != null && GameManager.Instance.CanPause)
                ShowPauseMenu();
        }
        #endregion
    }
}
