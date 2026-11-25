#if UNITY_EDITOR
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>떨어진 금화 캡슐 확인 진입점 (에디터 전용, 빌드에 안 들어감): 떨어진 금화 상태 줄, 줍는 몬스터 옆에 금화 놓기 (놓기는 자기 Place 경로, 몬스터는 받은 현장 규칙의 적 목록에서 고름)</summary>
    public sealed partial class FieldLootHandler
    {
        /// <summary>놓을 금화</summary>
        const int CheckGold = 100;

        /// <summary>고른 몬스터에서 금화까지 거리 (m, 마법사 반대쪽)</summary>
        const float CheckGoldGap = 6f;

        /// <summary>고르는 몬스터가 마법사에게서 알아채는 거리보다 더 떨어져 있어야 하는 여유 (m, 용병이 앞설 때)</summary>
        const float CheckPartyMargin = 6f;

        #region CheckStatus - 떨어진 금화와 자리 (없으면 없음)
        [CheckStatus("떨어진 금화")]
        string CheckStatus() => _data.Gold > 0
            ? string.Format(CultureInfo.InvariantCulture, "{0} · ({1:F0}, {2:F0})", _data.Gold, _data.Position.x, _data.Position.z)
            : "없음";
        #endregion

        #region CheckDropNearPicker - 보관 장소와 같은 층의 안 쫓고 빈손인 줍는 몬스터 중 마법사가 알아챌 거리 밖에서 가장 가까운 것의 6m 옆(마법사 반대쪽)에 금화 100을 놓음 (앞의 떨어진 금화는 사라짐) (3-1·3-2)
        [CheckButton("forest-dungeon", "3-1,3-2", "고블린 옆에 금화 100")]
        string CheckDropNearPicker()
        {
            // 마법사 자리
            Vector3 from = _field.Player.transform.position;
            // 고른 몬스터와 마법사까지 평면 거리 제곱
            EnemyController best = null;
            float bestSq = float.MaxValue;
            foreach (EnemyController enemy in _field.CheckEnemies)
            {
                if (enemy == null || !enemy.Unit.IsAlive || !enemy.Monster.PicksUpItems || enemy.Target != null || enemy.CarriedGold > 0) continue;
                if (_field.StashPoint != null && !FieldService.SameLevel(enemy.transform.position, _field.StashPoint.position)) continue;
                // 마법사까지 평면 거리 제곱
                float sq = Vector3.ProjectOnPlane(enemy.transform.position - from, Vector3.up).sqrMagnitude;
                // 알아챌 거리 + 여유
                float far = enemy.Monster.AggroRange + CheckPartyMargin;
                if (sq <= far * far || sq >= bestSq) continue;
                best = enemy;
                bestSq = sq;
            }
            if (best == null) return "알맞은 고블린 없음 (모두 파티 가까이 있거나 쫓는 중·나르는 중)";
            // 마법사 반대쪽 평면 방향
            Vector3 away = Vector3.ProjectOnPlane(best.transform.position - from, Vector3.up).normalized;
            Place(FieldService.Sample(best.transform.position + away * CheckGoldGap), CheckGold);
            return string.Format(CultureInfo.InvariantCulture, "{0:F0}m 앞 고블린 옆에 금화 {1}. 그 거리에서 지켜봐 (고블린 {2:F0}m 안으로 가면 금화 대신 파티를 쫓음)", Mathf.Sqrt(bestSq), CheckGold, best.Monster.AggroRange);
        }
        #endregion
    }
}
#endif
