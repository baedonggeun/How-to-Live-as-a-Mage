using System;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>대화 선택지 하나: 버튼 글자(테이블·키·자리 값)와 누르면 할 일</summary>
    public sealed class DialogueChoiceEntry
    {
        /// <summary>버튼 글자가 든 String Table</summary>
        public TableReference Table { get; }

        /// <summary>버튼 글자 키</summary>
        public string Key { get; }

        /// <summary>버튼 글자의 {0}, {1}… 자리에 넣을 값 (없으면 빈 배열)</summary>
        public object[] Args { get; }

        /// <summary>누르면 할 일 (다음 대사를 띄우거나 대화를 닫는 것까지 여기서 정함)</summary>
        public Action OnPick { get; }

        #region DialogueChoiceEntry - 키·할 일 검사하고 만듦
        /// <param name="table">버튼 글자가 든 String Table</param>
        /// <param name="key">버튼 글자 키</param>
        /// <param name="onPick">누르면 할 일</param>
        /// <param name="args">버튼 글자 자리 값</param>
        public DialogueChoiceEntry(TableReference table, string key, Action onPick, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("빈 선택지 키", nameof(key));
            Table = table;
            Key = key;
            OnPick = onPick ?? throw new ArgumentNullException(nameof(onPick));
            Args = args ?? Array.Empty<object>();
        }
        #endregion
    }
}
