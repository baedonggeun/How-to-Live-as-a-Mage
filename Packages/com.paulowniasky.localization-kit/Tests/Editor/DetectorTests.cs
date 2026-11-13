using System.Collections.Generic;
using NUnit.Framework;

namespace PaulowniaSky.LocalizationKit.Tests
{
    /// <summary>누락 검사(번역·글자)의 순수 함수 테스트</summary>
    public class DetectorTests
    {
        #region FindMissingKeys_EmptyOrNull_ReturnsThoseKeys - 빈 값·없는 값만 빠진 것
        [Test]
        public void FindMissingKeys_EmptyOrNull_ReturnsThoseKeys()
        {
            // 키 → 번역 값 (b는 빈 값, c는 없음)
            var values = new Dictionary<string, string> { { "a", "Hello" }, { "b", "" } };

            var missing = MissingTranslationDetector.FindMissingKeys(new[] { "a", "b", "c" },
                key => values.TryGetValue(key, out var value) ? value : null);

            CollectionAssert.AreEqual(new[] { "b", "c" }, missing);
        }
        #endregion

        #region DistinctCodePoints_SkipsDuplicatesAndControls - 중복·제어 문자 빼고 처음 순서대로
        [Test]
        public void DistinctCodePoints_SkipsDuplicatesAndControls()
        {
            var codePoints = GlyphCoverageDetector.DistinctCodePoints("가A가\nA");

            CollectionAssert.AreEqual(new[] { (int)'가', (int)'A' }, codePoints);
        }
        #endregion

        #region DistinctCodePoints_SurrogatePair_IsOneCodePoint - 서로게이트 쌍은 한 글자
        [Test]
        public void DistinctCodePoints_SurrogatePair_IsOneCodePoint()
        {
            var codePoints = GlyphCoverageDetector.DistinctCodePoints("😀");

            CollectionAssert.AreEqual(new[] { 0x1F600 }, codePoints);
        }
        #endregion
    }
}
