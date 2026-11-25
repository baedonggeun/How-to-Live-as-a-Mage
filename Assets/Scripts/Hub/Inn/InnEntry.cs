namespace Mage
{
    /// <summary>여관 판 데이터 (RunEntry가 씬을 넘어 들고 다님). InnService만 만짐</summary>
    public sealed class InnEntry
    {
        /// <summary>여관 방을 잡았는지 (방값은 처음 한 번)</summary>
        public bool HasRoom { get; set; }
    }
}
