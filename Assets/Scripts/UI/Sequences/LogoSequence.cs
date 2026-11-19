using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>게임을 켤 때 로고 연출: 로고마다 나타남 → 머묾 → 사라짐을 차례로 (엔진 → 회사. Boot CoverCanvas/Logo에 붙음). 검은 바탕은 그대로 둠. Skip이 오면 남은 로고 전부 끝</summary>
    public sealed class LogoSequence : MonoBehaviour
    {
        /// <summary>차례로 보일 로고 그림 묶음 (엔진 → 회사). 투명도로 나타나고 사라짐 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup[] _logos;

        /// <summary>로고 하나가 나타나는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeInSeconds = 0.6f;

        /// <summary>로고 하나가 다 나타난 채 머무는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _holdSeconds = 1.2f;

        /// <summary>로고 하나가 사라지는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeOutSeconds = 0.6f;

        /// <summary>넘기기 요청이 왔는지. 켜지면 남은 나타남·머묾·사라짐이 바로 끝남</summary>
        bool _skipped;

        #region Play - 로고마다 나타남 → 머묾 → 사라짐. 끝나면 로고는 다 안 보이고 바탕만 남음. Skip이 오면 다음 프레임에 끝남
        public IEnumerator Play()
        {
            if (_logos == null || _logos.Length == 0)
            {
                Debug.LogError("[Logo] 로고 CanvasGroup 연결 안 됨", this);
                yield break;
            }
            _skipped = false;
            foreach (CanvasGroup logo in _logos)
            {
                if (logo == null)
                {
                    Debug.LogError("[Logo] 로고 목록에 빈 칸 있음", this);
                    continue;
                }
                yield return Fade(logo, 0f, 1f, _fadeInSeconds);
                // elapsed: 다 나타난 뒤 지난 시간 (초). 넘기면 바로 끝
                for (float elapsed = 0f; elapsed < _holdSeconds && !_skipped; elapsed += Time.unscaledDeltaTime)
                {
                    yield return null;
                }
                yield return Fade(logo, 1f, 0f, _fadeOutSeconds);
            }
        }
        #endregion

        #region Skip - 남은 로고 연출을 끊음 (로고 화면은 TransitionManager가 끔)
        public void Skip() => _skipped = true;
        #endregion

        #region Fade - 로고 투명도를 시간에 맞춰 바꿈 (실제 시간 기준). 넘기면 바로 안 보이게 하고 끝
        /// <param name="logo">바꿀 로고</param>
        /// <param name="from">시작 투명도</param>
        /// <param name="to">끝 투명도</param>
        /// <param name="seconds">걸리는 시간 (초). 0이면 바로 끝 값</param>
        IEnumerator Fade(CanvasGroup logo, float from, float to, float seconds)
        {
            // elapsed: 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < seconds && !_skipped; elapsed += Time.unscaledDeltaTime)
            {
                logo.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }
            logo.alpha = _skipped ? 0f : to;
        }
        #endregion
    }
}
