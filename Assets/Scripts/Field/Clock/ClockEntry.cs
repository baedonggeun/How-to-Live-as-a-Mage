using UnityEngine;

namespace Mage
{
    /// <summary>판 시계 데이터 (RunEntry가 씬을 넘어 들고 다님). FieldClockController만 흘림</summary>
    public sealed class ClockEntry
    {
        /// <summary>하루 길이 (게임 초, 기획: 하루 10분)</summary>
        public const float DaySeconds = 600f;

        /// <summary>판에서 흐른 게임 시간 (초). 구현 임시: 숲에서만 흐름</summary>
        public float PlaySeconds { get; set; }

        /// <summary>지금 날짜 (1일부터)</summary>
        public int Day => 1 + Mathf.FloorToInt(PlaySeconds / DaySeconds);
    }
}
