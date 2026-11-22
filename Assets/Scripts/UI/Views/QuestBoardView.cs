using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>임무 게시판 창 참조 (Boot 씬 UICanvas/QuestBoard 루트. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class QuestBoardView : MonoBehaviour
    {
        /// <summary>의뢰 이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questTitle;

        /// <summary>의뢰 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questDescription;

        /// <summary>기간 글자 (Hub/hub.board.days, {0} 일 수. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questDays;

        /// <summary>받기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _takeButton;

        /// <summary>받기 버튼 글자 (받기·받음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _takeLabel;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>의뢰 이름 글자</summary>
        public LocalizeStringEvent QuestTitle => _questTitle;

        /// <summary>의뢰 설명 글자</summary>
        public LocalizeStringEvent QuestDescription => _questDescription;

        /// <summary>기간 글자</summary>
        public LocalizeStringEvent QuestDays => _questDays;

        /// <summary>받기 버튼</summary>
        public Button TakeButton => _takeButton;

        /// <summary>받기 버튼 글자</summary>
        public LocalizeStringEvent TakeLabel => _takeLabel;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
