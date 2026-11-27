using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>차원문 판 데이터 (RunEntry가 씬을 넘어 들고 다님). PortalService만 만짐</summary>
    public sealed class PortalEntry
    {
        /// <summary>발견한 차원문: 자리 id → 자리. 한 번 남기면 그 판 동안 안 바뀜 (안 남긴 차원문은 숲에 들어올 때마다 다시 뽑음)</summary>
        public Dictionary<string, Vector3> Found { get; } = new(StringComparer.Ordinal);
    }
}
