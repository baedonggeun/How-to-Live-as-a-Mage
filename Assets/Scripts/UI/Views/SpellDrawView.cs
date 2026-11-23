using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>주문 그리기 칸 참조 (그리기 칸 루트. 칸 자체 Image가 포인터를 받음)</summary>
    public sealed class SpellDrawView : MonoBehaviour
    {
        /// <summary>따라 그릴 주문 그림 (흐리게, 칸에 맞춤. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _guide;

        /// <summary>그린 획 (칸 좌표 그대로. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _ink;

        /// <summary>판정까지 남은 시간 막대 (Filled 가로. 시전 상태에만 켬. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _timer;

        /// <summary>판정 결과 줄 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _result;

        /// <summary>결과 자세히: 부분 일치율·가장 가까운 주문 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _detail;

        /// <summary>그리기 전 안내 글 (Cast/cast.hint, {0} 대기 초. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _hint;

        /// <summary>따라 그릴 주문 그림</summary>
        public StrokeDrawer Guide => _guide;

        /// <summary>그린 획</summary>
        public StrokeDrawer Ink => _ink;

        /// <summary>판정까지 남은 시간 막대</summary>
        public Image Timer => _timer;

        /// <summary>판정 결과 줄</summary>
        public LocalizeStringEvent Result => _result;

        /// <summary>결과 자세히</summary>
        public LocalizeStringEvent Detail => _detail;

        /// <summary>그리기 전 안내 글</summary>
        public LocalizeStringEvent Hint => _hint;
    }
}
