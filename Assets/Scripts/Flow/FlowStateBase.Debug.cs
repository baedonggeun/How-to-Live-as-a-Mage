#if UNITY_EDITOR
using System.Collections.Generic;

namespace Mage
{
    /// <summary>흐름 상태 공통 확인 진입점 기본값 (에디터 전용, 빌드에 안 들어감): 확인 패널(CheckWindow)이 버튼·상태 줄을 붙일 곳은 이 상태만, 버튼을 막는 때는 없음. 캡슐을 가진 상태가 덮어씀</summary>
    public abstract partial class FlowStateBase
    {
        /// <summary>지금 확인 버튼을 막을지 (기본: 안 막음)</summary>
        public virtual bool CheckBusy => false;

        #region CheckTargets - 확인 버튼·상태 줄을 붙일 곳 (기본: 이 상태만)
        public virtual IEnumerable<object> CheckTargets()
        {
            yield return this;
        }
        #endregion
    }
}
#endif
