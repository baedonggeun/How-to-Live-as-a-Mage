using UnityEngine;
using UnityEngine.Localization;
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

        /// <summary>도착 대사 말한 사람 (Hub/hub.arrival.speaker. 인스펙터에서만 정함)</summary>
        [SerializeField] LocalizedString _arrivalSpeaker;

        /// <summary>도착 대사 (Hub/hub.arrival.line. 인스펙터에서만 정함)</summary>
        [SerializeField] LocalizedString _arrivalLine;

        /// <summary>첫 목표 (여관에서 방 잡기. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _firstObjective;

        /// <summary>방을 잡은 뒤 목표 (길드에서 용병 구하고 의뢰 받기. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _guildObjective;

        /// <summary>파티·의뢰를 갖춘 뒤 목표 (식량을 챙겨 정문으로. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _departObjective;

        /// <summary>금화·식량 글자 (Hub/hub.money, {0} 금화·{1} 식량 인분. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _moneyText;

        /// <summary>숲에서 돌아오면 서는 곳 (정문 밖, 정문을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _returnPoint;

        /// <summary>코덱스 (RoomInterior/Codex의 CodexController. 인스펙터에서만 연결)</summary>
        [SerializeField] CodexController _codex;

        /// <summary>책상 시점 연출 (RoomInterior/Codex의 DeskSequence. 인스펙터에서만 연결)</summary>
        [SerializeField] DeskSequence _deskSequence;

        /// <summary>책상 시점 자리 (머리가 옮겨 갈 위치·방향. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _deskSeat;

        /// <summary>숲에서 죽어 깨어나는 곳 (여관 방 침대 옆, 방 문을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _bedPoint;

        /// <summary>의뢰를 받고 한 번 나간 뒤 목표 (숲 외곽의 고블린 소탕. 인스펙터에서만 연결)</summary>
        [SerializeField] ObjectiveSO _forestObjective;

        /// <summary>스태미나 막대 (Canvas/Hud/Stamina. 인스펙터에서만 연결)</summary>
        [SerializeField] StaminaUIBinder _stamina;

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

        /// <summary>도착 대사 말한 사람</summary>
        public LocalizedString ArrivalSpeaker => _arrivalSpeaker;

        /// <summary>도착 대사</summary>
        public LocalizedString ArrivalLine => _arrivalLine;

        /// <summary>첫 목표</summary>
        public ObjectiveSO FirstObjective => _firstObjective;

        /// <summary>방을 잡은 뒤 목표</summary>
        public ObjectiveSO GuildObjective => _guildObjective;

        /// <summary>파티·의뢰를 갖춘 뒤 목표</summary>
        public ObjectiveSO DepartObjective => _departObjective;

        /// <summary>금화·식량 글자</summary>
        public LocalizeStringEvent MoneyText => _moneyText;

        /// <summary>숲에서 돌아오면 서는 곳</summary>
        public Transform ReturnPoint => _returnPoint;

        /// <summary>코덱스</summary>
        public CodexController Codex => _codex;

        /// <summary>책상 시점 연출</summary>
        public DeskSequence DeskSequence => _deskSequence;

        /// <summary>책상 시점 자리</summary>
        public Transform DeskSeat => _deskSeat;

        /// <summary>숲에서 죽어 깨어나는 곳</summary>
        public Transform BedPoint => _bedPoint;

        /// <summary>의뢰를 받고 한 번 나간 뒤 목표</summary>
        public ObjectiveSO ForestObjective => _forestObjective;

        /// <summary>스태미나 막대</summary>
        public StaminaUIBinder Stamina => _stamina;
    }
}
