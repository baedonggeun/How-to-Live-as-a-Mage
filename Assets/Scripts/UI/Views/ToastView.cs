using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>토스트 참조 (Boot 씬 UICanvas)</summary>
    public sealed class ToastView : MonoBehaviour
    {
        /// <summary>토스트 전체 (켜고 끄는 단위. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _root;

        /// <summary>토스트 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _text;

        /// <summary>토스트 전체 (켜고 끄는 단위)</summary>
        public GameObject Root => _root;

        /// <summary>토스트 글자 (키는 UIManager가 넣음)</summary>
        public LocalizeStringEvent Text => _text;
    }
}
