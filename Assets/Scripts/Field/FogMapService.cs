using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 안개 지도 (v1, MonoBehaviour 아님): 지역을 3m 칸으로 나눠 걸으면 반경 24m를 밝힘. 진행도는 60m 구획 단위로, 구획 칸의 45%가 밝혀지면 그 구획을 셈. 밝힌 칸 배열은 판 데이터에 그대로 남음</summary>
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

        /// <summary>구획마다 밝힌 칸 수</summary>
        readonly int[] _blockRevealed;

        /// <summary>구획마다 전체 칸 수</summary>
        readonly int[] _blockCells;

        /// <summary>구획마다 셌는지</summary>
        readonly bool[] _blockCounted;

        /// <summary>센 구획 수</summary>
        int _counted;

        /// <summary>지역 (x = 월드 x 최소, y = 월드 z 최소, 너비·높이 m)</summary>
        public Rect Area { get; }

        /// <summary>가로 칸 수</summary>
        public int Width { get; }

        /// <summary>세로 칸 수</summary>
        public int Height { get; }

        /// <summary>밝힌 칸 배열 (판 데이터에 넣음)</summary>
        public bool[] Revealed => _revealed;

        /// <summary>진행도 (센 구획 / 전체 구획, 0~1)</summary>
        public float Progress => _blockCounted.Length > 0 ? (float)_counted / _blockCounted.Length : 0f;

        /// <summary>밝힐 때마다 오름 (지도 그림을 다시 그릴지)</summary>
        public int Version { get; private set; }

        #region FogMapService - 지역과 앞서 밝힌 칸(없거나 크기가 다르면 새로)을 받아 구획 수를 셈
        /// <param name="area">지역 (x·y = 월드 x·z 최소)</param>
        /// <param name="revealed">앞서 밝힌 칸 (판 데이터, 없으면 null)</param>
        public FogMapService(Rect area, bool[] revealed)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentException("지역 크기가 0", nameof(area));
            Area = area;
            Width = Mathf.CeilToInt(area.width / CellSize);
            Height = Mathf.CeilToInt(area.height / CellSize);
            _revealed = revealed != null && revealed.Length == Width * Height ? revealed : new bool[Width * Height];
            // 구획 가로·세로 수
            int blocksX = Mathf.CeilToInt(area.width / BlockSize);
            int blocksZ = Mathf.CeilToInt(area.height / BlockSize);
            _blockRevealed = new int[blocksX * blocksZ];
            _blockCells = new int[blocksX * blocksZ];
            _blockCounted = new bool[blocksX * blocksZ];
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                {
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockCells[block]++;
                    if (_revealed[z * Width + x]) _blockRevealed[block]++;
                }
            for (int b = 0; b < _blockCounted.Length; b++) Recount(b);
        }
        #endregion

        #region Reveal - 위치에서 반지름 24m 안 칸을 밝힘 (숲 층이 아니면 안 밝힘: 던전 안). 새로 밝힌 칸이 있으면 구획을 다시 세고 Version을 올림
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
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockRevealed[block]++;
                    Recount(block);
                    changed = true;
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

        #region Recount - 구획이 45%를 넘었으면 셈 (한 번 세면 안 풀림)
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
