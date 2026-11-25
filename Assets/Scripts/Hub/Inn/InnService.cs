using System;

namespace Mage
{
    /// <summary>여관 캡슐 (MonoBehaviour 아님. HubState가 만들고 이음): 여관 판 데이터를 받아 방을 잡았는지·방 잡기(한 번)·식량 하루치 값을 맡음. 금화를 내는 일과 대화는 상태가 함</summary>
    public sealed class InnService
    {
        /// <summary>여관 방값 (금화, 처음 방 잡을 때 한 번. 임시)</summary>
        public const int RoomPrice = 70;

        /// <summary>원정 식량 1인분 값 (금화, 한 사람 하루치. 임시)</summary>
        public const int RationPrice = 3;

        /// <summary>여관 판 데이터</summary>
        readonly InnEntry _data;

        /// <summary>여관 방을 잡았는지 (방 문이 열리고, 숲에서 죽으면 방 침대에서 깨어남)</summary>
        public bool HasRoom => _data.HasRoom;

        #region InnService - 여관 판 데이터를 받음
        /// <param name="data">여관 판 데이터</param>
        public InnService(InnEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region Rent - 방을 잡음 (이미 잡았으면 그대로)
        public void Rent() => _data.HasRoom = true;
        #endregion

        #region FoodPrice - 그 인원의 하루치 식량 값 (금화)
        /// <param name="partySize">원정 인원 (플레이어 포함)</param>
        public static int FoodPrice(int partySize) => RationPrice * Math.Max(0, partySize);
        #endregion
    }
}
