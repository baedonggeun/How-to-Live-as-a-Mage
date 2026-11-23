using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mage
{
    /// <summary>주문 그림의 부분 하나: 어느 룬의 그림인지와 그 획들</summary>
    [Serializable]
    public sealed class SpellPartEntry
    {
        /// <summary>이 부분의 룬</summary>
        [SerializeField, FormerlySerializedAs("_element")] RuneSO _rune;

        /// <summary>이 부분의 획 (1개 이상)</summary>
        [SerializeField] List<StrokeEntry> _strokes = new();

        /// <summary>이 부분의 룬</summary>
        public RuneSO Rune => _rune;

        /// <summary>이 부분의 획</summary>
        public IReadOnlyList<StrokeEntry> Strokes => _strokes;
    }
}
