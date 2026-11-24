using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>숲. 3구역 회색 숲에 마법사·용병·적(쓰러뜨린 적은 쓰러진 자리에 시체)과 1구역 고블린 던전(지하, 보스)을 두고 현장 시전·안개 지도·시계·12시 안내(전투 중엔 숨김)를 켬. 숲이 70% 넘게 빈 채 나갔으면 들어올 때 리셋 연출 뒤 시체가 일어남. 숲 입구에서 F로 귀환, 소탕 대상을 다 쓰러뜨리면 의뢰 목표 달성, 기간이 지나면 의뢰 실패. 죽으면 금화를 떨구고 덮개 밑에서 용병 도망을 빠르게 돌린 뒤 여관 방 침대(방이 없으면 정문)에서 부활. 던전·숲 리셋·떨어진 금화는 캡슐(DungeonService·ForestResetService·FieldLootHandler)이 맡고 여기선 만들어 이음</summary>
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

        /// <summary>용병을 입구 둘레에 놓는 간격 (m)</summary>
        const float PartySpread = 2f;

        /// <summary>죽은 뒤 도망 시뮬 게임 속도 (임시)</summary>
        const float DeathGameSpeed = 6f;

        /// <summary>죽은 뒤 도망 시뮬 최대 게임 시간 (초, 임시)</summary>
        const float DeathMaxSeconds = 40f;

        /// <summary>쫓는 적 없이 이만큼 지나면 무사 (게임 초, 임시)</summary>
        const float SafeCalmSeconds = 3f;

        /// <summary>적 키 형식 ({0} 자리 id, {1} 번호)</summary>
        const string SpawnKeyFormat = "{0}.{1}";

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

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

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

        /// <summary>데려온 용병 (파티 순서. 죽어도 남음)</summary>
        readonly List<MercenaryController> _mercs = new();

        /// <summary>살아 있는 소탕 대상</summary>
        readonly List<EnemyController> _targets = new();

        /// <summary>고블린 던전 캡슐</summary>
        DungeonService _dungeon;

        /// <summary>숲 리셋 캡슐</summary>
        ForestResetService _reset;

        /// <summary>떨어진 금화 캡슐</summary>
        FieldLootHandler _loot;

        /// <summary>지도 탭 입력 (Exit에서 구독 해제)</summary>
        InputAction _map;

        /// <summary>죽은 뒤 도망 시뮬 코루틴 (GameManager가 돌림. 없으면 null)</summary>
        Coroutine _death;

        /// <summary>죽어서 마을로 가는 중인지</summary>
        bool _dying;

        /// <summary>들어올 때 목표가 바뀌었는지 (Reveal에서 알림)</summary>
        bool _objectiveChanged;

        /// <summary>숲 적 자리 수 합 (리셋 캡슐의 70% 판정에 넘김)</summary>
        int _forestTotal;

        #region OnSceneReady - 현장 규칙(보관 장소 = 던전 입구 밖)·안개 지도, 캡슐(숲 리셋·던전·떨어진 금화)을 만들어 잇고(몬스터가 보관한 금화 → 던전), 차원문 자리, 마법사를 입구에, 용병·숲 적(시체는 리셋 캡슐에)·던전·떨어진 금화를 놓고, 시전·안개·시계·HUD·지도를 연결, 목표를 정함 (덮개 밑)
        protected override void OnSceneReady()
        {
            _view = FindView<FieldView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            _service = new FieldService(_view.PlayerUnit) { StashPoint = _view.Dungeon.Outside };
            _fog = new FogMapService(_view.MapArea, run.ForestFog);
            run.ForestFog = _fog.Revealed;
            _reset = new ForestResetService(run.ForestReset, _view.ResetSequence, _service);
            _reset.Finished += OnResetFinished;
            _dungeon = new DungeonService(run.Dungeon, _view.Dungeon, _service);
            _dungeon.Cleared += OnDungeonCleared;
            _loot = new FieldLootHandler(run.Loot, _service, _view.GoldPilePrefab);
            _service.LootStored += _dungeon.AddStash;
            PlacePortal(run);
            _view.Player.PlaceAt(_view.Entry);
            _view.PlayerUnit.Init(PlayerMaxHp);
            SpawnParty(run);
            SpawnEnemies(run);
            _dungeon.Spawn();
            _loot.Spawn();
            _view.Cast.Init(run, DataManager.Instance.Spells, DataManager.Instance.Runes, _service, _view.Player, _view.PlayerUnit, _view.Detector);
            _view.FogReveal.Init(_fog);
            _view.Clock.Init(run);
            _view.HudBinder.Bind(_view.PlayerUnit, _view.Player, _view.Cast, _mercs, _service, _fog, ZoneAt, () => Game.Run.Day);
            _view.Hud.Minimap.Bind(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            UIManager.Instance.SetMapSource(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            _view.Hud.InteractPrompt.gameObject.SetActive(false);
            if (!_reset.IsResetting && QuestActive(run) && _targets.Count == 0) run.QuestCleared = true;
            _objectiveChanged = UpdateObjective(false);
        }
        #endregion

        #region Reveal - 커서 잠금, 지점·죽음·전투 상황·시계·M 구독. 리셋이면 조작·HUD를 끄고 연출 → 끝나면 OnResetFinished, 아니면 바로 조작 시작 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.PlayerUnit.Died += OnPlayerDied;
            _service.CombatChanged += OnCombatChanged;
            _view.Clock.Expired += OnQuestExpired;
            _map = InputSystem.actions.FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMapPressed;
            else Debug.LogError($"[Field] {MapActionPath} 액션 없음. 지도 튜토리얼을 못 배움");
            if (_reset.IsResetting)
            {
                SetControl(false);
                _view.HudCanvas.enabled = false;
                _reset.Play(_view.Portal, _view.Player.transform);
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

        #region Exit - 도망 시뮬을 멈추고 게임 속도를 되돌림, 구독 해제, 조작 끔, HUD·지도 출처를 놓음, 안내 숨김 풀고 정리, 자막 숨김, 커서 풂
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
                _view.Clock.Expired -= OnQuestExpired;
                _view.HudCanvas.enabled = true;
                SetControl(false);
                _view.HudBinder.Unbind();
                _view.Hud.Minimap.Unbind();
            }
            if (_service != null) _service.CombatChanged -= OnCombatChanged;
            if (_reset != null) _reset.Finished -= OnResetFinished;
            if (_dungeon != null)
            {
                _dungeon.Cleared -= OnDungeonCleared;
                if (_service != null) _service.LootStored -= _dungeon.AddStash;
            }
            if (_map != null) _map.performed -= OnMapPressed;
            UIManager.Instance.ClearMapSource();
            TutorialManager.Instance.SetSuppressed(false);
            TutorialManager.Instance.Clear();
            UIManager.Instance.HideSubtitle();
            Cursor.lockState = CursorLockMode.None;
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

        #region OnTargetChanged - 바라보는 지점이 있으면 "[키] 이름", 없으면 숨김
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글
            var prompt = _view.Hud.InteractPrompt;
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
            MarkLeaving(Game.Run);
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

        #region SpawnParty - 파티 용병을 입구 둘레(마법사 뒤 양옆)에 놓고 마법사를 따라오게 함
        /// <param name="run">이번 판 데이터</param>
        void SpawnParty(RunEntry run)
        {
            for (int i = 0; i < run.Party.Count; i++)
            {
                // 입구 기준 자리 (뒤 양옆)
                Vector3 offset = new((i % 2 == 0 ? -1f : 1f) * PartySpread, 0f, -PartySpread);
                // 놓을 곳
                Vector3 position = FieldService.Sample(_view.Entry.position + _view.Entry.rotation * offset);
                // 놓은 용병
                MercenaryController merc = Object.Instantiate(_view.MercenaryPrefab, position, _view.Entry.rotation);
                merc.Init(run.Party[i], _service, _view.Player.transform, i);
                merc.Died += OnMercenaryDied;
                _mercs.Add(merc);
            }
        }
        #endregion

        #region SpawnEnemies - 적 자리마다 수만큼 놓음: 쓰러진 키는 쓰러진 곳에 시체(리셋 캡슐에 넘김), 아니면 자리 둘레에 살아 있는 적. 무리 번호는 자리 순서. 의뢰 소탕 대상이면 따로 셈
        /// <param name="run">이번 판 데이터</param>
        void SpawnEnemies(RunEntry run)
        {
            // 무리 번호 (자리마다 하나)
            int pack = 0;
            foreach (SpawnPointView point in _view.SpawnPoints)
            {
                if (point == null || point.Monster == null)
                {
                    Debug.LogWarning("[Field] 비었거나 몬스터가 없는 적 자리", point);
                    continue;
                }
                pack++;
                _forestTotal += point.Count;
                for (int i = 0; i < point.Count; i++)
                {
                    // 이 적 키
                    string key = string.Format(CultureInfo.InvariantCulture, SpawnKeyFormat, point.Id, i);
                    // 쓰러졌는지, 쓰러진 곳
                    bool dead = run.ForestDead.TryGetValue(key, out Vector3 corpseAt);
                    // 놓을 곳
                    Vector3 position = FieldService.Sample(dead ? corpseAt : SpawnPosition(point));
                    // 놓은 적
                    EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    if (dead)
                    {
                        enemy.InitCorpse(point.Monster, key, pack);
                        _reset.AddCorpse(enemy, point.transform.position);
                        continue;
                    }
                    enemy.Init(point.Monster, _service, key, pack);
                    enemy.Died += OnEnemyDied;
                    if (IsQuestTarget(run, point.Monster)) _targets.Add(enemy);
                }
            }
        }
        #endregion

        #region SpawnPosition - 적 자리 둘레 흩어 놓는 반지름 안 아무 곳
        /// <param name="point">적 자리</param>
        static Vector3 SpawnPosition(SpawnPointView point)
        {
            // 흩어 놓는 거리
            Vector2 offset = Random.insideUnitCircle * point.Spread;
            return point.transform.position + new Vector3(offset.x, 0f, offset.y);
        }
        #endregion

        #region PlacePortal - 이 판에서 처음이면 3구역 먼 쪽 절반에서 차원문 자리를 정해 판 데이터에 남기고, 차원문을 그 자리에
        /// <param name="run">이번 판 데이터</param>
        void PlacePortal(RunEntry run)
        {
            if (run.PortalPosition == null)
            {
                // 놓을 수 있는 곳
                Bounds area = _view.PortalArea;
                // 고른 곳
                var picked = new Vector3(Random.Range(area.min.x, area.max.x), area.center.y, Random.Range(area.min.z, area.max.z));
                run.PortalPosition = FieldService.Sample(picked);
            }
            _view.Portal.position = run.PortalPosition.Value;
        }
        #endregion

        #region OnEnemyDied - 쓰러진 곳을 판 데이터에 남김. 소탕 대상이 다 쓰러졌으면 목표 달성
        /// <param name="enemy">쓰러진 적</param>
        void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            Game.Run.ForestDead[enemy.SpawnKey] = enemy.transform.position;
            if (_targets.Remove(enemy) && _targets.Count == 0 && QuestActive(Game.Run)) ClearQuest();
        }
        #endregion

        #region QuestActive - 받은 의뢰가 있고 아직 목표를 안 끝냈는지
        /// <param name="run">이번 판 데이터</param>
        static bool QuestActive(RunEntry run) => run.Quest != null && !run.QuestCleared;
        #endregion

        #region IsQuestTarget - 이 몬스터가 받은 의뢰의 소탕 대상인지
        /// <param name="run">이번 판 데이터</param>
        /// <param name="monster">몬스터</param>
        static bool IsQuestTarget(RunEntry run, MonsterSO monster) => QuestActive(run) && run.Quest.Target != null && run.Quest.Target == monster;
        #endregion

        #region ClearQuest - 목표 달성 표시, 목표를 '숲 입구로 돌아가기'로, 토스트
        void ClearQuest()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            run.QuestCleared = true;
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestClearedKey, run.Quest.Title.GetLocalizedString());
        }
        #endregion

        #region UpdateObjective - 받은 의뢰가 없으면 없음, 목표를 안 끝냈으면 소탕, 끝냈으면 돌아가기. 바뀌었으면 판 데이터에 넣고 (notify면) 알림. 바뀌었으면 true
        /// <param name="notify">바뀌면 "임무 일지 갱신" 토스트를 띄울지</param>
        bool UpdateObjective(bool notify)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 정한 목표
            ObjectiveSO next = run.Quest == null ? null : run.QuestCleared ? _view.ReturnObjective : _view.ClearObjective;
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

        #region ObjectivePosition - 지도 목표 표시: 소탕 중이면 남은 대상 집의 가운데, 끝냈으면 숲 입구, 아니면 없음
        Vector3? ObjectivePosition()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Quest == null) return null;
            if (run.QuestCleared) return _view.Entry.position;
            // 남은 대상 집의 합과 수
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (EnemyController enemy in _targets)
            {
                if (enemy == null) continue;
                sum += enemy.Home;
                count++;
            }
            return count > 0 ? sum / count : null;
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

        #region OnMercenaryDied - 판 데이터 파티에서 빼고 토스트
        /// <param name="merc">죽은 용병</param>
        void OnMercenaryDied(MercenaryController merc)
        {
            merc.Died -= OnMercenaryDied;
            Game.Run.Party.Remove(merc.Entry);
            UIManager.Instance.ShowToast(Table, MercenaryLostKey, merc.Entry.Name.GetLocalizedString());
        }
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

        #region DeathRoutine - 살아 있는 용병을 숲 입구로 도망치게 하고 게임 속도를 올려 돌림: 입구에 닿거나 쫓는 적 없이 3초면 무사, 도중에 죽으면 죽음 (OnMercenaryDied). 다 정해지거나 40초가 지나면 속도를 되돌리고 부활 표시 후 전초기지로
        IEnumerator DeathRoutine()
        {
            foreach (MercenaryController merc in _mercs)
                if (merc != null && merc.Unit.IsAlive) merc.Flee(_view.ExitPoint);
            // 용병마다 쫓는 적 없이 지난 시간 (게임 초)
            var calm = new Dictionary<MercenaryController, float>();
            // 지난 게임 시간 (초)
            float elapsed = 0f;
            Game.SetGameSpeed(DeathGameSpeed);
            while (elapsed < DeathMaxSeconds)
            {
                yield return null;
                // 이번 프레임 게임 시간 (초)
                float dt = Time.deltaTime;
                elapsed += dt;
                // 아직 정해지지 않은 용병이 있는지
                bool pending = false;
                foreach (MercenaryController merc in _mercs)
                {
                    if (merc == null || !merc.Unit.IsAlive || merc.IsSafe) continue;
                    calm.TryGetValue(merc, out float quiet);
                    quiet = _service.IsTargeted(merc.Unit) ? 0f : quiet + dt;
                    calm[merc] = quiet;
                    if (quiet >= SafeCalmSeconds) merc.MarkSafe();
                    else pending = true;
                }
                if (!pending) break;
            }
            Game.SetGameSpeed(1f);
            _death = null;
            MarkLeaving(Game.Run);
            Game.Run.RespawningFromDeath = true;
            Game.ChangeState(new HubState());
        }
        #endregion

        #region OnResetFinished - 리셋 연출이 끝나 시체가 일어남: 살아난 적의 쓰러짐을 구독하고 소탕 대상이면 다시 셈, 숲 쓰러진 기록을 지움, HUD를 켜고 조작 시작
        /// <param name="revived">살아난 적</param>
        void OnResetFinished(IReadOnlyList<EnemyController> revived)
        {
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            foreach (EnemyController enemy in revived)
            {
                if (enemy == null) continue;
                enemy.Died += OnEnemyDied;
                if (IsQuestTarget(run, enemy.Monster)) _targets.Add(enemy);
            }
            run.ForestDead.Clear();
            if (QuestActive(run) && _targets.Count == 0) run.QuestCleared = true;
            if (UpdateObjective(false)) _objectiveChanged = true;
            _view.HudCanvas.enabled = true;
            StartControl();
        }
        #endregion

        #region OnDungeonCleared - 던전 보스가 쓰러짐: 토스트
        void OnDungeonCleared() => UIManager.Instance.ShowToast(Table, DungeonClearedKey);
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

        #region PlaceParty - 마법사를 그 자리에, 살아 있는 용병을 그 뒤 양옆에 순간이동
        /// <param name="point">옮길 자리</param>
        void PlaceParty(Transform point)
        {
            if (_view == null) return;
            _view.Player.PlaceAt(point);
            for (int i = 0; i < _mercs.Count; i++)
            {
                // 이 용병
                MercenaryController merc = _mercs[i];
                if (merc == null || !merc.Unit.IsAlive) continue;
                // 자리 기준 뒤 양옆
                Vector3 offset = new((i % 2 == 0 ? -1f : 1f) * PartySpread, 0f, -PartySpread);
                merc.WarpTo(FieldService.Sample(point.position + point.rotation * offset));
            }
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
        /// <param name="run">이번 판 데이터</param>
        void MarkLeaving(RunEntry run)
        {
            _reset.MarkLeaving(run.ForestDead.Count, _forestTotal);
            _dungeon.MarkLeaving();
        }
        #endregion

        #region OnQuestExpired - 받은 의뢰의 기간이 지남: 의뢰 실패로 비움 (게시판에 다시 붙음), 소탕 대상·목표 정리, 토스트
        void OnQuestExpired()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Quest == null || run.QuestCleared) return;
            // 실패한 의뢰
            QuestSO quest = run.Quest;
            run.Quest = null;
            run.QuestDeadline = 0f;
            _targets.Clear();
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestExpiredKey, quest.Title.GetLocalizedString());
        }
        #endregion
    }
}
