using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 적 판 데이터 (RunEntry가 씬을 넘어 들고 다님). ForestEnemyService만 만짐</summary>
    public sealed class ForestEntry
    {
        /// <summary>숲에서 쓰러뜨린 적: 적 자리 키({자리 id}.{번호}) → 쓰러진 곳. 다시 들어가도 그 자리에 시체로 남음</summary>
        public Dictionary<string, Vector3> Dead { get; } = new(StringComparer.Ordinal);
    }
}
