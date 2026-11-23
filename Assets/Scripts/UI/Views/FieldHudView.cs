using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD 참조 (Forest 씬 Canvas/Hud). 배치는 v1 임시 (기획 08 HUD)</summary>
    public sealed class FieldHudView : MonoBehaviour
    {
        /// <summary>체력 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _hpFill;

        /// <summary>체력 글자 (Field/field.hud.hp. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _hpText;

        /// <summary>마나 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _manaFill;

        /// <summary>마나 글자 (Field/field.hud.mana. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _manaText;

        /// <summary>스태미나 막대 (인스펙터에서만 연결)</summary>
        [SerializeField] StaminaUIBinder _stamina;

        /// <summary>동료 줄 (파티 순서, 남는 줄은 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] PartyRowView[] _partyRows;

        /// <summary>조준점 (그리는 중·조준 중엔 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _crosshair;

        /// <summary>조준 원 (조준 중에만. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _aimRing;

        /// <summary>조준 글 (키는 FieldCastController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _aimText;

        /// <summary>알림 글 (키는 FieldCastController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _noteText;

        /// <summary>펜 점 (그리기 칸 자식, 그리는 중에만. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _pen;

        /// <summary>구역·지도 진행도 글 (Field/field.zone_line. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _zoneText;

        /// <summary>미니맵 (인스펙터에서만 연결)</summary>
        [SerializeField] MapUIBinder _minimap;

        /// <summary>마지막으로 때린 적 체력 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _targetRoot;

        /// <summary>마지막으로 때린 적 이름 (키는 바인더가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _targetName;

        /// <summary>마지막으로 때린 적 체력 채움 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _targetFill;

        /// <summary>맞음·체력 낮음 붉은 화면 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _hurt;

        /// <summary>상호작용 안내 글 (UI/common.interact_prompt, {0} 키 이름·{1} 지점 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _interactPrompt;

        /// <summary>체력 채움</summary>
        public Image HpFill => _hpFill;

        /// <summary>체력 글자</summary>
        public LocalizeStringEvent HpText => _hpText;

        /// <summary>마나 채움</summary>
        public Image ManaFill => _manaFill;

        /// <summary>마나 글자</summary>
        public LocalizeStringEvent ManaText => _manaText;

        /// <summary>스태미나 막대</summary>
        public StaminaUIBinder Stamina => _stamina;

        /// <summary>동료 줄</summary>
        public PartyRowView[] PartyRows => _partyRows;

        /// <summary>조준점</summary>
        public GameObject Crosshair => _crosshair;

        /// <summary>조준 원</summary>
        public GameObject AimRing => _aimRing;

        /// <summary>조준 글</summary>
        public LocalizeStringEvent AimText => _aimText;

        /// <summary>알림 글</summary>
        public LocalizeStringEvent NoteText => _noteText;

        /// <summary>펜 점</summary>
        public RectTransform Pen => _pen;

        /// <summary>구역·지도 진행도 글</summary>
        public LocalizeStringEvent ZoneText => _zoneText;

        /// <summary>미니맵</summary>
        public MapUIBinder Minimap => _minimap;

        /// <summary>마지막으로 때린 적 체력 묶음</summary>
        public GameObject TargetRoot => _targetRoot;

        /// <summary>마지막으로 때린 적 이름</summary>
        public LocalizeStringEvent TargetName => _targetName;

        /// <summary>마지막으로 때린 적 체력 채움</summary>
        public Image TargetFill => _targetFill;

        /// <summary>맞음·체력 낮음 붉은 화면</summary>
        public CanvasGroup Hurt => _hurt;

        /// <summary>상호작용 안내 글</summary>
        public LocalizeStringEvent InteractPrompt => _interactPrompt;
    }
}
