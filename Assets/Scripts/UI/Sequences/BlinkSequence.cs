using UnityEngine;

namespace Mage
{
    /// <summary>켜져 있는 동안 CanvasGroup 투명도를 깜빡임 ('아무 키나 눌러 계속' 같은 안내). 꺼지면 안 보이게 둠</summary>
    public sealed class BlinkSequence : MonoBehaviour
    {
        /// <summary>깜빡일 묶음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>밝음 → 어두움 → 밝음 한 번 걸리는 시간 (초)</summary>
        [SerializeField, Min(0.1f)] float _period = 1.2f;

        /// <summary>가장 어두울 때 투명도</summary>
        [SerializeField, Range(0f, 1f)] float _minAlpha = 0.2f;

        /// <summary>켜진 시각 (실제 시간). 켤 때마다 밝게 시작하려고</summary>
        float _startTime;

        #region Awake - 묶음 연결 확인 (컴포넌트가 꺼져 있어도 오브젝트가 켜지면 불림)
        void Awake()
        {
            if (_group == null) Debug.LogError($"[Blink] {name}에 CanvasGroup 연결 안 됨", this);
        }
        #endregion

        #region OnEnable - 밝게 시작
        void OnEnable()
        {
            _startTime = Time.unscaledTime;
            if (_group != null) _group.alpha = 1f;
        }
        #endregion

        #region Update - 시간에 따라 투명도를 오르내림 (일시정지 중에도 깜빡이게 실제 시간)
        void Update()
        {
            if (_group == null) return;
            // 0(밝음) → 1(어두움) → 0을 오가는 값. 한 번 오가는 데 _period초
            float t = Mathf.PingPong((Time.unscaledTime - _startTime) * 2f / _period, 1f);
            _group.alpha = Mathf.Lerp(1f, _minAlpha, t);
        }
        #endregion

        #region OnDisable - 안 보이게
        void OnDisable()
        {
            if (_group != null) _group.alpha = 0f;
        }
        #endregion
    }
}
