using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>스태미나 막대 참조 (전초기지·숲 HUD)</summary>
    public sealed class StaminaBarView : MonoBehaviour
    {
        /// <summary>채움 (Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _fill;

        /// <summary>막대 전체 투명도 (가득 차면 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>채움</summary>
        public Image Fill => _fill;

        /// <summary>막대 전체 투명도</summary>
        public CanvasGroup Group => _group;
    }
}
