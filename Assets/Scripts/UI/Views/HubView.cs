using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>전초기지 화면·씬 참조</summary>
    public sealed class HubView : MonoBehaviour
    {
        /// <summary>파티 수 글자 (Hub/hub.party_count, {0}/{1}. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _partyText;

        /// <summary>상호작용 안내 글 (UI/common.interact_prompt, {0} 키 이름·{1} 지점 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _interactPrompt;

        /// <summary>1인칭 조작 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] PlayerController _player;

        /// <summary>바라본 지점 찾기 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] InteractDetector _detector;

        /// <summary>플레이어 시작 위치 (정문 안쪽. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _spawn;

        /// <summary>파티 수 글자</summary>
        public LocalizeStringEvent PartyText => _partyText;

        /// <summary>상호작용 안내 글</summary>
        public LocalizeStringEvent InteractPrompt => _interactPrompt;

        /// <summary>1인칭 조작</summary>
        public PlayerController Player => _player;

        /// <summary>바라본 지점 찾기</summary>
        public InteractDetector Detector => _detector;

        /// <summary>플레이어 시작 위치</summary>
        public Transform Spawn => _spawn;
    }
}
