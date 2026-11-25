using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>메뉴 창 탭</summary>
    public enum GameMenuTab
    {
        /// <summary>지도 (M)</summary>
        Map,

        /// <summary>임무 일지 (J)</summary>
        Journal,

        /// <summary>튜토리얼</summary>
        Tutorial,

        /// <summary>주문 도감</summary>
        Spells,
    }

    /// <summary>메뉴 창 탭 바꾸기: 탭 버튼을 누르면 그 페이지만 켜고 그 탭 버튼은 못 누르게. 닫기 버튼은 CloseClicked로 알림 (여닫기·일시정지는 UIManager)</summary>
    public sealed class GameMenuController : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>닫기 버튼을 누름 (UIManager가 받아 창을 닫음)</summary>
        public event Action CloseClicked;

        /// <summary>지금 보이는 탭</summary>
        public GameMenuTab CurrentTab { get; private set; }

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Awake - 연결 확인, 탭 버튼·닫기 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] GameMenuView 연결 안 됨", this);
                return;
            }
            _view.CloseButton.onClick.AddListener(() => CloseClicked?.Invoke());
            _view.MapTab.onClick.AddListener(() => ShowTab(GameMenuTab.Map));
            _view.JournalTab.onClick.AddListener(() => ShowTab(GameMenuTab.Journal));
            _view.TutorialTab.onClick.AddListener(() => ShowTab(GameMenuTab.Tutorial));
            _view.SpellsTab.onClick.AddListener(() => ShowTab(GameMenuTab.Spells));
        }
        #endregion

        #region ShowTab - 고른 탭 페이지만 켜고, 그 탭 버튼은 못 누르게 (지금 탭 표시)
        /// <param name="tab">보일 탭</param>
        public void ShowTab(GameMenuTab tab)
        {
            CurrentTab = tab;
            if (_view == null) return;
            SetTab(_view.MapTab, _view.MapPage, tab == GameMenuTab.Map);
            SetTab(_view.JournalTab, _view.JournalPage, tab == GameMenuTab.Journal);
            SetTab(_view.TutorialTab, _view.TutorialPage, tab == GameMenuTab.Tutorial);
            SetTab(_view.SpellsTab, _view.SpellsPage, tab == GameMenuTab.Spells);
        }
        #endregion

        #region SetTab - 탭 하나 켜고 끄기 (켜진 탭 버튼은 못 누름)
        /// <param name="button">탭 버튼</param>
        /// <param name="page">탭 페이지</param>
        /// <param name="on">이 탭을 보일지</param>
        static void SetTab(Button button, GameObject page, bool on)
        {
            button.interactable = !on;
            page.SetActive(on);
        }
        #endregion
    }
}
