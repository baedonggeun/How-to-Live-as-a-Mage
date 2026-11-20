using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>요소 카드 참조 (ElementCard 프리팹 루트에 붙음)</summary>
    public sealed class ElementCardView : MonoBehaviour
    {
        /// <summary>고르기 토글. 켜면 금색 테두리 (인스펙터에서만 연결)</summary>
        [SerializeField] Toggle _toggle;

        /// <summary>요소 그림 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _icon;

        /// <summary>요소 이름 글자. 테이블·키는 상태가 넣음 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>마우스를 올리면 요소 이름·설명 툴팁 (내용은 상태가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] TooltipHandler _tooltip;

        /// <summary>고르기 토글</summary>
        public Toggle Toggle => _toggle;

        /// <summary>요소 그림</summary>
        public Image Icon => _icon;

        /// <summary>요소 이름 글자</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>요소 툴팁</summary>
        public TooltipHandler Tooltip => _tooltip;
    }
}
