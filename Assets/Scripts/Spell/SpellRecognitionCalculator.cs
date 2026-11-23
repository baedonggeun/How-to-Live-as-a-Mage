using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>주문 그림 인식 (순수 함수 모음): 여러 획을 점 구름 하나로 합쳐 $P로 비교 그림과 전체 일치율을 재고, 부분마다 몇 %를 따라 그렸는지 본 뒤 시전·취소를 정함. 좌표는 y 위가 +. 그린 획 단위는 그리기 칸 단위, 주문 그림은 대략 -1.5~1.5</summary>
    public static class SpellRecognitionCalculator
    {
        /// <summary>전체 비교 점 구름의 점 수 (합친 그림은 부분이 많아서 v1 32보다 늘림)</summary>
        public const int CloudPoints = 48;

        /// <summary>그린 그림 테두리 상자 긴 변이 이보다 작으면 '너무 작다' (그리기 칸 단위)</summary>
        public const float MinDrawSize = 60f;

        /// <summary>정확(위력 ×1.4)으로 칠 전체 일치율</summary>
        public const float ExactAccuracy = 0.9f;

        /// <summary>부분 기준 기본값 (부분마다 이 비율 이상 따라 그려야 함, 임시)</summary>
        public const float DefaultPartMin = 0.8f;

        /// <summary>전체 기준 기본값 (전체 일치율, 임시)</summary>
        public const float DefaultAllMin = 0.6f;

        /// <summary>남는 선(주문 그림 어디에도 안 걸친 그린 선) 한도</summary>
        public const float MaxExtra = 0.1f;

        /// <summary>정확도 눈금이 맞춰진 점 수 (v1). 거리는 점 수에 비례해서 이 기준으로 환산</summary>
        const int ScalePoints = 32;

        /// <summary>정확도 눈금: 이 거리까지는 100%</summary>
        const float AccuracyFloor = 0.4f;

        /// <summary>정확도 눈금: 100%에서 0%까지 거리 폭</summary>
        const float AccuracySpan = 2.5f;

        /// <summary>얇은 그림으로 칠 짧은 변/긴 변 비율 (이보다 작으면 가로세로를 같은 비율로 맞춤)</summary>
        const float ThinRatio = 0.3f;

        /// <summary>점수 차이가 이 안이고 사실상 같은 그림이면 생략이 적은 쪽</summary>
        const float TieScore = 0.03f;

        /// <summary>템플릿끼리 닮음이 이 이상이면 사실상 같은 그림</summary>
        const float SameShape = 0.9f;

        /// <summary>부분 검사에서 '따라 그렸다'로 칠 거리 (주문 그림 크기 대비)</summary>
        const float CoverRadius = 0.08f;

        /// <summary>맞출 때 가로세로 배율 차이 한도</summary>
        const float MaxAspect = 1.25f;

        /// <summary>다른 주문이 남는 선을 이만큼 더 적게 설명하면 그 주문을 덜 그린 것으로 봄</summary>
        const float ExplainGap = 0.04f;

        /// <summary>다른 주문의 전체 일치율이 이만큼 더 높으면 그 주문을 덜 그린 것으로 봄</summary>
        const float RivalAccuracyGap = 0.08f;

        /// <summary>전체 일치율이 (전체 기준 − 이 값) 이상인 후보만 부분 검사 (1등은 늘)</summary>
        const float PartCheckMargin = 0.15f;

        /// <summary>맞추기에 쓸 그린 점 수</summary>
        const int AlignPoints = 64;

        /// <summary>덮인 비율을 잴 그린 점 수</summary>
        const int CoverPoints = 160;

        /// <summary>부분 검사 촘촘한 템플릿 점 간격 (주문 그림 좌표)</summary>
        const float DenseStep = 0.03f;

        /// <summary>맞추기용 성긴 템플릿 점 간격 (주문 그림 좌표)</summary>
        const float SparseStep = 0.09f;

        /// <summary>맞추기 되풀이 최대 횟수</summary>
        const int AlignIterations = 12;

        /// <summary>맞추기에서 짝이 가까운 순으로 남길 비율 (먼 15%는 버림)</summary>
        const float AlignKeep = 0.85f;

        /// <summary>맞추기 배율 한도: 시작 배율의 이 배수 안</summary>
        const float AlignScaleMin = 0.8f;

        /// <summary>맞추기 배율 한도: 시작 배율의 이 배수 안</summary>
        const float AlignScaleMax = 1.25f;

        /// <summary>그린 그림을 돌려 볼 각도 (라디안, ±17°)</summary>
        static readonly float[] Turns = { -0.3f, -0.15f, 0f, 0.15f, 0.3f };

        /// <summary>판정 중 후보 하나의 점수 (판정마다 새로)</summary>
        sealed class RankEntry
        {
            /// <summary>후보 목록 번호 (전체 일치율이 같을 때 순서)</summary>
            public int Order;

            /// <summary>전체 일치율 순으로 정렬한 뒤 자리 (점수가 같을 때 순서)</summary>
            public int Position;

            /// <summary>비교 그림</summary>
            public SpellCandidateEntry Candidate;

            /// <summary>전체 일치율 (0~1)</summary>
            public float Accuracy;

            /// <summary>가장 가까웠을 때 회전 (라디안)</summary>
            public float Angle;

            /// <summary>부분 검사를 했는지</summary>
            public bool Checked;

            /// <summary>남긴 부분마다 일치율</summary>
            public float[] Coverage = Array.Empty<float>();

            /// <summary>남는 선 비율</summary>
            public float Extra = 1f;

            /// <summary>부분 일치율 평균과 (1 − 남는 선)의 평균</summary>
            public float Fit;

            /// <summary>(전체 일치율 + Fit) / 2. 시전 조건을 넘은 후보만 씀</summary>
            public float Score;
        }

        /// <summary>그린 점을 템플릿 좌표로 옮기는 변환 (가로·세로 배율 + 이동)</summary>
        readonly struct AlignSpec
        {
            /// <summary>가로 배율</summary>
            public readonly float Sx;

            /// <summary>세로 배율</summary>
            public readonly float Sy;

            /// <summary>가로 이동</summary>
            public readonly float Tx;

            /// <summary>세로 이동</summary>
            public readonly float Ty;

            #region AlignSpec - 값을 받아 만듦
            /// <param name="sx">가로 배율</param>
            /// <param name="sy">세로 배율</param>
            /// <param name="tx">가로 이동</param>
            /// <param name="ty">세로 이동</param>
            public AlignSpec(float sx, float sy, float tx, float ty)
            {
                Sx = sx;
                Sy = sy;
                Tx = tx;
                Ty = ty;
            }
            #endregion

            #region Apply - 점 하나를 옮김
            /// <param name="p">그린 점</param>
            public Vector2 Apply(Vector2 p) => new(Sx * p.x + Tx, Sy * p.y + Ty);
            #endregion
        }

        #region BuildCandidates - 비교할 그림을 만듦: 주문 그림 전부(미습득 룬이 든 것도, 이유를 보이려고)와, 도감에 등록한 주문은 생략 가능 수까지 부분을 뺀 생략본
        /// <param name="shapes">도감 주문 그림 (번호가 결과의 ShapeIndex. 빈 칸·부분 없는 그림은 건너뜀)</param>
        /// <param name="learnedRuneIds">익힌 룬 id</param>
        /// <param name="collectedSpellIds">도감에 등록한 주문 id</param>
        /// <param name="omitLimit">생략 가능 수 (0이면 생략본 없음)</param>
        public static List<SpellCandidateEntry> BuildCandidates(IReadOnlyList<SpellShapeEntry> shapes, ICollection<string> learnedRuneIds,
            ICollection<string> collectedSpellIds, int omitLimit)
        {
            if (shapes == null) throw new ArgumentNullException(nameof(shapes));
            if (learnedRuneIds == null) throw new ArgumentNullException(nameof(learnedRuneIds));
            if (collectedSpellIds == null) throw new ArgumentNullException(nameof(collectedSpellIds));
            // 만든 비교 그림
            var result = new List<SpellCandidateEntry>();
            for (int s = 0; s < shapes.Count; s++)
            {
                // 이 주문 그림
                SpellShapeEntry shape = shapes[s];
                if (shape == null || shape.PartRuneIds.Count == 0) continue;
                // 룬을 다 익혔는지
                bool learned = true;
                foreach (string id in shape.PartRuneIds)
                    if (!learnedRuneIds.Contains(id))
                    {
                        learned = false;
                        break;
                    }
                // 부분 수
                int partCount = shape.PartRuneIds.Count;
                // 뺄 수 있는 부분 수 (도감에 등록한 주문만, 부분을 다 뺄 순 없음)
                int maxOmit = collectedSpellIds.Contains(shape.Id) ? Mathf.Min(Mathf.Max(0, omitLimit), partCount - 1) : 0;
                for (int k = 0; k <= maxOmit; k++)
                    foreach (int[] omit in Combinations(partCount, k))
                        result.Add(CreateCandidate(s, shape, omit, learned));
            }
            return result;
        }
        #endregion

        #region Judge - 그린 획을 비교 그림마다 대조해 시전·취소를 정함. 돌려 본 각도마다 전체 일치율을 재고, 기준 근처 이상은 부분 검사
        /// <param name="strokes">그린 획 (그리기 칸 단위, 점 2개 이상인 획)</param>
        /// <param name="candidates">BuildCandidates로 만든 비교 그림</param>
        /// <param name="learnedRuneIds">익힌 룬 id (안 익힌 룬 이유에 씀)</param>
        /// <param name="partMin">부분 기준 (0~1)</param>
        /// <param name="allMin">전체 기준 (0~1)</param>
        public static SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes, IReadOnlyList<SpellCandidateEntry> candidates,
            ICollection<string> learnedRuneIds, float partMin, float allMin)
        {
            if (strokes == null) throw new ArgumentNullException(nameof(strokes));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (learnedRuneIds == null) throw new ArgumentNullException(nameof(learnedRuneIds));
            // 그린 점 전부
            var points = new List<Vector2>();
            foreach (Vector2[] stroke in strokes)
                if (stroke != null) points.AddRange(stroke);
            if (points.Count < 2) return SpellJudgeSpec.Cancel(SpellJudgeReason.Empty);
            // 그린 그림 테두리 상자
            Rect box = Bounds(points);
            if (Mathf.Max(box.width, box.height) < MinDrawSize) return SpellJudgeSpec.Cancel(SpellJudgeReason.Small);
            if (candidates.Count == 0) return SpellJudgeSpec.Cancel(SpellJudgeReason.NoCandidates);

            // 돌리는 중심 (그린 점 평균)
            Vector2 center = Vector2.zero;
            foreach (Vector2 p in points) center += p;
            center /= points.Count;
            // 후보마다 가장 가까운 거리와 그때 각도
            var distance = new float[candidates.Count];
            var angle = new float[candidates.Count];
            for (int i = 0; i < distance.Length; i++) distance[i] = float.PositiveInfinity;
            foreach (float turn in Turns)
            {
                // 이 각도로 돌린 그린 그림의 점 구름
                Vector2[] cloud = Normalize(Rotate(strokes, center, turn));
                for (int i = 0; i < candidates.Count; i++)
                {
                    // 이 후보와의 거리
                    float d = CloudDistance(cloud, candidates[i].Cloud);
                    if (d < distance[i])
                    {
                        distance[i] = d;
                        angle[i] = turn;
                    }
                }
            }
            // 전체 일치율 높은 순 (같으면 후보 순서)
            var ranks = new List<RankEntry>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                ranks.Add(new RankEntry { Order = i, Candidate = candidates[i], Accuracy = ToAccuracy(distance[i]), Angle = angle[i] });
            ranks.Sort(ByAccuracy);
            for (int i = 0; i < ranks.Count; i++)
            {
                ranks[i].Position = i;
                if (i == 0 || ranks[i].Accuracy >= allMin - PartCheckMargin) CheckParts(strokes, center, ranks[i]);
            }
            return Pick(ranks, learnedRuneIds, partMin, allMin);
        }
        #endregion

        #region Pick - 시전 조건을 넘은 것 중 점수가 가장 높은 그림. 더 닮았는데 부분이 모자라 떨어진 주문이 있으면 덜 그린 것으로 보고 취소, 사실상 같은 그림이면 생략이 적은 쪽, 미습득·애매하면 취소
        /// <param name="ranks">전체 일치율 높은 순 후보</param>
        /// <param name="learnedRuneIds">익힌 룬 id</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="allMin">전체 기준</param>
        static SpellJudgeSpec Pick(List<RankEntry> ranks, ICollection<string> learnedRuneIds, float partMin, float allMin)
        {
            // 시전 조건을 넘은 후보 (점수 높은 순, 같으면 전체 일치율 순서)
            var passed = new List<RankEntry>();
            foreach (RankEntry r in ranks)
                if (Passes(r, partMin, allMin))
                {
                    r.Score = (r.Accuracy + r.Fit) / 2f;
                    passed.Add(r);
                }
            if (passed.Count == 0)
            {
                // 이유를 보일 그림 (전체 일치율 1등)
                RankEntry first = ranks[0];
                return Result(false, first.Checked && first.Accuracy >= allMin ? SpellJudgeReason.Parts : SpellJudgeReason.NoMatch, first, partMin);
            }
            passed.Sort(ByScore);
            // 점수 1등
            RankEntry top = passed[0];
            // 더 닮았는데 부분이 모자라 떨어진 주문 (전체 일치율 높은 순 첫째): 그 주문을 덜 그린 것으로 봄
            foreach (RankEntry r in ranks)
                if (r.Checked && !Passes(r, partMin, allMin) && r.Accuracy >= allMin
                    && (r.Extra < top.Extra - ExplainGap || r.Accuracy >= top.Accuracy + RivalAccuracyGap))
                    return Result(false, SpellJudgeReason.Parts, r, partMin);
            // 1등과 사실상 같은 그림 무리
            var same = new List<RankEntry>();
            foreach (RankEntry r in passed)
                if (r == top || (r.Score >= top.Score - TieScore && Similarity(r.Candidate, top.Candidate) >= SameShape)) same.Add(r);
            // 무리에서 가장 적은 생략 수
            int fewest = int.MaxValue;
            foreach (RankEntry r in same) fewest = Mathf.Min(fewest, r.Candidate.Omit.Count);
            // 생략이 가장 적은 그림들, 그중 룬을 다 익힌 것
            var group = new List<RankEntry>();
            var usable = new List<RankEntry>();
            foreach (RankEntry r in same)
            {
                if (r.Candidate.Omit.Count != fewest) continue;
                group.Add(r);
                if (r.Candidate.Learned) usable.Add(r);
            }
            if (usable.Count == 0)
                return Result(false, SpellJudgeReason.Unlearned, group[0], partMin, MissingRunes(group[0].Candidate.Shape, learnedRuneIds));
            // 겹친 주문 번호 (처음 나온 순서)
            var shapes = new List<int>();
            foreach (RankEntry r in usable)
                if (!shapes.Contains(r.Candidate.ShapeIndex)) shapes.Add(r.Candidate.ShapeIndex);
            if (shapes.Count > 1) return Result(false, SpellJudgeReason.Ambiguous, usable[0], partMin, null, shapes);
            return Result(true, SpellJudgeReason.None, usable[0], partMin);
        }
        #endregion

        #region Result - 후보 점수로 결과를 만듦
        /// <param name="isCast">시전했는지</param>
        /// <param name="reason">취소 이유</param>
        /// <param name="rank">결과에 담을 후보</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="missing">안 익힌 룬 id</param>
        /// <param name="ambiguous">같은 그림인 주문 번호</param>
        static SpellJudgeSpec Result(bool isCast, SpellJudgeReason reason, RankEntry rank, float partMin,
            IReadOnlyList<string> missing = null, IReadOnlyList<int> ambiguous = null) =>
            new(isCast, reason, rank.Candidate, rank.Accuracy, rank.Checked ? rank.Coverage : null, rank.Extra, missing, ambiguous, partMin);
        #endregion

        #region Passes - 시전 조건: 부분 검사를 했고 전체 일치율 ≥ 전체 기준, 부분마다 ≥ 부분 기준, 남는 선 ≤ 한도
        /// <param name="rank">볼 후보</param>
        /// <param name="partMin">부분 기준</param>
        /// <param name="allMin">전체 기준</param>
        static bool Passes(RankEntry rank, float partMin, float allMin)
        {
            if (!rank.Checked || rank.Accuracy < allMin || rank.Extra > MaxExtra) return false;
            foreach (float coverage in rank.Coverage)
                if (coverage < partMin) return false;
            return true;
        }
        #endregion

        #region ByAccuracy - 전체 일치율 높은 순, 같으면 후보 순서
        /// <param name="x">앞 후보</param>
        /// <param name="y">뒤 후보</param>
        static int ByAccuracy(RankEntry x, RankEntry y) => x.Accuracy != y.Accuracy ? y.Accuracy.CompareTo(x.Accuracy) : x.Order.CompareTo(y.Order);
        #endregion

        #region ByScore - 점수 높은 순, 같으면 전체 일치율 순 자리
        /// <param name="x">앞 후보</param>
        /// <param name="y">뒤 후보</param>
        static int ByScore(RankEntry x, RankEntry y) => x.Score != y.Score ? y.Score.CompareTo(x.Score) : x.Position.CompareTo(y.Position);
        #endregion

        #region MissingRunes - 주문 부분 룬 중 안 익힌 것 (부분 순서)
        /// <param name="shape">주문 그림</param>
        /// <param name="learnedRuneIds">익힌 룬 id</param>
        static List<string> MissingRunes(SpellShapeEntry shape, ICollection<string> learnedRuneIds)
        {
            // 안 익힌 룬 id
            var missing = new List<string>();
            foreach (string id in shape.PartRuneIds)
                if (!learnedRuneIds.Contains(id)) missing.Add(id);
            return missing;
        }
        #endregion

        #region Similarity - 비교 그림끼리 닮음 (정확도 눈금)
        /// <param name="a">비교 그림</param>
        /// <param name="b">비교 그림</param>
        static float Similarity(SpellCandidateEntry a, SpellCandidateEntry b) => ToAccuracy(CloudDistance(a.Cloud, b.Cloud));
        #endregion

        #region CheckParts - 후보의 부분 검사: 그 각도로 돌린 그림을 템플릿 위에 맞추고, 부분마다 덮인 비율과 남는 선을 잼
        /// <param name="strokes">그린 획</param>
        /// <param name="center">돌리는 중심</param>
        /// <param name="rank">검사할 후보 (결과를 여기에 넣음)</param>
        static void CheckParts(IReadOnlyList<Vector2[]> strokes, Vector2 center, RankEntry rank)
        {
            // 후보 각도로 돌린 그린 획
            List<Vector2[]> turned = Rotate(strokes, center, rank.Angle);
            // 비교 그림
            SpellCandidateEntry tp = rank.Candidate;
            rank.Checked = true;
            if (!Align(ResampleStrokes(turned, AlignPoints), tp, out AlignSpec fit))
            {
                rank.Coverage = new float[tp.PartRuneIds.Count];
                rank.Extra = 1f;
                rank.Fit = 0f;
                return;
            }
            Cover(ResampleStrokes(turned, CoverPoints), fit, tp, out float[] coverage, out float extra);
            rank.Coverage = coverage;
            rank.Extra = extra;
            // 부분 일치율 평균
            float mean = 0f;
            foreach (float c in coverage) mean += c;
            mean /= coverage.Length;
            rank.Fit = (mean + 1f - extra) / 2f;
        }
        #endregion

        #region Align - 그린 점을 템플릿 좌표로 옮기는 변환을 맞춤 (ICP): 짝이 먼 15%는 버리고 가로·세로 배율과 이동을 되풀이해 맞춤. 시작 배율 세 가지 중 남는 거리(+비율 벌점)가 가장 작은 것. 못 맞추면 false
        /// <param name="drawn">그린 점 (다시 뽑은 것)</param>
        /// <param name="tp">비교 그림</param>
        /// <param name="result">맞춘 변환</param>
        static bool Align(Vector2[] drawn, SpellCandidateEntry tp, out AlignSpec result)
        {
            result = default;
            // 그린 점·템플릿 테두리 상자
            Rect db = Bounds(drawn);
            Rect tb = tp.Bounds;
            // 그린 그림 가로·세로 (0이면 아주 작은 값)
            float dw = db.width > 0f ? db.width : 1e-6f;
            float dh = db.height > 0f ? db.height : 1e-6f;
            // 시작 배율 세 가지 (가로세로 따로, 세로 기준, 가로 기준)
            Vector2[] starts =
            {
                new(tb.width / dw, tb.height / dh),
                new(tb.height / dh, tb.height / dh),
                new(tb.width / dw, tb.width / dw),
            };
            // 그린 점 수, 남길 짝 수
            int n = drawn.Length;
            int keep = Mathf.Min(n, Mathf.Max(3, Mathf.FloorToInt(n * AlignKeep)));
            // 짝 거리 제곱, 짝 템플릿 점, 거리 순 번호 (되풀이마다 다시 씀)
            var dist = new float[n];
            var nearest = new Vector2[n];
            var order = new int[n];
            // 지금까지 가장 작은 벌점
            float bestPenalty = float.PositiveInfinity;
            bool found = false;
            foreach (Vector2 start in starts)
            {
                if (!(start.x > 0f && start.y > 0f) || float.IsInfinity(start.x) || float.IsInfinity(start.y)) continue;
                // 시작 배율 크기
                float s0 = Mathf.Sqrt(start.x * start.y);
                // 지금 변환
                float sx = start.x, sy = start.y;
                float tx = tb.center.x - sx * db.center.x, ty = tb.center.y - sy * db.center.y;
                // 남긴 짝의 평균 거리 제곱
                float residual = float.PositiveInfinity;
                for (int it = 0; it < AlignIterations; it++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        // 지금 변환으로 옮긴 그린 점
                        float x = sx * drawn[i].x + tx, y = sy * drawn[i].y + ty;
                        float m = float.PositiveInfinity;
                        Vector2 q = default;
                        foreach (Vector2 t in tp.Sparse)
                        {
                            float dx = x - t.x, dy = y - t.y, d = dx * dx + dy * dy;
                            if (d < m)
                            {
                                m = d;
                                q = t;
                            }
                        }
                        dist[i] = m;
                        nearest[i] = q;
                        order[i] = i;
                    }
                    Array.Sort(order, (a, b) => dist[a] != dist[b] ? dist[a].CompareTo(dist[b]) : a.CompareTo(b));
                    residual = 0f;
                    for (int k = 0; k < keep; k++) residual += dist[order[k]];
                    residual /= keep;
                    FitAxis(drawn, nearest, order, keep, 0, out float mpx, out float mqx, out float fitX);
                    FitAxis(drawn, nearest, order, keep, 1, out float mpy, out float mqy, out float fitY);
                    // 새 배율 (한쪽이 0 이하면 다른 쪽)
                    float nsx = fitX > 0f ? fitX : fitY;
                    float nsy = fitY > 0f ? fitY : fitX;
                    if (!(nsx > 0f && nsy > 0f)) break;
                    nsx = Mathf.Clamp(nsx, s0 * AlignScaleMin, s0 * AlignScaleMax);
                    nsy = Mathf.Clamp(nsy, s0 * AlignScaleMin, s0 * AlignScaleMax);
                    if (nsx / nsy > MaxAspect) nsx = nsy * MaxAspect;
                    else if (nsx / nsy < 1f / MaxAspect) nsx = nsy / MaxAspect;
                    // 새 이동
                    float ntx = mqx - nsx * mpx, nty = mqy - nsy * mpy;
                    // 이번에 바뀐 양
                    float moved = Mathf.Abs(ntx - tx) + Mathf.Abs(nty - ty) + (Mathf.Abs(nsx - sx) + Mathf.Abs(nsy - sy)) * Mathf.Max(dw, dh);
                    sx = nsx;
                    sy = nsy;
                    tx = ntx;
                    ty = nty;
                    if (moved < 1e-3f * tp.Size) break;
                }
                // 가로세로를 다르게 늘린 만큼 벌점 (덜 그린 그림을 늘려 맞추는 것보다 비율을 지킨 쪽)
                float penalty = residual * (1f + 2f * Mathf.Abs(Mathf.Log(sx / sy)));
                if (found && penalty >= bestPenalty) continue;
                found = true;
                bestPenalty = penalty;
                result = new AlignSpec(sx, sy, tx, ty);
            }
            return found;
        }
        #endregion

        #region FitAxis - 한 축의 최소제곱 배율: 남긴 짝의 그린 값 평균·템플릿 값 평균, 공분산/분산 (분산이 거의 0이면 0)
        /// <param name="drawn">그린 점</param>
        /// <param name="nearest">짝 템플릿 점</param>
        /// <param name="order">거리 순 번호</param>
        /// <param name="keep">남길 짝 수</param>
        /// <param name="axis">0 가로, 1 세로</param>
        /// <param name="meanP">그린 값 평균</param>
        /// <param name="meanQ">템플릿 값 평균</param>
        /// <param name="scale">배율</param>
        static void FitAxis(Vector2[] drawn, Vector2[] nearest, int[] order, int keep, int axis, out float meanP, out float meanQ, out float scale)
        {
            meanP = 0f;
            meanQ = 0f;
            for (int k = 0; k < keep; k++)
            {
                meanP += drawn[order[k]][axis];
                meanQ += nearest[order[k]][axis];
            }
            meanP /= keep;
            meanQ /= keep;
            // 공분산·분산 합
            float covariance = 0f, variance = 0f;
            for (int k = 0; k < keep; k++)
            {
                // 그린 값 편차
                float a = drawn[order[k]][axis] - meanP;
                covariance += a * (nearest[order[k]][axis] - meanQ);
                variance += a * a;
            }
            scale = variance > 1e-9f ? covariance / variance : 0f;
        }
        #endregion

        #region Cover - 그린 점마다 가장 가까운 템플릿 부분에 배정하고, 부분 템플릿 점은 그 부분에 배정된 그린 점이 거리 안에 있을 때만 덮인 걸로 침 (다른 부분 선이 대신 채우지 않게). 부분마다 덮인 비율과 남는 선
        /// <param name="drawn">그린 점 (다시 뽑은 것)</param>
        /// <param name="fit">맞춘 변환</param>
        /// <param name="tp">비교 그림</param>
        /// <param name="coverage">부분마다 덮인 비율</param>
        /// <param name="extra">남는 선 비율</param>
        static void Cover(Vector2[] drawn, AlignSpec fit, SpellCandidateEntry tp, out float[] coverage, out float extra)
        {
            // 따라 그린 걸로 칠 거리 제곱
            float r2 = CoverRadius * tp.Size * (CoverRadius * tp.Size);
            // 부분 수
            int parts = tp.PartRuneIds.Count;
            // 부분마다 배정된 그린 점 (템플릿 좌표)
            var byPart = new List<Vector2>[parts];
            for (int j = 0; j < parts; j++) byPart[j] = new List<Vector2>();
            // 어느 템플릿 점과도 거리 밖인 그린 점 수
            int off = 0;
            foreach (Vector2 p in drawn)
            {
                // 템플릿 좌표로 옮긴 점
                Vector2 moved = fit.Apply(p);
                float m = float.PositiveInfinity;
                int part = 0;
                for (int k = 0; k < tp.Dense.Length; k++)
                {
                    float d = (moved - tp.Dense[k]).sqrMagnitude;
                    if (d < m)
                    {
                        m = d;
                        part = tp.DenseParts[k];
                    }
                }
                if (m > r2) off++;
                else byPart[part].Add(moved);
            }
            // 부분마다 덮인 템플릿 점 수·전체 수
            var hit = new int[parts];
            var total = new int[parts];
            for (int k = 0; k < tp.Dense.Length; k++)
            {
                int part = tp.DenseParts[k];
                total[part]++;
                if (NearestSq(tp.Dense[k], byPart[part]) <= r2) hit[part]++;
            }
            coverage = new float[parts];
            for (int j = 0; j < parts; j++) coverage[j] = total[j] > 0 ? (float)hit[j] / total[j] : 0f;
            extra = drawn.Length > 0 ? (float)off / drawn.Length : 1f;
        }
        #endregion

        #region NearestSq - 점 목록 중 가장 가까운 점까지 거리 제곱 (목록이 비면 무한대)
        /// <param name="point">기준 점</param>
        /// <param name="points">찾을 점 목록</param>
        static float NearestSq(Vector2 point, List<Vector2> points)
        {
            // 지금까지 가장 가까운 거리 제곱
            float m = float.PositiveInfinity;
            foreach (Vector2 q in points) m = Mathf.Min(m, (point - q).sqrMagnitude);
            return m;
        }
        #endregion

        #region CreateCandidate - 남길 부분으로 점 구름·부분 점·테두리 상자를 계산해 비교 그림을 만듦
        /// <param name="shapeIndex">주문 그림 목록 번호</param>
        /// <param name="shape">주문 그림</param>
        /// <param name="omit">뺄 부분 번호</param>
        /// <param name="learned">룬을 다 익혔는지</param>
        static SpellCandidateEntry CreateCandidate(int shapeIndex, SpellShapeEntry shape, int[] omit, bool learned)
        {
            // 남긴 부분의 룬 id, 획
            var keptIds = new List<string>();
            var keptParts = new List<Vector2[][]>();
            for (int i = 0; i < shape.PartRuneIds.Count; i++)
            {
                if (Array.IndexOf(omit, i) >= 0) continue;
                keptIds.Add(shape.PartRuneIds[i]);
                keptParts.Add(shape.PartStrokes[i]);
            }
            // 남긴 부분의 획 전부
            var strokes = new List<Vector2[]>();
            foreach (Vector2[][] part in keptParts) strokes.AddRange(part);
            PartPoints(keptParts, DenseStep, out Vector2[] dense, out int[] denseParts);
            PartPoints(keptParts, SparseStep, out Vector2[] sparse, out _);
            // 촘촘한 점 테두리 상자
            Rect bounds = Bounds(dense);
            // 테두리 상자 긴 변 (0이면 1)
            float size = Mathf.Max(bounds.width, bounds.height);
            if (!(size > 0f)) size = 1f;
            return new SpellCandidateEntry(shapeIndex, shape, omit, learned, Normalize(strokes), keptIds.ToArray(), dense, denseParts, sparse, bounds, size);
        }
        #endregion

        #region PartPoints - 부분 검사용 템플릿 점: 부분 번호를 붙여 획마다 간격 step으로 다시 뽑음
        /// <param name="parts">부분마다 획</param>
        /// <param name="step">점 간격 (주문 그림 좌표)</param>
        /// <param name="points">템플릿 점</param>
        /// <param name="partIndices">점마다 부분 번호</param>
        static void PartPoints(IReadOnlyList<Vector2[][]> parts, float step, out Vector2[] points, out int[] partIndices)
        {
            // 뽑은 점, 부분 번호
            var pts = new List<Vector2>();
            var indices = new List<int>();
            for (int j = 0; j < parts.Count; j++)
                foreach (Vector2[] stroke in parts[j])
                {
                    if (stroke == null || stroke.Length == 0) continue;
                    // 이 획에서 뽑을 점 수
                    int count = Mathf.Max(2, Mathf.CeilToInt(PathLength(stroke) / step) + 1);
                    foreach (Vector2 q in Resample(stroke, count))
                    {
                        pts.Add(q);
                        indices.Add(j);
                    }
                }
            points = pts.ToArray();
            partIndices = indices.ToArray();
        }
        #endregion

        #region Combinations - 0~n-1에서 k개 고르는 조합 (오름차순, 사전 순)
        /// <param name="n">고를 수의 범위</param>
        /// <param name="k">고를 개수</param>
        static List<int[]> Combinations(int n, int k)
        {
            // 만든 조합
            var result = new List<int[]>();
            AddCombinations(0, n, k, new List<int>(k), result);
            return result;
        }
        #endregion

        #region AddCombinations - 조합 되부름: 지금까지 고른 것 뒤로 하나씩 더함
        /// <param name="start">이번에 고를 수의 시작</param>
        /// <param name="n">고를 수의 범위</param>
        /// <param name="k">고를 개수</param>
        /// <param name="current">지금까지 고른 수</param>
        /// <param name="result">다 고른 조합을 넣을 목록</param>
        static void AddCombinations(int start, int n, int k, List<int> current, List<int[]> result)
        {
            if (current.Count == k)
            {
                result.Add(current.ToArray());
                return;
            }
            for (int i = start; i < n; i++)
            {
                current.Add(i);
                AddCombinations(i + 1, n, k, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
        #endregion

        #region PathLength - 점 줄 길이 (null이면 0)
        /// <param name="points">점 줄</param>
        public static float PathLength(IReadOnlyList<Vector2> points)
        {
            if (points == null) return 0f;
            // 지금까지 길이
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }
        #endregion

        #region Resample - 획 하나를 길이 기준 count개 점으로 다시 뽑음 (길이가 0이면 첫 점 count개)
        /// <param name="points">획 (점 1개 이상)</param>
        /// <param name="count">뽑을 점 수 (2 이상)</param>
        public static Vector2[] Resample(IReadOnlyList<Vector2> points, int count)
        {
            if (points == null || points.Count == 0) throw new ArgumentException("빈 획", nameof(points));
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), count, "점 수는 2 이상");
            // 다시 뽑는 중인 점 (사이에 새 점을 끼워 넣음)
            var pts = new List<Vector2>(points);
            // 점 사이 간격
            float interval = PathLength(pts) / (count - 1);
            if (!(interval > 0f)) return Fill(pts[0], count);
            // 뽑은 점
            var result = new List<Vector2>(count) { pts[0] };
            // 마지막으로 뽑은 뒤 지난 길이
            float walked = 0f;
            Walk(pts, interval, ref walked, result);
            return Finish(result, pts[pts.Count - 1], count);
        }
        #endregion

        #region ResampleStrokes - 여러 획을 한 줄처럼 길이 기준 count개 점으로 (길이는 획 안에서만, 획 사이는 안 이음. 그리는 순서·방향·획 수와 상관없음)
        /// <param name="strokes">획 목록</param>
        /// <param name="count">뽑을 점 수 (2 이상)</param>
        public static Vector2[] ResampleStrokes(IReadOnlyList<Vector2[]> strokes, int count)
        {
            if (strokes == null) throw new ArgumentNullException(nameof(strokes));
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), count, "점 수는 2 이상");
            // 점이 있는 첫 획
            Vector2[] first = null;
            foreach (Vector2[] stroke in strokes)
                if (stroke != null && stroke.Length > 0)
                {
                    first = stroke;
                    break;
                }
            if (first == null) return Fill(Vector2.zero, count);
            // 모든 획 길이 합
            float total = 0f;
            foreach (Vector2[] stroke in strokes) total += PathLength(stroke);
            if (!(total > 0f)) return Fill(first[0], count);
            // 점 사이 간격
            float interval = total / (count - 1);
            // 뽑은 점
            var result = new List<Vector2>(count) { first[0] };
            // 마지막으로 뽑은 뒤 지난 길이 (획을 넘어 이어짐)
            float walked = 0f;
            // 마지막 획의 끝 점
            Vector2 last = first[0];
            foreach (Vector2[] stroke in strokes)
            {
                if (stroke == null || stroke.Length == 0) continue;
                // 이 획 점 (사이에 새 점을 끼워 넣음)
                var pts = new List<Vector2>(stroke);
                Walk(pts, interval, ref walked, result);
                last = pts[pts.Count - 1];
            }
            return Finish(result, last, count);
        }
        #endregion

        #region Walk - 점 줄을 따라가며 간격마다 점을 뽑아 넣음 (뽑은 자리를 줄에 끼워 거기서 다시 셈)
        /// <param name="pts">따라갈 점 줄 (뽑은 점이 끼워짐)</param>
        /// <param name="interval">점 사이 간격</param>
        /// <param name="walked">마지막으로 뽑은 뒤 지난 길이 (이어서 씀)</param>
        /// <param name="result">뽑은 점을 넣을 목록</param>
        static void Walk(List<Vector2> pts, float interval, ref float walked, List<Vector2> result)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                // 이번 선분
                Vector2 a = pts[i - 1], b = pts[i];
                float d = Vector2.Distance(a, b);
                if (walked + d >= interval && d > 0f)
                {
                    // 간격이 차는 자리
                    Vector2 q = a + (interval - walked) / d * (b - a);
                    result.Add(q);
                    pts.Insert(i, q);
                    walked = 0f;
                }
                else
                {
                    walked += d;
                }
            }
        }
        #endregion

        #region Finish - 모자라면 마지막 점으로 채우고 넘치면 자름 (반올림 오차)
        /// <param name="result">뽑은 점</param>
        /// <param name="last">채울 점</param>
        /// <param name="count">점 수</param>
        static Vector2[] Finish(List<Vector2> result, Vector2 last, int count)
        {
            while (result.Count < count) result.Add(last);
            if (result.Count > count) result.RemoveRange(count, result.Count - count);
            return result.ToArray();
        }
        #endregion

        #region Fill - 같은 점 count개
        /// <param name="point">채울 점</param>
        /// <param name="count">점 수</param>
        static Vector2[] Fill(Vector2 point, int count)
        {
            // 같은 점 배열
            var result = new Vector2[count];
            for (int i = 0; i < count; i++) result[i] = point;
            return result;
        }
        #endregion

        #region Normalize - 점 구름 정규화: CloudPoints개로 다시 뽑기 → 가로·세로를 따로 0~1로 (얇은 그림은 같은 비율) → 무게중심을 원점으로
        /// <param name="strokes">획 목록</param>
        public static Vector2[] Normalize(IReadOnlyList<Vector2[]> strokes)
        {
            // 다시 뽑은 점
            Vector2[] points = ResampleStrokes(strokes, CloudPoints);
            // 테두리 상자
            Rect box = Bounds(points);
            // 맞출 가로·세로 (0이면 1)
            float w = box.width, h = box.height;
            if (w == 0f) w = 1f;
            if (h == 0f) h = 1f;
            if (Mathf.Min(w, h) / Mathf.Max(w, h) < ThinRatio) w = h = Mathf.Max(w, h);
            // 무게중심
            Vector2 mean = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2((points[i].x - box.xMin) / w, (points[i].y - box.yMin) / h);
                mean += points[i];
            }
            mean /= points.Length;
            for (int i = 0; i < points.Length; i++) points[i] -= mean;
            return points;
        }
        #endregion

        #region CloudDistance - $P 점 구름 거리: 시작 점을 √n칸씩 바꿔 가며 양방향 탐욕 매칭 중 가장 작은 값
        /// <param name="a">점 구름 (점 수가 b와 같음)</param>
        /// <param name="b">점 구름</param>
        public static float CloudDistance(Vector2[] a, Vector2[] b)
        {
            if (a == null || b == null || a.Length == 0 || a.Length != b.Length) throw new ArgumentException("점 수가 같은 점 구름 둘이 필요");
            // 시작 점을 바꿀 간격
            int step = Mathf.Max(1, (int)Math.Floor(Math.Sqrt(a.Length)));
            // 지금까지 가장 작은 거리
            float best = float.PositiveInfinity;
            for (int i = 0; i < a.Length; i += step) best = Mathf.Min(best, Mathf.Min(GreedyCloud(a, b, i), GreedyCloud(b, a, i)));
            return best;
        }
        #endregion

        #region GreedyCloud - a의 start부터 차례로 b에서 아직 안 쓴 가장 가까운 점과 짝짓고, 늦게 짝지을수록 가중치를 줄여 거리를 더함
        /// <param name="a">짝을 찾는 쪽</param>
        /// <param name="b">짝이 되는 쪽</param>
        /// <param name="start">a의 시작 점 번호</param>
        static float GreedyCloud(Vector2[] a, Vector2[] b, int start)
        {
            // 점 수
            int n = a.Length;
            // b에서 이미 짝지은 점
            var used = new bool[n];
            // 가중 거리 합
            float sum = 0f;
            // 지금 a 점 번호
            int i = start;
            do
            {
                // 가장 가까운 안 쓴 b 점과 거리
                int pick = 0;
                float nearest = float.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    if (used[j]) continue;
                    float d = Vector2.Distance(a[i], b[j]);
                    if (d < nearest)
                    {
                        nearest = d;
                        pick = j;
                    }
                }
                used[pick] = true;
                sum += (1f - (float)((i - start + n) % n) / n) * nearest;
                i = (i + 1) % n;
            } while (i != start);
            return sum;
        }
        #endregion

        #region ToAccuracy - $P 거리를 정확도(0~1)로: 점 수 비례로 32점 기준 환산 뒤 v1 눈금
        /// <param name="distance">CloudDistance 값</param>
        public static float ToAccuracy(float distance) =>
            Mathf.Clamp01(1f - (distance * ScalePoints / CloudPoints - AccuracyFloor) / AccuracySpan);
        #endregion

        #region Rotate - 획을 중심 기준으로 돌림 (돌린 점은 중심이 원점)
        /// <param name="strokes">획 목록</param>
        /// <param name="center">돌리는 중심</param>
        /// <param name="angle">각도 (라디안)</param>
        static List<Vector2[]> Rotate(IReadOnlyList<Vector2[]> strokes, Vector2 center, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            // 돌린 획
            var result = new List<Vector2[]>(strokes.Count);
            foreach (Vector2[] stroke in strokes)
            {
                if (stroke == null) continue;
                // 이 획을 돌린 점
                var turned = new Vector2[stroke.Length];
                for (int i = 0; i < stroke.Length; i++)
                {
                    Vector2 d = stroke[i] - center;
                    turned[i] = new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
                }
                result.Add(turned);
            }
            return result;
        }
        #endregion

        #region Bounds - 점들의 테두리 상자
        /// <param name="points">점 목록 (1개 이상)</param>
        static Rect Bounds(IReadOnlyList<Vector2> points)
        {
            float x0 = float.PositiveInfinity, y0 = float.PositiveInfinity, x1 = float.NegativeInfinity, y1 = float.NegativeInfinity;
            foreach (Vector2 p in points)
            {
                x0 = Mathf.Min(x0, p.x);
                y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x);
                y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
        #endregion
    }
}
