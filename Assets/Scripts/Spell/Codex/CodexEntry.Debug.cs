#if UNITY_EDITOR
using System.Collections.Generic;

namespace Mage
{
    /// <summary>코덱스 판 데이터 디버그 칸 (에디터 전용, 빌드에 안 들어감): 디버그 패널로 켜기 전 목록 (끄면 이걸로 되돌림. 판 데이터에 있어서 화면이 바뀌어도 남음). CodexService 디버그 진입점만 만짐</summary>
    public sealed partial class CodexEntry
    {
        /// <summary>'룬 전부 익힘'을 켜기 전 익힌 룬 (안 켰으면 null)</summary>
        public List<RuneSO> DebugRunesBefore { get; set; }

        /// <summary>'다중 룬 마법 전부 등록'을 켜기 전 등록한 마법 (안 켰으면 null)</summary>
        public List<SpellSO> DebugSpellsBefore { get; set; }
    }
}
#endif
