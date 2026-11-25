#if UNITY_EDITOR
using System.Globalization;

namespace Mage
{
    /// <summary>의뢰 캡슐(숲 쪽) 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 의뢰 값 줄(남은 소탕 대상), 기한 바꾸기, 소탕 끝냄 켜고 끄기 (판 시각·목표 갱신은 숲 상태 디버그 진입점이 함. 자기 의뢰 데이터만 씀)</summary>
    public sealed partial class FieldQuestService
    {
        #region DebugStatus - 받은 의뢰가 없으면 없음, 끝냈으면 목표 끝냄, 아니면 의뢰 이름·남은 소탕 대상 수
        [DebugControl("의뢰", "의뢰 상태")]
        string DebugStatus
        {
            get
            {
                if (_data.Current == null) return "없음";
                if (_data.Cleared) return string.Format(CultureInfo.InvariantCulture, "{0} · 목표 끝냄 (숲 입구로)", _data.Current.name);
                return string.Format(CultureInfo.InvariantCulture, "{0} · 남은 소탕 대상 {1}", _data.Current.name, _targets.Count);
            }
        }
        #endregion

        #region DebugSetDeadline - 진행 중인 의뢰의 기한을 그 판 시각으로 (기한 지남 검사는 Tick 그대로). 진행 중인 의뢰가 없으면 false
        /// <param name="at">새 기한 (판 시각, 초)</param>
        public bool DebugSetDeadline(float at)
        {
            if (!IsActive) return false;
            _data.Deadline = at;
            return true;
        }
        #endregion

        #region DebugSetCleared - 받은 의뢰의 소탕을 끝냄·되돌림 (의뢰가 없으면 안 함)
        /// <param name="cleared">끝냈는지</param>
        public void DebugSetCleared(bool cleared)
        {
            if (_data.Current != null) _data.Cleared = cleared;
        }
        #endregion
    }
}
#endif
