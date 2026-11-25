namespace Mage
{
    /// <summary>임무 일지 판 데이터 (RunEntry가 씬을 넘어 들고 다님). JournalService만 만짐</summary>
    public sealed class JournalEntry
    {
        /// <summary>지금 목표. 없으면 null. 판마다 새로</summary>
        public ObjectiveSO Current { get; set; }
    }
}
