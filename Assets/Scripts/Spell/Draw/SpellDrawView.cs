using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>주문 그리기 칸 참조 (그리기 칸 루트. 숲 HUD와 책상 코덱스 연습이 같이 씀)</summary>
    public sealed class SpellDrawView : MonoBehaviour
    {
        /// <summary>따라 그릴 주문 그림 (흐리게, 칸에 맞춤. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _guide;

        /// <summary>그린 획 (칸 좌표 그대로. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _ink;

        /// <summary>펜 점 (시전 상태에만 켬. 칸 가운데 기준. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _pen;

        /// <summary>양피지 그림판 (시전 상태에만 켬. 칸을 꽉 채움. 안 보이게 두려면 비움. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _board;

        /// <summary>판정 결과 줄 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _result;

        /// <summary>결과 자세히: 부분 일치율·가장 가까운 주문 (키는 컨트롤러가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _detail;

        /// <summary>그리기 전 안내 글 (Cast/cast.hint. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _hint;

        /// <summary>따라 그릴 주문 그림</summary>
        public StrokeDrawer Guide => _guide;

        /// <summary>그린 획</summary>
        public StrokeDrawer Ink => _ink;

        /// <summary>펜 점</summary>
        public RectTransform Pen => _pen;

        /// <summary>양피지 그림판 (없으면 null)</summary>
        public GameObject Board => _board;

        /// <summary>판정 결과 줄</summary>
        public LocalizeStringEvent Result => _result;

        /// <summary>결과 자세히</summary>
        public LocalizeStringEvent Detail => _detail;

        /// <summary>그리기 전 안내 글</summary>
        public LocalizeStringEvent Hint => _hint;
    }
}
