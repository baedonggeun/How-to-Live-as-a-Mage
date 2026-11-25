using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>확인창: 문구를 넣고 켜서 확인을 누르면 창을 먼저 닫고 받은 할 일을 부름(할 일이 다른 창을 열어도 안 꼬이게), 취소면 그냥 닫음. 언제 띄울지·Esc 닫기는 UIManager가 정함 (Confirm 오브젝트에 붙음, 그 오브젝트를 켜고 끔)</summary>
    public sealed class ConfirmController : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] ConfirmView _view;

        /// <summary>확인을 누르면 할 일. 창이 닫혀 있으면 null</summary>
        Action _onConfirm;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Awake - 연결 확인, 확인·취소 버튼 연결 (처음 켜질 때 한 번)
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Confirm] ConfirmView 연결 안 됨", this);
                return;
            }
            _view.ConfirmButton.onClick.AddListener(OnConfirmClicked);
            _view.CancelButton.onClick.AddListener(Hide);
        }
        #endregion

        #region Show - 지정한 테이블의 문구로 창을 켬. 확인을 누르면 onConfirm, 취소·Esc면 그냥 닫힘 (빈 테이블·키는 무시)
        /// <param name="table">문구가 든 String Table</param>
        /// <param name="messageKey">문구 키</param>
        /// <param name="onConfirm">확인을 눌렀을 때 할 일</param>
        public void Show(TableReference table, string messageKey, Action onConfirm)
        {
            if (onConfirm == null) throw new ArgumentNullException(nameof(onConfirm));
            if (table.ReferenceType == TableReference.Type.Empty || string.IsNullOrWhiteSpace(messageKey))
            {
                Debug.LogWarning("[Confirm] 빈 테이블·키 확인창 무시");
                return;
            }
            if (_view == null)
            {
                Debug.LogError("[Confirm] ConfirmView 연결 안 됨", this);
                return;
            }
            _view.MessageText.StringReference.SetReference(table, messageKey);
            _onConfirm = onConfirm;
            gameObject.SetActive(true);
            _view.MessageText.RefreshString();
        }
        #endregion

        #region Hide - 할 일을 비우고 창을 끔 (취소 버튼·Esc)
        public void Hide()
        {
            _onConfirm = null;
            gameObject.SetActive(false);
        }
        #endregion

        #region OnConfirmClicked - 할 일을 꺼내 두고 창을 닫은 뒤 실행 (할 일이 다른 창을 열어도 안 꼬이게)
        void OnConfirmClicked()
        {
            // 이번 확인으로 할 일
            Action action = _onConfirm;
            Hide();
            action?.Invoke();
        }
        #endregion
    }
}
