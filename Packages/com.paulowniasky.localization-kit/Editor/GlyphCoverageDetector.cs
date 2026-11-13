using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 빠진 글자를 찾음. 언어마다 번역문에 쓰인 글자가 그 언어 폰트(FontSet) → 폰트 자체 폴백 → TMP 전역 폴백 →
    /// TMP 기본 폰트 순서(TMP가 글자를 찾는 순서) 안에 있는지 확인. 없으면 화면에 □로 나옴.
    /// Dynamic 폰트는 원본 폰트 파일로 확인해서 아틀라스를 건드리지 않음
    /// </summary>
    public static class GlyphCoverageDetector
    {
        #region Check - 메뉴: 빠진 글자 보고
        [MenuItem("Tools/PaulowniaSky/Localization Kit/Check Missing Glyphs")]
        public static void Check()
        {
            // 보고서 본문
            var report = new StringBuilder();
            // 빠진 글자 총수
            var total = 0;
            // 프로젝트의 폰트 세트 (본문, 제목 등)
            var fontSets = LoadFontSets();
            if (fontSets.Count == 0)
            {
                Debug.LogWarning("[Localization Kit] Locale Font Set 에셋이 없음. 셋업 메뉴를 먼저 실행");
                return;
            }

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                // 이 언어 번역문 전체
                var text = CollectText(locale);
                if (text.Length == 0)
                    continue;

                foreach (var fontSet in fontSets)
                {
                    // 이 언어 폰트로 못 그리는 글자
                    var missing = FindMissing(fontSet.GetFont(locale.Identifier.Code), text);
                    if (missing.Count == 0)
                        continue;

                    total += missing.Count;
                    report.AppendLine($"{fontSet.name} / {locale.Identifier.Code}: {missing.Count}자 — {Format(missing)}");
                }
            }

            Debug.Log(total == 0
                ? $"[Localization Kit] 빠진 글자 없음 (FontSet {fontSets.Count}개)"
                : $"[Localization Kit] 빠진 글자 {total}자\n{report}");
        }
        #endregion

        #region FindMissing - 폰트와 폴백으로 못 그리는 글자 찾기
        /// <summary>TMP가 글자를 찾는 순서대로 폰트를 따라가며, 어디에도 없는 글자를 모음</summary>
        /// <param name="primary">먼저 쓸 폰트 (null이면 TMP 폴백·기본 폰트만 봄)</param>
        /// <param name="text">검사할 글</param>
        /// <returns>못 그리는 글자의 코드 포인트 (처음 나온 순서)</returns>
        public static List<int> FindMissing(TMP_FontAsset primary, string text)
        {
            // 아직 못 찾은 글자
            var missing = DistinctCodePoints(text);
            foreach (var font in BuildChain(primary))
            {
                if (missing.Count == 0)
                    break;
                RemoveSupported(font, missing);
            }
            return missing;
        }
        #endregion

        #region DistinctCodePoints - 글에 쓰인 글자 (중복·제어 문자 빼고)
        /// <param name="text">글</param>
        /// <returns>코드 포인트 (처음 나온 순서, 서로게이트 쌍은 한 글자로)</returns>
        public static List<int> DistinctCodePoints(string text)
        {
            // 글자 목록
            var result = new List<int>();
            if (string.IsNullOrEmpty(text))
                return result;

            // 이미 넣은 글자
            var seen = new HashSet<int>();
            for (var i = 0; i < text.Length; i++)
            {
                // 이번 글자의 코드 포인트
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    if (char.IsControl(text[i]))
                        continue;
                    codePoint = text[i];
                }

                if (seen.Add(codePoint))
                    result.Add(codePoint);
            }
            return result;
        }
        #endregion

        #region BuildChain - TMP가 글자를 찾는 폰트 순서
        /// <param name="primary">먼저 쓸 폰트</param>
        static List<TMP_FontAsset> BuildChain(TMP_FontAsset primary)
        {
            // 찾는 순서대로 폰트 (중복 없이)
            var chain = new List<TMP_FontAsset>();
            AddUnique(chain, primary);
            if (primary != null && primary.fallbackFontAssetTable != null)
            {
                foreach (var font in primary.fallbackFontAssetTable)
                    AddUnique(chain, font);
            }
            // TMP 기본 리소스를 안 들였으면 전역 폴백·기본 폰트도 없음
            if (TMP_Settings.instance == null)
                return chain;
            if (TMP_Settings.fallbackFontAssets != null)
            {
                foreach (var font in TMP_Settings.fallbackFontAssets)
                    AddUnique(chain, font);
            }
            AddUnique(chain, TMP_Settings.defaultFontAsset);
            return chain;
        }
        #endregion

        #region AddUnique - 없을 때만 목록에 넣기
        /// <param name="chain">폰트 순서</param>
        /// <param name="font">넣을 폰트</param>
        static void AddUnique(List<TMP_FontAsset> chain, TMP_FontAsset font)
        {
            if (font != null && !chain.Contains(font))
                chain.Add(font);
        }
        #endregion

        #region RemoveSupported - 이 폰트에 있는 글자를 목록에서 빼기
        /// <param name="font">확인할 폰트</param>
        /// <param name="codePoints">아직 못 찾은 글자 (여기서 빠짐)</param>
        static void RemoveSupported(TMP_FontAsset font, List<int> codePoints)
        {
            // Dynamic 폰트는 원본 폰트 파일로 확인 (아틀라스에 아직 안 그린 글자도 그릴 수 있어서)
            var useSourceFont = font.atlasPopulationMode != AtlasPopulationMode.Static && font.sourceFontFile != null;
            if (useSourceFont && FontEngine.LoadFontFace(font.sourceFontFile) == FontEngineError.Success)
            {
                codePoints.RemoveAll(codePoint => FontEngine.TryGetGlyphIndex((uint)codePoint, out var glyphIndex) && glyphIndex != 0);
                return;
            }
            codePoints.RemoveAll(codePoint => font.characterLookupTable != null && font.characterLookupTable.ContainsKey((uint)codePoint));
        }
        #endregion

        #region CollectText - 한 언어의 번역문을 전부 이어 붙이기
        /// <param name="locale">언어</param>
        static string CollectText(Locale locale)
        {
            // 이어 붙인 번역문
            var text = new StringBuilder();
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (!(collection.GetTable(locale.Identifier) is StringTable table))
                    continue;
                foreach (var entry in table.Values)
                    text.Append(entry.LocalizedValue);
            }
            return text.ToString();
        }
        #endregion

        #region LoadFontSets - 프로젝트의 폰트 세트 모두
        static List<LocaleFontSetSO> LoadFontSets()
        {
            // 폰트 세트 목록
            var fontSets = new List<LocaleFontSetSO>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(LocaleFontSetSO)}"))
            {
                // 불러온 폰트 세트
                var fontSet = AssetDatabase.LoadAssetAtPath<LocaleFontSetSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (fontSet != null)
                    fontSets.Add(fontSet);
            }
            return fontSets;
        }
        #endregion

        #region Format - 빠진 글자를 글자 + 코드로 표시
        /// <param name="codePoints">빠진 글자</param>
        static string Format(List<int> codePoints)
        {
            // 글자 부분
            var glyphs = new StringBuilder();
            // 코드 부분
            var codes = new StringBuilder();
            foreach (var codePoint in codePoints)
            {
                // BMP 글자는 그대로 (짝 없는 서로게이트도 ConvertFromUtf32 예외 없이 표시)
                glyphs.Append(codePoint < 0x10000 ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint));
                codes.Append($" U+{codePoint:X4}");
            }
            return $"{glyphs} ({codes.ToString().Trim()})";
        }
        #endregion
    }
}
