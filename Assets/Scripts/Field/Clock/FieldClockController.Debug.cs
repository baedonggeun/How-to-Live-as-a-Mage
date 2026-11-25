#if UNITY_EDITOR
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 시계 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 판 시계 값 줄, 날짜 넣기, 시계만 빨리 흐르게 하는 배속 (적·용병 속도는 그대로), 다음 날 직전으로 옮기기 (자기 시계 데이터만 씀)</summary>
    public sealed partial class FieldClockController
    {
        /// <summary>다음 날까지 남길 판 시간 (초)</summary>
        const float DebugDayLead = 3f;

        /// <summary>시계 배속 최대</summary>
        const int DebugSpeedMax = 600;

        /// <summary>시계 배속 (1 = 보통. Update가 판 시간에 곱함, 숲을 나가면 1로)</summary>
        int _debugSpeed = 1;

        #region DebugStatus - 판 시각·날짜 (시계를 받기 전엔 -)
        [DebugControl("시계", "시계")]
        string DebugStatus => _data == null ? "-" : string.Format(CultureInfo.InvariantCulture, "{0:F0}초 · {1}일째", _data.PlaySeconds, _data.Day);
        #endregion

        #region DebugDay - 날짜 (넣으면 그날 아침으로. 1 이상)
        [DebugControl("시계", "날짜")]
        int DebugDay
        {
            get => _data != null ? _data.Day : 1;
            set
            {
                if (_data == null) return;
                _data.PlaySeconds = (Mathf.Max(1, value) - 1) * ClockEntry.DaySeconds;
            }
        }
        #endregion

        #region DebugSpeed - 시계 배속 (1~600. 하루 10분이 60배면 10초)
        [DebugControl("시계", "시계 배속")]
        int DebugSpeed
        {
            get => _debugSpeed;
            set => _debugSpeed = Mathf.Clamp(value, 1, DebugSpeedMax);
        }
        #endregion

        #region DebugNextDaySoon - 판 시각을 지금 날 끝 3초 전으로 (이미 그 안이면 그대로)
        [DebugControl("시계", "다음 날 직전으로")]
        string DebugNextDaySoon()
        {
            if (_data == null) return "시계를 아직 안 받음";
            // 지금 날 끝 3초 전 판 시각
            float at = _data.Day * ClockEntry.DaySeconds - DebugDayLead;
            if (_data.PlaySeconds < at) _data.PlaySeconds = at;
            return string.Format(CultureInfo.InvariantCulture, "{0}일째 끝 3초 전으로 (3초 뒤 {1}일째)", _data.Day, _data.Day + 1);
        }
        #endregion
    }
}
#endif
