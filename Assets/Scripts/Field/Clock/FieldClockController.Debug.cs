#if UNITY_EDITOR
using System.Globalization;

namespace Mage
{
    /// <summary>숲 시계 캡슐 확인 진입점 (에디터 전용, 빌드에 안 들어감): 판 시계 상태 줄, 다음 날 직전으로 옮기기 (자기 시계 데이터만 씀)</summary>
    public sealed partial class FieldClockController
    {
        /// <summary>다음 날까지 남길 판 시간 (초)</summary>
        const float CheckDayLead = 3f;

        #region CheckStatus - 판 시각·날짜 (시계를 받기 전엔 -)
        [CheckStatus("시계")]
        string CheckStatus() => _data == null ? "-" : string.Format(CultureInfo.InvariantCulture, "{0:F0}초 · {1}일째", _data.PlaySeconds, _data.Day);
        #endregion

        #region CheckNextDaySoon - 판 시각을 지금 날 끝 3초 전으로 (이미 그 안이면 그대로) (1-2)
        [CheckButton("forest-dungeon", "1-2", "다음 날 직전으로")]
        string CheckNextDaySoon()
        {
            if (_data == null) return "시계를 아직 안 받음";
            // 지금 날 끝 3초 전 판 시각
            float at = _data.Day * ClockEntry.DaySeconds - CheckDayLead;
            if (_data.PlaySeconds < at) _data.PlaySeconds = at;
            return string.Format(CultureInfo.InvariantCulture, "{0}일째 끝 3초 전으로 (3초 뒤 {1}일째)", _data.Day, _data.Day + 1);
        }
        #endregion
    }
}
#endif
