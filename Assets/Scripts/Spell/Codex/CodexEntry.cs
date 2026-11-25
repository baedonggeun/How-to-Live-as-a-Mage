using System.Collections.Generic;

namespace Mage
{
    /// <summary>코덱스 판 데이터 (RunEntry가 씬을 넘어 들고 다님). CodexService만 만짐 (디버그 판은 RunEntry.CreateDebug가 처음에 채움)</summary>
    public sealed class CodexEntry
    {
        /// <summary>익힌 룬 (캐릭터 생성에서 고름). 주문 판정·코덱스 룬 주문 장이 씀</summary>
        public List<RuneSO> LearnedRunes { get; } = new();

        /// <summary>코덱스에 등록한 다중 룬 마법 (생략 없이 처음 완성. 등록 순서). 생략은 등록한 마법에만</summary>
        public List<SpellSO> CollectedSpells { get; } = new();

        /// <summary>생략 가능 수 (레벨이 생기면 레벨이 정함. 지금은 프로토타입 기본 0)</summary>
        public int OmitLimit { get; set; }
    }
}
