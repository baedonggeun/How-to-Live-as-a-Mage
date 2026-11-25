#if UNITY_EDITOR
using System;

namespace Mage
{
    /// <summary>원정 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 식량 넣기, 첫 출발 표시 바꾸기 (자기 원정 데이터만 씀)</summary>
    public sealed partial class ExpeditionService
    {
        #region DebugSetRations - 식량을 그 인분으로 (0 이상)
        /// <param name="count">식량 (인분)</param>
        public void DebugSetRations(int count) => _data.Rations = Math.Max(0, count);
        #endregion

        #region DebugSetDeparted - 첫 출발을 했는지 바꿈 (했으면 경비병이 모자란 걸 안 따짐)
        /// <param name="departed">첫 출발을 했는지</param>
        public void DebugSetDeparted(bool departed) => _data.HasDeparted = departed;
        #endregion
    }
}
#endif
