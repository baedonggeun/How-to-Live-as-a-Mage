using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 안개 지도 (v1, MonoBehaviour 아님): 지역을 3m 칸으로 나눠 걸으면 반경 24m를 밝힘. 진행도는 60m 구획 단위로, 구획의 셀 칸 중 45%가 밝혀지면 그 구획을 셈. 지역 가운데에서 셀 반지름 밖 칸(둥근 숲의 네모 모서리)은 밝혀도 진행도에 안 셈. 밝힌 칸 배열은 판 데이터에 그대로 남음</summary>
    public sealed class FogMapService
    {
        /// <summary>안개 칸 한 변 (m, v1)</summary>
        public const float CellSize = 3f;

        /// <summary>걸을 때 밝히는 반지름 (m, v1)</summary>
        public const float RevealRadius = 24f;

        /// <summary>진행도 구획 한 변 (m, v1)</summary>
        public const float BlockSize = 60f;

        /// <summary>구획을 셀 밝힌 칸 비율 (v1)</summary>
        public const float BlockThreshold = 0.45f;

        /// <summary>칸마다 밝혔는지 (가로 Width × 세로 Height, 아래 줄부터)</summary>
        readonly bool[] _revealed;

        /// <summary>칸마다 진행도에 세는지 (지역 가운데에서 셀 반지름 안)</summary>
        readonly bool[] _counts;

        /// <summary>구획마다 밝힌 셀 칸 수</summary>
        readonly int[] _blockRevealed;

        /// <summary>구획마다 셀 칸 수</summary>
        readonly int[] _blockCells;

        /// <summary>구획마다 셌는지</summary>
        readonly bool[] _blockCounted;

        /// <summary>셀 칸이 하나라도 있는 구획 수 (진행도 분모)</summary>
        readonly int _blockTotal;

        /// <summary>센 구획 수</summary>
        int _counted;

        /// <summary>지역 (x = 월드 x 최소, y = 월드 z 최소, 너비·높이 m)</summary>
        public Rect Area { get; }

        /// <summary>가로 칸 수</summary>
        public int Width { get; }

        /// <summary>세로 칸 수</summary>
        public int Height { get; }

        /// <summary>진행도 (센 구획 / 셀 칸이 있는 구획, 0~1)</summary>
        public float Progress => _blockTotal > 0 ? (float)_counted / _blockTotal : 0f;

        /// <summary>밝힐 때마다 오름 (지도 그림을 다시 그릴지)</summary>
        public int Version { get; private set; }

        #region FogMapService - 지역·안개 판 데이터·셀 반지름을 받아 앞서 밝힌 칸(없거나 크기가 다르면 새로 만들어 판 데이터에 넣음)으로 구획 수를 셈. 셀 반지름이 0 이하면 칸을 다 셈
        /// <param name="area">지역 (x·y = 월드 x·z 최소)</param>
        /// <param name="data">안개 판 데이터</param>
        /// <param name="countRadius">진행도에 셀 반지름 (m, 지역 가운데에서. 0 이하면 다 셈)</param>
        public FogMapService(Rect area, ForestFogEntry data, float countRadius)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentException("지역 크기가 0", nameof(area));
            if (data == null) throw new ArgumentNullException(nameof(data));
            Area = area;
            Width = Mathf.CeilToInt(area.width / CellSize);
            Height = Mathf.CeilToInt(area.height / CellSize);
            // 앞서 밝힌 칸 (판 데이터)
            bool[] revealed = data.Revealed;
            _revealed = revealed != null && revealed.Length == Width * Height ? revealed : new bool[Width * Height];
            data.Revealed = _revealed;
            _counts = new bool[Width * Height];
            // 구획 가로·세로 수
            int blocksX = Mathf.CeilToInt(area.width / BlockSize);
            int blocksZ = Mathf.CeilToInt(area.height / BlockSize);
            _blockRevealed = new int[blocksX * blocksZ];
            _blockCells = new int[blocksX * blocksZ];
            _blockCounted = new bool[blocksX * blocksZ];
            // 셀 반지름 제곱 (0 이하면 끝없음)
            float countSq = countRadius > 0f ? countRadius * countRadius : float.MaxValue;
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                {
                    // 칸 번호
                    int index = z * Width + x;
                    // 지역 가운데에서 칸 가운데까지 (월드 x·z)
                    Vector2 fromCenter = new Vector2(area.xMin + (x + 0.5f) * CellSize, area.yMin + (z + 0.5f) * CellSize) - area.center;
                    if (fromCenter.sqrMagnitude > countSq) continue;
                    _counts[index] = true;
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockCells[block]++;
                    if (_revealed[index]) _blockRevealed[block]++;
                }
            for (int b = 0; b < _blockCounted.Length; b++)
            {
                if (_blockCells[b] > 0) _blockTotal++;
                Recount(b);
            }
        }
        #endregion

        #region Reveal - 위치에서 반지름 24m 안 칸을 밝힘 (숲 층이 아니면 안 밝힘: 던전 안). 새로 밝힌 칸이 있으면 Version을 올리고, 셀 칸이면 구획을 다시 셈
        /// <param name="position">월드 위치</param>
        public void Reveal(Vector3 position)
        {
            if (Mathf.Abs(position.y) > FieldService.LevelGap) return;
            // 위치 칸 좌표
            int cx = Mathf.FloorToInt((position.x - Area.xMin) / CellSize);
            int cz = Mathf.FloorToInt((position.z - Area.yMin) / CellSize);
            // 반지름 칸 수
            int reach = Mathf.CeilToInt(RevealRadius / CellSize);
            // 구획 가로 수
            int blocksX = Mathf.CeilToInt(Area.width / BlockSize);
            // 새로 밝힌 칸이 있는지
            bool changed = false;
            for (int z = Mathf.Max(0, cz - reach); z <= Mathf.Min(Height - 1, cz + reach); z++)
                for (int x = Mathf.Max(0, cx - reach); x <= Mathf.Min(Width - 1, cx + reach); x++)
                {
                    // 칸 번호
                    int index = z * Width + x;
                    if (_revealed[index]) continue;
                    // 칸 가운데 월드 x·z
                    float wx = Area.xMin + (x + 0.5f) * CellSize;
                    float wz = Area.yMin + (z + 0.5f) * CellSize;
                    if ((new Vector2(wx - position.x, wz - position.z)).sqrMagnitude > RevealRadius * RevealRadius) continue;
                    _revealed[index] = true;
                    changed = true;
                    if (!_counts[index]) continue;
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockRevealed[block]++;
                    Recount(block);
                }
            if (changed) Version++;
        }
        #endregion

        #region IsRevealed - 칸을 밝혔는지 (범위 밖은 false)
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        public bool IsRevealed(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Height && _revealed[z * Width + x];
        #endregion

        #region ToNormalized - 월드 위치를 지역 안 비율 (0~1, x·z)로
        /// <param name="world">월드 위치</param>
        public Vector2 ToNormalized(Vector3 world) => new((world.x - Area.xMin) / Area.width, (world.z - Area.yMin) / Area.height);
        #endregion

        #region BlockOf - 칸의 구획 번호
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        /// <param name="blocksX">구획 가로 수</param>
        static int BlockOf(int x, int z, int blocksX) =>
            Mathf.FloorToInt(z * CellSize / BlockSize) * blocksX + Mathf.FloorToInt(x * CellSize / BlockSize);
        #endregion

        #region Recount - 구획이 45%를 넘었으면 셈 (한 번 세면 안 풀림. 셀 칸이 없는 구획은 안 셈)
        /// <param name="block">구획 번호</param>
        void Recount(int block)
        {
            if (_blockCounted[block] || _blockCells[block] == 0) return;
            if (_blockRevealed[block] < _blockCells[block] * BlockThreshold) return;
            _blockCounted[block] = true;
            _counted++;
        }
        #endregion
    }
}
