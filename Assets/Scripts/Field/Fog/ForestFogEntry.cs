namespace Mage
{
    /// <summary>숲 안개 지도 데이터 (RunEntry가 씬을 넘어 들고 다님). FogMapService만 만짐</summary>
    public sealed class ForestFogEntry
    {
        /// <summary>밝힌 칸 (가로 × 세로, 아래 줄부터. 아직 안 들어갔으면 null)</summary>
        public bool[] Revealed { get; set; }
    }
}
