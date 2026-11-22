using System;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>용병 한 명 (영입 후보·파티원): 이름(임시 이름표), 직업, 레벨. 전투 수치는 직업이 정함 (임시)</summary>
    public sealed class MercenaryEntry
    {
        /// <summary>이름 (Name 테이블)</summary>
        public LocalizedString Name { get; }

        /// <summary>직업</summary>
        public JobSO Job { get; }

        /// <summary>레벨 (1 이상)</summary>
        public int Level { get; }

        /// <summary>전투 수치 (1~5, 직업 값). 숲 단계에서 동료 체력·공격력 계산에 씀</summary>
        public int Combat => Job.Combat;

        #region MercenaryEntry - 이름·직업·레벨을 검사하고 만듦
        /// <param name="name">이름</param>
        /// <param name="job">직업</param>
        /// <param name="level">레벨 (1 이상)</param>
        public MercenaryEntry(LocalizedString name, JobSO job, int level)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "레벨은 1 이상");
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Job = job;
            Level = level;
        }
        #endregion
    }
}
