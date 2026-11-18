using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>확인창 참조 (Boot 씬 UICanvas/Confirm 루트에 붙음. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class ConfirmView : MonoBehaviour
    {
        /// <summary>물어볼 문구 (테이블은 씬에서 UI로 정해 둠, 키는 UIManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _messageText;

        /// <summary>확인 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _confirmButton;

        /// <summary>취소 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _cancelButton;

        /// <summary>물어볼 문구</summary>
        public LocalizeStringEvent MessageText => _messageText;

        /// <summary>확인 버튼</summary>
        public Button ConfirmButton => _confirmButton;

        /// <summary>취소 버튼</summary>
        public Button CancelButton => _cancelButton;
    }
}
