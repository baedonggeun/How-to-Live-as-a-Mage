using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>글 목록 잇기 (static pure function, 지금 언어로 읽음). 캡슐이 UIManager 없이 목록 글을 만들 때 씀</summary>
    public static class TextListCalculator
    {
        /// <summary>목록 이름 사이 구분자 키 (UI 테이블, 끝 공백 포함)</summary>
        const string ListSeparatorKey = "common.list_separator";

        /// <summary>공용 문구 테이블</summary>
        static readonly TableReference UiTable = "UI";

        #region Join - 지금 언어의 목록 구분자(UI/common.list_separator)로 글을 이음. 빈 글은 뺌 (null이면 빈 글)
        /// <param name="items">이을 글 (이미 지금 언어로 읽은 것)</param>
        public static string Join(IEnumerable<string> items)
        {
            if (items == null) return string.Empty;
            // 지금 언어 목록 구분자
            string separator = new LocalizedString(UiTable, ListSeparatorKey).GetLocalizedString();
            // 빈 글을 뺀 목록
            var kept = new List<string>();
            foreach (string item in items)
                if (!string.IsNullOrEmpty(item)) kept.Add(item);
            return string.Join(separator, kept);
        }
        #endregion
    }
}
