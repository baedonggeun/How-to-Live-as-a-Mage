using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>영입 후보 추첨: 직업은 목록에서 무작위(중복 가능), 이름은 이름표에서 겹치지 않게. 지금은 후보 4명·레벨 1 고정 (임시)</summary>
    public static class RecruitPool
    {
        /// <summary>한 번에 뽑는 후보 수 (임시)</summary>
        public const int CandidateCount = 4;

        /// <summary>후보 레벨 (임시, 고정)</summary>
        public const int StartLevel = 1;

        /// <summary>이름표에 든 이름 수 (Name 테이블 name.mercenary.01 ~ 08)</summary>
        const int NameCount = 8;

        /// <summary>이름 키 형식 ({0} 두 자리 번호)</summary>
        const string NameKeyFormat = "name.mercenary.{0:00}";

        /// <summary>임시 이름표 테이블</summary>
        static readonly TableReference NameTable = "Name";

        #region Draw - 후보를 뽑음. 직업이 하나도 없으면 에러 로그 후 빈 목록
        /// <param name="jobs">뽑을 직업 (DataManager 목록, 빈 칸은 건너뜀)</param>
        public static List<MercenaryEntry> Draw(IReadOnlyList<JobSO> jobs)
        {
            // 뽑은 후보
            var result = new List<MercenaryEntry>(CandidateCount);
            // 빈 칸을 뺀 직업
            var validJobs = new List<JobSO>();
            if (jobs != null)
                foreach (JobSO job in jobs)
                    if (job != null) validJobs.Add(job);
            if (validJobs.Count == 0)
            {
                Debug.LogError("[Recruit] 직업 목록 비어 있음. 영입 후보 없음");
                return result;
            }

            // 이름 번호 (1 ~ NameCount). 앞에서부터 하나씩 섞어 뽑아 겹치지 않게
            var names = new List<int>(NameCount);
            for (int i = 1; i <= NameCount; i++) names.Add(i);
            // 뽑을 후보 수 (이름보다 많이 못 뽑음)
            int count = Mathf.Min(CandidateCount, NameCount);
            for (int i = 0; i < count; i++)
            {
                // 이번 이름 자리 (i 뒤에서 무작위로 골라 i와 바꿈)
                int pick = Random.Range(i, names.Count);
                (names[i], names[pick]) = (names[pick], names[i]);
                // 이번 후보 이름
                var name = new LocalizedString(NameTable, string.Format(CultureInfo.InvariantCulture, NameKeyFormat, names[i]));
                result.Add(new MercenaryEntry(name, validJobs[Random.Range(0, validJobs.Count)], StartLevel));
            }
            return result;
        }
        #endregion
    }
}
