using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 시계: 게임 시간만큼 판 데이터 시간(PlaySeconds)을 흐르게 하고 (구현 임시: 숲에서만), 받은 의뢰의 기간이 지나면 한 번 알림</summary>
    public sealed class FieldClockController : MonoBehaviour
    {
        /// <summary>이번 판 데이터 (없으면 안 함)</summary>
        RunEntry _run;

        /// <summary>이번 숲에서 기간 지남을 알렸는지</summary>
        bool _expired;

        /// <summary>받은 의뢰의 기간이 지남 (못 끝낸 의뢰가 있을 때 한 번)</summary>
        public event Action Expired;

        #region Init - 판 데이터를 받음
        /// <param name="run">이번 판 데이터</param>
        public void Init(RunEntry run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _expired = false;
        }
        #endregion

        #region Update - 멈췄으면 안 함. 시간을 더하고, 못 끝낸 의뢰의 기간이 지났으면 알림
        void Update()
        {
            if (_run == null) return;
            // 이번 프레임 게임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _run.PlaySeconds += dt;
            if (_expired || _run.Quest == null || _run.QuestCleared || _run.QuestDeadline <= 0f || _run.PlaySeconds < _run.QuestDeadline) return;
            _expired = true;
            Expired?.Invoke();
        }
        #endregion
    }
}
