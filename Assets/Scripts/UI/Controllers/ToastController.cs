using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>토스트: 문구를 화면 아래에 잠깐 띄우고 실제 시간 2초 뒤 숨김 (일시정지 중에도 흐름). 이미 떠 있으면 문구를 바꾸고 시간을 다시 셈. 언제 띄울지는 UIManager가 정함 (Toast 오브젝트에 붙음, 숨기면 그 오브젝트가 꺼져서 시간도 멈춤)</summary>
    public sealed class ToastController : MonoBehaviour
    {
        /// <summary>토스트 표시 시간 (초, 실제 시간)</summary>
        const float ShowSeconds = 2f;

        /// <summary>같은 토스트의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] ToastView _view;

        /// <summary>숨길 때까지 남은 실제 시간 (초). 숨겨져 있으면 0</summary>
        float _left;

        #region Show - 지정한 테이블의 문구를 넣고 켠 뒤 시간을 다시 셈 (빈 테이블·키는 무시)
        /// <param name="table">문구가 든 String Table</param>
        /// <param name="key">문구 키</param>
        /// <param name="args">문구의 {0}, {1}… 자리에 넣을 값</param>
        public void Show(TableReference table, string key, object[] args)
        {
            if (table.ReferenceType == TableReference.Type.Empty || string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("[Toast] 빈 테이블·키 토스트 무시");
                return;
            }
            if (_view == null)
            {
                Debug.LogError("[Toast] ToastView 연결 안 됨", this);
                return;
            }
            _view.Root.SetActive(true);
            // 토스트 글자의 String Reference (테이블·키를 부를 때마다 넣음)
            var text = _view.Text.StringReference;
            text.SetReference(table, key);
            text.Arguments = args;
            _view.Text.RefreshString();
            _left = ShowSeconds;
        }
        #endregion

        #region Hide - 바로 숨김 (UIManager가 처음 준비할 때, 시간이 다 됐을 때)
        public void Hide()
        {
            _left = 0f;
            if (_view != null) _view.Root.SetActive(false);
        }
        #endregion

        #region Update - 떠 있는 동안 실제 시간으로 세다 다 되면 숨김 (일시정지 중에도 흐르게)
        void Update()
        {
            if (_left <= 0f) return;
            _left -= Time.unscaledDeltaTime;
            if (_left <= 0f) Hide();
        }
        #endregion
    }
}
