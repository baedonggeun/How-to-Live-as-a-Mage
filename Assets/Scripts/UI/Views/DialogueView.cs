using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>NPC 대화 참조 (Boot 씬 UICanvas/Dialogue. 화면 아래 말한 사람·대사, 그 위에 선택지)</summary>
    public sealed class DialogueView : MonoBehaviour
    {
        /// <summary>대화 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>말한 사람 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _speaker;

        /// <summary>대사 글자 (키는 DialogueController가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _line;

        /// <summary>선택지 버튼을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _choiceList;

        /// <summary>선택지 버튼 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] DialogueChoiceView _choicePrefab;

        /// <summary>대화 전체</summary>
        public GameObject Root => _root;

        /// <summary>말한 사람 글자</summary>
        public LocalizeStringEvent Speaker => _speaker;

        /// <summary>대사 글자</summary>
        public LocalizeStringEvent Line => _line;

        /// <summary>선택지 버튼 자리</summary>
        public RectTransform ChoiceList => _choiceList;

        /// <summary>선택지 버튼 프리팹</summary>
        public DialogueChoiceView ChoicePrefab => _choicePrefab;
    }
}
