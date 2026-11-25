namespace Mage
{
    /// <summary>원정 판 데이터 (RunEntry가 씬을 넘어 들고 다님). ExpeditionService만 만짐</summary>
    public sealed class ExpeditionEntry
    {
        /// <summary>원정 식량 (인분. 한 사람 하루치가 1인분)</summary>
        public int Rations { get; set; }

        /// <summary>정문으로 한 번이라도 나갔는지. 그 전(첫 의뢰)엔 경비병이 파티·의뢰·식량을 봄</summary>
        public bool HasDeparted { get; set; }
    }
}
