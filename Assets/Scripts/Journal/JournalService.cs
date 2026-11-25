using System;

namespace Mage
{
    /// <summary>임무 일지 캡슐 (MonoBehaviour 아님. 상태가 만들고 이음): 임무 일지 판 데이터를 받아 지금 목표를 알려 주고 바꿈. 어떤 목표인지는 상태가 다른 캡슐 상태로 정해 넘기고, "임무 일지 갱신" 토스트도 상태가 함</summary>
    public sealed class JournalService
    {
        /// <summary>임무 일지 판 데이터</summary>
        readonly JournalEntry _data;

        /// <summary>지금 목표 (없으면 null)</summary>
        public ObjectiveSO Current => _data.Current;

        #region JournalService - 임무 일지 판 데이터를 받음
        /// <param name="data">임무 일지 판 데이터</param>
        public JournalService(JournalEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region Set - 목표를 바꿈 (목표가 없어지면 null). 바뀌었으면 true
        /// <param name="next">새 목표 (없으면 null)</param>
        public bool Set(ObjectiveSO next)
        {
            if (next == _data.Current) return false;
            _data.Current = next;
            return true;
        }
        #endregion
    }
}
