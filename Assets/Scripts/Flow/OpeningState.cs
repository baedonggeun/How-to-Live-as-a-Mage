namespace Mage
{
    /// <summary>오프닝. 계속 → 전초기지</summary>
    public sealed class OpeningState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Opening";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        #region OnSceneReady - 계속 버튼 연결
        protected override void OnSceneReady()
        {
            // 이 씬의 화면 참조
            var view = FindView<OpeningView>();
            if (view == null) return;
            view.ContinueButton.onClick.AddListener(OnContinue);
        }
        #endregion

        #region OnContinue - 전초기지로
        void OnContinue() => Game.ChangeState(new HubState());
        #endregion
    }
}
