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

        /// <summary>첫 도착 역마차 연출 (씬의 Opening. 인스펙터에서만 연결)</summary>
        [SerializeField] OpeningSequence _opening;

        /// <summary>첫 도착 때 플레이어가 서는 곳 (마차 오른쪽, 정문을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _arrivalPoint;

        /// <summary>조작하는 동안만 보이는 화면 글자 묶음 (파티 수·조준점·안내 글. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _hud;

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

        /// <summary>첫 도착 역마차 연출</summary>
        public OpeningSequence Opening => _opening;

        /// <summary>첫 도착 때 플레이어가 서는 곳</summary>
        public Transform ArrivalPoint => _arrivalPoint;

        /// <summary>조작하는 동안만 보이는 화면 글자 묶음</summary>
        public GameObject Hud => _hud;
    }
}
