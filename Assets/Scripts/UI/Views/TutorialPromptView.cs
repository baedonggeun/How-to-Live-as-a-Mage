using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>화면 12시 조작 안내 참조 (Boot 씬 UICanvas/TutorialPrompt)</summary>
    public sealed class TutorialPromptView : MonoBehaviour
    {
        /// <summary>안내 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>안내 글자 (키는 TutorialManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _text;

        /// <summary>안내 전체</summary>
        public GameObject Root => _root;

        /// <summary>안내 글자</summary>
        public LocalizeStringEvent Text => _text;
    }
}
