using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>임무 게시판 창: 의뢰 이름·설명·기간과 받기 버튼. 받으면 상태가 준 할 일을 부르고 버튼을 '받음'으로. 의뢰 데이터는 상태가 넘김. 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class QuestBoardController : MonoBehaviour
    {
        /// <summary>받기 버튼 글자 키</summary>
        const string TakeKey = "hub.board.take";

        /// <summary>받은 뒤 버튼 글자 키</summary>
        const string TakenKey = "hub.board.taken";

        /// <summary>기간 글자 키 ({0} 일 수)</summary>
        const string DaysKey = "hub.board.days";

        /// <summary>게시판 창 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] QuestBoardView _view;

        /// <summary>받기를 누르면 할 일 (상태가 판 데이터에 의뢰를 넣음). 창이 닫혀 있으면 null</summary>
        Action _onTake;

        /// <summary>같은 창의 참조</summary>
        public QuestBoardView View => _view;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Awake - 연결 확인, 받기 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Board] QuestBoardView 연결 안 됨", this);
                return;
            }
            _view.TakeButton.onClick.AddListener(OnTakeClicked);
        }
        #endregion

        #region Show - 의뢰 이름·설명·기간을 넣고 켬. 이미 받았으면 버튼을 '받음'으로
        /// <param name="quest">보일 의뢰</param>
        /// <param name="taken">이미 받았는지</param>
        /// <param name="onTake">받기를 누르면 할 일</param>
        public void Show(QuestSO quest, bool taken, Action onTake)
        {
            if (_view == null)
            {
                Debug.LogError("[Board] QuestBoardView 연결 안 됨", this);
                return;
            }
            if (quest == null) throw new ArgumentNullException(nameof(quest));
            _onTake = onTake ?? throw new ArgumentNullException(nameof(onTake));
            _view.QuestTitle.StringReference.SetReference(quest.Title.TableReference, quest.Title.TableEntryReference);
            _view.QuestDescription.StringReference.SetReference(quest.Description.TableReference, quest.Description.TableEntryReference);
            // 기간 글자의 String Reference
            var days = _view.QuestDays.StringReference;
            days.SetReference(Table, DaysKey);
            days.Arguments = new object[] { quest.Days };
            gameObject.SetActive(true);
            _view.QuestDays.RefreshString();
            SetTaken(taken);
        }
        #endregion

        #region Hide - 창을 끄고 할 일을 비움
        public void Hide()
        {
            _onTake = null;
            gameObject.SetActive(false);
        }
        #endregion

        #region OnTakeClicked - 받기: 할 일을 부르고 버튼을 '받음'으로
        void OnTakeClicked()
        {
            _onTake?.Invoke();
            SetTaken(true);
        }
        #endregion

        #region SetTaken - 받았으면 버튼을 못 누르게 하고 글자를 '받음'으로
        /// <param name="taken">받았는지</param>
        void SetTaken(bool taken)
        {
            _view.TakeButton.interactable = !taken;
            _view.TakeLabel.StringReference.SetReference(Table, taken ? TakenKey : TakeKey);
        }
        #endregion
    }
}
