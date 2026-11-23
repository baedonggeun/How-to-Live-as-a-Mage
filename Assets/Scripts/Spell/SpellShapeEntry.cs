using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>인식기에 넘기는 주문 그림 (SO와 떨어진 순수 데이터): id, 부분마다 룬 id와 획 점. y는 위가 +</summary>
    public sealed class SpellShapeEntry
    {
        /// <summary>주문 id</summary>
        public string Id { get; }

        /// <summary>부분마다 룬 id (부분 순서)</summary>
        public IReadOnlyList<string> PartRuneIds { get; }

        /// <summary>부분마다 획 → 점 (부분 순서)</summary>
        public IReadOnlyList<Vector2[][]> PartStrokes { get; }

        #region SpellShapeEntry - 부분 수가 맞는지 검사하고 만듦
        /// <param name="id">주문 id</param>
        /// <param name="partRuneIds">부분마다 룬 id</param>
        /// <param name="partStrokes">부분마다 획 → 점</param>
        public SpellShapeEntry(string id, IReadOnlyList<string> partRuneIds, IReadOnlyList<Vector2[][]> partStrokes)
        {
            if (partRuneIds == null) throw new ArgumentNullException(nameof(partRuneIds));
            if (partStrokes == null) throw new ArgumentNullException(nameof(partStrokes));
            if (partRuneIds.Count != partStrokes.Count) throw new ArgumentException("룬 id 수와 부분 수가 다름", nameof(partStrokes));
            Id = id ?? string.Empty;
            PartRuneIds = partRuneIds;
            PartStrokes = partStrokes;
        }
        #endregion
    }
}
