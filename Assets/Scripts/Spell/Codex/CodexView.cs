using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>코덱스 참조 (여관 방 RoomInterior/Codex 루트, World Space 캔버스). 덮인 표지(오른쪽 절반), 펼친 책(왼쪽·오른쪽 장, 넘김 띠, 묶음 드롭다운, 연습 목록·그리기 칸), 앉아 있을 때 위 줄(탭·일어나기)</summary>
    public sealed class CodexView : MonoBehaviour
    {
        /// <summary>코덱스 입력 (책상에 앉아 있을 때만 켬. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>앉아 있을 때만 보이는 위 줄 (일어나기·탭. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _seatedBar;

        /// <summary>일어나기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _leaveButton;

        /// <summary>탭 버튼 묶음 (펼쳤을 때만. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tabs;

        /// <summary>룬 주문 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _runesTab;

        /// <summary>다중 룬 마법 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _magicTab;

        /// <summary>주문 연습 탭 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _practiceTab;

        /// <summary>덮인 표지 (오른쪽 절반, 피벗이 책등. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _cover;

        /// <summary>표지를 누르면 펼침 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _coverButton;

        /// <summary>표지 가운데 코덱스 이름 (Codex/codex.name, {0} 플레이어 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _coverName;

        /// <summary>표지 마법진 (인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _emblem;

        /// <summary>펼친 책 (왼쪽·오른쪽 절반. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _openBook;

        /// <summary>펼친 책 왼쪽 절반 (표지 안쪽 + 왼쪽 장, 피벗이 책등. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _leftHalf;

        /// <summary>왼쪽 장 (인스펙터에서만 연결)</summary>
        [SerializeField] CodexPageView _leftPage;

        /// <summary>오른쪽 장 (인스펙터에서만 연결)</summary>
        [SerializeField] CodexPageView _rightPage;

        /// <summary>왼쪽으로 넘어가 펴지는 종이 (왼쪽 장 자리, 피벗이 책등. 인스펙터에서만 연결)</summary>
        [SerializeField] CodexPageView _flipLeft;

        /// <summary>오른쪽에서 접히는 종이 (오른쪽 장 자리, 피벗이 책등. 인스펙터에서만 연결)</summary>
        [SerializeField] CodexPageView _flipRight;

        /// <summary>이전 장 띠 (왼쪽 장 왼쪽 끝. 인스펙터에서만 연결)</summary>
        [SerializeField] Button _prevStrip;

        /// <summary>이전 장 띠 흐림 (넘길 장이 없을 때. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _prevStripGroup;

        /// <summary>다음 장 띠 (오른쪽 장 오른쪽 끝. 인스펙터에서만 연결)</summary>
        [SerializeField] Button _nextStrip;

        /// <summary>다음 장 띠 흐림 (넘길 장이 없을 때. 인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _nextStripGroup;

        /// <summary>묶음 드롭다운 버튼 (다중 룬 마법 탭 왼쪽 장 맨 위. 인스펙터에서만 연결)</summary>
        [SerializeField] Button _categoryButton;

        /// <summary>묶음 드롭다운 버튼 글자 (지금 묶음 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _categoryText;

        /// <summary>묶음 목록 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _categoryPanel;

        /// <summary>묶음 목록 밖을 누르면 닫는 막 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _categoryBlocker;

        /// <summary>주문 연습 목록 (왼쪽 장, 스크롤. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _practiceList;

        /// <summary>주문 연습 목록 항목 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _list;

        /// <summary>목록·묶음 항목 프리팹 (버튼 + 글자. 인스펙터에서만 연결)</summary>
        [SerializeField] TutorialListItemView _itemPrefab;

        /// <summary>연습 목록 묶음 제목 프리팹 (버튼 + 글자. 인스펙터에서만 연결)</summary>
        [SerializeField] TutorialListItemView _headerPrefab;

        /// <summary>주문 연습 장 (오른쪽 장. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _practicePage;

        /// <summary>주문 연습 제목 (따라 그릴 이름 또는 자유 그리기. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _practiceTitle;

        /// <summary>그리기 칸 (인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawController _draw;

        /// <summary>코덱스 입력</summary>
        public CanvasGroup Group => _group;

        /// <summary>앉아 있을 때만 보이는 위 줄</summary>
        public GameObject SeatedBar => _seatedBar;

        /// <summary>일어나기 버튼</summary>
        public Button LeaveButton => _leaveButton;

        /// <summary>탭 버튼 묶음</summary>
        public GameObject Tabs => _tabs;

        /// <summary>룬 주문 탭 버튼</summary>
        public Button RunesTab => _runesTab;

        /// <summary>다중 룬 마법 탭 버튼</summary>
        public Button MagicTab => _magicTab;

        /// <summary>주문 연습 탭 버튼</summary>
        public Button PracticeTab => _practiceTab;

        /// <summary>덮인 표지</summary>
        public RectTransform Cover => _cover;

        /// <summary>표지 버튼</summary>
        public Button CoverButton => _coverButton;

        /// <summary>표지 코덱스 이름</summary>
        public LocalizeStringEvent CoverName => _coverName;

        /// <summary>표지 마법진</summary>
        public StrokeDrawer Emblem => _emblem;

        /// <summary>펼친 책</summary>
        public GameObject OpenBook => _openBook;

        /// <summary>펼친 책 왼쪽 절반</summary>
        public RectTransform LeftHalf => _leftHalf;

        /// <summary>왼쪽 장</summary>
        public CodexPageView LeftPage => _leftPage;

        /// <summary>오른쪽 장</summary>
        public CodexPageView RightPage => _rightPage;

        /// <summary>왼쪽으로 펴지는 종이</summary>
        public CodexPageView FlipLeft => _flipLeft;

        /// <summary>오른쪽에서 접히는 종이</summary>
        public CodexPageView FlipRight => _flipRight;

        /// <summary>이전 장 띠</summary>
        public Button PrevStrip => _prevStrip;

        /// <summary>이전 장 띠 흐림</summary>
        public CanvasGroup PrevStripGroup => _prevStripGroup;

        /// <summary>다음 장 띠</summary>
        public Button NextStrip => _nextStrip;

        /// <summary>다음 장 띠 흐림</summary>
        public CanvasGroup NextStripGroup => _nextStripGroup;

        /// <summary>묶음 드롭다운 버튼</summary>
        public Button CategoryButton => _categoryButton;

        /// <summary>묶음 드롭다운 버튼 글자</summary>
        public LocalizeStringEvent CategoryText => _categoryText;

        /// <summary>묶음 목록 자리</summary>
        public RectTransform CategoryPanel => _categoryPanel;

        /// <summary>묶음 목록 닫는 막</summary>
        public Button CategoryBlocker => _categoryBlocker;

        /// <summary>주문 연습 목록</summary>
        public GameObject PracticeList => _practiceList;

        /// <summary>주문 연습 목록 항목 자리</summary>
        public RectTransform List => _list;

        /// <summary>목록·묶음 항목 프리팹</summary>
        public TutorialListItemView ItemPrefab => _itemPrefab;

        /// <summary>연습 목록 묶음 제목 프리팹</summary>
        public TutorialListItemView HeaderPrefab => _headerPrefab;

        /// <summary>주문 연습 장</summary>
        public GameObject PracticePage => _practicePage;

        /// <summary>주문 연습 제목</summary>
        public LocalizeStringEvent PracticeTitle => _practiceTitle;

        /// <summary>그리기 칸</summary>
        public SpellDrawController Draw => _draw;
    }
}
