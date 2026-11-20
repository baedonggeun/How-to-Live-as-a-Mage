using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>전초기지. 첫 도착이면 역마차 연출부터. 1인칭으로 돌아다니며 E 지점에서 임시 영입·의뢰 받기·출발 → 숲</summary>
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

        /// <summary>이동 튜토리얼 id</summary>
        const string MoveTutorialId = "move";

        /// <summary>둘러보기 튜토리얼 id</summary>
        const string LookTutorialId = "look";

        /// <summary>달리기 튜토리얼 id</summary>
        const string SprintTutorialId = "sprint";

        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 E를 쓰면 배움)</summary>
        const string InteractTutorialId = "interact";

        /// <summary>목표가 바뀌었을 때 토스트 키 ({0} 목표 이름, Menu 테이블)</summary>
        const string ObjectiveUpdatedKey = "menu.journal.updated";

        /// <summary>전초기지 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬)</summary>
        public override bool UsesPauseMenu => true;

        /// <summary>이 씬의 화면 참조</summary>
        HubView _view;

        /// <summary>이번 진입이 첫 도착이라 역마차 연출을 트는지 (OnSceneReady에서 정함)</summary>
        bool _arriving;

        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 아니면 시작 위치. 파티 수 표시, 안내 글 숨김
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _arriving = !Game.Run.HasArrived;
            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _view.Spawn);
            _view.Player.gameObject.SetActive(!_arriving);
            _view.Hud.SetActive(!_arriving);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
        }
        #endregion

        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            if (!_arriving)
            {
                StartControl();
                return;
            }
            _view.Opening.Finished += OnOpeningFinished;
            _view.Opening.Play();
        }
        #endregion

        #region OnOpeningFinished - 연출이 끝남: 도착 표시, 플레이어·화면 글자 켜고 조작 시작 (연출 카메라는 이미 꺼져서 같은 프레임에 플레이어 카메라로 넘어감). 첫 목표를 정하고 동승 용병 대사 자막 → 끝나면 임무 일지 갱신 알림
        void OnOpeningFinished()
        {
            _view.Opening.Finished -= OnOpeningFinished;
            _arriving = false;
            Game.Run.HasArrived = true;
            _view.Player.gameObject.SetActive(true);
            _view.Hud.SetActive(true);
            StartControl();
            Game.Run.CurrentObjective = _view.FirstObjective;
            UIManager.Instance.ShowSubtitle(_view.ArrivalSpeaker, _view.ArrivalLine, NotifyObjective);
        }
        #endregion

        #region NotifyObjective - 지금 목표가 있으면 "임무 일지 갱신: 이름" 토스트
        void NotifyObjective()
        {
            // 이번 판 현재 목표
            ObjectiveSO objective = Game.Run.CurrentObjective;
            if (objective == null) return;
            UIManager.Instance.ShowToast(MenuTable, ObjectiveUpdatedKey, objective.Title.GetLocalizedString());
        }
        #endregion

        #region StartControl - 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 E는 구독 전이라 안 들어감), 안 배운 조작 안내 알림
        void StartControl()
        {
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.Player.enabled = true;
            _view.Detector.enabled = true;
            // 안 배운 조작 안내만 차례로 12시에 (배운 건 TutorialManager가 거름)
            TutorialManager.Instance.Notify(MoveTutorialId);
            TutorialManager.Instance.Notify(LookTutorialId);
            TutorialManager.Instance.Notify(SprintTutorialId);
        }
        #endregion

        #region Exit - 연출 중이면 알림 없이 멈춤, 알림 구독 해제, 조작·지점 찾기 끔, 커서 풀기, 조작 안내·자막 정리
        public override void Exit()
        {
            if (_view != null)
            {
                _view.Opening.Finished -= OnOpeningFinished;
                _view.Opening.Cancel();
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.Player.enabled = false;
                _view.Detector.enabled = false;
            }
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
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

        #region OnInteracted - 처음 쓰면 상호작용 배움. 지점 id로 나눔: 영입·의뢰 받기·출발. 모르는 id는 경고
        /// <param name="target">E를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
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
