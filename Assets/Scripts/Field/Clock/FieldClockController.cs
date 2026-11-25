using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 시계 캡슐: 시계 판 데이터를 받아 게임 시간만큼 판 시간을 흐르게 하고 (구현 임시: 숲에서만, 멈췄으면 안 흐름) 매 프레임 지금 판 시각을 알림. 의뢰 기한 검사는 의뢰 캡슐이 함</summary>
    public sealed partial class FieldClockController : MonoBehaviour
    {
        /// <summary>시계 판 데이터 (없으면 안 함)</summary>
        ClockEntry _data;

        /// <summary>판 시간이 흐름 (지금 판 시각, 초)</summary>
        public event Action<float> Ticked;

        /// <summary>지금 날짜 (1일부터. 받기 전엔 1)</summary>
        public int Day => _data != null ? _data.Day : 1;

        #region Init - 시계 판 데이터를 받음
        /// <param name="data">시계 판 데이터</param>
        public void Init(ClockEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region Update - 멈췄으면 안 함. 시간을 더하고 지금 판 시각을 알림
        void Update()
        {
            if (_data == null) return;
            // 이번 프레임 게임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
#if UNITY_EDITOR
            // 디버그 패널 시계 배속 (에디터 전용)
            dt *= _debugSpeed;
#endif
            _data.PlaySeconds += dt;
            Ticked?.Invoke(_data.PlaySeconds);
        }
        #endregion
    }
}
