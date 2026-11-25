using System;
using UnityEngine;

namespace Mage
{
    /// <summary>정문에서 원정을 막는 이유 (첫 출발 전에만)</summary>
    public enum ExpeditionBlock
    {
        /// <summary>막는 것 없음 (나갈 수 있음)</summary>
        None,

        /// <summary>용병이 모자람</summary>
        Party,

        /// <summary>받은 의뢰가 없음</summary>
        Quest,

        /// <summary>식량이 모자람</summary>
        Food,
    }

    /// <summary>원정 캡슐 (MonoBehaviour 아님. HubState가 만들고 이음): 원정 판 데이터를 받아 식량(여관에서 산 만큼 더함)·첫 출발 전 검사(용병·의뢰·식량)·출발(필요한 식량을 빼고 출발 표시)을 맡음. 대화·화면 전환은 상태가 함</summary>
    public sealed class ExpeditionService
    {
        /// <summary>원정 판 데이터</summary>
        readonly ExpeditionEntry _data;

        /// <summary>원정 식량 (인분)</summary>
        public int Rations => _data.Rations;

        /// <summary>정문으로 한 번이라도 나갔는지</summary>
        public bool HasDeparted => _data.HasDeparted;

        #region ExpeditionService - 원정 판 데이터를 받음
        /// <param name="data">원정 판 데이터</param>
        public ExpeditionService(ExpeditionEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region AddRations - 식량을 더함 (0 이하면 무시)
        /// <param name="count">더할 인분</param>
        public void AddRations(int count)
        {
            if (count > 0) _data.Rations += count;
        }
        #endregion

        #region Check - 첫 출발 전이면 용병·의뢰·식량 순서로 모자란 것을 알려 줌. 출발한 적이 있거나 다 됐으면 None
        /// <param name="members">파티 용병 수</param>
        /// <param name="required">필요한 용병 수</param>
        /// <param name="hasQuest">받은 의뢰가 있는지</param>
        /// <param name="neededRations">필요한 식량 (인분)</param>
        public ExpeditionBlock Check(int members, int required, bool hasQuest, int neededRations)
        {
            if (_data.HasDeparted) return ExpeditionBlock.None;
            if (members < required) return ExpeditionBlock.Party;
            if (!hasQuest) return ExpeditionBlock.Quest;
            return _data.Rations < neededRations ? ExpeditionBlock.Food : ExpeditionBlock.None;
        }
        #endregion

        #region Depart - 출발: 필요한 식량을 뺌 (모자라면 있는 만큼), 출발 표시
        /// <param name="neededRations">필요한 식량 (인분)</param>
        public void Depart(int neededRations)
        {
            _data.Rations -= Mathf.Min(_data.Rations, Mathf.Max(0, neededRations));
            _data.HasDeparted = true;
        }
        #endregion
    }
}
