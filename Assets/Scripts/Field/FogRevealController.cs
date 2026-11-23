using UnityEngine;

namespace Mage
{
    /// <summary>붙은 오브젝트(숲 플레이어)가 1m 움직일 때마다 안개 지도를 밝힘. 안개 지도는 상태가 Init으로 넘김</summary>
    public sealed class FogRevealController : MonoBehaviour
    {
        /// <summary>이만큼 움직일 때마다 밝힘 (m)</summary>
        const float MinMove = 1f;

        /// <summary>안개 지도 (없으면 안 함)</summary>
        FogMapService _fog;

        /// <summary>마지막으로 밝힌 위치</summary>
        Vector3 _last;

        /// <summary>아직 한 번도 안 밝혔는지</summary>
        bool _first;

        #region Init - 안개 지도를 받음 (다음 프레임에 지금 자리를 밝힘)
        /// <param name="fog">안개 지도</param>
        public void Init(FogMapService fog)
        {
            _fog = fog ?? throw new System.ArgumentNullException(nameof(fog));
            _first = true;
        }
        #endregion

        #region Update - 처음이거나 1m 넘게 움직였으면 지금 자리를 밝힘
        void Update()
        {
            if (_fog == null) return;
            // 지금 위치
            Vector3 position = transform.position;
            if (!_first && (position - _last).sqrMagnitude < MinMove * MinMove) return;
            _first = false;
            _last = position;
            _fog.Reveal(position);
        }
        #endregion
    }
}
