using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>설명창 참조 (Boot 씬 UICanvas/TutorialWindow 루트. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class TutorialWindowView : MonoBehaviour
    {
        /// <summary>제목 (튜토리얼 제목 키를 코드가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>본문 (장 키를 코드가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _body;

        /// <summary>장 번호 (UI/common.page_of, {0} 지금 장·{1} 장 수. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _page;

        /// <summary>다음 장 버튼 (마지막 장엔 숨김. 인스펙터에서만 연결)</summary>
        [SerializeField] Button _nextButton;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>제목</summary>
        public LocalizeStringEvent Title => _title;

        /// <summary>본문</summary>
        public LocalizeStringEvent Body => _body;

        /// <summary>장 번호</summary>
        public LocalizeStringEvent Page => _page;

        /// <summary>다음 장 버튼</summary>
        public Button NextButton => _nextButton;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
