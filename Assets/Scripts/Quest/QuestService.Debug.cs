#if UNITY_EDITOR
namespace Mage
{
    /// <summary>의뢰 캡슐(전초기지 쪽) 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 소탕을 끝냈는지 읽고 바꾸기, 받은 의뢰 비우기 (목표 갱신은 전초기지 상태 디버그 진입점이 함. 자기 의뢰 데이터만 씀)</summary>
    public sealed partial class QuestService
    {
        /// <summary>받은 의뢰의 소탕을 끝냈는지 (의뢰가 없으면 false)</summary>
        public bool DebugCleared => _data.Current != null && _data.Cleared;

        #region DebugSetCleared - 받은 의뢰의 소탕을 끝냄·되돌림 (의뢰가 없으면 안 함)
        /// <param name="cleared">끝냈는지</param>
        public void DebugSetCleared(bool cleared)
        {
            if (_data.Current != null) _data.Cleared = cleared;
        }
        #endregion

        #region DebugDrop - 받은 의뢰를 비움 (끝낸 의뢰로 안 남김. 끝냄·기한도 비움)
        public void DebugDrop()
        {
            _data.Current = null;
            _data.Cleared = false;
            _data.Deadline = 0f;
        }
        #endregion
    }
}
#endif
