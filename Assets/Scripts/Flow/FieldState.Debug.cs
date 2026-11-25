#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>숲 확인 패널(FieldCheckWindow) 진입점 (에디터 전용, 빌드에 안 들어감): 확인 항목 상황을 바로 만듦. 죽기·나가기·던전 드나들기·보스 소환은 실제 코드 경로(OnPlayerDied·MarkLeaving·MoveParty·피해)를 그대로 탐</summary>
    public sealed partial class FieldState
    {
        /// <summary>옮길 곳 (확인 패널 버튼)</summary>
        public enum CheckSpot
        {
            /// <summary>던전 입구 밖</summary>
            DungeonFront,

            /// <summary>던전 안 시작 자리</summary>
            DungeonStart,

            /// <summary>보스방 앞 (시작 자리에서 보스 자리 쪽, 보스 12m 앞)</summary>
            BossFront,

            /// <summary>던전 첫 무리 자리(방A) 바로 위 숲</summary>
            AboveRoomA,
        }

        /// <summary>보스방 앞에서 보스까지 거리 (m)</summary>
        const float CheckBossFrontGap = 12f;

        /// <summary>떨굴 금화 (확인용)</summary>
        const int CheckGold = 100;

        /// <summary>금화를 떨굴 거리 (마법사 앞, m)</summary>
        const float CheckGoldAhead = 6f;

        /// <summary>깨울 숲 적 수</summary>
        const int CheckWakeCount = 5;

        /// <summary>리셋 비율 (ForestResetService.ResetRatio와 같게)</summary>
        const float CheckResetRatio = 0.7f;

        /// <summary>임시 옮길 자리 (보스방 앞·방A 위. 숲 씬에 두어 씬과 같이 지워짐)</summary>
        Transform _checkPoint;

        /// <summary>버튼을 받을 수 없는지 (씬 준비 전·죽는 중·리셋 연출 중)</summary>
        public bool CheckBusy => _view == null || _dying || _reset == null || _reset.IsResetting;

        /// <summary>쓰러뜨린 숲 적 수 (준비 전엔 0)</summary>
        public int CheckDeadCount => _forest != null ? _forest.DeadCount : 0;

        /// <summary>숲 적 자리 수 합 (준비 전엔 0)</summary>
        public int CheckTotal => _forest != null ? _forest.Total : 0;

        /// <summary>리셋에 필요한 쓰러뜨린 수 (70%, 올림)</summary>
        public int CheckResetNeed => Mathf.CeilToInt(CheckTotal * CheckResetRatio);

        /// <summary>마법사가 있는 곳 ("숲 N구역" 또는 "던전 안")</summary>
        public string CheckWhere
        {
            get
            {
                if (_view == null) return "-";
                return FieldService.SameLevel(_view.Player.transform.position, _view.Entry.position) ? $"숲 {ZoneAt()}구역" : "던전 안";
            }
        }

        #region CheckNextDayShortly - 판 시계를 다음 날 3초 전으로 (1번)
        public void CheckNextDayShortly()
        {
            if (CheckBusy) return;
            // 판 시계
            ClockEntry clock = Game.Run.Clock;
            clock.PlaySeconds = clock.Day * ClockEntry.DaySeconds - 3f;
        }
        #endregion

        #region CheckExpireSoon - 받은 의뢰가 진행 중이면 기한을 5초 뒤로 (2·8번). 없거나 끝냈으면 false
        public bool CheckExpireSoon()
        {
            if (CheckBusy || !_quest.HasQuest || _quest.IsCleared) return false;
            Game.Run.Quest.Deadline = Game.Run.Clock.PlaySeconds + 5f;
            return true;
        }
        #endregion

        #region CheckDropGold - 마법사 앞 6m에 금화 100을 떨어진 금화로 놓음 (가진 금화는 그대로, 금화 캡슐 경로) (3번)
        public void CheckDropGold()
        {
            if (CheckBusy) return;
            // 마법사 몸
            Transform player = _view.Player.transform;
            _loot.DropOnDeath(FieldService.Sample(player.position + player.forward * CheckGoldAhead), CheckGold);
            _loot.Spawn();
        }
        #endregion

        #region CheckDie - 마법사를 죽임 (실제 죽음 경로: 금화 떨굼 → 용병 도망 → 여관 부활) (3·6번)
        public void CheckDie()
        {
            if (CheckBusy) return;
            _view.PlayerUnit.TakeDamage(float.MaxValue, null);
        }
        #endregion

        #region CheckMoveTo - 마법사·용병을 그곳으로 (덮개 페이드, 던전 드나들기와 같은 경로) (4·11·12번)
        /// <param name="spot">옮길 곳</param>
        public void CheckMoveTo(CheckSpot spot)
        {
            if (CheckBusy) return;
            // 던전 씬 참조
            DungeonView dungeon = _view.Dungeon;
            switch (spot)
            {
                case CheckSpot.DungeonFront: MoveParty(dungeon.Outside); break;
                case CheckSpot.DungeonStart: MoveParty(dungeon.StartPoint); break;
                case CheckSpot.BossFront:
                    {
                        // 시작 자리 → 보스 평면 방향
                        Vector3 toBoss = Vector3.ProjectOnPlane(dungeon.BossPoint.position - dungeon.StartPoint.position, Vector3.up).normalized;
                        MoveParty(CheckPointAt(dungeon.BossPoint.position - toBoss * CheckBossFrontGap, toBoss));
                        break;
                    }
                case CheckSpot.AboveRoomA:
                    {
                        if (dungeon.SpawnPoints == null || dungeon.SpawnPoints.Length == 0 || dungeon.SpawnPoints[0] == null) return;
                        // 방A 자리
                        Vector3 room = dungeon.SpawnPoints[0].transform.position;
                        MoveParty(CheckPointAt(new Vector3(room.x, _view.Entry.position.y, room.z), _view.Entry.forward));
                        break;
                    }
            }
        }
        #endregion

        #region CheckPointAt - 임시 옮길 자리를 그곳(NavMesh 위)·방향으로 (처음이면 숲 씬에 만듦)
        /// <param name="position">자리</param>
        /// <param name="forward">바라볼 평면 방향</param>
        Transform CheckPointAt(Vector3 position, Vector3 forward)
        {
            if (_checkPoint == null)
            {
                // 임시 자리 오브젝트
                var go = new GameObject("FieldCheckPoint");
                SceneManager.MoveGameObjectToScene(go, _view.gameObject.scene);
                _checkPoint = go.transform;
            }
            _checkPoint.SetPositionAndRotation(FieldService.Sample(position), Quaternion.LookRotation(forward.sqrMagnitude > 0f ? forward : Vector3.forward, Vector3.up));
            return _checkPoint;
        }
        #endregion

        #region CheckWakeNearby - 같은 층 가까운 살아 있는 적 5마리가 마법사를 쫓음 (12번 준비). 깨운 수
        public int CheckWakeNearby()
        {
            if (CheckBusy) return 0;
            // 마법사 자리
            Vector3 from = _view.Player.transform.position;
            // 같은 층 살아 있는 적
            var near = new List<EnemyController>();
            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (enemy.Unit.IsAlive && FieldService.SameLevel(enemy.transform.position, from)) near.Add(enemy);
            near.Sort((a, b) => (a.transform.position - from).sqrMagnitude.CompareTo((b.transform.position - from).sqrMagnitude));
            // 깨운 수
            int count = Mathf.Min(CheckWakeCount, near.Count);
            for (int i = 0; i < count; i++) near[i].Wake(_service.Player);
            return count;
        }
        #endregion

        #region CheckBossHalf - 보스를 마법사가 때린 것으로 체력 절반까지 (쫓기 시작 → 부하 소환 경로). 보스가 없거나 죽었으면 false (5번)
        public bool CheckBossHalf()
        {
            // 살아 있는 보스
            EnemyController boss = CheckFindBoss();
            if (boss == null) return false;
            boss.Unit.TakeDamage(Mathf.Max(0.1f, boss.Unit.Hp - boss.Unit.MaxHp * 0.5f), _service.Player);
            return true;
        }
        #endregion

        #region CheckBossKill - 보스를 마법사가 때린 것으로 즉사 (클리어 경로). 보스가 없거나 죽었으면 false (5·6번)
        public bool CheckBossKill()
        {
            // 살아 있는 보스
            EnemyController boss = CheckFindBoss();
            if (boss == null) return false;
            boss.Unit.TakeDamage(float.MaxValue, _service.Player);
            return true;
        }
        #endregion

        #region CheckFindBoss - 씬의 살아 있는 보스 (보스 패턴이 붙은 적). 없으면 null
        EnemyController CheckFindBoss()
        {
            if (CheckBusy) return null;
            foreach (BossPatternController pattern in Object.FindObjectsByType<BossPatternController>(FindObjectsSortMode.None))
                if (pattern.TryGetComponent(out EnemyController enemy) && enemy.Unit.IsAlive) return enemy;
            return null;
        }
        #endregion

        #region CheckClearForest70 - 쓰러뜨린 숲 적이 70%가 될 때까지 살아 있는 숲 적을 쓰러뜨림. 소탕 대상이 아닌 적부터 (8번: 의뢰를 못 끝낸 채 리셋). 때린 몸 없음 → 무리를 안 깨움, 숲 적 캡슐·의뢰 캡슐 경로 그대로. 쓰러뜨린 수 (7·8번)
        public int CheckClearForest70()
        {
            if (CheckBusy) return 0;
            // 소탕 대상 몬스터 (의뢰가 없으면 null)
            MonsterSO target = _quest.Current != null ? _quest.Current.Target : null;
            // 살아 있는 숲 적 (소탕 대상이 아닌 적 먼저)
            var alive = new List<EnemyController>();
            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (enemy.Unit.IsAlive && !string.IsNullOrEmpty(enemy.SpawnKey) && !enemy.SpawnKey.StartsWith("dungeon.", System.StringComparison.Ordinal)) alive.Add(enemy);
            alive.Sort((a, b) => (a.Monster == target).CompareTo(b.Monster == target));
            // 쓰러뜨린 수
            int killed = Mathf.Clamp(CheckResetNeed - CheckDeadCount, 0, alive.Count);
            for (int i = 0; i < killed; i++) alive[i].Unit.TakeDamage(float.MaxValue, null);
            return killed;
        }
        #endregion

        #region CheckReenter - 숲을 나갔다 바로 다시 들어옴 (나갈 때 경로 MarkLeaving 그대로, 전초기지는 건너뜀 → 로딩창) (6·7번)
        public void CheckReenter()
        {
            if (CheckBusy) return;
            MarkLeaving();
            Game.ChangeState(new FieldState());
        }
        #endregion
    }
}
#endif
