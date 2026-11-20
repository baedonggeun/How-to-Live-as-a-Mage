using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>툴팁 참조 (Boot 씬 UICanvas/Tooltip, 맨 위)</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        /// <summary>마우스를 따라가는 패널 (앵커 가운데, 피벗 왼쪽 위. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _panel;

        /// <summary>제목 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _body;

        /// <summary>마우스를 따라가는 패널</summary>
        public RectTransform Panel => _panel;

        /// <summary>제목 글자</summary>
        public LocalizeStringEvent Title => _title;

        /// <summary>설명 글자</summary>
        public LocalizeStringEvent Body => _body;
    }
}
