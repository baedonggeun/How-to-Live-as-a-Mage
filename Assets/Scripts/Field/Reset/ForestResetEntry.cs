namespace Mage
{
    /// <summary>숲 리셋 판 데이터 (RunEntry가 씬을 넘어 들고 다님). ForestResetService만 만짐</summary>
    public sealed class ForestResetEntry
    {
        /// <summary>다음에 숲에 들어갈 때 리셋할지 (숲을 나갈 때 쓰러뜨린 적이 70% 이상이면 켬)</summary>
        public bool Pending { get; set; }
    }
}
