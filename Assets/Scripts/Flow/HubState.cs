using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>전초기지. 1인칭으로 돌아다니며 E 지점에서 임시 영입·의뢰 받기·출발 → 숲</summary>
    public sealed class HubState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Outpost";

        /// <summary>출발에 필요한 용병 수</summary>
        public const int RequiredMercenaries = 2;

        /// <summary>영입소 지점 id</summary>
        const string RecruitId = "recruit";

        /// <summary>게시판 지점 id</summary>
        const string BoardId = "board";

        /// <summary>출발 표지판 지점 id</summary>
        const string DepartId = "depart";

        /// <summary>파티가 모자랄 때 토스트 키 ({0} 필요한 인원)</summary>
        const string PartyRequiredKey = "hub.party_required";

        /// <summary>의뢰를 받았을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestTakenKey = "hub.quest_taken";

        /// <summary>의뢰 없이 출발할 때 토스트 키</summary>
        const string QuestRequiredKey = "hub.quest_required";

        /// <summary>전초기지 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬)</summary>
        public override bool UsesPauseMenu => true;

        /// <summary>이 씬의 화면 참조</summary>
        HubView _view;

        #region OnSceneReady - 플레이어를 시작 위치에, 파티 수 표시, 안내 글 숨김
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _view.Player.PlaceAt(_view.Spawn);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
        }
        #endregion

        #region Reveal - 커서 잠금, 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 E는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.Player.enabled = true;
            _view.Detector.enabled = true;
        }
        #endregion

        #region Exit - 지점 알림 구독 해제, 조작·지점 찾기 끔, 커서 풀기
        public override void Exit()
        {
            if (_view != null)
            {
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.Player.enabled = false;
                _view.Detector.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[E] 이름", 없으면 숨김
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글
            var prompt = _view.InteractPrompt;
            if (target == null)
            {
                prompt.gameObject.SetActive(false);
                return;
            }
            prompt.StringReference.Arguments = new object[] { _view.Detector.KeyName, target.Label.GetLocalizedString() };
            prompt.gameObject.SetActive(true);
            prompt.RefreshString();
        }
        #endregion

        #region OnInteracted - 지점 id로 나눔: 영입·의뢰 받기·출발. 모르는 id는 경고
        /// <param name="target">E를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            if (string.Equals(target.Id, RecruitId, StringComparison.Ordinal)) Recruit();
            else if (string.Equals(target.Id, BoardId, StringComparison.Ordinal)) TakeQuest();
            else if (string.Equals(target.Id, DepartId, StringComparison.Ordinal)) Depart();
            else Debug.LogWarning($"[Hub] 모르는 지점 id: {target.Id} ({target.name})");
        }
        #endregion

        #region Recruit - 임시 영입: 파티가 비었을 때만 고정 용병을 넣음 (4번에서 후보 선택으로 바뀜)
        void Recruit()
        {
            if (Game.Run.Party.Count >= RequiredMercenaries) return;
            Game.Run.Party.AddRange(RunEntry.StubMercenaries());
            RefreshParty();
        }
        #endregion

        #region TakeQuest - 임시 의뢰 받기 + 토스트 (4번에서 게시판 창으로 바뀜)
        void TakeQuest()
        {
            Game.Run.QuestTitle = RunEntry.StubQuest();
            UIManager.Instance.ShowToast(Table, QuestTakenKey, Game.Run.QuestTitle.GetLocalizedString());
        }
        #endregion

        #region Depart - 파티가 모자라거나 의뢰가 없으면 토스트, 아니면 숲으로
        void Depart()
        {
            if (Game.Run.Party.Count < RequiredMercenaries)
            {
                UIManager.Instance.ShowToast(Table, PartyRequiredKey, RequiredMercenaries);
                return;
            }
            if (Game.Run.QuestTitle == null)
            {
                UIManager.Instance.ShowToast(Table, QuestRequiredKey);
                return;
            }
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
