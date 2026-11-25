#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>현장 규칙 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 쫓는 적 값 줄(마법사 층·다른 층), 등록된 적 목록(다른 캡슐 디버그 진입점이 읽음), 근처 적 깨우기, 금화 든 적 쓰러뜨리기</summary>
    public sealed partial class FieldService
    {
        /// <summary>한 번에 깨울 적 수</summary>
        const int DebugWakeCount = 5;

        /// <summary>등록된 적 (죽은 적 포함, 등록 순서. 디버그 진입점만 읽음)</summary>
        public IReadOnlyList<EnemyController> DebugEnemies => _enemies;

        #region DebugChasing - 쫓는 중인 살아 있는 적 수 (마법사와 같은 층 / 다른 층)
        [DebugControl("숲", "쫓는 적")]
        string DebugChasing
        {
            get
            {
                // 마법사 자리
                Vector3 from = Player.transform.position;
                // 같은 층·다른 층 수
                int same = 0;
                int other = 0;
                foreach (EnemyController enemy in _enemies)
                {
                    if (enemy == null || !enemy.Unit.IsAlive || enemy.Target == null) continue;
                    if (SameLevel(enemy.transform.position, from)) same++;
                    else other++;
                }
                return string.Format(CultureInfo.InvariantCulture, "내 층 {0} · 다른 층 {1}", same, other);
            }
        }
        #endregion

        #region DebugWakeNearby - 마법사와 같은 층, 포기 거리(46m) 안 안 쫓는 살아 있는 적 중 가까운 5마리가 마법사를 쫓음
        [DebugControl("숲", "근처 적 깨우기")]
        string DebugWakeNearby()
        {
            if (!Player.IsAlive) return "마법사가 쓰러져 있음";
            // 마법사 자리
            Vector3 from = Player.transform.position;
            // 포기 거리 제곱
            float rangeSq = EnemyController.GiveUpDistance * EnemyController.GiveUpDistance;
            // 깨울 후보 (포기 거리 안)
            var near = new List<EnemyController>();
            foreach (EnemyController enemy in _enemies)
            {
                if (enemy == null || !enemy.Unit.IsAlive || enemy.Target != null || !SameLevel(enemy.transform.position, from)) continue;
                if (Flat(enemy.transform.position - from).sqrMagnitude <= rangeSq) near.Add(enemy);
            }
            near.Sort((a, b) => Flat(a.transform.position - from).sqrMagnitude.CompareTo(Flat(b.transform.position - from).sqrMagnitude));
            // 깨운 수
            int count = Mathf.Min(DebugWakeCount, near.Count);
            for (int i = 0; i < count; i++) near[i].Wake(Player);
            return count > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0:F0}m 안 적 {1}마리가 마법사를 쫓음", EnemyController.GiveUpDistance, count)
                : string.Format(CultureInfo.InvariantCulture, "{0:F0}m 안에 안 쫓는 적 없음", EnemyController.GiveUpDistance);
        }
        #endregion

        #region DebugKillCarrier - 금화를 들고 가는 살아 있는 적을 다 쓰러뜨림 (때린 몸 없음 → 무리를 안 깨움. 쓰러진 자리에 금화를 떨구는 경로 그대로)
        [DebugControl("숲", "금화 든 적 쓰러뜨리기")]
        string DebugKillCarrier()
        {
            // 쓰러뜨린 수
            int count = 0;
            foreach (EnemyController enemy in _enemies)
            {
                if (enemy == null || !enemy.Unit.IsAlive || enemy.CarriedGold <= 0) continue;
                enemy.Unit.TakeDamage(float.MaxValue, null);
                count++;
            }
            return count > 0
                ? string.Format(CultureInfo.InvariantCulture, "금화 든 적 {0}마리를 쓰러뜨림 → 그 자리에 [F] 떨어진 금화", count)
                : "금화를 든 적 없음 (고블린이 아직 안 주웠거나 이미 보관함)";
        }
        #endregion
    }
}
#endif
