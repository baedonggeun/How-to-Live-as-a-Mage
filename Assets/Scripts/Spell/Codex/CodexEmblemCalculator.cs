using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mage
{
    /// <summary>코덱스 표지 마법진 그림 (static pure function): 원 셋(바깥·안쪽·가운데), 안쪽 원에 닿는 육망성(세모 둘), 둘레에 룬 그림 (룬 순서로 위에서 시계 방향). 바깥 원 반지름 1 기준 좌표</summary>
    public static class CodexEmblemCalculator
    {
        /// <summary>마법진 원 점 수</summary>
        const int CirclePoints = 64;

        /// <summary>마법진 안쪽 원 반지름 (바깥 원 1 대비. 육망성이 이 원에 닿음)</summary>
        const float Inner = 0.78f;

        /// <summary>마법진 가운데 원 반지름</summary>
        const float Center = 0.3f;

        /// <summary>마법진 둘레 룬 그림 자리 반지름</summary>
        const float GlyphRadius = 0.89f;

        /// <summary>마법진 둘레 룬 그림 크기 (룬 그림 좌표 1 대비)</summary>
        const float GlyphScale = 0.07f;

        #region Build - 마법진 획: 원 셋, 육망성, 둘레 룬 그림
        /// <param name="runes">둘레에 그릴 룬 (빈 칸은 건너뜀, null이면 룬 없이)</param>
        public static List<Vector2[]> Build(IReadOnlyList<RuneSO> runes)
        {
            // 마법진 획
            var strokes = new List<Vector2[]> { Circle(1f), Circle(Inner), Circle(Center) };
            for (int t = 0; t < 2; t++)
            {
                // 세모 하나 (꼭짓점 셋 + 처음 점)
                var triangle = new Vector2[4];
                for (int i = 0; i < 4; i++)
                {
                    // 꼭짓점 각도 (라디안)
                    float a = t * Mathf.PI + i * 2f * Mathf.PI / 3f;
                    triangle[i] = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Inner;
                }
                strokes.Add(triangle);
            }
            if (runes == null) return strokes;
            // 그릴 룬
            List<RuneSO> drawn = runes.Where(rune => rune != null).ToList();
            for (int i = 0; i < drawn.Count; i++)
            {
                // 이 룬 그림 자리
                float a = i * 2f * Mathf.PI / drawn.Count;
                Vector2 center = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * GlyphRadius;
                foreach (StrokeEntry stroke in drawn[i].Strokes)
                    strokes.Add(stroke.Points.Select(p => center + p * GlyphScale).ToArray());
            }
            return strokes;
        }
        #endregion

        #region Circle - 원점 중심 원 (위에서 시작해 시계 방향, CirclePoints칸)
        /// <param name="radius">반지름</param>
        static Vector2[] Circle(float radius)
        {
            // 원 점
            var points = new Vector2[CirclePoints + 1];
            for (int i = 0; i <= CirclePoints; i++)
            {
                // 각도 (라디안)
                float a = i * 2f * Mathf.PI / CirclePoints;
                points[i] = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
            }
            return points;
        }
        #endregion
    }
}
