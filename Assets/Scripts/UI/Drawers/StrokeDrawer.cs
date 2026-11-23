using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>획(점 줄)을 굵은 선 메시로 그리는 UI 그래픽 (로직 없음). 점을 이 그래픽 좌표 그대로 그리거나(그린 획), 칸에 맞춰 그림(주문 그림)</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StrokeDrawer : MaskableGraphic
    {
        /// <summary>선 굵기 (이 그래픽 단위)</summary>
        [SerializeField, Min(0.5f)] float _thickness = 6f;

        /// <summary>칸에 맞춰 그릴 때 둘레 여백 (칸 짧은 변 대비)</summary>
        [SerializeField, Range(0f, 0.45f)] float _fitPadding = 0.1f;

        /// <summary>그릴 획 (점 복사본)</summary>
        readonly List<Vector2[]> _strokes = new();

        /// <summary>칸에 맞춰 그릴지 (false면 점이 이 그래픽 좌표 그대로)</summary>
        bool _fit;

        #region SetStrokes - 그릴 획을 바꾸고 다시 그림 (점은 복사, 빈 획은 뺌)
        /// <param name="strokes">획 → 점 (null이면 지움)</param>
        /// <param name="fit">칸에 맞춰 그릴지 (주문 그림 좌표면 true, 그린 획이면 false)</param>
        public void SetStrokes(IEnumerable<IReadOnlyList<Vector2>> strokes, bool fit)
        {
            _strokes.Clear();
            if (strokes != null)
                foreach (IReadOnlyList<Vector2> stroke in strokes)
                {
                    if (stroke == null || stroke.Count == 0) continue;
                    // 이 획 점 복사본
                    var points = new Vector2[stroke.Count];
                    for (int i = 0; i < points.Length; i++) points[i] = stroke[i];
                    _strokes.Add(points);
                }
            _fit = fit;
            SetVerticesDirty();
        }
        #endregion

        #region Clear - 획을 다 지움
        public void Clear()
        {
            _strokes.Clear();
            SetVerticesDirty();
        }
        #endregion

        #region OnPopulateMesh - 선분마다 굵기만큼 사각형, 점마다 굵기만 한 사각형으로 이음매·끝을 채움
        /// <param name="vh">메시를 쌓을 곳</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_strokes.Count == 0) return;
            GetMapping(out float scale, out Vector2 offset);
            // 굵기 절반
            float half = _thickness * 0.5f;
            foreach (Vector2[] stroke in _strokes)
                for (int i = 0; i < stroke.Length; i++)
                {
                    // 이 점 (그래픽 좌표)
                    Vector2 p = stroke[i] * scale + offset;
                    AddQuad(vh, p + new Vector2(-half, -half), p + new Vector2(-half, half), p + new Vector2(half, half), p + new Vector2(half, -half));
                    if (i == 0) continue;
                    // 앞 점 (그래픽 좌표)
                    Vector2 a = stroke[i - 1] * scale + offset;
                    // 앞 점에서 이 점으로
                    Vector2 direction = p - a;
                    if (direction.sqrMagnitude < 1e-6f) continue;
                    // 선분에 수직인 굵기 절반
                    Vector2 normal = new Vector2(-direction.y, direction.x).normalized * half;
                    AddQuad(vh, a - normal, a + normal, p + normal, p - normal);
                }
        }
        #endregion

        #region GetMapping - 맞춤이면 모든 점의 테두리 상자를 칸 가운데에 비율을 지켜 맞추는 배율·이동, 아니면 그대로
        /// <param name="scale">배율</param>
        /// <param name="offset">이동</param>
        void GetMapping(out float scale, out Vector2 offset)
        {
            scale = 1f;
            offset = Vector2.zero;
            if (!_fit) return;
            // 모든 점의 테두리
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector2[] stroke in _strokes)
                foreach (Vector2 p in stroke)
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            // 이 그래픽 칸
            Rect rect = rectTransform.rect;
            // 둘레 여백
            float pad = Mathf.Min(rect.width, rect.height) * _fitPadding;
            // 그림 크기 (얇은 그림은 아주 작은 값)
            Vector2 size = Vector2.Max(max - min, new Vector2(1e-4f, 1e-4f));
            scale = Mathf.Min((rect.width - 2f * pad) / size.x, (rect.height - 2f * pad) / size.y);
            offset = rect.center - (min + max) * 0.5f * scale;
        }
        #endregion

        #region AddQuad - 사각형 하나 (꼭짓점 4개, 삼각형 2개, 그래픽 색)
        /// <param name="vh">메시를 쌓을 곳</param>
        /// <param name="a">꼭짓점</param>
        /// <param name="b">꼭짓점</param>
        /// <param name="c">꼭짓점</param>
        /// <param name="d">꼭짓점</param>
        void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // 첫 꼭짓점 번호
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
        #endregion
    }
}
