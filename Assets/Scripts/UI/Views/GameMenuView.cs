using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>메뉴 창 참조 (Boot 씬 UICanvas/GameMenu 루트. 지도·임무 일지·튜토리얼·주문 도감 탭. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class GameMenuView : MonoBehaviour
    {
        /// <summary>지도 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _mapTab;

        /// <summary>임무 일지 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _journalTab;

        /// <summary>튜토리얼 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _tutorialTab;

        /// <summary>주문 도감 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _spellsTab;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>지도 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _mapPage;

        /// <summary>임무 일지 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _journalPage;

        /// <summary>튜토리얼 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialPage;

        /// <summary>주문 도감 페이지 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _spellsPage;

        /// <summary>임무 일지: 목표 이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _objectiveTitle;

        /// <summary>임무 일지: 목표 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _objectiveDescription;

        /// <summary>임무 일지: 목표가 없을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _journalEmpty;

        /// <summary>임무 일지: 받은 의뢰 글자 (Menu/menu.journal.quest, {0} 이름·{1} 기간. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _questText;

        /// <summary>튜토리얼: "모두 보기 (스포일러가 있을 수 있습니다)" 토글 (인스펙터에서만 연결)</summary>
        [SerializeField] Toggle _spoilerToggle;

        /// <summary>튜토리얼: 목록 항목을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _tutorialList;

        /// <summary>튜토리얼: 목록 항목 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] TutorialListItemView _tutorialItemPrefab;

        /// <summary>튜토리얼: 고른 항목 설명 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialDetail;

        /// <summary>튜토리얼: 고른 항목 제목 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tutorialTitle;

        /// <summary>튜토리얼: 고른 항목 설명 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tutorialBody;

        /// <summary>튜토리얼: 보일 항목이 없을 때 글 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tutorialEmpty;

        /// <summary>지도 탭 버튼</summary>
        public Button MapTab => _mapTab;

        /// <summary>임무 일지 탭 버튼</summary>
        public Button JournalTab => _journalTab;

        /// <summary>튜토리얼 탭 버튼</summary>
        public Button TutorialTab => _tutorialTab;

        /// <summary>주문 도감 탭 버튼</summary>
        public Button SpellsTab => _spellsTab;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;

        /// <summary>지도 페이지</summary>
        public GameObject MapPage => _mapPage;

        /// <summary>임무 일지 페이지</summary>
        public GameObject JournalPage => _journalPage;

        /// <summary>튜토리얼 페이지</summary>
        public GameObject TutorialPage => _tutorialPage;

        /// <summary>주문 도감 페이지</summary>
        public GameObject SpellsPage => _spellsPage;

        /// <summary>임무 일지: 목표 이름 글자</summary>
        public LocalizeStringEvent ObjectiveTitle => _objectiveTitle;

        /// <summary>임무 일지: 목표 설명 글자</summary>
        public LocalizeStringEvent ObjectiveDescription => _objectiveDescription;

        /// <summary>임무 일지: 목표가 없을 때 글</summary>
        public GameObject JournalEmpty => _journalEmpty;

        /// <summary>임무 일지: 받은 의뢰 글자</summary>
        public LocalizeStringEvent QuestText => _questText;

        /// <summary>튜토리얼: 모두 보기 토글</summary>
        public Toggle SpoilerToggle => _spoilerToggle;

        /// <summary>튜토리얼: 목록 자리</summary>
        public RectTransform TutorialList => _tutorialList;

        /// <summary>튜토리얼: 목록 항목 프리팹</summary>
        public TutorialListItemView TutorialItemPrefab => _tutorialItemPrefab;

        /// <summary>튜토리얼: 고른 항목 설명 묶음</summary>
        public GameObject TutorialDetail => _tutorialDetail;

        /// <summary>튜토리얼: 고른 항목 제목 글자</summary>
        public LocalizeStringEvent TutorialTitle => _tutorialTitle;

        /// <summary>튜토리얼: 고른 항목 설명 글자</summary>
        public LocalizeStringEvent TutorialBody => _tutorialBody;

        /// <summary>튜토리얼: 보일 항목이 없을 때 글</summary>
        public GameObject TutorialEmpty => _tutorialEmpty;
    }
}
