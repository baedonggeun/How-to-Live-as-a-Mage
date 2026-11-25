#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>숲 상태 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 디버그 패널(DebugWindow)에 붙일 곳(이 상태·현장 규칙·시계·캡슐)을 넘기고, 캡슐 여럿에 걸치는 조작(금화·무적·소탕 끝냄·숲 적 쓰러뜨린 수·죽기·나갔다 다시 들어오기·파티 옮기기·의뢰 기한·숲 적 쓰러뜨리기)을 캡슐 진입점과 실제 경로(OnPlayerDied·MarkLeaving·MoveParty·피해)로 만듦</summary>
    public sealed partial class FieldState
    {
        /// <summary>보스방 앞에서 보스까지 거리 (m)</summary>
        const float DebugBossFrontGap = 12f;

        /// <summary>의뢰 기한까지 남길 판 시간 (초)</summary>
        const float DebugDeadlineLead = 5f;

        /// <summary>'고블린 1마리만 남기고'에서 남길 소탕 대상 수</summary>
        const int DebugKeepTargets = 1;

        /// <summary>임시 옮길 자리 (보스방 앞·방A 위. 숲 씬에 두어 씬과 같이 지워짐)</summary>
        Transform _debugPoint;

        /// <summary>조작을 막을지 (씬 준비 전·죽는 중·리셋 연출 중)</summary>
        public override bool DebugBusy => _view == null || _dying || _reset == null || _reset.IsResetting;

        #region DebugTargets - 조작을 붙일 곳: 기본(이 상태·튜토리얼 담당), 현장 규칙, 시계, 의뢰·숲 적·숲 리셋·떨어진 금화·던전·코덱스 캡슐 (씬 준비 전엔 기본만)
        public override IEnumerable<object> DebugTargets()
        {
            foreach (object target in base.DebugTargets()) yield return target;
            if (_view == null) yield break;
            yield return _service;
            yield return _view.Clock;
            yield return _quest;
            yield return _forest;
            yield return _reset;
            yield return _loot;
            yield return _dungeon;
            yield return _codex;
        }
        #endregion

        #region DebugGold - 가진 금화 (넣으면 0 이상으로 자름. 숲 HUD엔 금화가 없음)
        [DebugControl("판", "금화")]
        int DebugGold
        {
            get => Game.Run.Gold;
            set => Game.Run.Gold = Mathf.Max(0, value);
        }
        #endregion

        #region DebugWhere - 마법사가 있는 곳 ("숲 N구역" 또는 "던전 안")
        [DebugControl("숲", "위치")]
        string DebugWhere => FieldService.SameLevel(_view.Player.transform.position, _view.Entry.position)
            ? string.Format(CultureInfo.InvariantCulture, "숲 {0}구역", ZoneAt())
            : "던전 안";
        #endregion

        #region DebugInvincible - 마법사가 피해를 안 받는지 (방호막과 따로)
        [DebugControl("숲", "무적")]
        bool DebugInvincible
        {
            get => _view.PlayerUnit.DebugInvincible;
            set => _view.PlayerUnit.DebugInvincible = value;
        }
        #endregion

        #region DebugForestKills - 쓰러뜨린 숲 적 수 (늘리기만: 소탕 대상이 아닌 적부터 그 수까지 쓰러뜨림. 진행 중인 의뢰의 소탕 대상은 남김)
        [DebugControl("숲", "숲 적 쓰러뜨린 수")]
        int DebugForestKills
        {
            get => _forest.DeadCount;
            set
            {
                if (value > _forest.DeadCount) DebugKillForest(Mathf.Min(value, _forest.Total), false);
            }
        }
        #endregion

        #region DebugResetStatus - 다음 진입에 리셋하려면 더 쓰러뜨릴 수 (리셋 캡슐 기준 / 숲 적 캡슐 수)
        [DebugControl("숲", "리셋까지")]
        string DebugResetStatus
        {
            get
            {
                // 리셋에 필요한 쓰러뜨린 수
                int need = ForestResetService.DebugNeed(_forest.Total);
                return string.Format(CultureInfo.InvariantCulture, "{0}마리 더 ({1} / {2})", Mathf.Max(0, need - _forest.DeadCount), need, _forest.Total);
            }
        }
        #endregion

        #region DebugDie - 마법사를 죽임 (실제 죽음 경로: 금화 떨굼 → 용병 도망 → 여관 부활). 방호막·무적으로 안 죽으면 그렇다고 알림
        [DebugControl("숲", "죽기")]
        string DebugDie()
        {
            if (DebugBusy) return "지금은 안 됨";
            _view.PlayerUnit.TakeDamage(float.MaxValue, null);
            return _view.PlayerUnit.IsAlive ? "안 죽음 (방호막·무적이면 끝나거나 끈 뒤 다시)" : "죽음 → 금화 떨굼 → 용병 도망 → 여관 침대에서 깸";
        }
        #endregion

        #region DebugReenter - 숲을 나갔다 바로 다시 들어옴 (나갈 때 경로 MarkLeaving 그대로, 전초기지는 건너뜀 → 로딩창)
        [DebugControl("숲", "나갔다 다시 들어오기")]
        string DebugReenter()
        {
            if (DebugBusy) return "지금은 안 됨";
            MarkLeaving();
            Game.ChangeState(new FieldState());
            return "숲을 나갔다 다시 들어옴 (로딩창 넘기기). 클리어했으면 새 던전, 리셋 기준을 넘었으면 리셋 연출";
        }
        #endregion

        #region DebugMoveAboveRoomA - 마법사·용병을 던전 첫 무리 자리(방A) 바로 위 숲으로
        [DebugControl("숲", "방A 위 숲으로")]
        string DebugMoveAboveRoomA()
        {
            if (DebugBusy) return "지금은 안 됨";
            // 던전 적 무리 자리
            SpawnPointView[] points = _view.Dungeon.SpawnPoints;
            if (points == null || points.Length == 0 || points[0] == null) return "던전 적 자리 없음";
            // 방A 자리
            Vector3 room = points[0].transform.position;
            return DebugMove(DebugPointAt(new Vector3(room.x, _view.Entry.position.y, room.z), _view.Entry.forward), "방A 바로 위 숲으로");
        }
        #endregion

        #region DebugKillForReset - 숲 적을 리셋 기준까지 쓰러뜨림, 소탕 대상이 아닌 적부터
        [DebugControl("숲", "숲 적 70% 쓰러뜨리기")]
        string DebugKillForReset()
        {
            if (DebugBusy) return "지금은 안 됨";
            return DebugKillResult(DebugKillForest(ForestResetService.DebugNeed(_forest.Total), false));
        }
        #endregion

        #region DebugKillBeforeReset - 숲 적을 리셋 기준 한 마리 전까지 쓰러뜨림, 소탕 대상이 아닌 적부터
        [DebugControl("숲", "리셋 한 마리 전까지")]
        string DebugKillBeforeReset()
        {
            if (DebugBusy) return "지금은 안 됨";
            // 리셋에 필요한 쓰러뜨린 수
            int need = ForestResetService.DebugNeed(_forest.Total);
            if (_forest.DeadCount >= need) return "이미 리셋 기준을 넘음 (나갔다 오면 리셋)";
            return DebugKillResult(DebugKillForest(need - 1, false));
        }
        #endregion

        #region DebugKillLeavingOneTarget - 진행 중인 의뢰의 소탕 대상을 1마리만 남기고 쓰러뜨린 뒤, 리셋 기준까지 대상 아닌 적을 쓰러뜨림
        [DebugControl("숲", "고블린 1마리만 남기고 70%")]
        string DebugKillLeavingOneTarget()
        {
            if (DebugBusy) return "지금은 안 됨";
            if (!_quest.HasQuest || _quest.IsCleared) return "진행 중인 의뢰 없음 (의뢰를 못 끝낸 채로)";
            return DebugKillResult(DebugKillForest(ForestResetService.DebugNeed(_forest.Total), true));
        }
        #endregion

        #region DebugDeadlineStatus - 의뢰 기한까지 남은 판 시간 (의뢰 캡슐 기한 - 판 시각. 진행 중인 의뢰·기한이 없으면 -)
        [DebugControl("의뢰", "의뢰 마감까지")]
        string DebugDeadlineStatus
        {
            get
            {
                if (!_quest.HasQuest || _quest.IsCleared || _quest.Deadline <= 0f) return "-";
                return string.Format(CultureInfo.InvariantCulture, "{0:F0}초", Mathf.Max(0f, _quest.Deadline - Game.Run.Clock.PlaySeconds));
            }
        }
        #endregion

        #region DebugQuestCleared - 받은 의뢰의 소탕을 끝냈는지 (의뢰가 없으면 안 바뀜). 목표 갱신 (끝냈으면 '숲 입구로 돌아가기')
        [DebugControl("의뢰", "소탕 끝냄")]
        bool DebugQuestCleared
        {
            get => _quest.IsCleared;
            set
            {
                _quest.DebugSetCleared(value);
                UpdateObjective(true);
            }
        }
        #endregion

        #region DebugExpireSoon - 의뢰 캡슐에 기한 = 지금 판 시각 + 5초
        [DebugControl("의뢰", "기간 5초 남기기")]
        string DebugExpireSoon()
        {
            if (DebugBusy) return "지금은 안 됨";
            return _quest.DebugSetDeadline(Game.Run.Clock.PlaySeconds + DebugDeadlineLead) ? "의뢰 기한을 5초 뒤로" : "진행 중인 의뢰 없음 (없거나 이미 끝냄)";
        }
        #endregion

        #region DebugMoveDungeonFront - 마법사·용병을 던전 입구 밖으로 (덮개 페이드, 던전 드나들기와 같은 경로)
        [DebugControl("던전", "던전 입구 앞으로")]
        string DebugMoveDungeonFront() => DebugMove(_view != null ? _view.Dungeon.Outside : null, "던전 입구 밖으로");
        #endregion

        #region DebugMoveDungeonStart - 마법사·용병을 던전 안 시작 자리로
        [DebugControl("던전", "던전 안으로")]
        string DebugMoveDungeonStart() => DebugMove(_view != null ? _view.Dungeon.StartPoint : null, "던전 시작 자리로");
        #endregion

        #region DebugMoveBossFront - 마법사·용병을 보스 12m 앞으로 (시작 자리에서 보스 쪽, 보스방 입구 바로 안)
        [DebugControl("던전", "보스방 앞으로")]
        string DebugMoveBossFront()
        {
            if (DebugBusy) return "지금은 안 됨";
            // 던전 씬 참조
            DungeonView dungeon = _view.Dungeon;
            // 시작 자리 → 보스 평면 방향
            Vector3 toBoss = Vector3.ProjectOnPlane(dungeon.BossPoint.position - dungeon.StartPoint.position, Vector3.up).normalized;
            return DebugMove(DebugPointAt(dungeon.BossPoint.position - toBoss * DebugBossFrontGap, toBoss), "보스 12m 앞으로");
        }
        #endregion

        #region DebugMove - 덮개를 덮고 마법사·용병을 그 자리로 (MoveParty 경로). 한 일
        /// <param name="point">옮길 자리</param>
        /// <param name="where">어디로 (한 일 글)</param>
        string DebugMove(Transform point, string where)
        {
            if (DebugBusy || point == null) return "지금은 안 됨";
            MoveParty(point);
            return string.Format(CultureInfo.InvariantCulture, "덮개 → 마법사·용병을 {0}", where);
        }
        #endregion

        #region DebugPointAt - 임시 옮길 자리를 그곳(NavMesh 위)·방향으로 (처음이면 숲 씬에 만듦)
        /// <param name="position">자리</param>
        /// <param name="forward">바라볼 평면 방향</param>
        Transform DebugPointAt(Vector3 position, Vector3 forward)
        {
            if (_debugPoint == null)
            {
                // 임시 자리 오브젝트
                var go = new GameObject("DebugPoint");
                SceneManager.MoveGameObjectToScene(go, _view.gameObject.scene);
                _debugPoint = go.transform;
            }
            _debugPoint.SetPositionAndRotation(FieldService.Sample(position), Quaternion.LookRotation(forward.sqrMagnitude > 0f ? forward : Vector3.forward, Vector3.up));
            return _debugPoint;
        }
        #endregion

        #region DebugKillForest - 살아 있는 숲 적을 쓰러뜨림 (때린 몸 없음 → 무리를 안 깨움, 숲 적·의뢰 캡슐 쓰러짐 경로 그대로): targetsFirst면 먼저 소탕 대상을 1마리만 남기고, 그다음 쓰러뜨린 숲 적이 goal이 될 때까지 대상 아닌 적. 진행 중인 의뢰의 소탕 대상은 늘 남김 (목표가 안 끝나게). 쓰러뜨린 수
        /// <param name="goal">쓰러뜨린 숲 적이 이만큼 될 때까지</param>
        /// <param name="targetsFirst">먼저 소탕 대상을 1마리만 남기고 쓰러뜨릴지</param>
        int DebugKillForest(int goal, bool targetsFirst)
        {
            // 소탕 대상 몬스터 (진행 중인 의뢰가 없으면 null)
            MonsterSO target = _quest.HasQuest && !_quest.IsCleared ? _quest.Current.Target : null;
            // 살아 있는 숲 적
            var alive = new List<EnemyController>();
            _forest.DebugAlive(alive);
            // 쓰러뜨린 수
            int killed = 0;
            if (targetsFirst && target != null)
            {
                // 살아 있는 소탕 대상 수
                int targets = 0;
                foreach (EnemyController enemy in alive)
                    if (enemy.Monster == target) targets++;
                foreach (EnemyController enemy in alive)
                {
                    if (targets <= DebugKeepTargets) break;
                    if (enemy.Monster != target) continue;
                    enemy.Unit.TakeDamage(float.MaxValue, null);
                    targets--;
                    killed++;
                }
            }
            foreach (EnemyController enemy in alive)
            {
                if (_forest.DeadCount >= goal) break;
                if (!enemy.Unit.IsAlive || (target != null && enemy.Monster == target)) continue;
                enemy.Unit.TakeDamage(float.MaxValue, null);
                killed++;
            }
            return killed;
        }
        #endregion

        #region DebugKillResult - 쓰러뜨린 수와 지금 숲 적·리셋 기준 글
        /// <param name="killed">쓰러뜨린 수</param>
        string DebugKillResult(int killed) => string.Format(CultureInfo.InvariantCulture, "숲 적 {0}마리를 쓰러뜨림 → 쓰러뜨림 {1} / {2} (리셋 {3}). 이제 [나갔다 다시 들어오기]",
            killed, _forest.DeadCount, _forest.Total, ForestResetService.DebugNeed(_forest.Total));
        #endregion
    }
}
#endif
