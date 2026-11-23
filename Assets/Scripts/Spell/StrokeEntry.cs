using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>획 하나의 점 (주문 그림 데이터). y는 위가 +, 단위는 주문 그림 좌표 (대략 -1.5~1.5)</summary>
    [Serializable]
    public sealed class StrokeEntry
    {
        /// <summary>획을 이루는 점 (그린 순서, 2개 이상)</summary>
        [SerializeField] List<Vector2> _points = new();

        /// <summary>획을 이루는 점</summary>
        public IReadOnlyList<Vector2> Points => _points;
    }
}
