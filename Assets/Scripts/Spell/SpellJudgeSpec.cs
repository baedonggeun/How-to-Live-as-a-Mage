using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>판정이 시전을 못 한 이유 (시전이면 None)</summary>
    public enum SpellJudgeReason
    {
        /// <summary>시전</summary>
        None,

        /// <summary>그린 것이 없음 (점 2개 미만)</summary>
        Empty,

        /// <summary>그림이 너무 작음</summary>
        Small,

        /// <summary>비교할 주문 그림이 없음</summary>
        NoCandidates,

        /// <summary>맞는 주문이 없음</summary>
        NoMatch,

        /// <summary>부분 일치율·남는 선이 모자람 (덜 그림)</summary>
        Parts,

        /// <summary>습득하지 않은 룬이 든 주문</summary>
        Unlearned,

        /// <summary>같은 그림인 주문이 둘 이상</summary>
        Ambiguous,

        /// <summary>직접 취소 (Esc·우클릭)</summary>
        Canceled,
    }

    /// <summary>판정 결과 하나 (그릴 때마다 새로). 시전이면 고른 그림, 취소면 이유와 이유를 보일 가장 가까운 그림</summary>
    public readonly struct SpellJudgeSpec
    {
        /// <summary>시전했는지</summary>
        public bool IsCast { get; }

        /// <summary>취소 이유 (시전이면 None)</summary>
        public SpellJudgeReason Reason { get; }

        /// <summary>시전한 그림 또는 이유를 보일 그림. 없으면 null</summary>
        public SpellCandidateEntry Candidate { get; }

        /// <summary>Candidate의 전체 일치율 (0~1)</summary>
        public float Accuracy { get; }

        /// <summary>Candidate 남긴 부분마다 일치율 (0~1, PartRuneIds 순서. 부분 검사를 안 했으면 빈 목록)</summary>
        public IReadOnlyList<float> PartCoverage { get; }

        /// <summary>남는 선 비율 (0~1)</summary>
        public float Extra { get; }

        /// <summary>Unlearned: 안 익힌 룬 id (주문 부분 순서)</summary>
        public IReadOnlyList<string> MissingRuneIds { get; }

        /// <summary>Ambiguous: 같은 그림인 주문의 주문 그림 목록 번호 (처음 나온 순서)</summary>
        public IReadOnlyList<int> AmbiguousShapes { get; }

        /// <summary>이 판정에 쓴 부분 기준 (0~1, 이유 글에 씀)</summary>
        public float PartMin { get; }

        #region SpellJudgeSpec - 값을 받아 만듦 (빈 목록은 null로 받아도 됨)
        /// <param name="isCast">시전했는지</param>
        /// <param name="reason">취소 이유</param>
        /// <param name="candidate">시전한 그림 또는 이유를 보일 그림</param>
        /// <param name="accuracy">전체 일치율</param>
        /// <param name="partCoverage">부분마다 일치율</param>
        /// <param name="extra">남는 선 비율</param>
        /// <param name="missingRuneIds">안 익힌 룬 id</param>
        /// <param name="ambiguousShapes">같은 그림인 주문 번호</param>
        /// <param name="partMin">부분 기준</param>
        public SpellJudgeSpec(bool isCast, SpellJudgeReason reason, SpellCandidateEntry candidate, float accuracy,
            IReadOnlyList<float> partCoverage, float extra, IReadOnlyList<string> missingRuneIds, IReadOnlyList<int> ambiguousShapes, float partMin)
        {
            IsCast = isCast;
            Reason = reason;
            Candidate = candidate;
            Accuracy = accuracy;
            PartCoverage = partCoverage ?? Array.Empty<float>();
            Extra = extra;
            MissingRuneIds = missingRuneIds ?? Array.Empty<string>();
            AmbiguousShapes = ambiguousShapes ?? Array.Empty<int>();
            PartMin = partMin;
        }
        #endregion

        #region Cancel - 그림 없이 이유만 있는 취소 (빈 그림·너무 작음·비교 그림 없음·직접 취소)
        /// <param name="reason">취소 이유</param>
        public static SpellJudgeSpec Cancel(SpellJudgeReason reason) => new(false, reason, null, 0f, null, 0f, null, null, 0f);
        #endregion
    }
}
