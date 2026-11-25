using UnityEngine;

namespace Mage
{
    /// <summary>게임 시간 배율 규칙 (MonoBehaviour 아님. GameManager가 만들어 씀): 멈출 이유(로딩창·일시정지 메뉴)가 하나라도 있으면 0, 없으면 게임 속도. 바뀐 배율을 Time.timeScale에 넣는 건 GameManager가 함</summary>
    public sealed class TimeScaleService
    {
        /// <summary>로딩창이 떠 있는지</summary>
        bool _covered;

        /// <summary>일시정지 메뉴(메뉴 창·대화류 포함)가 떠 있는지</summary>
        bool _menuPaused;

        /// <summary>게임 속도 (기본 1, 숲 죽음 뒤 도망 시뮬만 올림)</summary>
        float _gameSpeed = 1f;

        /// <summary>지금 게임 시간 배율 (멈출 이유가 있으면 0, 없으면 게임 속도)</summary>
        public float TimeScale => _covered || _menuPaused ? 0f : _gameSpeed;

        #region SetCovered - 로딩창이 떴는지 넣음
        /// <param name="covered">로딩창이 떠 있는지</param>
        public void SetCovered(bool covered) => _covered = covered;
        #endregion

        #region SetMenuPaused - 일시정지 메뉴가 떴는지 넣음. 바뀌었으면 true (같으면 false, GameManager가 입력·커서를 건드리지 않게)
        /// <param name="paused">메뉴가 떠 있는지</param>
        public bool SetMenuPaused(bool paused)
        {
            if (paused == _menuPaused) return false;
            _menuPaused = paused;
            return true;
        }
        #endregion

        #region SetGameSpeed - 게임 속도를 넣음 (0 이상으로 자름)
        /// <param name="speed">게임 속도 (1 = 보통)</param>
        public void SetGameSpeed(float speed) => _gameSpeed = Mathf.Max(0f, speed);
        #endregion
    }
}
