using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>캐릭터 생성 화면 참조</summary>
    public sealed class CharacterCreationView : MonoBehaviour
    {
        /// <summary>마법사 이름 입력 칸 (인스펙터에서만 연결)</summary>
        [SerializeField] TMP_InputField _nameInput;

        /// <summary>확정 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _confirmButton;

        /// <summary>요소 카드가 생길 자리 (가로 레이아웃. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _elementGrid;

        /// <summary>요소 카드 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] ElementCardView _elementCardPrefab;

        /// <summary>고른 요소 수 글자 (키 creation.element_count, {0}/{1}. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _elementCountText;

        /// <summary>생성 화면 배경음. 비면 무음 (인스펙터에서만 연결)</summary>
        [SerializeField] SoundDataSO _bgm;

        /// <summary>마법사 이름 입력 칸</summary>
        public TMP_InputField NameInput => _nameInput;

        /// <summary>확정 버튼</summary>
        public Button ConfirmButton => _confirmButton;

        /// <summary>요소 카드가 생길 자리</summary>
        public RectTransform ElementGrid => _elementGrid;

        /// <summary>요소 카드 프리팹</summary>
        public ElementCardView ElementCardPrefab => _elementCardPrefab;

        /// <summary>고른 요소 수 글자 ({0} 고른 수, {1} 골라야 할 수)</summary>
        public LocalizeStringEvent ElementCountText => _elementCountText;

        /// <summary>생성 화면 배경음. 비면 null</summary>
        public SoundDataSO Bgm => _bgm;
    }
}
