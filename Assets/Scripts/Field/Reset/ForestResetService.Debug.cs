#if UNITY_EDITOR
using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 리셋 값 줄, 리셋에 필요한 쓰러뜨린 수 (MarkLeaving과 같은 비율)</summary>
    public sealed partial class ForestResetService
    {
        #region DebugStatus - 이번 진입에 연출 중이면 연출 중, 아니면 다음 진입에 리셋하는지
        [DebugControl("숲", "숲 리셋")]
        string DebugStatus => IsResetting ? "연출 중" : _data.Pending ? "다음 진입에 함" : "안 함";
        #endregion

        #region DebugNeed - 숲 적 자리 수 합에서 다음 진입에 리셋하려면 쓰러뜨려야 하는 수 (리셋 비율, 올림)
        /// <param name="total">숲 적 자리 수 합</param>
        public static int DebugNeed(int total) => Mathf.CeilToInt(total * ResetRatio);
        #endregion
    }
}
#endif
