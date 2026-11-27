using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mage
{
    /// <summary>차원문 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 판 데이터·차원문 표지·숲 지역·숲 입구를 받아, 숲에 들어올 때마다 차원문을 놓음 (발견한 것은 남긴 자리, 아직이면 자기 지역 고리 안 무작위. 입구 40m·차원문끼리 60m 띄우고 전초기지 가운데를 보게). 마법사가 24m 안(같은 층)에 들어오면 발견: 자리를 판 데이터에 남기고 Found로 지역 번호를 알림. 리셋 연출에 쓸 가장 가까운 차원문, 지도에 보일 발견한 자리를 줌</summary>
    public sealed partial class PortalService
    {
        /// <summary>이 거리 안에 들어오면 발견 (m, 안개를 밝히는 반지름과 같게. 임시)</summary>
        const float FindRadius = FogMapService.RevealRadius;

        /// <summary>지역 경계에서 띄우는 거리 (m, 차원문이 두 지역에 걸치지 않게)</summary>
        const float EdgeMargin = 8f;

        /// <summary>숲 입구에서 띄우는 거리 (m, 임시)</summary>
        const float EntryClearance = 40f;

        /// <summary>차원문끼리 띄우는 거리 (m, 임시)</summary>
        const float Spacing = 60f;

        /// <summary>자리를 뽑아 보는 횟수 (다 안 맞으면 마지막으로 뽑은 곳)</summary>
        const int PickTries = 30;

        /// <summary>뽑은 곳에서 길(NavMesh)을 찾는 거리 (m)</summary>
        const float PickSampleDistance = 4f;

        /// <summary>차원문 판 데이터</summary>
        readonly PortalEntry _data;

        /// <summary>차원문 표지</summary>
        readonly PortalView[] _portals;

        /// <summary>숲 지역 (자기 지역 고리에서 자리를 뽑음)</summary>
        readonly ForestRegionSpec _regions;

        /// <summary>숲 입구 자리 (이 둘레엔 안 놓음)</summary>
        readonly Vector3 _entry;

        /// <summary>이번 진입에 놓은 자리 (뽑을 때 간격 비교)</summary>
        readonly List<Vector3> _placed = new();

        /// <summary>차원문 수 (빈 칸 뺌)</summary>
        public int Total { get; }

        /// <summary>발견한 차원문 수</summary>
        public int FoundCount => _data.Found.Count;

        /// <summary>차원문을 발견함 (그 차원문 지역 번호)</summary>
        public event Action<int> Found;

        #region PortalService - 판 데이터·차원문 표지·숲 지역·숲 입구를 받고 차원문 수를 셈
        /// <param name="data">차원문 판 데이터</param>
        /// <param name="portals">차원문 표지</param>
        /// <param name="regions">숲 지역</param>
        /// <param name="entry">숲 입구 자리</param>
        public PortalService(PortalEntry data, PortalView[] portals, ForestRegionSpec regions, Vector3 entry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _portals = portals ?? throw new ArgumentNullException(nameof(portals));
            _regions = regions;
            _entry = entry;
            // 빈 칸 뺀 차원문 수
            int total = 0;
            foreach (PortalView portal in _portals)
                if (portal != null) total++;
            Total = total;
        }
        #endregion

        #region Place - 차원문을 놓음: 발견한 것은 남긴 자리에 먼저, 아직인 것은 자기 지역 고리 안에서 뽑은 자리에. 다 전초기지 가운데를 봄 (빈 칸·id 없는 표지는 경고 후 건너뜀)
        public void Place()
        {
            _placed.Clear();
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || !_data.Found.TryGetValue(portal.Id, out Vector3 at)) continue;
                portal.transform.position = at;
                Face(portal.transform);
                _placed.Add(at);
            }
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id))
                {
                    Debug.LogWarning("[Portal] 비었거나 id가 없는 차원문 표지", portal);
                    continue;
                }
                if (_data.Found.ContainsKey(portal.Id)) continue;
                // 뽑은 자리
                Vector3 at = Pick(portal.Region);
                portal.transform.position = at;
                Face(portal.transform);
                _placed.Add(at);
            }
        }
        #endregion

        #region Pick - 지역 고리(경계 8m 안쪽)에서 뽑아 숲 층 길 위·같은 지역·입구 40m 밖·이번에 놓은 차원문과 60m 밖이면 그곳 (30번 다 안 맞으면 마지막으로 뽑은 곳을 길 위로)
        /// <param name="region">지역 번호</param>
        Vector3 Pick(int region)
        {
            // 뽑을 고리 반지름
            float min = _regions.MinRadius(region) + EdgeMargin;
            float max = _regions.MaxRadius(region) - EdgeMargin;
            // 마지막으로 뽑은 곳
            Vector3 picked = _entry;
            for (int i = 0; i < PickTries; i++)
            {
                picked = _regions.RandomPoint(min, max, _entry.y);
                if (!NavMesh.SamplePosition(picked, out NavMeshHit hit, PickSampleDistance, NavMesh.AllAreas)) continue;
                if (!FieldService.SameLevel(hit.position, _entry) || _regions.RegionOf(hit.position) != region) continue;
                if (FlatSqr(hit.position, _entry) < EntryClearance * EntryClearance || IsCrowded(hit.position)) continue;
                return hit.position;
            }
            return FieldService.Sample(picked);
        }
        #endregion

        #region IsCrowded - 이번에 놓은 차원문과 60m 안인지
        /// <param name="position">볼 자리</param>
        bool IsCrowded(Vector3 position)
        {
            foreach (Vector3 placed in _placed)
                if (FlatSqr(position, placed) < Spacing * Spacing) return true;
            return false;
        }
        #endregion

        #region Face - 차원문이 전초기지 가운데를 보게 (평면 방향만)
        /// <param name="portal">차원문</param>
        void Face(Transform portal)
        {
            // 차원문 → 지역 가운데 평면 방향
            Vector3 toCenter = _regions.Center - portal.position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0f) portal.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
        }
        #endregion

        #region Check - 아직 발견 안 한 차원문 중 마법사 자리에서 24m 안(같은 층)이면 발견: 자리를 판 데이터에 남기고 지역 번호를 알림
        /// <param name="position">마법사 자리</param>
        public void Check(Vector3 position)
        {
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || _data.Found.ContainsKey(portal.Id)) continue;
                // 차원문 자리
                Vector3 at = portal.transform.position;
                if (!FieldService.SameLevel(at, position) || FlatSqr(at, position) > FindRadius * FindRadius) continue;
                _data.Found[portal.Id] = at;
                Found?.Invoke(portal.Region);
            }
        }
        #endregion

        #region Nearest - 그 자리에서 가장 가까운 차원문 (발견 여부 무관, 없으면 null). 리셋 연출에 씀
        /// <param name="from">기준 자리</param>
        public Transform Nearest(Vector3 from)
        {
            // 찾은 차원문과 거리 제곱
            Transform best = null;
            float bestSq = float.MaxValue;
            foreach (PortalView portal in _portals)
            {
                if (portal == null) continue;
                // 거리 제곱
                float sq = FlatSqr(portal.transform.position, from);
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = portal.transform;
            }
            return best;
        }
        #endregion

        #region CopyFound - 발견한 차원문 자리를 result에 채움 (먼저 비움. 지도 표시)
        /// <param name="result">채울 목록</param>
        public void CopyFound(List<Vector3> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (Vector3 at in _data.Found.Values) result.Add(at);
        }
        #endregion

        #region FlatSqr - 두 자리 평면 거리 제곱
        /// <param name="a">한 자리</param>
        /// <param name="b">다른 자리</param>
        static float FlatSqr(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude;
        #endregion
    }
}
