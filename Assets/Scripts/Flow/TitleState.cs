namespace Mage
{
    /// <summary>초기 화면. 새 게임 → 캐릭터 생성</summary>
    public sealed class TitleState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Title";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        #region OnSceneReady - 새 게임 버튼 연결
        protected override void OnSceneReady()
        {
            // 이 씬의 화면 참조
            var view = FindView<TitleView>();
            if (view == null) return;
            view.NewGameButton.onClick.AddListener(OnNewGame);
        }
        #endregion

        #region OnNewGame - 캐릭터 생성으로
        void OnNewGame() => Game.ChangeState(new CharacterCreationState());
        #endregion
    }
}
