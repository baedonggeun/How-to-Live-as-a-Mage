using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>전초기지. 첫 도착이면 역마차 연출부터. 1인칭으로 돌아다니며 상호작용 키(F)로 여관(방·식량)·여관 방 책상(코덱스: 룬 주문·다중 룬 마법·주문 연습)·길드(영입·게시판)·정문(경비병)을 씀 → 숲. 여관·원정·영입·의뢰·임무 일지·코덱스·책상은 캡슐이 맡고, 여기선 캡슐을 만들어 이음 (대화 선택지·금화·토스트·목표·화면 글자·화면 전환·주문 연습 소리)</summary>
    public sealed class HubState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Outpost";

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

        /// <summary>여관 방 책상 위 코덱스 지점 id</summary>
        const string CodexId = "codex";

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

        /// <summary>숲에서 죽어 여관 방에서 깨어났을 때 토스트 키</summary>
        const string RespawnInnKey = "hub.respawn.inn";

        /// <summary>숲에서 죽어 정문에서 깨어났을 때 토스트 키 (방을 안 잡았을 때)</summary>
        const string RespawnGateKey = "hub.respawn.gate";

        /// <summary>의뢰를 끝냈을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestDoneKey = "hub.quest_done";

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

        /// <summary>주문 그리기 튜토리얼 id (책상 연습에서 판정하면 배움, 직접 취소는 뺌)</summary>
        const string DrawTutorialId = "draw";

        /// <summary>마법 등록 토스트 키 ({0} 마법 이름, Codex 테이블)</summary>
        const string SpellRegisteredKey = "codex.toast.registered";

        /// <summary>전초기지 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

        /// <summary>코덱스 문구 테이블 (마법 등록 토스트)</summary>
        static readonly TableReference CodexTable = "Codex";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬)</summary>
        public override bool UsesPauseMenu => true;

        /// <summary>이 씬의 화면 참조</summary>
        HubView _view;

        /// <summary>여관 캡슐</summary>
        InnService _inn;

        /// <summary>원정 캡슐</summary>
        ExpeditionService _expedition;

        /// <summary>용병 파티 캡슐 (영입 쪽)</summary>
        RecruitService _recruit;

        /// <summary>의뢰 캡슐 (전초기지 쪽)</summary>
        QuestService _quest;

        /// <summary>책상 캡슐</summary>
        DeskService _desk;

        /// <summary>코덱스 캡슐 (판정·이름·등록. 책상 코덱스·메뉴 주문 도감에 넘김)</summary>
        CodexService _codex;

        /// <summary>임무 일지 캡슐 (지금 목표)</summary>
        JournalService _journal;

        /// <summary>이번 진입이 첫 도착이라 역마차 연출을 트는지 (OnSceneReady에서 정함)</summary>
        bool _arriving;

        /// <summary>이번 진입이 숲에서 돌아온 것인지 (정문 밖에서 시작. OnSceneReady에서 정함)</summary>
        bool _returning;

        /// <summary>이번 진입이 숲에서 죽어 부활한 것인지 (여관 방 침대·정문에서 시작. OnSceneReady에서 정함)</summary>
        bool _respawning;

        #region OnSceneReady - 캡슐(여관·원정·영입·의뢰·임무 일지·코덱스·책상)을 만들어 잇고 (코덱스 등록 → 토스트, 책상 연습 판정 → 소리·튜토리얼, 메뉴 임무 일지·주문 도감에 출처), 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 숲에서 죽었으면 여관 방 침대(방이 없으면 정문 안쪽), 숲에서 돌아왔으면 정문 밖, 아니면 시작 위치. 파티 수·금화 표시, 안내 글 숨김, 코덱스 표지(이름·마법진), 스태미나 막대
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            _inn = new InnService(run.Inn);
            _expedition = new ExpeditionService(run.Expedition);
            _recruit = new RecruitService(run.Party);
            _quest = new QuestService(run.Quest);
            _journal = new JournalService(run.Journal);
            _codex = new CodexService(run.Codex, DataManager.Instance.Spells, DataManager.Instance.Runes);
            _codex.Registered += OnSpellRegistered;
            _view.Codex.PracticeJudged += OnPracticeJudged;
            _desk = new DeskService(_view.DeskSequence, _view.Codex, _view.Player.Head, _view.DeskSeat);
            _desk.Left += OnDeskLeft;
            _arriving = !run.HasArrived;
            _respawning = !_arriving && run.RespawningFromDeath;
            _returning = !_arriving && !_respawning && run.ReturningFromField;
            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _respawning ? RespawnPoint() : _returning ? _view.ReturnPoint : _view.Spawn);
            _view.Player.gameObject.SetActive(!_arriving);
            _view.Hud.SetActive(!_arriving);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
            RefreshMoney();
            _view.Codex.ShowCover(run.PlayerName, DataManager.Instance.Runes);
            UIManager.Instance.SetJournalSource(JournalNow);
            UIManager.Instance.SetCodexSource(_codex, run.PlayerName);
            _view.Stamina.Bind(_view.Player);
        }
        #endregion

        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작. 숲에서 죽어 왔으면 소탕을 끝냈을 때 의뢰를 끝내고 깨어남 토스트. 숲에서 돌아왔으면 소탕을 끝냈을 때 의뢰를 끝내고(토스트) 경비병 자막 → 끝나면 목표 알림
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
            if (_respawning)
            {
                Game.Run.RespawningFromDeath = false;
                Game.Run.ReturningFromField = false;
                FinishQuestIfCleared(false);
                UpdateObjective(false);
                UIManager.Instance.ShowToast(Table, _inn.HasRoom ? RespawnInnKey : RespawnGateKey);
                return;
            }
            if (!_returning)
            {
                UpdateObjective(false);
                return;
            }
            // 숲에서 돌아옴: 소탕을 끝냈으면 의뢰를 끝냄 (정산은 나중. 못 끝냈으면 의뢰는 남음)
            Game.Run.ReturningFromField = false;
            FinishQuestIfCleared(true);
            UpdateObjective(false);
            UIManager.Instance.ShowSubtitle(new LocalizedString(Table, GuardKey), new LocalizedString(Table, GuardWelcomeKey), NotifyObjective);
        }
        #endregion

        #region RespawnPoint - 숲에서 죽어 깨어나는 곳: 방을 잡았으면 여관 방 침대 옆, 아니면 정문 안쪽
        Transform RespawnPoint() => _inn.HasRoom ? _view.BedPoint : _view.Spawn;
        #endregion

        #region FinishQuestIfCleared - 받은 의뢰의 소탕을 끝냈으면 의뢰 캡슐이 끝낸 의뢰로 남기고 비움 (notify면 토스트)
        /// <param name="notify">"의뢰를 끝냈어" 토스트를 띄울지</param>
        void FinishQuestIfCleared(bool notify)
        {
            // 끝낸 의뢰 (안 끝냈으면 null)
            QuestSO quest = _quest.Finish();
            if (quest != null && notify) UIManager.Instance.ShowToast(Table, QuestDoneKey, quest.Title.GetLocalizedString());
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
            // 지금 목표 (임무 일지 캡슐)
            ObjectiveSO objective = _journal.Current;
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

        #region Exit - 연출 중이면 알림 없이 멈춤, 알림 구독 해제, 책상이면 코덱스를 바로 덮고 머리를 바로 되돌림(책상 캡슐), 조작·지점 찾기 끔, 커서 풀기, 조작 안내·자막 정리, 메뉴 임무 일지·주문 도감 출처를 놓음
        public override void Exit()
        {
            if (_view != null)
            {
                _view.Opening.Finished -= OnOpeningFinished;
                _view.Opening.Cancel();
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.Codex.PracticeJudged -= OnPracticeJudged;
                if (_desk != null)
                {
                    _desk.Left -= OnDeskLeft;
                    _desk.ForceStop();
                }
                SetControl(false);
            }
            if (_codex != null) _codex.Registered -= OnSpellRegistered;
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
            UIManager.Instance.ClearJournalSource();
            UIManager.Instance.ClearCodexSource();
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김 (Play를 끌 때처럼 안내 글이 먼저 지워졌으면 무시)
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글 (씬이 내려가는 중이면 없음)
            var prompt = _view != null ? _view.InteractPrompt : null;
            if (prompt == null) return;
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

        #region OnInteracted - 처음 쓰면 상호작용 배움. 문이면 드나들고, 아니면 지점 id로 나눔: 여관 주인·접수원·게시판·경비병·코덱스. 모르는 id는 경고
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
                case CodexId: UseDesk(); break;
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
            if (string.Equals(target.Id, RoomDoorId, StringComparison.Ordinal) && !_inn.HasRoom)
            {
                UIManager.Instance.ShowToast(Table, RoomLockedKey);
                return;
            }
            if (!UIManager.Instance.FadeThrough(() => _view.Player.PlaceAt(door.Destination), () => SetControl(true))) return;
            SetControl(false);
        }
        #endregion

        #region UseDesk - 책상에 앉음: 조작·화면 글자를 끄고 책상 캡슐이 커서를 풀고 머리를 옮긴 뒤 코덱스를 엶 (코덱스 캡슐을 넘김)
        void UseDesk()
        {
            if (_desk.IsActive) return;
            SetControl(false);
            _view.Hud.SetActive(false);
            _desk.Sit(_codex);
        }
        #endregion

        #region OnDeskLeft - 책상에서 일어나기 연출이 끝남 (책상 캡슐 알림): 화면 글자·조작 켬
        void OnDeskLeft()
        {
            if (_view == null) return;
            _view.Hud.SetActive(true);
            SetControl(true);
        }
        #endregion

        #region OnPracticeJudged - 책상 연습 판정 (코덱스 알림): 성공이면 그 주문의 성공 사운드, 실패면 실패 사운드 (실전과 같게. 소리를 안 넣었으면 조용히 넘어감), 직접 취소가 아니면 주문 그리기 배움
        /// <param name="spec">판정 결과</param>
        void OnPracticeJudged(SpellJudgeSpec spec)
        {
            // 소리를 낼 곳 (없으면 무음)
            AudioManager audio = AudioManager.Instance;
            if (audio != null)
            {
                if (spec.IsFailure) audio.Play(DataManager.Instance.CastFailSound);
                else if (spec.IsCast) audio.Play(_codex.SuccessSoundAt(spec.Candidate.ShapeIndex));
            }
            if (spec.Reason != SpellJudgeReason.Canceled) TutorialManager.Instance.MarkLearned(DrawTutorialId);
        }
        #endregion

        #region OnSpellRegistered - 다중 룬 마법을 새로 등록함 (코덱스 캡슐 알림): 토스트
        /// <param name="spell">등록한 마법</param>
        void OnSpellRegistered(SpellSO spell) => UIManager.Instance.ShowToast(CodexTable, SpellRegisteredKey, spell.Name.GetLocalizedString());
        #endregion

        #region HandleCancel - Esc: 책상이면 책상 캡슐이 처리 (그리는 중엔 그리기 취소, 아니면 일어남. 일시정지 메뉴 안 엶). 책상이 아니면 처리 안 함
        public override bool HandleCancel() => _view != null && _desk != null && _desk.HandleCancel();
        #endregion

        #region TalkInnkeeper - 여관 주인 대화: 방 잡기(안 잡았을 때)·식량 하루치 사기·그만
        /// <param name="lineKey">이번 대사 키</param>
        /// <param name="lineArgs">대사 자리 값</param>
        void TalkInnkeeper(string lineKey, params object[] lineArgs)
        {
            // 이번 선택지
            var choices = new List<DialogueChoiceEntry>();
            if (!_inn.HasRoom) choices.Add(new DialogueChoiceEntry(Table, ChoiceRentKey, RentRoom, InnService.RoomPrice));
            choices.Add(new DialogueChoiceEntry(Table, ChoiceFoodKey, BuyFood, InnService.FoodPrice(PartySize())));
            choices.Add(new DialogueChoiceEntry(Table, ChoiceByeKey, UIManager.Instance.HideDialogue));
            UIManager.Instance.ShowDialogue(Table, InnkeeperKey, lineKey, choices, lineArgs);
        }
        #endregion

        #region RentRoom - 금화가 되면 방값을 내고 여관 캡슐로 방을 잡음 (한 번). 주인이 길드를 알려 주고 목표 갱신
        void RentRoom()
        {
            if (Game.Run.Gold < InnService.RoomPrice)
            {
                TalkInnkeeper(InnNoGoldKey);
                return;
            }
            Game.Run.Gold -= InnService.RoomPrice;
            _inn.Rent();
            RefreshMoney();
            TalkInnkeeper(InnRentedKey);
            UpdateObjective(true);
        }
        #endregion

        #region BuyFood - 금화가 되면 지금 인원의 하루치 식량을 사서 원정 캡슐에 더함
        void BuyFood()
        {
            // 하루치 값 (금화)
            int price = InnService.FoodPrice(PartySize());
            if (Game.Run.Gold < price)
            {
                TalkInnkeeper(InnNoGoldKey);
                return;
            }
            Game.Run.Gold -= price;
            _expedition.AddRations(PartySize());
            RefreshMoney();
            TalkInnkeeper(InnFoodKey, _expedition.Rations);
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

        #region OpenRecruit - 처음이면 영입 캡슐이 후보를 뽑고 영입 창을 엶 (대화는 창을 연 뒤 닫아서 게임이 안 풀림)
        void OpenRecruit()
        {
            _recruit.EnsureCandidates(DataManager.Instance.Jobs);
            ShowRecruit();
            UIManager.Instance.HideDialogue();
        }
        #endregion

        #region ShowRecruit - 영입 창을 지금 후보·파티 복사본으로 띄움 (영입·내보내기 뒤에도 다시 불러 목록을 새로 넘김)
        void ShowRecruit() => UIManager.Instance.ShowRecruit(_recruit.Candidates, _recruit.Members, RecruitService.MaxMembers, Hire, Dismiss);
        #endregion

        #region Hire - 영입 캡슐이 후보를 파티로 옮기면(자리가 있을 때) 창 목록·파티 수·목표 갱신
        /// <param name="entry">영입할 후보</param>
        void Hire(MercenaryEntry entry)
        {
            if (!_recruit.Hire(entry)) return;
            ShowRecruit();
            RefreshParty();
            UpdateObjective(true);
        }
        #endregion

        #region Dismiss - 영입 캡슐이 파티원을 후보로 되돌리면 창 목록·파티 수·목표 갱신
        /// <param name="entry">내보낼 파티원</param>
        void Dismiss(MercenaryEntry entry)
        {
            if (!_recruit.Dismiss(entry)) return;
            ShowRecruit();
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
            UIManager.Instance.ShowQuestBoard(quest, _quest.IsTaken(quest), _quest.IsCompleted(quest), () => TakeQuest(quest));
        }
        #endregion

        #region TakeQuest - 의뢰 캡슐로 의뢰를 받음. 목표가 바뀌면 목표 알림, 아니면 "의뢰를 받았어" 토스트
        /// <param name="quest">받을 의뢰</param>
        void TakeQuest(QuestSO quest)
        {
            _quest.Take(quest);
            if (!UpdateObjective(true)) UIManager.Instance.ShowToast(Table, QuestTakenKey, quest.Title.GetLocalizedString());
        }
        #endregion

        #region TalkGuard - 정문 경비병: 원정 캡슐이 첫 출발 전 모자란 것(용병·의뢰·식량)을 찾으면 말로 막음. 됐거나 첫 출발 뒤면 출발·아직
        void TalkGuard()
        {
            switch (_expedition.Check(_recruit.MemberCount, RecruitService.MaxMembers, _quest.HasQuest, NeededRations()))
            {
                case ExpeditionBlock.Party:
                    GuardLine(PartyRequiredKey, RecruitService.MaxMembers);
                    return;
                case ExpeditionBlock.Quest:
                    GuardLine(QuestRequiredKey);
                    return;
                case ExpeditionBlock.Food:
                    GuardLine(GuardNoFoodKey, NeededRations());
                    return;
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

        #region Depart - 원정 캡슐이 식량을 빼고(모자라면 있는 만큼) 출발 표시, 의뢰 캡슐이 기한을 정함(처음 한 번), 대화를 닫고 숲으로
        void Depart()
        {
            _expedition.Depart(NeededRations());
            _quest.StartDeadline(Game.Run.Clock.PlaySeconds, ClockEntry.DaySeconds);
            UpdateObjective(false);
            UIManager.Instance.HideDialogue();
            Game.ChangeState(new FieldState());
        }
        #endregion

        #region PartySize - 원정 인원 (플레이어 + 파티 용병)
        int PartySize() => 1 + _recruit.MemberCount;
        #endregion

        #region NeededRations - 받은 의뢰에 필요한 식량 (인원 × 기간 인분. 의뢰가 없으면 0)
        int NeededRations() => _quest.NeededDays * PartySize();
        #endregion

        #region NextObjective - 캡슐 상태로 지금 목표를 정함: 방 → 길드(파티·의뢰) → 정문 → (의뢰를 받고 나간 뒤) 숲 소탕 → (끝낸 뒤) 없음
        ObjectiveSO NextObjective()
        {
            if (!_inn.HasRoom) return _view.FirstObjective;
            if (_quest.HasQuest && _expedition.HasDeparted) return _view.ForestObjective;
            if (_recruit.MemberCount < RecruitService.MaxMembers || !_quest.HasQuest) return _quest.HasCompletedAny ? null : _view.GuildObjective;
            return _expedition.HasDeparted ? null : _view.DepartObjective;
        }
        #endregion

        #region UpdateObjective - 캡슐 상태로 정한 목표를 임무 일지 캡슐에 넣고, 바뀌었으면 (notify면) 알림. 바뀌었으면 true
        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
        bool UpdateObjective(bool notify)
        {
            if (!_journal.Set(NextObjective())) return false;
            if (notify) NotifyObjective();
            return true;
        }
        #endregion

        #region JournalNow - 메뉴 임무 일지 탭에 보일 것: 임무 일지 캡슐의 목표, 의뢰 캡슐의 받은 의뢰·기한, 판 시각 (탭이 열릴 때마다 부름)
        JournalSpec JournalNow() => new(_journal.Current, _quest.Current, _quest.Deadline, Game.Run.Clock.PlaySeconds);
        #endregion

        #region RefreshParty - 파티 수 표시 갱신 ({0}/{1})
        void RefreshParty()
        {
            _view.PartyText.StringReference.Arguments = new object[] { _recruit.MemberCount, RecruitService.MaxMembers };
            _view.PartyText.RefreshString();
        }
        #endregion

        #region RefreshMoney - 금화·식량 표시 갱신 ({0} 금화, {1} 식량 인분)
        void RefreshMoney()
        {
            _view.MoneyText.StringReference.Arguments = new object[] { Game.Run.Gold, _expedition.Rations };
            _view.MoneyText.RefreshString();
        }
        #endregion
    }
}
