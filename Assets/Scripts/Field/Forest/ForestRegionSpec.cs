using UnityEngine;

namespace Mage
{
    /// <summary>숲 지역 (숲 상태가 숲 화면 값으로 만들어 넘김): 전초기지 가운데를 둘러싼 원형 고리 지역. 가운데에서 잰 평면 거리로 몇 지역인지, 지역의 안쪽·바깥 반지름, 두 반지름 사이 아무 자리를 알려 줌. 1지역은 안쪽 반지름부터 폭만큼, 다음 지역은 바깥으로 폭만큼씩</summary>
    public readonly struct ForestRegionSpec
    {
        /// <summary>지역 가운데 (전초기지 가운데, 월드. y는 안 씀)</summary>
        public Vector3 Center { get; }

        /// <summary>1지역이 시작하는 반지름 (m. 안쪽은 전초기지)</summary>
        public float InnerRadius { get; }

        /// <summary>지역 하나 폭 (m)</summary>
        public float Width { get; }

        /// <summary>지역 수</summary>
        public int Count { get; }

        /// <summary>마지막 지역이 끝나는 반지름 (m, 숲 바깥 경계)</summary>
        public float OuterRadius => InnerRadius + Width * Count;

        #region ForestRegionSpec - 가운데·안쪽 반지름·폭·지역 수를 받음 (반지름 0·폭 1m·지역 1개 아래로는 자름)
        /// <param name="center">지역 가운데 (전초기지 가운데)</param>
        /// <param name="innerRadius">1지역이 시작하는 반지름 (m)</param>
        /// <param name="width">지역 하나 폭 (m)</param>
        /// <param name="count">지역 수</param>
        public ForestRegionSpec(Vector3 center, float innerRadius, float width, int count)
        {
            Center = center;
            InnerRadius = Mathf.Max(0f, innerRadius);
            Width = Mathf.Max(1f, width);
            Count = Mathf.Max(1, count);
        }
        #endregion

        #region Distance - 가운데에서 평면 거리 (m, 높이 무시)
        /// <param name="position">월드 위치</param>
        public float Distance(Vector3 position) => new Vector2(position.x - Center.x, position.z - Center.z).magnitude;
        #endregion

        #region RegionOf - 위치가 있는 지역 번호 (1~지역 수. 전초기지 쪽은 1, 바깥 경계 밖은 마지막 지역)
        /// <param name="position">월드 위치</param>
        public int RegionOf(Vector3 position) => Mathf.Clamp(Mathf.FloorToInt((Distance(position) - InnerRadius) / Width) + 1, 1, Count);
        #endregion

        #region MinRadius - 지역이 시작하는 반지름 (m)
        /// <param name="region">지역 번호 (1~지역 수, 밖이면 가까운 끝 지역)</param>
        public float MinRadius(int region) => InnerRadius + (Mathf.Clamp(region, 1, Count) - 1) * Width;
        #endregion

        #region MaxRadius - 지역이 끝나는 반지름 (m)
        /// <param name="region">지역 번호 (1~지역 수, 밖이면 가까운 끝 지역)</param>
        public float MaxRadius(int region) => MinRadius(region) + Width;
        #endregion

        #region RandomPoint - 두 반지름 사이 고리 안 아무 자리 (넓이 고르게: 각도는 고르게, 반지름은 제곱 기준으로 고르게)
        /// <param name="minRadius">안쪽 반지름 (m)</param>
        /// <param name="maxRadius">바깥 반지름 (m)</param>
        /// <param name="y">높이 (월드)</param>
        public Vector3 RandomPoint(float minRadius, float maxRadius, float y)
        {
            // 각도 (라디안, +z에서 +x 쪽으로)
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // 반지름 (넓이 고르게)
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            return new Vector3(Center.x + Mathf.Sin(angle) * radius, y, Center.z + Mathf.Cos(angle) * radius);
        }
        #endregion
    }
}
