using UnityEngine;

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

        #region OnSceneReady - 버튼 연결, 이어하기 끔, 배경음 재생
        protected override void OnSceneReady()
        {
            // 이 씬의 화면 참조
            var view = FindView<TitleView>();
            if (view == null) return;
            view.NewGameButton.onClick.AddListener(OnNewGame);
            // 저장이 없어서 이어할 판이 없음 (저장은 프로토타입 범위 밖)
            view.ContinueButton.interactable = false;
            view.SettingsButton.onClick.AddListener(OnSettings);
            view.QuitButton.onClick.AddListener(OnQuit);
            // 비어 있으면 AudioManager가 무시함
            AudioManager.Instance.PlayBgm(view.Bgm);
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
