using UnityEngine;

namespace Mage
{
    /// <summary>임무 일지 탭에 보일 것 (상태가 탭이 열릴 때마다 만들어 넘김): 지금 목표·받은 의뢰·의뢰 줄 날 수</summary>
    public readonly struct JournalSpec
    {
        /// <summary>지금 목표 (없으면 null)</summary>
        public ObjectiveSO Objective { get; }

        /// <summary>받은 의뢰 (없으면 null)</summary>
        public QuestSO Quest { get; }

        /// <summary>의뢰 기간을 세는 중인지 (처음 출발 뒤)</summary>
        public bool Counting { get; }

        /// <summary>의뢰 줄에 보일 날 수 (세는 중이면 남은 날 올림, 아니면 의뢰 기간. 의뢰가 없으면 0)</summary>
        public int Days { get; }

        #region JournalSpec - 목표·의뢰·기한·지금 판 시각으로 만듦 (기한이 0이면 아직 안 셈)
        /// <param name="objective">지금 목표 (없으면 null)</param>
        /// <param name="quest">받은 의뢰 (없으면 null)</param>
        /// <param name="deadline">의뢰 기한 (판 시각 초, 아직 안 셌으면 0)</param>
        /// <param name="now">지금 판 시각 (초)</param>
        public JournalSpec(ObjectiveSO objective, QuestSO quest, float deadline, float now)
        {
            Objective = objective;
            Quest = quest;
            Counting = quest != null && deadline > 0f;
            if (quest == null) Days = 0;
            else Days = Counting ? Mathf.Max(0, Mathf.CeilToInt((deadline - now) / ClockEntry.DaySeconds)) : quest.PeriodDays;
        }
        #endregion
    }
}
