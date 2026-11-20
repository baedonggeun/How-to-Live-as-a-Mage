using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>튜토리얼 탭 ← 배운 튜토리얼 (이 PC). 탭이 켜질 때 목록을 다시 만들고, 스포일러 토글을 켜면 안 배운 것까지 다 보임. 항목을 누르면 제목·설명</summary>
    public sealed class TutorialListUIBinder : MonoBehaviour
    {
        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>지금 만든 목록 항목</summary>
        readonly List<TutorialListItemView> _items = new();

        #region OnEnable - 토글 구독, 목록 만들기
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] TutorialListUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            _view.SpoilerToggle.onValueChanged.AddListener(OnSpoilerChanged);
            Rebuild();
        }
        #endregion

        #region OnDisable - 토글 구독 해제
        void OnDisable()
        {
            if (_view == null) return;
            _view.SpoilerToggle.onValueChanged.RemoveListener(OnSpoilerChanged);
        }
        #endregion

        #region OnSpoilerChanged - 모두 보기를 켜고 끄면 목록을 다시 만듦
        /// <param name="isOn">켜졌는지 (안 씀, 토글 값을 다시 읽음)</param>
        void OnSpoilerChanged(bool isOn) => Rebuild();
        #endregion

        #region Rebuild - 목록 항목을 지우고 DataManager 순서대로 다시 만듦 (토글이 꺼져 있으면 배운 것만). 첫 항목 설명을 보임
        void Rebuild()
        {
            foreach (TutorialListItemView item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();

            // 안 배운 것까지 다 보일지
            bool showAll = _view.SpoilerToggle.isOn;
            // 목록 첫 항목 (설명을 처음 보일 것. 없으면 null)
            TutorialSO first = null;
            foreach (TutorialSO tutorial in DataManager.Instance.Tutorials)
            {
                if (tutorial == null) continue;
                if (!showAll && !TutorialManager.Instance.IsLearned(tutorial.Id)) continue;
                // 이 튜토리얼 항목
                TutorialListItemView item = Instantiate(_view.TutorialItemPrefab, _view.TutorialList);
                item.Title.StringReference.SetReference(tutorial.Title.TableReference, tutorial.Title.TableEntryReference);
                // 버튼이 누를 때 쓸 튜토리얼 (반복 변수를 붙잡지 않게)
                TutorialSO picked = tutorial;
                item.Button.onClick.AddListener(() => ShowDetail(picked));
                _items.Add(item);
                if (first == null) first = tutorial;
            }
            _view.TutorialEmpty.SetActive(first == null);
            _view.TutorialDetail.SetActive(first != null);
            if (first != null) ShowDetail(first);
        }
        #endregion

        #region ShowDetail - 고른 튜토리얼의 제목·설명 키를 넣음
        /// <param name="tutorial">고른 튜토리얼</param>
        void ShowDetail(TutorialSO tutorial)
        {
            _view.TutorialTitle.StringReference.SetReference(tutorial.Title.TableReference, tutorial.Title.TableEntryReference);
            _view.TutorialBody.StringReference.SetReference(tutorial.Body.TableReference, tutorial.Body.TableEntryReference);
        }
        #endregion
    }
}
