using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>전초기지 화면 참조</summary>
    public sealed class HubView : MonoBehaviour
    {
        /// <summary>파티 수 글자 (키 hub.party_count. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _partyText;

        /// <summary>임시 영입 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _recruitButton;

        /// <summary>출발 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _departButton;

        /// <summary>파티 수 글자 (키 hub.party_count, {0}/{1})</summary>
        public LocalizeStringEvent PartyText => _partyText;

        /// <summary>임시 영입 버튼</summary>
        public Button RecruitButton => _recruitButton;

        /// <summary>출발 버튼</summary>
        public Button DepartButton => _departButton;
    }
}
