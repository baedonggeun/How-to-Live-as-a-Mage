using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Mage
{
    /// <summary>초기 화면. 새 게임 → 캐릭터 생성, 설정 창, 종료 확인</summary>
    public sealed class TitleState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Title";

        /// <summary>종료 확인창 문구 키 (UI 테이블)</summary>
        const string QuitConfirmKey = "title.quit_confirm";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>이 씬의 화면 참조</summary>
        TitleView _view;

        /// <summary>'클릭하여 계속' 입력 구독. 누르면 CallOnce가 풀고, 누르기 전에 나가면 Exit가 풂. 없으면 null</summary>
        System.IDisposable _pressSubscription;

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

        #region OnNewGame - 캐릭터 생성으로
        void OnNewGame() => Game.ChangeState(new CharacterCreationState());
        #endregion

        #region OnSettings - 설정 창 열기
        static void OnSettings() => UIManager.Instance.ShowSettings();
        #endregion

        #region OnQuit - 종료 확인창 열기
        static void OnQuit() => UIManager.Instance.ShowConfirm(QuitConfirmKey, QuitGame);
        #endregion

        #region QuitGame - 게임 종료 (에디터는 Play만 멈춤. Application.Quit은 에디터에서 무시됨)
        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}
