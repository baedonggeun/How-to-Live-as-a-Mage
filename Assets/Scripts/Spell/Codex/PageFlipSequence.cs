using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>코덱스 장 넘김·표지 여닫기 연출: 대상의 가로 배율을 피벗(책등 쪽)을 축으로 줄이거나 늘림. 위에서 내려다보면 종이가 책등으로 접혔다 반대쪽으로 펴지는 모양. 게임 시간이라 일시정지 중엔 멈춤</summary>
    public sealed class PageFlipSequence : MonoBehaviour
    {
        /// <summary>장 한 장 넘기는 시간 (초, 접기 + 펴기)</summary>
        [SerializeField, Min(0.02f)] float _pageSeconds = 0.22f;

        /// <summary>표지를 여닫는 시간 (초, 접기 + 펴기)</summary>
        [SerializeField, Min(0.02f)] float _coverSeconds = 0.44f;

        /// <summary>장 넘김 반쪽 시간 (접기 또는 펴기, 초)</summary>
        public float PageHalf => _pageSeconds * 0.5f;

        /// <summary>표지 여닫기 반쪽 시간 (접기 또는 펴기, 초)</summary>
        public float CoverHalf => _coverSeconds * 0.5f;

        #region Fold - 대상 가로 배율을 시간 동안 from에서 to로 (처음·끝 느리게). 축은 대상의 피벗
        /// <param name="target">접거나 펼 대상 (피벗이 책등 쪽)</param>
        /// <param name="from">시작 가로 배율</param>
        /// <param name="to">끝 가로 배율</param>
        /// <param name="seconds">걸리는 시간 (초)</param>
        public IEnumerator Fold(RectTransform target, float from, float to, float seconds)
        {
            // 진행 (0~1)
            float t = 0f;
            SetWidth(target, from);
            while (t < 1f)
            {
                t = seconds > 0f ? Mathf.Min(1f, t + Time.deltaTime / seconds) : 1f;
                SetWidth(target, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
        }
        #endregion

        #region SetWidth - 대상의 가로 배율만 바꿈
        /// <param name="target">바꿀 대상</param>
        /// <param name="scale">가로 배율</param>
        public static void SetWidth(RectTransform target, float scale)
        {
            // 지금 배율
            Vector3 s = target.localScale;
            s.x = scale;
            target.localScale = s;
        }
        #endregion
    }
}
