using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>NPC 대사 자막 참조 (Boot 씬 UICanvas/Subtitle, 화면 아래 토스트 위)</summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        /// <summary>자막 전체 (켜고 끄는 단위, 자식 Panel. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>말한 사람 글자 (키는 UIManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _speaker;

        /// <summary>대사 글자 (키는 UIManager가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _line;

        /// <summary>자막 전체</summary>
        public GameObject Root => _root;

        /// <summary>말한 사람 글자</summary>
        public LocalizeStringEvent Speaker => _speaker;

        /// <summary>대사 글자</summary>
        public LocalizeStringEvent Line => _line;
    }
}
