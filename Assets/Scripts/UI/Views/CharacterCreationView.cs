using TMPro;
using UnityEngine;
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

        /// <summary>마법사 이름 입력 칸</summary>
        public TMP_InputField NameInput => _nameInput;

        /// <summary>확정 버튼</summary>
        public Button ConfirmButton => _confirmButton;
    }
}
