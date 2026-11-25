#if UNITY_EDITOR
using System.Collections.Generic;

namespace Mage
{
    /// <summary>흐름 상태 공통 디버그 진입점 기본값 (에디터 전용, 빌드에 안 들어감): 디버그 패널(DebugWindow)이 조작을 붙일 곳은 이 상태와 어느 화면에서나 쓰는 튜토리얼 담당, 조작을 막는 때는 없음. 캡슐을 가진 상태가 덮어씀 (기본 붙일 곳은 그대로 넘기고 캡슐을 더함)</summary>
    public abstract partial class FlowStateBase
    {
        /// <summary>지금 조작을 막을지 (기본: 안 막음)</summary>
        public virtual bool DebugBusy => false;

        #region DebugTargets - 조작을 붙일 곳 (기본: 이 상태, 튜토리얼 담당)
        public virtual IEnumerable<object> DebugTargets()
        {
            yield return this;
            // 어느 화면에서나 쓰는 튜토리얼 담당 (Boot 상주)
            TutorialManager tutorials = TutorialManager.Instance;
            if (tutorials != null) yield return tutorials;
        }
        #endregion
    }
}
#endif
