using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>타이틀 시작 연출: '클릭하여 계속' 화면 → 화면이 아래로 내려감 → 제목·메뉴가 나타남 (Title Canvas에 붙음)</summary>
    public sealed class TitleIntroSequence : MonoBehaviour
    {
        /// <summary>위로 밀릴 화면 묶음 (Sky·ClickPrompt, 두 화면 높이). 위로 밀려서 화면이 아래로 내려가 보임 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _pan;

        /// <summary>'클릭하여 계속' 깜빡임. Play가 오면 끔 (인스펙터에서만 연결)</summary>
        [SerializeField] BlinkSequence _prompt;

        /// <summary>제목·메뉴 묶음. 화면이 다 내려간 뒤 나타나고 그때부터 누를 수 있음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _main;

        /// <summary>화면이 내려가는 거리 (CanvasScaler 기준 해상도 단위. 1080 = 한 화면)</summary>
        [SerializeField, Min(0f)] float _panDistance = 1080f;

        /// <summary>화면이 내려가는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _panSeconds = 1.2f;

        /// <summary>제목·메뉴가 나타나는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _fadeInSeconds = 0.6f;

        /// <summary>화면 묶음의 처음 위치 ('클릭하여 계속' 화면)</summary>
        Vector2 _panStart;

        #region Awake - 연결 확인, 처음 상태로: 화면 위, 안내 깜빡임, 제목·메뉴 숨김·못 누름
        void Awake()
        {
            if (_pan == null || _prompt == null || _main == null)
            {
                Debug.LogError("[TitleIntro] 화면 묶음·안내·제목 메뉴 중 연결 안 된 것 있음", this);
                return;
            }
            _panStart = _pan.anchoredPosition;
            _prompt.enabled = true;
            _main.alpha = 0f;
            _main.interactable = false;
            _main.blocksRaycasts = false;
        }
        #endregion

        #region Play - 연출 시작 (TitleState가 '클릭하여 계속'을 받으면 한 번 부름)
        public void Play()
        {
            if (_pan == null || _prompt == null || _main == null) return;
            StartCoroutine(PlayRoutine());
        }
        #endregion

        #region ShowMain - 연출 없이 바로 메뉴 화면 (게임 중 타이틀로 돌아왔을 때): 안내 끔, 화면은 내려간 끝 위치, 제목·메뉴 보이고 누를 수 있게
        public void ShowMain()
        {
            if (_pan == null || _prompt == null || _main == null) return;
            _prompt.enabled = false;
            _pan.anchoredPosition = _panStart + Vector2.up * _panDistance;
            _main.alpha = 1f;
            _main.interactable = true;
            _main.blocksRaycasts = true;
        }
        #endregion

        #region PlayRoutine - 안내 끔 → 화면 내림(천천히 출발·도착) → 제목·메뉴 나타남 → 누를 수 있게 (실제 시간 기준)
        IEnumerator PlayRoutine()
        {
            _prompt.enabled = false;
            // 화면 묶음의 끝 위치. 위로 _panDistance만큼 밀림 = 화면이 아래로 내려감
            Vector2 panEnd = _panStart + Vector2.up * _panDistance;
            // elapsed: 내려가기 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < _panSeconds; elapsed += Time.unscaledDeltaTime)
            {
                _pan.anchoredPosition = Vector2.Lerp(_panStart, panEnd, Mathf.SmoothStep(0f, 1f, elapsed / _panSeconds));
                yield return null;
            }
            _pan.anchoredPosition = panEnd;
            // elapsed: 나타나기 시작부터 지난 시간 (초)
            for (float elapsed = 0f; elapsed < _fadeInSeconds; elapsed += Time.unscaledDeltaTime)
            {
                _main.alpha = elapsed / _fadeInSeconds;
                yield return null;
            }
            _main.alpha = 1f;
            _main.interactable = true;
            _main.blocksRaycasts = true;
        }
        #endregion
    }
}
