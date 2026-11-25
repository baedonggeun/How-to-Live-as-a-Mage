using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>코덱스 (마법사가 늘 들고 다니는 책이자 주문 도감. 방에선 책상 위에 덮어 두고 앉으면 씀): 덮인 표지를 누르면 왼쪽으로 펼침. 탭은 룬 주문·다중 룬 마법(장마다 하나, 장 바깥 끝 띠로 넘김, 다중 룬 마법은 묶음 드롭다운)·주문 연습(목록 + 그리기 판정, 다중 룬 마법은 생략 없이 처음 완성하면 등록). 익힌 룬·등록한 마법만 나옴. 판정·등록은 상태가 넘긴 코덱스 캡슐이 하고, 연습 판정은 PracticeJudged로 알림 (소리·튜토리얼·등록 토스트는 상태가 함). 표지 마법진은 CodexEmblemCalculator</summary>
    public sealed class CodexController : MonoBehaviour
    {
        /// <summary>펼친 책 탭</summary>
        enum Tab
        {
            /// <summary>익힌 룬 주문 (장마다 하나)</summary>
            Runes,

            /// <summary>등록한 다중 룬 마법 (룬 수 묶음, 장마다 하나)</summary>
            Magic,

            /// <summary>주문 연습 (왼쪽 목록, 오른쪽 그리기 칸)</summary>
            Practice,
        }

        /// <summary>장 하나 (룬 주문 또는 다중 룬 마법). 빈 장은 null</summary>
        sealed class PageEntry
        {
            /// <summary>룬 주문의 룬 (다중 룬 마법이면 null)</summary>
            public RuneSO Rune;

            /// <summary>다중 룬 마법 (룬 주문이면 null)</summary>
            public SpellSO Spell;

            /// <summary>다중 룬 마법의 룬 수 (룬 주문이면 0)</summary>
            public int RuneCount;
        }

        /// <summary>다중 룬 마법 묶음의 최대 룬 수 (1룬 ~ 8룬 마법)</summary>
        const int MaxRuneCount = 8;

        /// <summary>룬 주문 이름 키 ({0} 룬 이름)</summary>
        const string RuneSpellKey = "codex.rune_spell";

        /// <summary>룬 주문 묶음 이름 키</summary>
        const string RunesCategoryKey = "codex.category.runes";

        /// <summary>다중 룬 마법 묶음 이름 키 ({0} 룬 수)</summary>
        const string MagicCategoryKey = "codex.category.magic";

        /// <summary>필요한 룬 키 ({0} 룬 목록)</summary>
        const string RunesKey = "codex.runes";

        /// <summary>쪽 번호 키 ({0} 쪽)</summary>
        const string PageKey = "codex.page";

        /// <summary>룬 주문 탭이 비었을 때 키</summary>
        const string EmptyRunesKey = "codex.empty.runes";

        /// <summary>다중 룬 마법 탭이 비었을 때 키</summary>
        const string EmptyMagicKey = "codex.empty.magic";

        /// <summary>자유 그리기 키</summary>
        const string FreeDrawKey = "codex.practice.free";

        /// <summary>코덱스 문구 테이블</summary>
        static readonly TableReference Table = "Codex";

        /// <summary>같은 책의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] CodexView _view;

        /// <summary>장 넘김·표지 여닫기 연출 (인스펙터에서만 연결)</summary>
        [SerializeField] PageFlipSequence _flip;

        /// <summary>지금 만든 연습 목록 항목 (묶음 제목 포함)</summary>
        readonly List<TutorialListItemView> _listItems = new();

        /// <summary>지금 만든 묶음 드롭다운 항목</summary>
        readonly List<TutorialListItemView> _categoryItems = new();

        /// <summary>룬 주문 탭 장 (짝수 개, 빈 장은 null)</summary>
        readonly List<PageEntry> _runePages = new();

        /// <summary>다중 룬 마법 탭 장 (룬 수 순서, 묶음마다 왼쪽 장에서 시작, 짝수 개, 빈 장은 null)</summary>
        readonly List<PageEntry> _magicPages = new();

        /// <summary>다중 룬 마법 묶음 → 첫 장 번호 (마법이 있는 묶음만)</summary>
        readonly Dictionary<int, int> _magicStart = new();

        /// <summary>판정·등록을 맡는 코덱스 캡슐 (앉아 있는 동안. 일어나면 null)</summary>
        CodexService _codex;

        /// <summary>일어나기를 누르면 할 일 (상태가 넘김)</summary>
        Action _onLeave;

        /// <summary>지금 탭</summary>
        Tab _tab;

        /// <summary>지금 펼침 번호 (0부터, 왼쪽 장 = 2×펼침)</summary>
        int _spread;

        /// <summary>책이 펼쳐져 있는지</summary>
        bool _bookOpen;

        /// <summary>여닫기·장 넘김 연출 중인지 (그동안 입력 무시)</summary>
        bool _busy;

        /// <summary>도는 연출 (상태를 나갈 때 멈춤). 없으면 null</summary>
        Coroutine _routine;

        /// <summary>앉아서 쓰는 중인지 (Open ~ Close)</summary>
        public bool IsOpen => _codex != null;

        /// <summary>여닫기·장 넘김 연출 중인지</summary>
        public bool IsBusy => _busy;

        /// <summary>주문 연습에서 그리는 중인지 (Esc를 그리기 취소로 씀)</summary>
        public bool IsCasting => _view != null && _view.Draw.IsCasting;

        /// <summary>주문 연습 판정·취소가 끝남 (판정 결과. 상태가 소리·튜토리얼을 맡음)</summary>
        public event Action<SpellJudgeSpec> PracticeJudged;

        #region Awake - 연결 확인, 버튼 연결 (처음 한 번), 덮인 책으로 두고 입력을 막음 (책상에 앉으면 Open)
        void Awake()
        {
            if (_view == null || _flip == null)
            {
                Debug.LogError("[Codex] CodexView·PageFlipSequence 연결 안 됨", this);
                return;
            }
            _view.RunesTab.onClick.AddListener(() => ShowTab(Tab.Runes));
            _view.MagicTab.onClick.AddListener(() => ShowTab(Tab.Magic));
            _view.PracticeTab.onClick.AddListener(() => ShowTab(Tab.Practice));
            _view.LeaveButton.onClick.AddListener(() => _onLeave?.Invoke());
            _view.CoverButton.onClick.AddListener(OpenBook);
            _view.PrevStrip.onClick.AddListener(() => TurnPage(-1));
            _view.NextStrip.onClick.AddListener(() => TurnPage(1));
            _view.CategoryButton.onClick.AddListener(ToggleCategories);
            _view.CategoryBlocker.onClick.AddListener(HideCategories);
            ResetClosed();
            SetSeated(false);
        }
        #endregion

        #region ShowCover - 표지를 채움: 가운데 코덱스 이름('{플레이어}'s Codex'), 마법진 (원 셋 + 육망성 + 둘레 룬 그림). 씬이 준비될 때 상태가 부름
        /// <param name="playerName">플레이어 이름</param>
        /// <param name="runes">룬 전부 (둘레에 그릴 룬 그림)</param>
        public void ShowCover(string playerName, IReadOnlyList<RuneSO> runes)
        {
            if (_view == null) return;
            _view.CoverName.StringReference.Arguments = new object[] { playerName ?? string.Empty };
            _view.CoverName.RefreshString();
            _view.Emblem.SetStrokes(CodexEmblemCalculator.Build(runes), true);
        }
        #endregion

        #region Open - 앉아서 쓰기 시작: 코덱스 캡슐을 받아 장을 만들고 입력을 켬. 책은 덮인 채 (표지를 누르면 펼침)
        /// <param name="codex">판정·등록을 맡는 코덱스 캡슐</param>
        /// <param name="onLeave">일어나기를 누르면 할 일</param>
        public void Open(CodexService codex, Action onLeave)
        {
            if (_view == null) return;
            _codex = codex ?? throw new ArgumentNullException(nameof(codex));
            _onLeave = onLeave;
            BuildPages();
            SetSeated(true);
        }
        #endregion

        #region Close - 그만 씀: 펼쳐져 있으면 덮는 연출 뒤, 입력을 막고 판 데이터를 놓은 다음 onClosed (연출 중이면 무시)
        /// <param name="onClosed">다 덮은 뒤 할 일</param>
        public void Close(Action onClosed)
        {
            if (_view == null || _busy) return;
            if (_bookOpen)
            {
                _routine = StartCoroutine(CloseBookRoutine(() =>
                {
                    Release();
                    onClosed?.Invoke();
                }));
                return;
            }
            Release();
            onClosed?.Invoke();
        }
        #endregion

        #region ForceClose - 연출을 멈추고(안에서 도는 접기·펴기까지) 바로 덮인 책으로 되돌린 뒤 그만 씀 (상태를 나갈 때)
        public void ForceClose()
        {
            if (_view == null) return;
            StopAllCoroutines();
            _routine = null;
            _busy = false;
            ResetClosed();
            Release();
        }
        #endregion

        #region CancelCast - 그리는 중이면 그리기를 취소 (Esc)
        public void CancelCast()
        {
            if (_view != null) _view.Draw.CancelCast();
        }
        #endregion

        #region Release - 그리기·목록·드롭다운을 지우고 입력을 막고 판 데이터를 놓음
        void Release()
        {
            HideCategories();
            _view.Draw.End();
            ClearItems(_listItems);
            SetSeated(false);
            _codex = null;
            _onLeave = null;
        }
        #endregion

        #region SetSeated - 앉아 있는지에 맞춰 입력과 위 줄(일어나기, 펼쳤으면 탭)을 켜고 끔
        /// <param name="on">앉아 있는지</param>
        void SetSeated(bool on)
        {
            _view.Group.interactable = on;
            _view.Group.blocksRaycasts = on;
            _view.SeatedBar.SetActive(on);
            _view.Tabs.SetActive(on && _bookOpen);
        }
        #endregion

        #region ResetClosed - 덮인 책 모습으로 바로 되돌림 (표지 보임, 펼친 책·넘기는 종이·드롭다운 숨김)
        void ResetClosed()
        {
            _bookOpen = false;
            _view.Cover.gameObject.SetActive(true);
            PageFlipSequence.SetWidth(_view.Cover, 1f);
            PageFlipSequence.SetWidth(_view.LeftHalf, 1f);
            _view.OpenBook.SetActive(false);
            _view.FlipLeft.gameObject.SetActive(false);
            _view.FlipRight.gameObject.SetActive(false);
            _view.Tabs.SetActive(false);
            HideCategories();
        }
        #endregion

        #region OpenBook - 표지를 누름: 앉아 있고 덮여 있으면 펼치는 연출
        void OpenBook()
        {
            if (!IsOpen || _busy || _bookOpen) return;
            _routine = StartCoroutine(OpenBookRoutine());
        }
        #endregion

        #region OpenBookRoutine - 펼치기: 룬 주문 첫 펼침을 깔고, 표지를 책등으로 접은 뒤 왼쪽 절반을 책등에서 펴고 탭을 보임
        IEnumerator OpenBookRoutine()
        {
            _busy = true;
            _tab = Tab.Runes;
            _spread = 0;
            _view.OpenBook.SetActive(true);
            PageFlipSequence.SetWidth(_view.LeftHalf, 0f);
            ApplyTabLayout();
            yield return StartCoroutine(_flip.Fold(_view.Cover, 1f, 0f, _flip.CoverHalf));
            _view.Cover.gameObject.SetActive(false);
            yield return StartCoroutine(_flip.Fold(_view.LeftHalf, 0f, 1f, _flip.CoverHalf));
            _bookOpen = true;
            _view.Tabs.SetActive(true);
            _busy = false;
            _routine = null;
            RefreshTabs();
            RefreshSpreadControls();
        }
        #endregion

        #region CloseBookRoutine - 덮기: 그리기·드롭다운·탭을 끄고, 왼쪽 절반을 책등으로 접은 뒤 표지를 책등에서 펴고 펼친 책을 숨김. 끝나면 onDone
        /// <param name="onDone">다 덮은 뒤 할 일</param>
        IEnumerator CloseBookRoutine(Action onDone)
        {
            _busy = true;
            HideCategories();
            _view.Draw.End();
            _view.Tabs.SetActive(false);
            yield return StartCoroutine(_flip.Fold(_view.LeftHalf, 1f, 0f, _flip.CoverHalf));
            _view.Cover.gameObject.SetActive(true);
            yield return StartCoroutine(_flip.Fold(_view.Cover, 0f, 1f, _flip.CoverHalf));
            _view.OpenBook.SetActive(false);
            PageFlipSequence.SetWidth(_view.LeftHalf, 1f);
            _bookOpen = false;
            _busy = false;
            _routine = null;
            onDone?.Invoke();
        }
        #endregion

        #region ShowTab - 탭을 바꿈 (펼쳐 있고 연출 중이 아닐 때): 첫 펼침부터, 드롭다운 닫음
        /// <param name="tab">보일 탭</param>
        void ShowTab(Tab tab)
        {
            if (!_bookOpen || _busy) return;
            _tab = tab;
            _spread = 0;
            HideCategories();
            ApplyTabLayout();
            RefreshTabs();
        }
        #endregion

        #region RefreshTabs - 지금 탭 버튼은 못 누르게
        void RefreshTabs()
        {
            _view.RunesTab.interactable = _tab != Tab.Runes;
            _view.MagicTab.interactable = _tab != Tab.Magic;
            _view.PracticeTab.interactable = _tab != Tab.Practice;
        }
        #endregion

        #region ApplyTabLayout - 탭에 맞춰 장을 바꿈: 룬 주문·다중 룬 마법은 양쪽 장과 넘김 띠, 주문 연습은 왼쪽 목록과 오른쪽 그리기 칸 (처음엔 자유 그리기)
        void ApplyTabLayout()
        {
            // 장을 보이는 탭인지
            bool pages = _tab != Tab.Practice;
            _view.LeftPage.gameObject.SetActive(pages);
            _view.RightPage.gameObject.SetActive(pages);
            _view.PrevStrip.gameObject.SetActive(pages);
            _view.NextStrip.gameObject.SetActive(pages);
            _view.PracticeList.SetActive(!pages);
            _view.PracticePage.SetActive(!pages);
            if (pages)
            {
                ClearItems(_listItems);
                ShowSpread();
                return;
            }
            _view.CategoryButton.gameObject.SetActive(false);
            RebuildList();
            SelectPractice(null);
        }
        #endregion

        #region ShowSpread - 지금 펼침의 왼쪽·오른쪽 장을 채우고 넘김 띠·드롭다운을 맞춤
        void ShowSpread()
        {
            // 지금 탭 장
            List<PageEntry> pages = CurrentPages();
            FillPage(_view.LeftPage, pages[2 * _spread], 2 * _spread + 1, true);
            FillPage(_view.RightPage, pages[2 * _spread + 1], 2 * _spread + 2, false);
            RefreshSpreadControls();
        }
        #endregion

        #region RefreshSpreadControls - 넘길 장이 있을 때만 넘김 띠를 켬 (연출 중엔 끔). 다중 룬 마법 탭이면 왼쪽 장 묶음 이름 대신 드롭다운 버튼
        void RefreshSpreadControls()
        {
            if (_tab == Tab.Practice) return;
            // 지금 탭 장, 펼침 수
            List<PageEntry> pages = CurrentPages();
            int spreads = pages.Count / 2;
            SetStrip(_view.PrevStrip, _view.PrevStripGroup, !_busy && _spread > 0);
            SetStrip(_view.NextStrip, _view.NextStripGroup, !_busy && _spread < spreads - 1);
            // 지금 펼침의 묶음 (왼쪽 장, 비었으면 오른쪽 장)
            PageEntry first = pages[2 * _spread] ?? pages[2 * _spread + 1];
            // 드롭다운을 보일지 (넘기는 중엔 숨김)
            bool dropdown = _tab == Tab.Magic && first != null && !_busy;
            _view.CategoryButton.gameObject.SetActive(dropdown);
            if (dropdown)
            {
                _view.LeftPage.Category.gameObject.SetActive(false);
                SetText(_view.CategoryText, Table, MagicCategoryKey, first.RuneCount);
            }
        }
        #endregion

        #region SetStrip - 넘김 띠 켜고 끄기 (꺼지면 흐림)
        /// <param name="strip">띠 버튼</param>
        /// <param name="group">띠 흐림</param>
        /// <param name="on">켤지</param>
        static void SetStrip(UnityEngine.UI.Button strip, CanvasGroup group, bool on)
        {
            strip.interactable = on;
            group.alpha = on ? 1f : 0.35f;
        }
        #endregion

        #region CurrentPages - 지금 탭 장 목록
        List<PageEntry> CurrentPages() => _tab == Tab.Magic ? _magicPages : _runePages;
        #endregion

        #region FillPage - 장 하나를 채움: 룬 주문(묶음·'불 룬 주문'·룬 그림·설명) 또는 다중 룬 마법(묶음·이름·그림·필요한 룬·설명), 빈 장이면 (첫 장만) 빈 탭 글. 쪽 번호는 늘
        /// <param name="page">채울 장</param>
        /// <param name="entry">장 내용 (빈 장이면 null)</param>
        /// <param name="number">쪽 번호 (1부터)</param>
        /// <param name="showEmpty">빈 장이면 빈 탭 글을 보일지 (왼쪽 장)</param>
        void FillPage(CodexPageView page, PageEntry entry, int number, bool showEmpty)
        {
            // 내용이 있는지
            bool has = entry != null;
            page.Category.gameObject.SetActive(has);
            page.Title.gameObject.SetActive(has);
            page.Drawing.gameObject.SetActive(has);
            page.Body.gameObject.SetActive(has);
            page.Runes.gameObject.SetActive(has && entry.Spell != null);
            // 빈 탭 글을 보일지 (탭에 장이 하나도 없을 때 첫 장)
            bool empty = !has && showEmpty && number == 1;
            page.Empty.gameObject.SetActive(empty);
            if (empty) SetText(page.Empty, Table, _tab == Tab.Magic ? EmptyMagicKey : EmptyRunesKey);
            SetText(page.Number, Table, PageKey, number);
            if (!has)
            {
                page.Drawing.Clear();
                return;
            }
            if (entry.Rune != null)
            {
                SetText(page.Category, Table, RunesCategoryKey);
                SetText(page.Title, Table, RuneSpellKey, entry.Rune.Name.GetLocalizedString());
                page.Drawing.SetStrokes(entry.Rune.Shape.PartStrokes.SelectMany(part => part), true);
                SetText(page.Body, entry.Rune.Description);
                return;
            }
            SetText(page.Category, Table, MagicCategoryKey, entry.RuneCount);
            SetText(page.Title, entry.Spell.Name);
            page.Drawing.SetStrokes(entry.Spell.Shape.PartStrokes.SelectMany(part => part), true);
            SetText(page.Runes, Table, RunesKey, RuneNames(entry.Spell));
            SetText(page.Body, entry.Spell.Description);
        }
        #endregion

        #region TurnPage - 넘김 띠: 펼쳐 있고 연출 중이 아니면 한 펼침 넘김
        /// <param name="delta">넘길 펼침 수 (-1 이전, 1 다음)</param>
        void TurnPage(int delta)
        {
            if (!_bookOpen || _busy || _tab == Tab.Practice) return;
            // 넘긴 펼침 번호
            int target = _spread + delta;
            if (target < 0 || target >= CurrentPages().Count / 2) return;
            _routine = StartCoroutine(FlipRoutine(target));
        }
        #endregion

        #region FlipRoutine - 목표 펼침까지 한 펼침씩 넘김. 다음: 오른쪽 장이 책등으로 접히고(밑엔 새 오른쪽 장) 새 왼쪽 장이 책등에서 펴짐. 이전: 거꾸로
        /// <param name="target">목표 펼침 번호</param>
        IEnumerator FlipRoutine(int target)
        {
            _busy = true;
            HideCategories();
            RefreshSpreadControls();
            // 지금 탭 장
            List<PageEntry> pages = CurrentPages();
            while (_spread != target)
            {
                // 넘길 방향, 넘기기 전·뒤 펼침
                int step = Math.Sign(target - _spread);
                int from = _spread, to = _spread + step;
                if (step > 0)
                {
                    FillPage(_view.FlipRight, pages[2 * from + 1], 2 * from + 2, false);
                    _view.FlipRight.gameObject.SetActive(true);
                    FillPage(_view.RightPage, pages[2 * to + 1], 2 * to + 2, false);
                    yield return StartCoroutine(_flip.Fold((RectTransform)_view.FlipRight.transform, 1f, 0f, _flip.PageHalf));
                    _view.FlipRight.gameObject.SetActive(false);
                    FillPage(_view.FlipLeft, pages[2 * to], 2 * to + 1, false);
                    _view.FlipLeft.gameObject.SetActive(true);
                    yield return StartCoroutine(_flip.Fold((RectTransform)_view.FlipLeft.transform, 0f, 1f, _flip.PageHalf));
                    FillPage(_view.LeftPage, pages[2 * to], 2 * to + 1, true);
                    _view.FlipLeft.gameObject.SetActive(false);
                }
                else
                {
                    FillPage(_view.FlipLeft, pages[2 * from], 2 * from + 1, false);
                    _view.FlipLeft.gameObject.SetActive(true);
                    FillPage(_view.LeftPage, pages[2 * to], 2 * to + 1, true);
                    yield return StartCoroutine(_flip.Fold((RectTransform)_view.FlipLeft.transform, 1f, 0f, _flip.PageHalf));
                    _view.FlipLeft.gameObject.SetActive(false);
                    FillPage(_view.FlipRight, pages[2 * to + 1], 2 * to + 2, false);
                    _view.FlipRight.gameObject.SetActive(true);
                    yield return StartCoroutine(_flip.Fold((RectTransform)_view.FlipRight.transform, 0f, 1f, _flip.PageHalf));
                    FillPage(_view.RightPage, pages[2 * to + 1], 2 * to + 2, false);
                    _view.FlipRight.gameObject.SetActive(false);
                }
                _spread = to;
            }
            _busy = false;
            _routine = null;
            ShowSpread();
        }
        #endregion

        #region ToggleCategories - 묶음 드롭다운 버튼: 열려 있으면 닫고, 닫혀 있으면 엶
        void ToggleCategories()
        {
            if (_view.CategoryPanel.gameObject.activeSelf) HideCategories();
            else ShowCategories();
        }
        #endregion

        #region ShowCategories - 묶음 목록을 엶 (다중 룬 마법 탭, 연출 중이 아닐 때): 마법이 있는 묶음만, 누르면 그 묶음 첫 펼침까지 장 넘김
        void ShowCategories()
        {
            if (!_bookOpen || _busy || _tab != Tab.Magic) return;
            ClearItems(_categoryItems);
            for (int n = 1; n <= MaxRuneCount; n++)
            {
                if (!_magicStart.ContainsKey(n)) continue;
                // 누를 때 쓸 룬 수 (반복 변수를 붙잡지 않게)
                int count = n;
                // 이 묶음 항목
                TutorialListItemView item = CreateItem(_view.ItemPrefab, _view.CategoryPanel, _categoryItems, () => JumpToCategory(count));
                SetText(item.Title, Table, MagicCategoryKey, count);
            }
            _view.CategoryBlocker.gameObject.SetActive(true);
            _view.CategoryPanel.gameObject.SetActive(true);
        }
        #endregion

        #region HideCategories - 묶음 목록을 닫음 (항목은 프레임 끝에 지워짐)
        void HideCategories()
        {
            ClearItems(_categoryItems);
            _view.CategoryPanel.gameObject.SetActive(false);
            _view.CategoryBlocker.gameObject.SetActive(false);
        }
        #endregion

        #region JumpToCategory - 묶음을 고름: 목록을 닫고 그 묶음 첫 펼침까지 한 펼침씩 넘김
        /// <param name="runeCount">고른 묶음의 룬 수</param>
        void JumpToCategory(int runeCount)
        {
            HideCategories();
            if (_busy || !_magicStart.TryGetValue(runeCount, out int start)) return;
            // 목표 펼침 번호
            int target = start / 2;
            if (target == _spread) return;
            _routine = StartCoroutine(FlipRoutine(target));
        }
        #endregion

        #region RebuildList - 주문 연습 목록을 다시 만듦: 묶음 제목(룬 주문, N룬 마법. 비면 안 나옴)과 항목. 제목을 누르면 처음 화면, 항목을 누르면 그 그림을 흐리게
        void RebuildList()
        {
            ClearItems(_listItems);
            // 익힌 룬 주문 (룬 순서)
            List<PageEntry> runes = _runePages.Where(page => page != null).ToList();
            AddSection(Table, RunesCategoryKey, 0, runes);
            for (int n = 1; n <= MaxRuneCount; n++)
                AddSection(Table, MagicCategoryKey, n, _magicPages.Where(page => page != null && page.RuneCount == n).ToList());
        }
        #endregion

        #region AddSection - 연습 목록 묶음 하나: 제목 + 항목 (항목이 없으면 안 만듦)
        /// <param name="table">제목 문구 테이블</param>
        /// <param name="titleKey">제목 키</param>
        /// <param name="runeCount">다중 룬 마법 룬 수 (룬 주문이면 0, 제목 자리 값)</param>
        /// <param name="entries">묶음 항목</param>
        void AddSection(TableReference table, string titleKey, int runeCount, List<PageEntry> entries)
        {
            if (entries.Count == 0) return;
            // 묶음 제목 (누르면 처음 화면)
            TutorialListItemView header = CreateItem(_view.HeaderPrefab, _view.List, _listItems, () => SelectPractice(null));
            if (runeCount > 0) SetText(header.Title, table, titleKey, runeCount);
            else SetText(header.Title, table, titleKey);
            foreach (PageEntry entry in entries)
            {
                // 누를 때 쓸 항목 (반복 변수를 붙잡지 않게)
                PageEntry picked = entry;
                // 이 항목
                TutorialListItemView item = CreateItem(_view.ItemPrefab, _view.List, _listItems, () => SelectPractice(picked));
                if (entry.Rune != null) SetText(item.Title, Table, RuneSpellKey, entry.Rune.Name.GetLocalizedString());
                else SetText(item.Title, entry.Spell.Name);
            }
        }
        #endregion

        #region SelectPractice - 주문 연습 오른쪽: 제목을 넣고 그리기를 처음부터 받음. 항목이면 그 그림을 흐리게 깔고 안내 글을 숨김, 없으면 자유 그리기 + 안내 글
        /// <param name="entry">따라 그릴 항목 (자유 그리기면 null)</param>
        void SelectPractice(PageEntry entry)
        {
            if (entry == null) SetText(_view.PracticeTitle, Table, FreeDrawKey);
            else if (entry.Rune != null) SetText(_view.PracticeTitle, Table, RuneSpellKey, entry.Rune.Name.GetLocalizedString());
            else SetText(_view.PracticeTitle, entry.Spell.Name);
            _view.Draw.Begin(_codex, OnJudged);
            _view.Draw.ShowGuide(entry == null ? null : entry.Rune != null ? entry.Rune.Shape : entry.Spell.Shape);
        }
        #endregion

        #region OnJudged - 판정 뒤: 판정을 알림 (상태가 소리·튜토리얼). 코덱스 캡슐이 생략 없이 시전한 미등록 다중 룬 마법을 등록하면 (룬 주문은 등록 없음) 장·목록을 다시 만듦
        /// <param name="spec">판정 결과</param>
        void OnJudged(SpellJudgeSpec spec)
        {
            if (_codex == null) return;
            PracticeJudged?.Invoke(spec);
            if (!_codex.TryRegister(spec)) return;
            BuildPages();
            if (_tab == Tab.Practice) RebuildList();
        }
        #endregion

        #region BuildPages - 장 목록을 다시 만듦 (코덱스 캡슐의 목록): 룬 주문(익힌 룬, 룬 순서), 다중 룬 마법(등록한 마법, 룬 수 순서·묶음마다 왼쪽 장에서 시작). 둘 다 짝수 개로
        void BuildPages()
        {
            _runePages.Clear();
            foreach (RuneSO rune in _codex.LearnedRunes()) _runePages.Add(new PageEntry { Rune = rune });
            PadPages(_runePages);
            _magicPages.Clear();
            _magicStart.Clear();
            // 등록한 다중 룬 마법 (DataManager 순서)
            SpellSO[] collected = _codex.CollectedSpells();
            for (int n = 1; n <= MaxRuneCount; n++)
            {
                // 이 룬 수의 등록한 마법
                List<SpellSO> group = collected.Where(spell => spell.Parts.Count == n).ToList();
                if (group.Count == 0) continue;
                if (_magicPages.Count % 2 == 1) _magicPages.Add(null);
                _magicStart[n] = _magicPages.Count;
                foreach (SpellSO spell in group) _magicPages.Add(new PageEntry { Spell = spell, RuneCount = n });
            }
            PadPages(_magicPages);
            if (_spread >= CurrentPages().Count / 2) _spread = 0;
        }
        #endregion

        #region PadPages - 빈 장을 붙여 짝수 개로 (비었으면 빈 장 둘)
        /// <param name="pages">장 목록</param>
        static void PadPages(List<PageEntry> pages)
        {
            if (pages.Count == 0) pages.Add(null);
            if (pages.Count % 2 == 1) pages.Add(null);
        }
        #endregion

        #region CreateItem - 목록 항목 하나를 만들고 버튼을 연결
        /// <param name="prefab">항목 프리팹</param>
        /// <param name="parent">넣을 자리</param>
        /// <param name="items">만든 항목을 넣을 목록</param>
        /// <param name="onClick">누르면 할 일</param>
        static TutorialListItemView CreateItem(TutorialListItemView prefab, RectTransform parent, List<TutorialListItemView> items, Action onClick)
        {
            // 만든 항목
            TutorialListItemView item = Instantiate(prefab, parent);
            item.Button.onClick.AddListener(() => onClick());
            items.Add(item);
            return item;
        }
        #endregion

        #region ClearItems - 만든 항목을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        /// <param name="items">지울 항목 목록</param>
        static void ClearItems(List<TutorialListItemView> items)
        {
            foreach (TutorialListItemView item in items)
                if (item != null) Destroy(item.gameObject);
            items.Clear();
        }
        #endregion

        #region RuneNames - 마법 그림 부분 룬 이름 목록 (지금 언어)
        /// <param name="spell">다중 룬 마법</param>
        static string RuneNames(SpellSO spell) =>
            TextListCalculator.Join(spell.Parts.Select(part => part.Rune != null ? part.Rune.Name.GetLocalizedString() : null));
        #endregion

        #region SetText - 글자에 테이블 키·자리 값을 넣고 다시 읽음
        /// <param name="text">넣을 글자</param>
        /// <param name="table">문구 테이블</param>
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값 (없으면 비움)</param>
        static void SetText(LocalizeStringEvent text, TableReference table, string key, params object[] args)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.Arguments = args != null && args.Length > 0 ? args : null;
            reference.SetReference(table, key);
            text.RefreshString();
        }
        #endregion

        #region SetText - 글자에 SO의 LocalizedString 테이블·키를 넣음 (SO 쪽은 읽기만, 자리 값 비움)
        /// <param name="text">넣을 글자</param>
        /// <param name="source">SO의 이름·설명</param>
        static void SetText(LocalizeStringEvent text, LocalizedString source)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.Arguments = null;
            reference.SetReference(source.TableReference, source.TableEntryReference);
            text.RefreshString();
        }
        #endregion
    }
}
