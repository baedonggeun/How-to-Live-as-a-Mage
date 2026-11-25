using System;

namespace Mage
{
    /// <summary>의뢰 캡슐의 전초기지 쪽 (MonoBehaviour 아님. HubState가 만들고 이음): 의뢰 판 데이터를 받아 게시판 의뢰 받기, 받았는지·끝냈는지, 출발할 때 기한 정하기(처음 한 번), 숲에서 소탕을 끝낸 의뢰를 끝낸 의뢰로 남기기를 맡음</summary>
    public sealed class QuestService
    {
        /// <summary>의뢰 판 데이터</summary>
        readonly QuestEntry _data;

        /// <summary>받은 의뢰가 있는지</summary>
        public bool HasQuest => _data.Current != null;

        /// <summary>끝낸 의뢰가 하나라도 있는지</summary>
        public bool HasCompletedAny => _data.CompletedIds.Count > 0;

        /// <summary>받은 의뢰의 기간 (일, 식량 계산. 의뢰가 없으면 0)</summary>
        public int NeededDays => _data.Current != null ? _data.Current.Days : 0;

        #region QuestService - 의뢰 판 데이터를 받음
        /// <param name="data">의뢰 판 데이터</param>
        public QuestService(QuestEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region IsTaken - 이 의뢰를 받은 중인지
        /// <param name="quest">의뢰</param>
        public bool IsTaken(QuestSO quest) => quest != null && _data.Current == quest;
        #endregion

        #region IsCompleted - 이 의뢰를 끝냈는지
        /// <param name="quest">의뢰</param>
        public bool IsCompleted(QuestSO quest) => quest != null && _data.CompletedIds.Contains(quest.Id);
        #endregion

        #region Take - 의뢰를 받음 (목표 끝냄·기한을 비움)
        /// <param name="quest">받을 의뢰</param>
        public void Take(QuestSO quest)
        {
            _data.Current = quest != null ? quest : throw new ArgumentNullException(nameof(quest));
            _data.Cleared = false;
            _data.Deadline = 0f;
        }
        #endregion

        #region StartDeadline - 받은 의뢰가 있고 기한을 아직 안 셌으면 지금부터 의뢰 기간만큼 뒤로 기한을 정함 (처음 출발할 때)
        /// <param name="now">지금 판 시각 (초)</param>
        /// <param name="daySeconds">하루 길이 (초)</param>
        public void StartDeadline(float now, float daySeconds)
        {
            if (_data.Current == null || _data.Deadline > 0f) return;
            _data.Deadline = now + _data.Current.PeriodDays * daySeconds;
        }
        #endregion

        #region Finish - 받은 의뢰의 소탕을 끝냈으면 끝낸 의뢰로 남기고 비움. 끝낸 의뢰 (아니면 null)
        public QuestSO Finish()
        {
            if (_data.Current == null || !_data.Cleared) return null;
            // 끝낸 의뢰
            QuestSO quest = _data.Current;
            _data.CompletedIds.Add(quest.Id);
            _data.Current = null;
            _data.Cleared = false;
            _data.Deadline = 0f;
            return quest;
        }
        #endregion
    }
}
