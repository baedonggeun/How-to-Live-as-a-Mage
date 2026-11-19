namespace Mage
{
    /// <summary>전초기지. 임시 영입과 출발 검사 → 숲</summary>
    public sealed class HubState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Outpost";

        /// <summary>출발에 필요한 용병 수</summary>
        public const int RequiredMercenaries = 2;

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>이 씬의 화면 참조</summary>
        HubView _view;

        #region OnSceneReady - 영입·출발 버튼 연결, 파티 수 표시
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _view.RecruitButton.onClick.AddListener(OnRecruit);
            _view.DepartButton.onClick.AddListener(OnDepart);
            RefreshParty();
        }
        #endregion

        #region OnRecruit - 임시 영입: 파티가 비었을 때만 고정 용병을 넣음 (4단계에서 후보 선택으로 바뀜)
        void OnRecruit()
        {
            if (Game.Run.Party.Count >= RequiredMercenaries) return;
            Game.Run.Party.AddRange(RunEntry.StubMercenaries());
            RefreshParty();
        }
        #endregion

        #region OnDepart - 파티가 모자라면 토스트, 아니면 임시 의뢰를 받고 숲으로
        void OnDepart()
        {
            if (Game.Run.Party.Count < RequiredMercenaries)
            {
                UIManager.Instance.ShowToast("hub.party_required", RequiredMercenaries);
                return;
            }
            Game.Run.QuestTitle = RunEntry.StubQuest();
            Game.ChangeState(new FieldState());
        }
        #endregion

        #region RefreshParty - 파티 수 표시 갱신 ({0}/{1})
        void RefreshParty()
        {
            _view.PartyText.StringReference.Arguments = new object[] { Game.Run.Party.Count, RequiredMercenaries };
            _view.PartyText.RefreshString();
        }
        #endregion
    }
}
