using UnityEngine;

namespace Mage
{
    /// <summary>스태미나 막대 ← 1인칭 조작 스태미나 (출처는 상태가 Bind로 넘김). 줄어 있으면 나타나고 가득 차면 사라짐</summary>
    public sealed class StaminaUIBinder : MonoBehaviour
    {
        /// <summary>나타나고 사라지는 빠르기 (초당 투명도)</summary>
        const float FadeSpeed = 4f;

        /// <summary>가득 찼다고 볼 비율</summary>
        const float FullRatio = 0.999f;

        /// <summary>같은 막대의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] StaminaBarView _view;

        /// <summary>1인칭 조작 (없으면 숨김)</summary>
        PlayerController _player;

        #region Awake - 연결 확인, 처음엔 숨김
        void Awake()
        {
            if (_view == null) Debug.LogError("[Stamina] StaminaBarView 연결 안 됨", this);
            else _view.Group.alpha = 0f;
        }
        #endregion

        #region Bind - 1인칭 조작을 받음 (null이면 놓음)
        /// <param name="player">1인칭 조작</param>
        public void Bind(PlayerController player) => _player = player;
        #endregion

        #region Update - 스태미나 비율을 채우고, 줄어 있으면 나타나게·가득이면 사라지게 (실제 시간)
        void Update()
        {
            if (_view == null) return;
            // 스태미나 비율 (출처가 없으면 가득)
            float ratio = _player != null && _player.StaminaMax > 0f ? _player.Stamina / _player.StaminaMax : 1f;
            _view.Fill.fillAmount = ratio;
            _view.Group.alpha = Mathf.MoveTowards(_view.Group.alpha, ratio < FullRatio ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
        }
        #endregion
    }
}
