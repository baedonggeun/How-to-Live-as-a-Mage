using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>씬을 넘어 쓰는 공용 UI 담당. 지금은 토스트, 나중에 일시정지·확인창</summary>
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>토스트 표시 시간 (초)</summary>
        const float ToastSeconds = 2f;

        /// <summary>화면 아래 잠깐 뜨는 알림 (Boot 씬 UICanvas 자식)</summary>
        [SerializeField] ToastView _toast;

        /// <summary>토스트를 숨기는 코루틴. 새 토스트가 오면 끊고 다시 시작</summary>
        Coroutine _hideToast;

        #region InitSingleton - 토스트 연결 확인, 처음엔 숨김
        protected override void InitSingleton()
        {
            if (_toast == null)
            {
                Debug.LogError("[UI] ToastView 연결 안 됨");
                return;
            }
            _toast.Root.SetActive(false);
        }
        #endregion

        #region ShowToast - String Table 키의 문구를 잠깐 띄움. 이미 떠 있으면 문구를 바꾸고 시간을 다시 셈
        /// <param name="key">UI 테이블 키</param>
        /// <param name="args">문구의 {0}, {1}… 자리에 넣을 값</param>
        public void ShowToast(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("[UI] 빈 키 토스트 무시");
                return;
            }
            if (_toast == null)
            {
                Debug.LogError("[UI] ToastView 연결 안 됨");
                return;
            }
            _toast.Root.SetActive(true);
            // 토스트 글자의 String Reference (테이블은 씬에서 UI로 정해 둠)
            var text = _toast.Text.StringReference;
            text.TableEntryReference = key;
            text.Arguments = args;
            _toast.Text.RefreshString();
            if (_hideToast != null) StopCoroutine(_hideToast);
            _hideToast = StartCoroutine(HideToastAfter(ToastSeconds));
        }
        #endregion

        #region HideToastAfter - 정해진 시간 뒤 토스트 숨김 (일시정지 중에도 흐르게 실제 시간 기준)
        /// <param name="seconds">기다릴 시간 (초)</param>
        IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _toast.Root.SetActive(false);
            _hideToast = null;
        }
        #endregion
    }
}
