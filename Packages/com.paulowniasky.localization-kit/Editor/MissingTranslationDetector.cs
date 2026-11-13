using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 빠진 번역을 찾음. 모든 String Table 묶음 × 모든 언어에서 값이 없거나 빈 키를 셈.
    /// 결과는 번역할 일 목록 (실행 중엔 폴백 언어로 보이지만 번역은 안 된 칸)
    /// </summary>
    public static class MissingTranslationDetector
    {
        /// <summary>언어마다 콘솔에 보여 줄 빠진 키 최대 개수</summary>
        const int k_MaxKeysShown = 10;

        #region Check - 메뉴: 빠진 번역 보고
        [MenuItem("Tools/PaulowniaSky/Localization Kit/Check Missing Translations")]
        public static void Check()
        {
            // 보고서 본문
            var report = new StringBuilder();
            // 빠진 칸 총수
            var total = 0;
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                // 이 묶음의 키 목록
                var keys = new List<string>();
                foreach (var shared in collection.SharedData.Entries)
                    keys.Add(shared.Key);

                foreach (var locale in LocalizationEditorSettings.GetLocales())
                {
                    // 이 언어 테이블 (없으면 전부 빠진 것)
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    // 값이 없거나 빈 키
                    var missing = FindMissingKeys(keys, key => table != null ? table.GetEntry(key)?.LocalizedValue : null);
                    if (missing.Count == 0)
                        continue;

                    total += missing.Count;
                    report.AppendLine($"{collection.TableCollectionName} / {locale.Identifier.Code}: {missing.Count}칸 — {Preview(missing)}");
                }
            }

            Debug.Log(total == 0
                ? "[Localization Kit] 빠진 번역 없음"
                : $"[Localization Kit] 빠진 번역 {total}칸\n{report}");
        }
        #endregion

        #region FindMissingKeys - 값이 없거나 빈 키 찾기
        /// <summary>키마다 번역 값을 물어보고, 없거나 빈 키를 모음</summary>
        /// <param name="keys">검사할 키</param>
        /// <param name="getValue">키 → 번역 값 (없으면 null)</param>
        /// <returns>값이 없거나 빈 키 (입력 순서대로)</returns>
        public static List<string> FindMissingKeys(IEnumerable<string> keys, Func<string, string> getValue)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            // 빠진 키
            var missing = new List<string>();
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(getValue(key)))
                    missing.Add(key);
            }
            return missing;
        }
        #endregion

        #region Preview - 빠진 키 앞부분만 한 줄로
        /// <param name="keys">빠진 키</param>
        static string Preview(List<string> keys)
        {
            // 보여 줄 개수
            var shown = Math.Min(keys.Count, k_MaxKeysShown);
            // 앞부분 키를 쉼표로 이은 글
            var text = string.Join(", ", keys.GetRange(0, shown));
            return keys.Count > shown ? $"{text} 외 {keys.Count - shown}개" : text;
        }
        #endregion
    }
}
