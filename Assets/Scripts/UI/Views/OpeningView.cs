using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>오프닝 화면 참조. 본문은 씬에서 키만 고른 고정 글자라 참조 안 함</summary>
    public sealed class OpeningView : MonoBehaviour
    {
        /// <summary>계속 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _continueButton;

        /// <summary>계속 버튼</summary>
        public Button ContinueButton => _continueButton;
    }
}
