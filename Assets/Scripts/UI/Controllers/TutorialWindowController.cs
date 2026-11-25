using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>설명창: 튜토리얼의 장을 하나씩 보임 (다음·닫기, 장이 없으면 튜토리얼 설명 한 장). 닫히면 받은 할 일을 부름. 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class TutorialWindowController : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] TutorialWindowView _view;

        /// <summary>장마다 본문 (Show에서 받음)</summary>
        IReadOnlyList<LocalizedString> _pages;

        /// <summary>장이 없을 때 본문 (튜토리얼 설명)</summary>
        LocalizedString _fallback;

        /// <summary>지금 장 (0부터)</summary>
        int _index;

        /// <summary>닫히면 할 일 (창이 닫혀 있으면 null)</summary>
        Action _onClosed;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>닫기 버튼을 누름 (UIManager가 받아 창을 닫음)</summary>
        public event Action CloseClicked;

        #region Awake - 연결 확인, 다음 버튼·닫기 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[TutorialWindow] TutorialWindowView 연결 안 됨", this);
                return;
            }
            _view.NextButton.onClick.AddListener(Next);
            _view.CloseButton.onClick.AddListener(() => CloseClicked?.Invoke());
        }
        #endregion

        #region Show - 튜토리얼 제목·첫 장을 넣고 켬
        /// <param name="tutorial">보일 튜토리얼</param>
        /// <param name="onClosed">닫히면 할 일 (없으면 null)</param>
        public void Show(TutorialSO tutorial, Action onClosed)
        {
            if (tutorial == null) throw new ArgumentNullException(nameof(tutorial));
            if (_view == null)
            {
                Debug.LogError("[TutorialWindow] TutorialWindowView 연결 안 됨", this);
                onClosed?.Invoke();
                return;
            }
            _pages = tutorial.Pages;
            _fallback = tutorial.Body;
            _index = 0;
            _onClosed = onClosed;
            _view.Title.StringReference.SetReference(tutorial.Title.TableReference, tutorial.Title.TableEntryReference);
            gameObject.SetActive(true);
            ShowPage();
        }
        #endregion

        #region Hide - 창을 끄고 할 일을 꺼내 비운 뒤 부름 (닫혀 있으면 안 함)
        public void Hide()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            // 닫히면 할 일 (꺼내고 비운 뒤 실행)
            Action done = _onClosed;
            _onClosed = null;
            done?.Invoke();
        }
        #endregion

        #region Next - 마지막 장이 아니면 다음 장
        void Next()
        {
            if (_pages == null || _index >= _pages.Count - 1) return;
            _index++;
            ShowPage();
        }
        #endregion

        #region ShowPage - 지금 장 본문·"지금 / 장 수"를 넣고, 마지막 장이면 다음 버튼을 숨김
        void ShowPage()
        {
            // 장 수 (장이 없으면 설명 한 장)
            int count = Mathf.Max(1, _pages.Count);
            // 보일 본문
            LocalizedString body = _pages.Count > 0 ? _pages[_index] : _fallback;
            _view.Body.StringReference.SetReference(body.TableReference, body.TableEntryReference);
            _view.Page.StringReference.Arguments = new object[] { _index + 1, count };
            _view.Page.RefreshString();
            _view.NextButton.gameObject.SetActive(_index < count - 1);
        }
        #endregion
    }
}
