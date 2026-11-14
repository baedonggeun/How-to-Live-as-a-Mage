using System;

namespace Mage
{
    /// <summary>파티 용병 한 명. 지금은 전투 수치만 (이름 방식은 4단계 영입 설계 때 정함)</summary>
    public sealed class MercenaryEntry
    {
        /// <summary>전투 수치 최솟값</summary>
        public const int MinCombat = 1;

        /// <summary>전투 수치 최댓값</summary>
        public const int MaxCombat = 5;

        /// <summary>전투 수치 (1~5). 3단계에서 동료 체력·공격력 계산에 씀</summary>
        public int Combat { get; }

        #region MercenaryEntry - 전투 수치 범위를 검사하고 만듦
        /// <param name="combat">전투 수치 (1~5)</param>
        public MercenaryEntry(int combat)
        {
            if (combat < MinCombat || combat > MaxCombat)
                throw new ArgumentOutOfRangeException(nameof(combat), combat, $"전투는 {MinCombat}~{MaxCombat}");
            Combat = combat;
        }
        #endregion
    }
}
