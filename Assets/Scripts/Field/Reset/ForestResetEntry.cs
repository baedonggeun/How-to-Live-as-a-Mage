using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋·차원문 판 데이터 (RunEntry가 씬을 넘어 들고 다님). ForestResetService만 만짐</summary>
    public sealed class ForestResetEntry
    {
        /// <summary>다음에 숲에 들어갈 때 리셋할지 (숲을 나갈 때 쓰러뜨린 적이 70% 이상이면 켬)</summary>
        public bool Pending { get; set; }

        /// <summary>차원문 위치 (그 판에서 처음 숲에 들어갈 때 3구역 먼 쪽 절반에서 정함). 안 정했으면 null</summary>
        public Vector3? PortalPosition { get; set; }
    }
}
