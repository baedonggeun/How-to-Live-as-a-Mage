using System;
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>화면을 잠깐 검게 덮었다가 걷는 연출 (Boot 씬 UICanvas/Fade). 다 덮였을 때 할 일을 부르고, 걷힌 뒤 끝 알림. 실제 시간이라 일시정지 중에도 돎</summary>
    public sealed class FadeSequence : MonoBehaviour
    {
        /// <summary>검은 화면 투명도 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>덮거나 걷는 데 걸리는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _halfSeconds = 0.25f;

        /// <summary>지금 도는 연출. 없으면 null</summary>
        Coroutine _routine;

        #region Awake - 연결 확인, 처음엔 투명·클릭 통과
        void Awake()
        {
            if (_group == null)
            {
                Debug.LogError("[Fade] CanvasGroup 연결 안 됨", this);
                return;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }
        #endregion

        #region Play - 덮기 → onBlack → 걷기 → onDone. 연결이 없거나 이미 도는 중이면 안 틀고 false
        /// <param name="onBlack">다 덮였을 때 할 일 (없으면 null)</param>
        /// <param name="onDone">다 걷힌 뒤 할 일 (없으면 null)</param>
        public bool Play(Action onBlack, Action onDone)
        {
            if (_group == null || _routine != null) return false;
            _routine = StartCoroutine(Run(onBlack, onDone));
            return true;
        }
        #endregion

        #region Run - 덮는 동안 클릭을 막고, 덮인 뒤 할 일, 걷은 뒤 끝 알림
        /// <param name="onBlack">다 덮였을 때 할 일</param>
        /// <param name="onDone">다 걷힌 뒤 할 일</param>
        IEnumerator Run(Action onBlack, Action onDone)
        {
            _group.blocksRaycasts = true;
            yield return FadeTo(1f);
            onBlack?.Invoke();
            yield return FadeTo(0f);
            _group.blocksRaycasts = false;
            _routine = null;
            onDone?.Invoke();
        }
        #endregion

        #region FadeTo - 투명도를 목표까지 정해진 시간 동안 바꿈 (실제 시간)
        /// <param name="target">목표 투명도 (0~1)</param>
        IEnumerator FadeTo(float target)
        {
            // 시작 투명도
            float start = _group.alpha;
            // 지난 시간 (초)
            float elapsed = 0f;
            while (elapsed < _halfSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, elapsed / _halfSeconds);
                yield return null;
            }
            _group.alpha = target;
        }
        #endregion
    }
}
