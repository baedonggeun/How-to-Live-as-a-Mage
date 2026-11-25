#if UNITY_EDITOR
namespace Mage
{
    /// <summary>여관 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 잡은 방 놓기 (자기 여관 데이터만 씀)</summary>
    public sealed partial class InnService
    {
        #region DebugCancel - 잡은 방을 놓음 (방 문이 다시 잠김)
        public void DebugCancel() => _data.HasRoom = false;
        #endregion
    }
}
#endif
