using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>전초기지. 첫 도착이면 역마차 연출부터. 1인칭으로 돌아다니며 상호작용 키(F)로 여관(방·식량)·길드(영입·게시판)·정문(경비병)을 씀 → 숲</summary>
    public sealed class HubState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Outpost";

        /// <summary>파티에 넣을 수 있는 용병 수 (플레이어 + 2명). 첫 출발에도 이만큼 필요</summary>
        public const int RequiredMercenaries = 2;

        /// <summary>여관 방값 (금화, 처음 방 잡을 때 한 번. 임시)</summary>
        public const int RoomPrice = 70;

        /// <summary>원정 식량 1인분 값 (금화, 한 사람 하루치. 임시)</summary>
        public const int RationPrice = 3;

        /// <summary>여관 주인 지점 id</summary>
        const string InnkeeperId = "innkeeper";

        /// <summary>길드 접수원 지점 id</summary>
        const string ClerkId = "clerk";

        /// <summary>임무 게시판 지점 id</summary>
        const string BoardId = "board";

        /// <summary>정문 경비병 지점 id</summary>
        const string GuardId = "guard";

        /// <summary>여관 방 문 지점 id (방을 잡아야 열림)</summary>
        const string RoomDoorId = "room_door";

        /// <summary>경비병: 용병이 모자랄 때 대사 키 ({0} 필요한 인원)</summary>
        const string PartyRequiredKey = "hub.party_required";

        /// <summary>의뢰를 받았을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestTakenKey = "hub.quest_taken";

        /// <summary>경비병: 의뢰 없이 나가려 할 때 대사 키</summary>
        const string QuestRequiredKey = "hub.quest_required";

        /// <summary>방을 안 잡고 방 문을 쓸 때 토스트 키</summary>
        const string RoomLockedKey = "hub.room_locked";

        /// <summary>여관 주인 이름 키 (지점 이름·말한 사람)</summary>
        const string InnkeeperKey = "hub.innkeeper";

        /// <summary>길드 접수원 이름 키</summary>
        const string ClerkKey = "hub.clerk";

        /// <summary>경비병 이름 키</summary>
        const string GuardKey = "hub.guard";

        /// <summary>여관 주인: 처음 말 걸 때 대사 키</summary>
        const string InnGreetKey = "hub.innkeeper.greet";

        /// <summary>여관 주인: 방을 잡은 뒤 대사 키 (길드를 알려 줌)</summary>
        const string InnRentedKey = "hub.innkeeper.rented";

        /// <summary>여관 주인: 식량을 판 뒤 대사 키 ({0} 지금 식량 인분)</summary>
        const string InnFoodKey = "hub.innkeeper.food_bought";

        /// <summary>여관 주인: 금화가 모자랄 때 대사 키</summary>
        const string InnNoGoldKey = "hub.innkeeper.no_gold";

        /// <summary>길드 접수원: 말 걸 때 대사 키</summary>
        const string ClerkGreetKey = "hub.clerk.greet";

        /// <summary>경비병: 식량이 모자랄 때 대사 키 ({0} 필요한 인분)</summary>
        const string GuardNoFoodKey = "hub.guard.no_food";

        /// <summary>경비병: 나갈 수 있을 때 대사 키</summary>
        const string GuardReadyKey = "hub.guard.ready";

        /// <summary>경비병: 숲에서 돌아왔을 때 자막 키</summary>
        const string GuardWelcomeKey = "hub.guard.welcome";

        /// <summary>선택지: 방 잡기 키 ({0} 방값)</summary>
        const string ChoiceRentKey = "hub.choice.rent";

        /// <summary>선택지: 식량 하루치 사기 키 ({0} 값)</summary>
        const string ChoiceFoodKey = "hub.choice.food";

        /// <summary>선택지: 용병 구하기 키</summary>
        const string ChoiceRecruitKey = "hub.choice.recruit";

        /// <summary>선택지: 출발 키</summary>
        const string ChoiceDepartKey = "hub.depart";

        /// <summary>선택지: 아직 (안 나감) 키</summary>
        const string ChoiceStayKey = "hub.choice.stay";

        /// <summary>선택지: 알았어 키</summary>
        const string ChoiceOkKey = "hub.choice.ok";

        /// <summary>선택지: 그만 (대화 끝) 키</summary>
        const string ChoiceByeKey = "hub.choice.bye";

        /// <summary>이동 튜토리얼 id</summary>
        const string MoveTutorialId = "move";

        /// <summary>둘러보기 튜토리얼 id</summary>
        const string LookTutorialId = "look";

        /// <summary>달리기 튜토리얼 id</summary>
        const string SprintTutorialId = "sprint";

        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 상호작용 키를 쓰면 배움)</summary>
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

        /// <summary>이번 진입이 숲에서 돌아온 것인지 (정문 밖에서 시작. OnSceneReady에서 정함)</summary>
        bool _returning;

        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 숲에서 돌아왔으면 정문 밖, 아니면 시작 위치. 파티 수·금화 표시, 안내 글 숨김
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _arriving = !Game.Run.HasArrived;
            _returning = !_arriving && Game.Run.ReturningFromField;
            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _returning ? _view.ReturnPoint : _view.Spawn);
            _view.Player.gameObject.SetActive(!_arriving);
            _view.Hud.SetActive(!_arriving);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
            RefreshMoney();
        }
        #endregion

        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작. 숲에서 돌아왔으면 의뢰를 비우고 경비병 자막 → 끝나면 목표 알림
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            if (_arriving)
            {
                _view.Opening.Finished += OnOpeningFinished;
                _view.Opening.Play();
                return;
            }
            StartControl();
            if (!_returning)
            {
                UpdateObjective(false);
                return;
            }
            // 숲에서 돌아옴: 받은 의뢰는 다녀온 걸로 비움 (정산은 나중)
            Game.Run.ReturningFromField = false;
            Game.Run.Quest = null;
            UpdateObjective(false);
            UIManager.Instance.ShowSubtitle(new LocalizedString(Table, GuardKey), new LocalizedString(Table, GuardWelcomeKey), NotifyObjective);
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
            UpdateObjective(false);
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

        #region StartControl - 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감), 안 배운 조작 안내 알림
        void StartControl()
        {
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            SetControl(true);
            // 안 배운 조작 안내만 차례로 12시에 (배운 건 TutorialManager가 거름)
            TutorialManager.Instance.Notify(MoveTutorialId);
            TutorialManager.Instance.Notify(LookTutorialId);
            TutorialManager.Instance.Notify(SprintTutorialId);
        }
        #endregion

        #region SetControl - 조작·지점 찾기 켜고 끄기 (문을 드나드는 페이드 동안 끔. 씬이 내려갔으면 무시)
        /// <param name="on">켤지</param>
        void SetControl(bool on)
        {
            if (_view == null) return;
            _view.Player.enabled = on;
            _view.Detector.enabled = on;
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
                SetControl(false);
            }
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김
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

        #region OnInteracted - 처음 쓰면 상호작용 배움. 문이면 드나들고, 아니면 지점 id로 나눔: 여관 주인·접수원·게시판·경비병. 모르는 id는 경고
        /// <param name="target">상호작용 키를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
            if (target.TryGetComponent(out DoorView door))
            {
                UseDoor(target, door);
                return;
            }
            switch (target.Id)
            {
                case InnkeeperId: TalkInnkeeper(InnGreetKey); break;
                case ClerkId: TalkClerk(); break;
                case BoardId: OpenBoard(); break;
                case GuardId: TalkGuard(); break;
                default: Debug.LogWarning($"[Hub] 모르는 지점 id: {target.Id} ({target.name})"); break;
            }
        }
        #endregion

        #region UseDoor - 방 문은 방을 잡아야 열림. 화면이 덮인 사이 도착 지점으로 옮기고, 다 걷히면 조작을 돌려줌
        /// <param name="target">쓴 문 지점</param>
        /// <param name="door">문 도착 지점</param>
        void UseDoor(InteractPointView target, DoorView door)
        {
            if (door.Destination == null)
            {
                Debug.LogError($"[Hub] 문 도착 지점 없음: {target.name}", target);
                return;
            }
            if (string.Equals(target.Id, RoomDoorId, StringComparison.Ordinal) && !Game.Run.HasRoom)
            {
                UIManager.Instance.ShowToast(Table, RoomLockedKey);
                return;
            }
            if (!UIManager.Instance.FadeThrough(() => _view.Player.PlaceAt(door.Destination), () => SetControl(true))) return;
            SetControl(false);
        }
        #endregion

        #region TalkInnkeeper - 여관 주인 대화: 방 잡기(안 잡았을 때)·식량 하루치 사기·그만
        /// <param name="lineKey">이번 대사 키</param>
        /// <param name="lineArgs">대사 자리 값</param>
        void TalkInnkeeper(string lineKey, params object[] lineArgs)
        {
            // 이번 선택지
            var choices = new List<DialogueChoiceEntry>();
            if (!Game.Run.HasRoom) choices.Add(new DialogueChoiceEntry(Table, ChoiceRentKey, RentRoom, RoomPrice));
            choices.Add(new DialogueChoiceEntry(Table, ChoiceFoodKey, BuyFood, DayFoodPrice()));
            choices.Add(new DialogueChoiceEntry(Table, ChoiceByeKey, UIManager.Instance.HideDialogue));
            UIManager.Instance.ShowDialogue(Table, InnkeeperKey, lineKey, choices, lineArgs);
        }
        #endregion

        #region RentRoom - 금화가 되면 방값을 내고 방을 잡음 (한 번). 주인이 길드를 알려 주고 목표 갱신
        void RentRoom()
        {
            if (Game.Run.Gold < RoomPrice)
            {
                TalkInnkeeper(InnNoGoldKey);
                return;
            }
            Game.Run.Gold -= RoomPrice;
            Game.Run.HasRoom = true;
            RefreshMoney();
            TalkInnkeeper(InnRentedKey);
            UpdateObjective(true);
        }
        #endregion

        #region BuyFood - 금화가 되면 지금 인원의 하루치 식량을 삼
        void BuyFood()
        {
            // 하루치 값 (금화)
            int price = DayFoodPrice();
            if (Game.Run.Gold < price)
            {
                TalkInnkeeper(InnNoGoldKey);
                return;
            }
            Game.Run.Gold -= price;
            Game.Run.Rations += PartySize();
            RefreshMoney();
            TalkInnkeeper(InnFoodKey, Game.Run.Rations);
        }
        #endregion

        #region TalkClerk - 길드 접수원 대화: 용병 구하기·그만
        void TalkClerk()
        {
            // 이번 선택지
            var choices = new[]
            {
                new DialogueChoiceEntry(Table, ChoiceRecruitKey, OpenRecruit),
                new DialogueChoiceEntry(Table, ChoiceByeKey, UIManager.Instance.HideDialogue),
            };
            UIManager.Instance.ShowDialogue(Table, ClerkKey, ClerkGreetKey, choices);
        }
        #endregion

        #region OpenRecruit - 처음이면 후보를 뽑고 영입 창을 엶 (대화는 창을 연 뒤 닫아서 게임이 안 풀림)
        void OpenRecruit()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (!run.CandidatesDrawn)
            {
                run.RecruitCandidates.AddRange(RecruitPool.Draw(DataManager.Instance.Jobs));
                run.CandidatesDrawn = true;
            }
            UIManager.Instance.ShowRecruit(run.RecruitCandidates, run.Party, RequiredMercenaries, Hire, Dismiss);
            UIManager.Instance.HideDialogue();
        }
        #endregion

        #region Hire - 후보를 파티로 옮김 (자리가 있을 때)
        /// <param name="entry">영입할 후보</param>
        void Hire(MercenaryEntry entry)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Party.Count >= RequiredMercenaries || !run.RecruitCandidates.Remove(entry)) return;
            run.Party.Add(entry);
            RefreshParty();
            UpdateObjective(true);
        }
        #endregion

        #region Dismiss - 파티원을 후보로 되돌림
        /// <param name="entry">내보낼 파티원</param>
        void Dismiss(MercenaryEntry entry)
        {
            if (!Game.Run.Party.Remove(entry)) return;
            Game.Run.RecruitCandidates.Add(entry);
            RefreshParty();
            UpdateObjective(true);
        }
        #endregion

        #region OpenBoard - 게시판 창: 첫 의뢰 (지금은 하나뿐)
        void OpenBoard()
        {
            // 게시판 의뢰 목록
            IReadOnlyList<QuestSO> quests = DataManager.Instance.Quests;
            if (quests.Count == 0 || quests[0] == null)
            {
                Debug.LogWarning("[Hub] 게시판에 붙일 의뢰 없음");
                return;
            }
            // 붙어 있는 의뢰
            QuestSO quest = quests[0];
            UIManager.Instance.ShowQuestBoard(quest, Game.Run.Quest == quest, () => TakeQuest(quest));
        }
        #endregion

        #region TakeQuest - 의뢰를 받음. 목표가 바뀌면 목표 알림, 아니면 "의뢰를 받았어" 토스트
        /// <param name="quest">받을 의뢰</param>
        void TakeQuest(QuestSO quest)
        {
            Game.Run.Quest = quest;
            if (!UpdateObjective(true)) UIManager.Instance.ShowToast(Table, QuestTakenKey, quest.Title.GetLocalizedString());
        }
        #endregion

        #region TalkGuard - 정문 경비병: 첫 출발 전엔 파티·의뢰·식량이 모자라면 말로 막음. 됐거나 첫 출발 뒤면 출발·아직
        void TalkGuard()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (!run.HasDeparted)
            {
                if (run.Party.Count < RequiredMercenaries)
                {
                    GuardLine(PartyRequiredKey, RequiredMercenaries);
                    return;
                }
                if (run.Quest == null)
                {
                    GuardLine(QuestRequiredKey);
                    return;
                }
                if (run.Rations < NeededRations())
                {
                    GuardLine(GuardNoFoodKey, NeededRations());
                    return;
                }
            }
            // 나갈 때 선택지
            var choices = new[]
            {
                new DialogueChoiceEntry(Table, ChoiceDepartKey, Depart),
                new DialogueChoiceEntry(Table, ChoiceStayKey, UIManager.Instance.HideDialogue),
            };
            UIManager.Instance.ShowDialogue(Table, GuardKey, GuardReadyKey, choices);
        }
        #endregion

        #region GuardLine - 경비병 한마디 + 알았어
        /// <param name="lineKey">대사 키</param>
        /// <param name="lineArgs">대사 자리 값</param>
        void GuardLine(string lineKey, params object[] lineArgs)
        {
            // 하나뿐인 선택지
            var choices = new[] { new DialogueChoiceEntry(Table, ChoiceOkKey, UIManager.Instance.HideDialogue) };
            UIManager.Instance.ShowDialogue(Table, GuardKey, lineKey, choices, lineArgs);
        }
        #endregion

        #region Depart - 원정 식량을 빼고(모자라면 있는 만큼) 출발 표시, 대화를 닫고 숲으로
        void Depart()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            run.Rations -= Mathf.Min(run.Rations, NeededRations());
            run.HasDeparted = true;
            UpdateObjective(false);
            UIManager.Instance.HideDialogue();
            Game.ChangeState(new FieldState());
        }
        #endregion

        #region PartySize - 원정 인원 (플레이어 + 파티 용병)
        int PartySize() => 1 + Game.Run.Party.Count;
        #endregion

        #region DayFoodPrice - 지금 인원의 하루치 식량 값 (금화)
        int DayFoodPrice() => RationPrice * PartySize();
        #endregion

        #region NeededRations - 받은 의뢰에 필요한 식량 (인원 × 기간 인분. 의뢰가 없으면 0)
        int NeededRations() => Game.Run.Quest != null ? Game.Run.Quest.Days * PartySize() : 0;
        #endregion

        #region NextObjective - 판 데이터로 지금 목표를 정함: 방 → 길드(파티·의뢰) → 정문 → (첫 출발 뒤) 없음
        ObjectiveSO NextObjective()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (!run.HasRoom) return _view.FirstObjective;
            if (run.Party.Count < RequiredMercenaries || run.Quest == null) return _view.GuildObjective;
            return run.HasDeparted ? null : _view.DepartObjective;
        }
        #endregion

        #region UpdateObjective - 목표가 바뀌었으면 판 데이터에 넣고 (notify면) 알림. 바뀌었으면 true
        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
        bool UpdateObjective(bool notify)
        {
            // 판 데이터로 정한 목표
            ObjectiveSO next = NextObjective();
            if (next == Game.Run.CurrentObjective) return false;
            Game.Run.CurrentObjective = next;
            if (notify) NotifyObjective();
            return true;
        }
        #endregion

        #region RefreshParty - 파티 수 표시 갱신 ({0}/{1})
        void RefreshParty()
        {
            _view.PartyText.StringReference.Arguments = new object[] { Game.Run.Party.Count, RequiredMercenaries };
            _view.PartyText.RefreshString();
        }
        #endregion

        #region RefreshMoney - 금화·식량 표시 갱신 ({0} 금화, {1} 식량 인분)
        void RefreshMoney()
        {
            _view.MoneyText.StringReference.Arguments = new object[] { Game.Run.Gold, Game.Run.Rations };
            _view.MoneyText.RefreshString();
        }
        #endregion
    }
}
