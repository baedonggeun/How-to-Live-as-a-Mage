#if UNITY_EDITOR
using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋 캡슐 확인 진입점 (에디터 전용, 빌드에 안 들어감): 리셋 상태 줄, 리셋에 필요한 쓰러뜨린 수 (MarkLeaving과 같은 비율)</summary>
    public sealed partial class ForestResetService
    {
        #region CheckStatus - 이번 진입에 연출 중이면 연출 중, 아니면 다음 진입에 리셋하는지
        [CheckStatus("숲 리셋")]
        string CheckStatus() => IsResetting ? "연출 중" : _data.Pending ? "다음 진입에 함" : "안 함";
        #endregion

        #region CheckNeed - 숲 적 자리 수 합에서 다음 진입에 리셋하려면 쓰러뜨려야 하는 수 (리셋 비율, 올림)
        /// <param name="total">숲 적 자리 수 합</param>
        public static int CheckNeed(int total) => Mathf.CeilToInt(total * ResetRatio);
        #endregion
    }
}
#endif
