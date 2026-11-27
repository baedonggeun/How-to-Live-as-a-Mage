#if UNITY_EDITOR
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>차원문 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 발견 수 값 줄, 발견 기록 지우기, 가장 가까운 안 발견 차원문 자리 (숲 상태 디버그 진입점이 그 근처로 옮김)</summary>
    public sealed partial class PortalService
    {
        #region DebugStatus - 발견한 차원문 / 차원문 수
        [DebugControl("숲", "차원문")]
        string DebugStatus => string.Format(CultureInfo.InvariantCulture, "발견 {0} / {1}", FoundCount, Total);
        #endregion

        #region DebugForget - 발견 기록을 지움 (지금 자리는 그대로, 다음 진입에 다시 뽑음)
        [DebugControl("숲", "차원문 발견 지우기")]
        string DebugForget()
        {
            _data.Found.Clear();
            return "발견 기록을 지움 → 다음 진입에 자리를 다시 뽑음 (지금 자리에서 24m 안에 가면 다시 발견)";
        }
        #endregion

        #region DebugNearestHidden - 그 자리에서 가장 가까운 안 발견 차원문 자리 (다 발견했으면 null)
        /// <param name="from">기준 자리</param>
        public Vector3? DebugNearestHidden(Vector3 from)
        {
            // 찾은 자리와 거리 제곱
            Vector3? best = null;
            float bestSq = float.MaxValue;
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || _data.Found.ContainsKey(portal.Id)) continue;
                // 거리 제곱
                float sq = FlatSqr(portal.transform.position, from);
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = portal.transform.position;
            }
            return best;
        }
        #endregion
    }
}
#endif
