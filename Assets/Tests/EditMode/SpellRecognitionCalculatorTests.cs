using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Mage.Tests
{
    /// <summary>주문 그림 인식기 판정 (기대값은 프로토타입 spell-core.js에 같은 그림을 넣어 확인한 값. 그림 데이터는 Assets/Data/Spells·Runes와 같음. 룬 그림을 더한 판정은 에디터에서 계산해 확인)</summary>
    public sealed class SpellRecognitionCalculatorTests
    {
        /// <summary>그림 배율 (주문 그림 좌표 1 = 그리기 칸 100)</summary>
        const float DrawScale = 100f;

        /// <summary>룬 id: 불</summary>
        const string Fire = "fire";

        /// <summary>룬 id: 응축</summary>
        const string Condense = "condense";

        /// <summary>룬 id: 직선</summary>
        const string Line = "line";

        /// <summary>룬 id: 차단</summary>
        const string Block = "block";

        /// <summary>룬 id: 물 (시험용)</summary>
        const string Water = "water";

        /// <summary>룬 id: 확산 (시험용)</summary>
        const string Spread = "spread";

        /// <summary>룬 id: 벽 (시험용)</summary>
        const string Wall = "wall";

        /// <summary>그리기 칸 안 그림 중심</summary>
        static readonly Vector2 DrawCenter = new(300f, 300f);

        /// <summary>룬 전부</summary>
        static readonly string[] AllRunes = { Fire, Condense, Line, Block, Water, Spread, Wall };

        /// <summary>불: 정방향 세모</summary>
        static readonly Vector2[] Tri = { new(0f, 1f), new(0.52f, 0.1f), new(-0.52f, 0.1f), new(0f, 1f) };

        /// <summary>직선: 세모 위 꼭짓점에서 내리그음</summary>
        static readonly Vector2[] Shaft = { new(0f, 1f), new(0f, -1f) };

        /// <summary>응축: 세모를 감싼 원</summary>
        static readonly Vector2[] Orb = Circle(32, 0f, 0.45f, 0.9f);

        /// <summary>차단: 화살대 아래 오른쪽 Z</summary>
        static readonly Vector2[] Zed = { new(0.25f, -0.55f), new(0.75f, -0.55f), new(0.25f, -0.95f), new(0.75f, -0.95f) };

        /// <summary>물: 화살대 머리 물결</summary>
        static readonly Vector2[] WaveHead = Wave(24, -0.6f, 0.6f, 0.75f, 0.18f);

        /// <summary>직선 (물 투사)</summary>
        static readonly Vector2[] ShaftW = { new(0f, 0.75f), new(0f, -1f) };

        /// <summary>불 (화염 확산)</summary>
        static readonly Vector2[] TriLow = { new(0f, 0.2f), new(0.52f, -0.7f), new(-0.52f, -0.7f), new(0f, 0.2f) };

        /// <summary>확산: 세모 꼭짓점에서 벌어지는 V</summary>
        static readonly Vector2[] Fan = { new(-0.7f, 1f), new(0f, 0.2f), new(0.7f, 1f) };

        /// <summary>불 (화염 벽)</summary>
        static readonly Vector2[] TriHigh = { new(0f, 0.9f), new(0.52f, 0f), new(-0.52f, 0f), new(0f, 0.9f) };

        /// <summary>벽: 세모 허리를 가로지르는 막대</summary>
        static readonly Vector2[] BarLow = { new(-1.1f, 0.35f), new(1.1f, 0.35f) };

        /// <summary>응축 (방호막)</summary>
        static readonly Vector2[] Orb0 = Circle(32, 0f, 0f, 0.85f);

        /// <summary>벽: 원을 가로지르는 막대</summary>
        static readonly Vector2[] BarMid = { new(-1.1f, 0f), new(1.1f, 0f) };

        /// <summary>차단 (방호막 두루마리)</summary>
        static readonly Vector2[] ZedLow = { new(-0.3f, -1.1f), new(0.3f, -1.1f), new(-0.3f, -1.5f), new(0.3f, -1.5f) };

        /// <summary>주문 그림 9개 (데이터 에셋·DataManager와 같은 순서)</summary>
        static readonly SpellShapeEntry[] Shapes =
        {
            Shape("flame_shot", (Fire, Tri), (Line, Shaft)),
            Shape("fireball", (Fire, Tri), (Condense, Orb), (Line, Shaft)),
            Shape("water_shot", (Water, WaveHead), (Line, ShaftW)),
            Shape("flame_fan", (Fire, TriLow), (Spread, Fan)),
            Shape("flame_wall", (Fire, TriHigh), (Wall, BarLow)),
            Shape("shield", (Condense, Orb0), (Wall, BarMid)),
            Shape("fireball_scroll", (Fire, Tri), (Condense, Orb), (Line, Shaft), (Block, Zed)),
            Shape("flame_shot_scroll", (Fire, Tri), (Line, Shaft), (Block, Zed)),
            Shape("shield_scroll", (Condense, Orb0), (Wall, BarMid), (Block, ZedLow)),
        };

        /// <summary>룬 그림 7개 (룬 주문, 데이터 에셋·DataManager와 같은 순서. 프로토타입 spell-core.js 룬 그림의 y를 뒤집음)</summary>
        static readonly SpellShapeEntry[] RuneShapes =
        {
            Shape(Fire, (Fire, new[] { new Vector2(0f, 1f), new Vector2(0.866f, -0.5f), new Vector2(-0.866f, -0.5f), new Vector2(0f, 1f) })),
            Shape(Condense, (Condense, Circle(32, 0f, 0f, 1f))),
            Shape(Line, (Line, new[] { new Vector2(0f, 1f), new Vector2(0f, -1f) })),
            Shape(Block, (Block, new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) })),
            Shape(Water, (Water, Wave(24, -1f, 1f, 0f, 0.45f))),
            Shape(Spread, (Spread, new[] { new Vector2(-0.8f, 0.8f), new Vector2(0f, -0.8f), new Vector2(0.8f, 0.8f) })),
            Shape(Wall, (Wall, new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f) })),
        };

        #region ExactDrawingCastsItsOwnSpell - 주문 그림을 그대로 그리면 그 주문 (생략 없음, 정확)
        /// <param name="id">주문 id</param>
        [Test]
        public void ExactDrawingCastsItsOwnSpell([ValueSource(nameof(SpellIds))] string id)
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Draw(id));
            Assert.That(CastId(spec), Is.EqualTo(id));
            Assert.That(spec.Candidate.Omit, Is.Empty);
            Assert.That(spec.Accuracy, Is.GreaterThanOrEqualTo(SpellRecognitionCalculator.ExactAccuracy));
        }
        #endregion

        #region FireballWithoutCondenseCastsFlameShot - 화염구에서 응축을 빼면 화살표라 화염 투사
        [Test]
        public void FireballWithoutCondenseCastsFlameShot() =>
            Assert.That(CastId(Judge(Without("fireball", Condense))), Is.EqualTo("flame_shot"));
        #endregion

        #region ScrollWithoutBlockCastsCombatSpell - 두루마리에서 차단을 빼면 전투 주문
        [Test]
        public void ScrollWithoutBlockCastsCombatSpell() =>
            Assert.That(CastId(Judge(Without("fireball_scroll", Block))), Is.EqualTo("fireball"));
        #endregion

        #region FlameShotWithoutLineHasNoMatch - 세모만 그리면 맞는 주문 없음
        [Test]
        public void FlameShotWithoutLineHasNoMatch() =>
            Assert.That(Judge(Without("flame_shot", Line)).Reason, Is.EqualTo(SpellJudgeReason.NoMatch));
        #endregion

        #region HalfOrbIsCanceledForParts - 화염구의 원을 반만 그리면 부분 부족 (화염구, 응축이 부분 기준 아래)
        [Test]
        public void HalfOrbIsCanceledForParts()
        {
            // 세모 + 원 앞쪽 절반 + 화살대
            var strokes = new List<Vector2[]>
            {
                Scaled(Tri),
                Scaled(SpellRecognitionCalculator.Resample(Orb, 100).Take(50)),
                Scaled(Shaft),
            };
            // 판정 결과
            SpellJudgeSpec spec = Judge(strokes);
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Parts));
            Assert.That(spec.Candidate.Shape.Id, Is.EqualTo("fireball"));
            // 응축 부분 번호
            int condense = spec.Candidate.PartRuneIds.ToList().IndexOf(Condense);
            Assert.That(spec.PartCoverage[condense], Is.LessThan(SpellRecognitionCalculator.DefaultPartMin));
        }
        #endregion

        #region RotatedOrStretchedDrawingsStillCast - 조금 돌리거나 늘려도 시전
        [Test]
        public void RotatedOrStretchedDrawingsStillCast()
        {
            Assert.That(CastId(Judge(Transform(Draw("fireball"), -0.2f, 1.1f, 1f))), Is.EqualTo("fireball"));
            Assert.That(CastId(Judge(Transform(Draw("flame_shot"), 0.25f, 1f, 0.9f))), Is.EqualTo("flame_shot"));
        }
        #endregion

        #region TinyDrawingIsTooSmall - 최소 크기보다 작으면 너무 작음
        [Test]
        public void TinyDrawingIsTooSmall() =>
            Assert.That(Judge(Draw("fireball", 20f)).Reason, Is.EqualTo(SpellJudgeReason.Small));
        #endregion

        #region NothingDrawnIsEmpty - 안 그리면 빈 그림
        [Test]
        public void NothingDrawnIsEmpty() =>
            Assert.That(Judge(new List<Vector2[]>()).Reason, Is.EqualTo(SpellJudgeReason.Empty));
        #endregion

        #region NonSpellShapesAreCanceled - 가로선은 맞는 주문 없음, 네모는 방호막 부분 부족
        [Test]
        public void NonSpellShapesAreCanceled()
        {
            Assert.That(Judge(new List<Vector2[]> { Scaled(new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f) }) }).Reason,
                Is.EqualTo(SpellJudgeReason.NoMatch));
            // 네모 판정 결과
            SpellJudgeSpec square = Judge(new List<Vector2[]>
            {
                Scaled(new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f), new Vector2(-1f, 1f) }),
            });
            Assert.That(square.Reason, Is.EqualTo(SpellJudgeReason.Parts));
            Assert.That(square.Candidate.Shape.Id, Is.EqualTo("shield"));
        }
        #endregion

        #region UnlearnedRuneIsCanceled - 응축을 안 익힌 채 화염구를 그리면 미습득 (빠진 룬 응축)
        [Test]
        public void UnlearnedRuneIsCanceled()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Draw("fireball"), learned: AllRunes.Where(e => e != Condense));
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Unlearned));
            Assert.That(spec.Candidate.Shape.Id, Is.EqualTo("fireball"));
            Assert.That(spec.MissingRuneIds, Is.EqualTo(new[] { Condense }));
        }
        #endregion

        #region OmissionNeedsCollectedSpell - 생략은 도감에 등록한 주문만: 미등록이면 부분 부족, 등록했으면 화염구 (직선 생략)
        [Test]
        public void OmissionNeedsCollectedSpell()
        {
            Assert.That(Judge(Without("fireball", Line), omit: 1).Reason, Is.EqualTo(SpellJudgeReason.Parts));
            // 다 등록했을 때 판정 결과
            SpellJudgeSpec spec = Judge(Without("fireball", Line), collected: SpellIds(), omit: 1);
            Assert.That(CastId(spec), Is.EqualTo("fireball"));
            Assert.That(spec.Candidate.Omit, Is.EqualTo(new[] { 2 }));
        }
        #endregion

        #region SameDrawingPrefersFewerOmissions - 생략본이 다른 주문 그림과 같으면 생략이 적은 쪽
        [Test]
        public void SameDrawingPrefersFewerOmissions()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(Without("fireball", Condense), collected: SpellIds(), omit: 1);
            Assert.That(CastId(spec), Is.EqualTo("flame_shot"));
            Assert.That(spec.Candidate.Omit, Is.Empty);
        }
        #endregion

        #region LookAlikeOmissionsAreAmbiguous - 세로선 하나는 화염 투사(불 생략)·물 투사(물 생략)라 하나로 안 정해짐
        [Test]
        public void LookAlikeOmissionsAreAmbiguous()
        {
            // 판정 결과
            SpellJudgeSpec spec = Judge(new List<Vector2[]> { Scaled(new[] { new Vector2(0f, 1f), new Vector2(0f, -1f) }) }, collected: SpellIds(), omit: 1);
            Assert.That(spec.Reason, Is.EqualTo(SpellJudgeReason.Ambiguous));
            Assert.That(spec.AmbiguousShapes.Select(i => Shapes[i].Id), Is.EquivalentTo(new[] { "flame_shot", "water_shot" }));
        }
        #endregion

        #region RuneGlyphCastsItsRune - 룬 그림을 더하면 룬 그림을 그대로 그렸을 때 그 룬 주문
        /// <param name="id">룬 id</param>
        [Test]
        public void RuneGlyphCastsItsRune([ValueSource(nameof(RuneIds))] string id) =>
            Assert.That(CastId(Judge(Strokes(RuneShapes.First(shape => shape.Id == id), DrawScale, null), runes: true)), Is.EqualTo(id));
        #endregion

        #region SpellsStillCastWithRunes - 룬 그림을 더해도 주문 그림을 그대로 그리면 그 주문
        /// <param name="id">주문 id</param>
        [Test]
        public void SpellsStillCastWithRunes([ValueSource(nameof(SpellIds))] string id) =>
            Assert.That(CastId(Judge(Draw(id), runes: true)), Is.EqualTo(id));
        #endregion

        #region TriangleAloneCastsFireRune - 룬 그림을 더하면 세모만 그렸을 때 불 룬 주문 (룬이 없으면 맞는 주문 없음)
        [Test]
        public void TriangleAloneCastsFireRune() =>
            Assert.That(CastId(Judge(Without("flame_shot", Line), runes: true)), Is.EqualTo(Fire));
        #endregion

        #region SpellIds - 주문 id 전부 (ValueSource·전부 등록용)
        static IEnumerable<string> SpellIds() => Shapes.Select(shape => shape.Id);
        #endregion

        #region RuneIds - 룬 id 전부 (ValueSource용)
        static IEnumerable<string> RuneIds() => RuneShapes.Select(shape => shape.Id);
        #endregion

        #region Judge - 기본 기준으로 판정 (익힌 룬 기본 전부, 등록 기본 없음, 생략 기본 0, 룬 그림 기본 뺌)
        /// <param name="strokes">그린 획</param>
        /// <param name="learned">익힌 룬 id</param>
        /// <param name="collected">도감에 등록한 주문 id</param>
        /// <param name="omit">생략 가능 수</param>
        /// <param name="runes">룬 그림도 비교할지 (코덱스처럼 주문 그림 뒤에 룬 그림)</param>
        static SpellJudgeSpec Judge(List<Vector2[]> strokes, IEnumerable<string> learned = null, IEnumerable<string> collected = null, int omit = 0, bool runes = false)
        {
            // 익힌 룬, 등록한 주문
            var learnedSet = new HashSet<string>(learned ?? AllRunes);
            var collectedSet = new HashSet<string>(collected ?? Enumerable.Empty<string>());
            // 비교 그림
            List<SpellCandidateEntry> candidates = SpellRecognitionCalculator.BuildCandidates(runes ? Shapes.Concat(RuneShapes).ToArray() : Shapes, learnedSet, collectedSet, omit);
            return SpellRecognitionCalculator.Judge(strokes, candidates, learnedSet, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        }
        #endregion

        #region CastId - 시전한 주문 id (취소면 이유를 담은 글, 실패 메시지용)
        /// <param name="spec">판정 결과</param>
        static string CastId(SpellJudgeSpec spec) => spec.IsCast ? spec.Candidate.Shape.Id : "취소 " + spec.Reason;
        #endregion

        #region Draw - 주문 그림을 그리기 칸 크기로 그대로 그림
        /// <param name="id">주문 id</param>
        /// <param name="scale">배율</param>
        static List<Vector2[]> Draw(string id, float scale = DrawScale) => Strokes(Find(id), scale, null);
        #endregion

        #region Without - 주문 그림에서 룬 부분 하나를 빼고 그림
        /// <param name="id">주문 id</param>
        /// <param name="rune">뺄 룬 id</param>
        static List<Vector2[]> Without(string id, string rune) => Strokes(Find(id), DrawScale, rune);
        #endregion

        #region Strokes - 주문 그림의 획을 배율·중심으로 옮김 (뺄 룬 부분은 건너뜀)
        /// <param name="shape">주문 그림</param>
        /// <param name="scale">배율</param>
        /// <param name="skip">뺄 룬 id (없으면 null)</param>
        static List<Vector2[]> Strokes(SpellShapeEntry shape, float scale, string skip)
        {
            // 옮긴 획
            var strokes = new List<Vector2[]>();
            for (int i = 0; i < shape.PartRuneIds.Count; i++)
            {
                if (shape.PartRuneIds[i] == skip) continue;
                foreach (Vector2[] stroke in shape.PartStrokes[i]) strokes.Add(Scaled(stroke, scale));
            }
            return strokes;
        }
        #endregion

        #region Scaled - 주문 그림 좌표 점을 그리기 칸 좌표로
        /// <param name="points">주문 그림 좌표 점</param>
        /// <param name="scale">배율</param>
        static Vector2[] Scaled(IEnumerable<Vector2> points, float scale = DrawScale) => points.Select(p => DrawCenter + p * scale).ToArray();
        #endregion

        #region Transform - 그림을 점 평균 기준으로 늘리고(가로·세로) 돌림
        /// <param name="strokes">획</param>
        /// <param name="angle">회전 (라디안)</param>
        /// <param name="sx">가로 배율</param>
        /// <param name="sy">세로 배율</param>
        static List<Vector2[]> Transform(List<Vector2[]> strokes, float angle, float sx, float sy)
        {
            // 점 평균
            Vector2 center = Vector2.zero;
            int count = 0;
            foreach (Vector2[] stroke in strokes)
                foreach (Vector2 p in stroke)
                {
                    center += p;
                    count++;
                }
            center /= count;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return strokes.Select(stroke => stroke.Select(p =>
            {
                // 늘린 편차
                float x = (p.x - center.x) * sx, y = (p.y - center.y) * sy;
                return new Vector2(center.x + x * c - y * s, center.y + x * s + y * c);
            }).ToArray()).ToList();
        }
        #endregion

        #region Find - id로 주문 그림
        /// <param name="id">주문 id</param>
        static SpellShapeEntry Find(string id) => Shapes.First(shape => shape.Id == id);
        #endregion

        #region Shape - 부분마다 (룬 id, 획 하나)로 주문 그림
        /// <param name="id">주문 id</param>
        /// <param name="parts">부분 (룬 id, 획)</param>
        static SpellShapeEntry Shape(string id, params (string rune, Vector2[] stroke)[] parts) =>
            new(id, parts.Select(part => part.rune).ToArray(), parts.Select(part => new[] { part.stroke }).ToArray());
        #endregion

        #region Circle - 원 점 k+1개 (위에서 시작해 시계 방향, 소수 셋째 자리 반올림. 프로토타입 circlePoly를 y 위 +로)
        /// <param name="k">칸 수</param>
        /// <param name="cx">중심 x</param>
        /// <param name="cy">중심 y</param>
        /// <param name="r">반지름</param>
        static Vector2[] Circle(int k, float cx, float cy, float r)
        {
            // 원 점
            var points = new Vector2[k + 1];
            for (int i = 0; i <= k; i++)
            {
                // 각도 (라디안)
                double t = (double)i / k * Math.PI * 2;
                points[i] = new Vector2(Round(cx + r * Math.Sin(t)), Round(cy + r * Math.Cos(t)));
            }
            return points;
        }
        #endregion

        #region Wave - 물결 점 k+1개 (x0 → x1, 세 번 반 출렁임, 소수 셋째 자리 반올림. 프로토타입 wavePoly를 y 위 +로)
        /// <param name="k">칸 수</param>
        /// <param name="x0">시작 x</param>
        /// <param name="x1">끝 x</param>
        /// <param name="cy">가운데 y</param>
        /// <param name="amp">높이</param>
        static Vector2[] Wave(int k, float x0, float x1, float cy, float amp)
        {
            // 물결 점
            var points = new Vector2[k + 1];
            for (int i = 0; i <= k; i++)
            {
                // 진행 (0~1)
                double t = (double)i / k;
                points[i] = new Vector2(Round(x0 + (x1 - x0) * t), Round(cy + amp * Math.Sin(t * Math.PI * 3)));
            }
            return points;
        }
        #endregion

        #region Round - 소수 셋째 자리 반올림 (0에서 먼 쪽, 프로토타입 toFixed(3)과 같음)
        /// <param name="value">값</param>
        static float Round(double value) => (float)Math.Round(value, 3, MidpointRounding.AwayFromZero);
        #endregion
    }
}
