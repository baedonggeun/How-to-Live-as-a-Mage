using System.Collections.Generic;

namespace Mage
{
    /// <summary>용병 파티 판 데이터 (RunEntry가 씬을 넘어 들고 다님). 파티 캡슐(숲: FieldPartyService, 전초기지: 영입)만 만짐</summary>
    public sealed class PartyEntry
    {
        /// <summary>파티에 든 용병들</summary>
        public List<MercenaryEntry> Members { get; } = new();

        /// <summary>길드 영입 후보 (처음 영입 창을 열 때 뽑음. 주마다 새로 뽑는 건 나중)</summary>
        public List<MercenaryEntry> Candidates { get; } = new();

        /// <summary>영입 후보를 뽑았는지</summary>
        public bool CandidatesDrawn { get; set; }
    }
}
