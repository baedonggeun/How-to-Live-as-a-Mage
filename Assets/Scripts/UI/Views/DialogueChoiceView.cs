using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>대화 선택지 버튼 참조 (DialogueChoice 프리팹 루트)</summary>
    public sealed class DialogueChoiceView : MonoBehaviour
    {
        /// <summary>선택지 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>선택지 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _label;

        /// <summary>선택지 버튼</summary>
        public Button Button => _button;

        /// <summary>선택지 글자</summary>
        public LocalizeStringEvent Label => _label;
    }
}
