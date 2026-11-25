using UnityEngine;

namespace Mage
{
    /// <summary>숲 구역 (숲 상태가 숲 화면 값으로 만들어 넘김): 지역 범위(x·z)와 구역 깊이·수로 월드 z가 몇 구역인지, 구역의 z 범위를 알려 줌. 구역은 지역 앞(입구 쪽, z 최소)부터 깊이만큼씩 1·2·3…</summary>
    public readonly struct ForestZoneSpec
    {
        /// <summary>지역 (x·y = 월드 x·z 최소, m)</summary>
        public Rect Area { get; }

        /// <summary>구역 하나 깊이 (m)</summary>
        public float Depth { get; }

        /// <summary>구역 수</summary>
        public int Count { get; }

        #region ForestZoneSpec - 지역·구역 깊이·구역 수를 받음 (깊이 1m·구역 1개 아래로는 자름)
        /// <param name="area">지역 (x·y = 월드 x·z 최소, m)</param>
        /// <param name="depth">구역 하나 깊이 (m)</param>
        /// <param name="count">구역 수</param>
        public ForestZoneSpec(Rect area, float depth, int count)
        {
            Area = area;
            Depth = Mathf.Max(1f, depth);
            Count = Mathf.Max(1, count);
        }
        #endregion

        #region ZoneOf - 월드 z가 있는 구역 번호 (1~구역 수, 지역 밖이면 가까운 끝 구역)
        /// <param name="z">월드 z</param>
        public int ZoneOf(float z) => Mathf.Clamp(Mathf.FloorToInt((z - Area.yMin) / Depth) + 1, 1, Count);
        #endregion

        #region MinZ - 구역이 시작하는 월드 z
        /// <param name="zone">구역 번호 (1~구역 수, 밖이면 가까운 끝 구역)</param>
        public float MinZ(int zone) => Area.yMin + (Mathf.Clamp(zone, 1, Count) - 1) * Depth;
        #endregion

        #region MaxZ - 구역이 끝나는 월드 z (지역 끝을 넘지 않게)
        /// <param name="zone">구역 번호 (1~구역 수, 밖이면 가까운 끝 구역)</param>
        public float MaxZ(int zone) => Mathf.Min(Area.yMax, MinZ(zone) + Depth);
        #endregion
    }
}
