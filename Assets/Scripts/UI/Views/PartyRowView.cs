using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD 동료 줄 하나 참조 (이름·체력)</summary>
    public sealed class PartyRowView : MonoBehaviour
    {
        /// <summary>이름 (키는 바인더가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>체력 채움 (Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _hpFill;

        /// <summary>줄 투명도 (죽으면 흐리게. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>이름</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>체력 채움</summary>
        public Image HpFill => _hpFill;

        /// <summary>줄 투명도</summary>
        public CanvasGroup Group => _group;
    }
}
