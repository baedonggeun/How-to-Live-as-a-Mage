using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>판정 결과 글 (static pure function, 지금 언어로 읽음): 취소·실패 이유, 부분 일치율 목록, 주문 이름, %. 주문·룬 이름은 받은 코덱스 캡슐에서 읽음</summary>
    public static class SpellJudgeTextCalculator
    {
        /// <summary>이유: 그린 것이 없음</summary>
        const string WhyEmptyKey = "cast.why.empty";

        /// <summary>이유: 너무 작음</summary>
        const string WhySmallKey = "cast.why.small";

        /// <summary>이유: 비교할 주문 그림이 없음</summary>
        const string WhyNoneKey = "cast.why.none";

        /// <summary>이유: 맞는 주문이 없음</summary>
        const string WhyNoMatchKey = "cast.why.nomatch";

        /// <summary>이유: 부분이 모자람 ({0} 주문, {1} 모자란 부분, {2} 부분 기준 %)</summary>
        const string WhyPartsKey = "cast.why.parts";

        /// <summary>이유: 안 익힌 룬 ({0} 주문, {1} 룬)</summary>
        const string WhyUnlearnedKey = "cast.why.unlearned";

        /// <summary>이유: 하나로 안 정해짐 ({0} 주문 이름 목록)</summary>
        const string WhyAmbiguousKey = "cast.why.ambiguous";

        /// <summary>이유: 직접 취소</summary>
        const string WhyCanceledKey = "cast.why.canceled";

        /// <summary>부분 하나 ({0} 룬, {1} 일치율 %)</summary>
        const string PartKey = "cast.part";

        /// <summary>남는 선 ({0} %)</summary>
        const string ExtraKey = "cast.extra";

        /// <summary>주문 그리기 문구 테이블</summary>
        static readonly TableReference Table = "Cast";

        #region Reason - 취소·실패 이유 글 (부분 부족: 모자란 부분, 미습득: 빠진 룬, 애매함: 겹친 주문 이름)
        /// <param name="spec">판정 결과</param>
        /// <param name="codex">이름을 읽을 코덱스 캡슐</param>
        public static string Reason(SpellJudgeSpec spec, CodexService codex) => spec.Reason switch
        {
            SpellJudgeReason.Parts => Text(WhyPartsKey, SpellName(spec.Candidate, codex), Parts(spec, codex, true), Percent(spec.PartMin)),
            SpellJudgeReason.Unlearned => Text(WhyUnlearnedKey, SpellName(spec.Candidate, codex), TextListCalculator.Join(spec.MissingRuneIds.Select(id => RuneName(id, codex)))),
            SpellJudgeReason.Ambiguous => Text(WhyAmbiguousKey, TextListCalculator.Join(spec.AmbiguousShapes.Select(index => NameAt(index, codex)))),
            SpellJudgeReason.Empty => Text(WhyEmptyKey),
            SpellJudgeReason.Small => Text(WhySmallKey),
            SpellJudgeReason.NoCandidates => Text(WhyNoneKey),
            SpellJudgeReason.Canceled => Text(WhyCanceledKey),
            _ => Text(WhyNoMatchKey),
        };
        #endregion

        #region Parts - 부분 일치율 목록 ("불 96%, …". lowOnly면 부분 기준 아래만) + 남는 선 (lowOnly면 한도를 넘을 때만)
        /// <param name="spec">판정 결과 (Candidate 있음)</param>
        /// <param name="codex">이름을 읽을 코덱스 캡슐</param>
        /// <param name="lowOnly">모자란 것만 넣을지</param>
        public static string Parts(SpellJudgeSpec spec, CodexService codex, bool lowOnly)
        {
            // 넣을 항목 글
            var items = new List<string>();
            for (int i = 0; i < spec.PartCoverage.Count && i < spec.Candidate.PartRuneIds.Count; i++)
            {
                // 이 부분 일치율
                float coverage = spec.PartCoverage[i];
                if (lowOnly && coverage >= spec.PartMin) continue;
                items.Add(Text(PartKey, RuneName(spec.Candidate.PartRuneIds[i], codex), Percent(coverage)));
            }
            if (!lowOnly || spec.Extra > SpellRecognitionCalculator.MaxExtra) items.Add(Text(ExtraKey, Percent(spec.Extra)));
            return TextListCalculator.Join(items);
        }
        #endregion

        #region SpellName - 비교 그림의 주문 이름 (지금 언어, 없으면 빈 글)
        /// <param name="candidate">비교 그림</param>
        /// <param name="codex">이름을 읽을 코덱스 캡슐</param>
        public static string SpellName(SpellCandidateEntry candidate, CodexService codex) => candidate != null ? NameAt(candidate.ShapeIndex, codex) : string.Empty;
        #endregion

        #region Percent - 0~1을 % 정수로
        /// <param name="value">0~1 값</param>
        public static int Percent(float value) => Mathf.RoundToInt(value * 100f);
        #endregion

        #region NameAt - 주문 그림 번호의 주문 이름 (지금 언어, 코덱스가 없으면 빈 글)
        /// <param name="index">주문 그림 번호 (판정의 ShapeIndex)</param>
        /// <param name="codex">이름을 읽을 코덱스 캡슐</param>
        static string NameAt(int index, CodexService codex) => codex != null ? codex.NameAt(index) ?? string.Empty : string.Empty;
        #endregion

        #region RuneName - 룬 id의 이름 (지금 언어, 코덱스가 없거나 모르는 id면 id 그대로)
        /// <param name="id">룬 id</param>
        /// <param name="codex">이름을 읽을 코덱스 캡슐</param>
        static string RuneName(string id, CodexService codex) => codex != null ? codex.RuneName(id) : id;
        #endregion

        #region Text - 이 테이블 문구를 지금 언어로 읽음
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        static string Text(string key, params object[] args) => new LocalizedString(Table, key).GetLocalizedString(args);
        #endregion
    }
}
