using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>숲. 3구역 회색 숲에 마법사·용병·적(쓰러뜨린 적은 쓰러진 자리에 시체)과 1구역 고블린 던전(지하, 보스)을 두고 현장 시전·안개 지도·시계·12시 안내(전투 중엔 숨김)를 켬. 숲이 70% 넘게 빈 채 나갔으면 들어올 때 리셋 연출 뒤 시체가 일어남. 숲 입구에서 F로 귀환, 소탕 대상을 다 쓰러뜨리면 의뢰 목표 달성, 기간이 지나면 의뢰 실패. 죽으면 금화를 떨구고 덮개 밑에서 용병 도망을 빠르게 돌린 뒤 여관 방 침대(방이 없으면 정문)에서 부활. 숲 적·의뢰·용병·던전·숲 리셋·떨어진 금화·코덱스는 캡슐이 맡고, 여기선 캡슐을 만들어 이음 (토스트·금화·목표·튜토리얼·화면 전환·시전 소리)</summary>
    public sealed class FieldState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름 (지역이 늘면 의뢰 데이터에서 받음)</summary>
        public const string Scene = "Forest";

        /// <summary>지도 탭 입력 액션 경로 (전역 액션, M. 누르면 지도 튜토리얼 배움)</summary>
        const string MapActionPath = "UI/Map";

        /// <summary>전투 설명창 튜토리얼 id (첫 전투에 한 번)</summary>
        const string CombatTutorialId = "combat";

        /// <summary>지도 튜토리얼 id</summary>
        const string MapTutorialId = "map";

        /// <summary>상호작용 튜토리얼 id (12시 안내 없음, 처음 F를 쓰면 배움)</summary>
        const string InteractTutorialId = "interact";

        /// <summary>숲 입구 지점 id (귀환)</summary>
        const string ExitId = "forest_exit";

        /// <summary>떨어진 금화 지점 id (줍기)</summary>
        const string GoldId = "gold";

        /// <summary>던전 입구 지점 id (들어가기)</summary>
        const string DungeonEnterId = "dungeon_enter";

        /// <summary>던전 안 나가는 문 지점 id</summary>
        const string DungeonExitId = "dungeon_exit";

        /// <summary>던전 보관함 지점 id</summary>
        const string StashId = "stash";

        /// <summary>마법사 최대 체력 (v1: 100 + 생존 4 × 6, 임시)</summary>
        const float PlayerMaxHp = 124f;

        /// <summary>죽은 뒤 도망 시뮬 게임 속도 (임시)</summary>
        const float DeathGameSpeed = 6f;

        /// <summary>죽은 뒤 도망 시뮬 최대 게임 시간 (초, 임시)</summary>
        const float DeathMaxSeconds = 40f;

        /// <summary>소탕 목표 달성 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestClearedKey = "field.quest_cleared";

        /// <summary>금화를 주웠을 때 토스트 키 ({0} 금화)</summary>
        const string GoldPickedKey = "field.gold_picked";

        /// <summary>동료를 잃었을 때 토스트 키 ({0} 이름)</summary>
        const string MercenaryLostKey = "field.mercenary_lost";

        /// <summary>의뢰 기간이 지났을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestExpiredKey = "field.quest_expired";

        /// <summary>보관함에서 금화를 챙겼을 때 토스트 키 ({0} 금화)</summary>
        const string StashTakenKey = "field.stash_taken";

        /// <summary>보관함이 비었을 때 토스트 키</summary>
        const string StashEmptyKey = "field.stash_empty";

        /// <summary>던전 보스를 쓰러뜨렸을 때 토스트 키</summary>
        const string DungeonClearedKey = "field.dungeon_cleared";

        /// <summary>목표가 바뀌었을 때 토스트 키 ({0} 목표 이름, Menu 테이블)</summary>
        const string ObjectiveUpdatedKey = "menu.journal.updated";

        /// <summary>마법 등록 토스트 키 ({0} 마법 이름, Codex 테이블)</summary>
        const string SpellRegisteredKey = "codex.toast.registered";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

        /// <summary>코덱스 문구 테이블 (마법 등록 토스트)</summary>
        static readonly TableReference CodexTable = "Codex";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (죽은 뒤 도망 시뮬 중엔 못 엶)</summary>
        public override bool UsesPauseMenu => !_dying;

        /// <summary>이 씬의 화면 참조</summary>
        FieldView _view;

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>안개 지도</summary>
        FogMapService _fog;

        /// <summary>숲 적 캡슐</summary>
        ForestEnemyService _forest;

        /// <summary>의뢰 캡슐 (숲 쪽)</summary>
        FieldQuestService _quest;

        /// <summary>용병 파티 캡슐 (숲 쪽)</summary>
        FieldPartyService _party;

        /// <summary>고블린 던전 캡슐</summary>
        DungeonService _dungeon;

        /// <summary>숲 리셋·차원문 캡슐</summary>
        ForestResetService _reset;

        /// <summary>떨어진 금화 캡슐</summary>
        FieldLootHandler _loot;

        /// <summary>코덱스 캡슐 (판정·이름·등록. 현장 시전에 넘김)</summary>
        CodexService _codex;

        /// <summary>지도 탭 입력 (Exit에서 구독 해제)</summary>
        InputAction _map;

        /// <summary>죽은 뒤 도망 시뮬 코루틴 (GameManager가 돌림. 없으면 null)</summary>
        Coroutine _death;

        /// <summary>죽어서 마을로 가는 중인지</summary>
        bool _dying;

        /// <summary>들어올 때 목표가 바뀌었는지 (Reveal에서 알림)</summary>
        bool _objectiveChanged;

        #region OnSceneReady - 현장 규칙(보관 장소 = 던전 입구 밖)·안개 지도와 캡슐(의뢰·숲 리셋·숲 적·용병·던전·떨어진 금화·코덱스)을 만들어 잇고(숲 적 → 의뢰 소탕 대상·리셋 시체, 몬스터가 보관한 금화 → 던전, 코덱스 등록 → 토스트, 시전 성공·실패 → 소리), 차원문을 놓고, 마법사를 입구에, 용병·숲 적·던전·떨어진 금화를 놓고, 시전·안개·시계·HUD·지도를 연결, 목표를 정함 (덮개 밑)
        protected override void OnSceneReady()
        {
            _view = FindView<FieldView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            _service = new FieldService(_view.PlayerUnit) { StashPoint = _view.Dungeon.Outside };
            _fog = new FogMapService(_view.MapArea, run.Fog);
            _quest = new FieldQuestService(run.Quest);
            _quest.Cleared += OnQuestCleared;
            _quest.Expired += OnQuestExpired;
            _reset = new ForestResetService(run.ForestReset, _view.ResetSequence, _view.Portal, _view.PortalArea, _service);
            _reset.Finished += OnResetFinished;
            _forest = new ForestEnemyService(run.Forest, _view.SpawnPoints, _view.EnemyPrefab, _service);
            _forest.EnemySpawned += _quest.Track;
            _forest.EnemyDied += _quest.Untrack;
            _forest.CorpseSpawned += _reset.AddCorpse;
            _party = new FieldPartyService(run.Party, _view.MercenaryPrefab, _service, _view.Player.transform);
            _party.Lost += OnMercenaryLost;
            _dungeon = new DungeonService(run.Dungeon, _view.Dungeon, _service);
            _dungeon.Cleared += OnDungeonCleared;
            _loot = new FieldLootHandler(run.Loot, _service, _view.GoldPilePrefab);
            _service.LootStored += _dungeon.AddStash;
            _reset.PlacePortal();
            _view.Player.PlaceAt(_view.Entry);
            _view.PlayerUnit.Init(PlayerMaxHp);
            _party.Spawn(_view.Entry);
            _forest.Spawn();
            _dungeon.Spawn();
            _loot.Spawn();
            _codex = new CodexService(run.Codex, DataManager.Instance.Spells, DataManager.Instance.Runes);
            _codex.Registered += OnSpellRegistered;
            _view.Cast.Init(_codex, _service, _view.Player, _view.PlayerUnit, _view.Detector);
            _view.Cast.CastAccepted += OnCastAccepted;
            _view.Cast.CastFailed += OnCastFailed;
            _view.FogReveal.Init(_fog);
            _view.Clock.Init(run.Clock);
            _view.HudBinder.Bind(_view.PlayerUnit, _view.Player, _view.Cast, _party.Mercenaries, _service, _fog, ZoneAt, () => _view.Clock.Day);
            _view.Hud.Minimap.Bind(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            UIManager.Instance.SetMapSource(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            _view.Hud.InteractPrompt.gameObject.SetActive(false);
            if (!_reset.IsResetting) _quest.SettleIfEmpty();
            _objectiveChanged = UpdateObjective(false);
        }
        #endregion

        #region Reveal - 커서 잠금, 지점·죽음·전투 상황 구독, 시계 → 의뢰 기한, M 구독. 리셋이면 조작·HUD를 끄고 연출 → 끝나면 OnResetFinished, 아니면 바로 조작 시작 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.PlayerUnit.Died += OnPlayerDied;
            _service.CombatChanged += OnCombatChanged;
            _view.Clock.Ticked += _quest.Tick;
            _map = InputSystem.actions.FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMapPressed;
            else Debug.LogError($"[Field] {MapActionPath} 액션 없음. 지도 튜토리얼을 못 배움");
            if (_reset.IsResetting)
            {
                SetControl(false);
                _view.HudCanvas.enabled = false;
                _reset.Play(_view.Player.transform);
                return;
            }
            StartControl();
        }
        #endregion

        #region StartControl - 조작 켬, 안 배운 12시 안내(지도) 알림, 들어올 때 목표가 바뀌었으면 알림
        void StartControl()
        {
            SetControl(true);
            TutorialManager.Instance.Notify(MapTutorialId);
            if (_objectiveChanged) NotifyObjective();
        }
        #endregion

        #region Exit - 도망 시뮬을 멈추고 게임 속도를 되돌림, 구독·캡슐 연결 해제, 조작 끔, HUD·지도 출처를 놓음, 안내 숨김 풀고 정리, 자막 숨김, 커서 풂
        public override void Exit()
        {
            if (_death != null)
            {
                Game.StopCoroutine(_death);
                _death = null;
            }
            Game.SetGameSpeed(1f);
            if (_view != null)
            {
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.PlayerUnit.Died -= OnPlayerDied;
                if (_quest != null) _view.Clock.Ticked -= _quest.Tick;
                _view.HudCanvas.enabled = true;
                SetControl(false);
                _view.HudBinder.Unbind();
                _view.Hud.Minimap.Unbind();
            }
            if (_service != null) _service.CombatChanged -= OnCombatChanged;
            Unwire();
            if (_map != null) _map.performed -= OnMapPressed;
            UIManager.Instance.ClearMapSource();
            TutorialManager.Instance.SetSuppressed(false);
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region Unwire - OnSceneReady에서 이은 캡슐 연결을 끊음 (안 만든 캡슐은 건너뜀)
        void Unwire()
        {
            if (_quest != null)
            {
                _quest.Cleared -= OnQuestCleared;
                _quest.Expired -= OnQuestExpired;
            }
            if (_reset != null) _reset.Finished -= OnResetFinished;
            if (_forest != null)
            {
                if (_quest != null)
                {
                    _forest.EnemySpawned -= _quest.Track;
                    _forest.EnemyDied -= _quest.Untrack;
                }
                if (_reset != null) _forest.CorpseSpawned -= _reset.AddCorpse;
            }
            if (_party != null) _party.Lost -= OnMercenaryLost;
            if (_dungeon != null)
            {
                _dungeon.Cleared -= OnDungeonCleared;
                if (_service != null) _service.LootStored -= _dungeon.AddStash;
            }
            if (_codex != null) _codex.Registered -= OnSpellRegistered;
            if (_view != null)
            {
                _view.Cast.CastAccepted -= OnCastAccepted;
                _view.Cast.CastFailed -= OnCastFailed;
            }
        }
        #endregion

        #region HandleCancel - 죽어서 마을로 가는 중엔 Esc를 먹음 (일시정지 메뉴 안 엶)
        public override bool HandleCancel() => _dying;
        #endregion

        #region SetControl - 조작·지점 찾기·현장 시전 켜고 끄기 (씬이 내려갔으면 무시)
        /// <param name="on">켤지</param>
        void SetControl(bool on)
        {
            if (_view == null) return;
            _view.Player.enabled = on;
            _view.Detector.enabled = on;
            _view.Cast.enabled = on;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김 (Play를 끌 때처럼 안내 글이 먼저 지워졌으면 무시)
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글 (씬이 내려가는 중이면 없음)
            var prompt = _view != null && _view.Hud != null ? _view.Hud.InteractPrompt : null;
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

        #region OnInteracted - 처음 쓰면 상호작용 배움. 숲 입구면 귀환, 떨어진 금화면 줍기, 던전 입구·나가는 문이면 드나들기, 보관함이면 챙기기. 모르는 id는 경고
        /// <param name="target">상호작용 키를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
            switch (target.Id)
            {
                case ExitId: Return(); break;
                case GoldId: PickGold(); break;
                case DungeonEnterId: MoveParty(_view.Dungeon.StartPoint); break;
                case DungeonExitId: MoveParty(_view.Dungeon.Outside); break;
                case StashId: TakeStash(); break;
                default: Debug.LogWarning($"[Field] 모르는 지점 id: {target.Id} ({target.name})"); break;
            }
        }
        #endregion

        #region Return - 숲에서 돌아감 표시 후 전초기지로 (정문 밖에서 시작)
        void Return()
        {
            if (_dying) return;
            MarkLeaving();
            Game.Run.ReturningFromField = true;
            Game.ChangeState(new HubState());
        }
        #endregion

        #region PickGold - 떨어진 금화를 다 주워 금화에 더함 (지점은 금화 캡슐이 지움)
        void PickGold()
        {
            // 주운 금화
            int gold = _loot.Pick();
            if (gold <= 0) return;
            Game.Run.Gold += gold;
            UIManager.Instance.ShowToast(Table, GoldPickedKey, gold);
        }
        #endregion

        #region OnQuestCleared - 소탕 대상을 다 쓰러뜨림 (의뢰 캡슐이 끝냄 표시): 목표를 '숲 입구로 돌아가기'로, 토스트
        /// <param name="quest">끝낸 의뢰</param>
        void OnQuestCleared(QuestSO quest)
        {
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestClearedKey, quest.Title.GetLocalizedString());
        }
        #endregion

        #region UpdateObjective - 받은 의뢰가 없으면 없음, 목표를 안 끝냈으면 소탕, 끝냈으면 돌아가기. 바뀌었으면 판 데이터에 넣고 (notify면) 알림. 바뀌었으면 true
        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
        bool UpdateObjective(bool notify)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 정한 목표
            ObjectiveSO next = !_quest.HasQuest ? null : _quest.IsCleared ? _view.ReturnObjective : _view.ClearObjective;
            if (next == run.CurrentObjective) return false;
            run.CurrentObjective = next;
            if (notify) NotifyObjective();
            return true;
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

        #region ObjectivePosition - 지도 목표 표시: 소탕 중이면 남은 대상 집의 가운데(의뢰 캡슐), 끝냈으면 숲 입구, 아니면 없음
        Vector3? ObjectivePosition()
        {
            if (!_quest.HasQuest) return null;
            return _quest.IsCleared ? _view.Entry.position : _quest.TargetCenter();
        }
        #endregion

        #region ZoneAt - 플레이어가 있는 구역 번호 (입구에서 z로 잰 깊이, 1~구역 수)
        int ZoneAt() => Mathf.Clamp(Mathf.FloorToInt((_view.Player.transform.position.z - _view.MapArea.yMin) / _view.ZoneDepth) + 1, 1, _view.ZoneCount);
        #endregion

        #region OnCombatChanged - 전투 상황이면 12시 안내를 숨기고 (이 PC가 처음이면) 게임을 멈추고 설명창. 끝나면 안내를 다시 보임
        /// <param name="inCombat">전투 상황인지</param>
        void OnCombatChanged(bool inCombat)
        {
            TutorialManager.Instance.SetSuppressed(inCombat);
            if (inCombat) TryShowCombatTutorial();
        }
        #endregion

        #region TryShowCombatTutorial - 이 PC가 아직 안 본 전투 설명창이면 게임을 멈추고 띄움 (닫으면 배움). 죽어서 가는 중엔 안 띄움
        void TryShowCombatTutorial()
        {
            if (_dying || TutorialManager.Instance.IsLearned(CombatTutorialId)) return;
            // 전투 설명창 튜토리얼
            TutorialSO tutorial = DataManager.Instance.FindTutorial(CombatTutorialId);
            if (tutorial == null)
            {
                Debug.LogWarning($"[Field] 모르는 튜토리얼 id: {CombatTutorialId}");
                return;
            }
            UIManager.Instance.ShowTutorialWindow(tutorial, () => TutorialManager.Instance.MarkLearned(CombatTutorialId));
        }
        #endregion

        #region OnMapPressed - M을 누르면 지도 배움
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnMapPressed(InputAction.CallbackContext context) => TutorialManager.Instance.MarkLearned(MapTutorialId);
        #endregion

        #region OnMercenaryLost - 용병을 잃음 (파티에서는 용병 캡슐이 뺌): 토스트
        /// <param name="entry">잃은 용병</param>
        void OnMercenaryLost(MercenaryEntry entry) => UIManager.Instance.ShowToast(Table, MercenaryLostKey, entry.Name.GetLocalizedString());
        #endregion

        #region OnPlayerDied - 한 번만: 조작 끔, 금화를 떨굼, 안내 정리, 덮개를 덮고 도망 시뮬 시작
        /// <param name="unit">죽은 몸 (마법사)</param>
        void OnPlayerDied(FieldUnitController unit)
        {
            if (_dying) return;
            _dying = true;
            SetControl(false);
            DropGold(Game.Run);
            TutorialManager.Instance.Clear();
            TransitionManager.Instance.ShowCover();
            _death = Game.StartCoroutine(DeathRoutine());
        }
        #endregion

        #region DropGold - 들고 있던 금화를 죽은 자리에 떨굼 (금화 캡슐이 앞 금화를 지우고 다음 진입 때 놓음. 없으면 떨어진 것 없음)
        /// <param name="run">이번 판 데이터</param>
        void DropGold(RunEntry run)
        {
            _loot.DropOnDeath(_view.Player.transform.position, run.Gold);
            run.Gold = 0;
        }
        #endregion

        #region DeathRoutine - 게임 속도를 올리고 살아 있는 용병을 숲 입구로 도망치게 해 돌림 (용병 캡슐이 무사·죽음을 정함). 다 정해지거나 40초가 지나면 속도를 되돌리고 부활 표시 후 전초기지로
        IEnumerator DeathRoutine()
        {
            _party.StartFlee(_view.ExitPoint);
            // 지난 게임 시간 (초)
            float elapsed = 0f;
            Game.SetGameSpeed(DeathGameSpeed);
            while (elapsed < DeathMaxSeconds)
            {
                yield return null;
                // 이번 프레임 게임 시간 (초)
                float dt = Time.deltaTime;
                elapsed += dt;
                if (!_party.UpdateFlee(dt)) break;
            }
            Game.SetGameSpeed(1f);
            _death = null;
            MarkLeaving();
            Game.Run.RespawningFromDeath = true;
            Game.ChangeState(new HubState());
        }
        #endregion

        #region OnResetFinished - 리셋 연출이 끝나 시체가 일어남: 살아난 적을 숲 적 캡슐에 다시 넘기고(의뢰 캡슐이 소탕 대상이면 다시 셈), 남은 대상이 없으면 목표를 끝냄, HUD를 켜고 조작 시작
        /// <param name="revived">살아난 적</param>
        void OnResetFinished(IReadOnlyList<EnemyController> revived)
        {
            if (_view == null) return;
            _forest.Adopt(revived);
            _quest.SettleIfEmpty();
            if (UpdateObjective(false)) _objectiveChanged = true;
            _view.HudCanvas.enabled = true;
            StartControl();
        }
        #endregion

        #region OnDungeonCleared - 던전 보스가 쓰러짐: 토스트
        void OnDungeonCleared() => UIManager.Instance.ShowToast(Table, DungeonClearedKey);
        #endregion

        #region OnCastAccepted - 맞는 주문을 받아들임 (현장 시전 알림): 그 주문의 성공 사운드를 2D로 냄 (소리를 안 넣었으면 조용히 넘어감)
        /// <param name="cast">받아들여진 주문</param>
        void OnCastAccepted(FieldCastSpec cast)
        {
            // 소리를 낼 곳 (없으면 무음)
            AudioManager audio = AudioManager.Instance;
            if (audio != null) audio.Play(cast.Sound);
        }
        #endregion

        #region OnCastFailed - 시전 실패 (현장 시전 알림): 실패 사운드 (벌칙은 현장 시전이 함)
        void OnCastFailed()
        {
            // 소리를 낼 곳과 데이터 (둘 중 하나라도 없으면 무음)
            AudioManager audio = AudioManager.Instance;
            DataManager data = DataManager.Instance;
            if (audio != null && data != null) audio.Play(data.CastFailSound);
        }
        #endregion

        #region OnSpellRegistered - 다중 룬 마법을 새로 등록함 (코덱스 캡슐 알림): 토스트
        /// <param name="spell">등록한 마법</param>
        void OnSpellRegistered(SpellSO spell) => UIManager.Instance.ShowToast(CodexTable, SpellRegisteredKey, spell.Name.GetLocalizedString());
        #endregion

        #region MoveParty - 화면이 덮인 사이 마법사·살아 있는 용병을 그 자리로 옮기고, 다 걷히면 조작을 돌려줌 (던전 드나들기)
        /// <param name="point">옮길 자리</param>
        void MoveParty(Transform point)
        {
            if (_dying || point == null) return;
            if (!UIManager.Instance.FadeThrough(() => PlaceParty(point), () => { if (!_dying) SetControl(true); })) return;
            SetControl(false);
        }
        #endregion

        #region PlaceParty - 마법사를 그 자리에, 살아 있는 용병을 그 뒤 양옆에 순간이동 (용병 캡슐)
        /// <param name="point">옮길 자리</param>
        void PlaceParty(Transform point)
        {
            if (_view == null) return;
            _view.Player.PlaceAt(point);
            _party.WarpTo(point);
        }
        #endregion

        #region TakeStash - 던전 보관 금화가 있으면 다 챙겨 금화에 더함 (없으면 비었다)
        void TakeStash()
        {
            // 챙긴 보관 금화
            int gold = _dungeon.TakeStash();
            if (gold <= 0)
            {
                UIManager.Instance.ShowToast(Table, StashEmptyKey);
                return;
            }
            Game.Run.Gold += gold;
            UIManager.Instance.ShowToast(Table, StashTakenKey, gold);
        }
        #endregion

        #region MarkLeaving - 숲을 나갈 때(입구 귀환·죽음): 리셋 캡슐에 숲 적 자리 대비 쓰러뜨린 수를 넘기고(70% 이상이면 다음에 리셋), 던전 캡슐에 알림(클리어했으면 새 던전·보관 금화는 사라짐)
        void MarkLeaving()
        {
            _reset.MarkLeaving(_forest.DeadCount, _forest.Total);
            _dungeon.MarkLeaving();
        }
        #endregion

        #region OnQuestExpired - 받은 의뢰의 기간이 지남 (의뢰 캡슐이 의뢰를 비움, 게시판에 다시 붙음): 목표 정리, 토스트
        /// <param name="quest">실패한 의뢰</param>
        void OnQuestExpired(QuestSO quest)
        {
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestExpiredKey, quest.Title.GetLocalizedString());
        }
        #endregion
    }
}
