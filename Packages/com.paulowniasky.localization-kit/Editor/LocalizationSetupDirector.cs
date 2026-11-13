using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 다국어 셋업 메뉴. LocalizationSetupSO를 읽어 설정, Locale, 셀렉터, String Table, 폰트 에셋,
    /// 본문 FontSet, TMP Settings, 텍스트 프리팹을 만들거나 맞춤.
    /// 다시 실행해도 안전: 없는 것만 만들고 값이 다른 것만 고침. 언어 삭제는 번역이 지워져서 안 함
    /// </summary>
    public static class LocalizationSetupDirector
    {
        /// <summary>셋업 값 에셋을 새로 만들 때 위치</summary>
        const string k_SetupAssetPath = "Assets/Localization/LocalizationSetup.asset";

        /// <summary>본문 폰트 세트 이름</summary>
        const string k_BodyFontSetName = "Body";

        /// <summary>텍스트 프리팹 이름</summary>
        const string k_TextPrefabName = "LocalizedText";

        /// <summary>폰트 에셋 아틀라스 한 변 크기 (한글·한자는 글자가 많아서 2048)</summary>
        const int k_AtlasSize = 2048;

        /// <summary>폰트 에셋 샘플링 크기 (TMP 기본값)</summary>
        const int k_SamplingPointSize = 90;

        /// <summary>폰트 에셋 아틀라스 패딩 (TMP 기본값)</summary>
        const int k_AtlasPadding = 9;

        /// <summary>폰트 에셋 머티리얼 셰이더 (TMP 메뉴로 만든 것과 같은 데스크톱 SDF)</summary>
        const string k_FontShaderName = "TextMeshPro/Distance Field";

        #region SetupProject - 메뉴: 셋업 값 에셋을 찾거나 만들고 실행
        [MenuItem("Tools/PaulowniaSky/Localization Kit/Setup Project")]
        public static void SetupProject()
        {
            Run(FindOrCreateSetup());
        }
        #endregion

        #region Run - 셋업 실행
        /// <summary>셋업 값대로 프로젝트를 맞춤</summary>
        /// <param name="setup">셋업 값</param>
        public static void Run(LocalizationSetupSO setup)
        {
            if (setup == null)
                throw new ArgumentNullException(nameof(setup));

            // 셋업 값 검사 결과 (문제 없으면 null)
            var error = setup.Validate();
            if (error != null)
            {
                Debug.LogError($"[Localization Kit] 셋업 중단: {error}", setup);
                return;
            }

            if (!EnsureTmpEssentials())
                return;

            // 이번 실행에서 만들거나 바꾼 것 (마지막에 콘솔에 한 번에 출력)
            var log = new StringBuilder();
            EnsureFolders(setup.OutputFolder);
            // 활성 Localization 설정
            var settings = EnsureSettings(setup, log);
            EnsureFallbackEnabled(settings, log);
            // 셋업 목록 순서의 Locale
            var locales = EnsureLocales(setup, log);
            EnsureSelectors(settings, setup, log);
            EnsureStringTable(setup, locales, log);
            // 원본 폰트 → TMP 폰트 에셋
            var fontAssets = EnsureFontAssets(setup, log);
            // 본문 폰트 세트
            var fontSet = EnsureFontSet(setup, fontAssets, log);
            EnsureTmpSettings(setup, fontAssets, log);
            EnsureTextPrefab(setup, fontSet, log);
            AssetDatabase.SaveAssets();

            Debug.Log(log.Length == 0
                ? "[Localization Kit] 셋업: 바뀐 것 없음"
                : $"[Localization Kit] 셋업 완료\n{log}", setup);
        }
        #endregion

        #region FindOrCreateSetup - 셋업 값 에셋 찾기, 없으면 기본값으로 만들기
        /// <returns>프로젝트의 셋업 값 에셋</returns>
        static LocalizationSetupSO FindOrCreateSetup()
        {
            // 프로젝트에 있는 셋업 값 에셋 GUID
            var guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationSetupSO)}");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<LocalizationSetupSO>(AssetDatabase.GUIDToAssetPath(guids[0]));

            EnsureFolder(Path.GetDirectoryName(k_SetupAssetPath).Replace('\\', '/'));
            // 새 셋업 값 (13개 언어 프리셋)
            var setup = ScriptableObject.CreateInstance<LocalizationSetupSO>();
            setup.FillDefaults();
            AssetDatabase.CreateAsset(setup, k_SetupAssetPath);
            Debug.Log($"[Localization Kit] 셋업 값 에셋 생성: {k_SetupAssetPath}", setup);
            return setup;
        }
        #endregion

        #region EnsureTmpEssentials - TMP 기본 리소스 확인, 없으면 임포트
        /// <returns>이미 있으면 true. 방금 임포트했으면 false (끝난 뒤 메뉴 다시 실행)</returns>
        static bool EnsureTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
                return true;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.LogWarning("[Localization Kit] TMP 기본 리소스를 임포트함. 끝나면 셋업 메뉴를 다시 실행");
            return false;
        }
        #endregion

        #region EnsureFolders - 출력 폴더와 하위 폴더 만들기
        /// <param name="root">출력 폴더 (Assets/ 아래)</param>
        static void EnsureFolders(string root)
        {
            EnsureFolder(root);
            foreach (var child in new[] { "Locales", "Tables", "Fonts", "FontSets", "Prefabs" })
                EnsureFolder($"{root}/{child}");
        }
        #endregion

        #region EnsureFolder - 폴더 하나 만들기 (없는 상위 폴더까지)
        /// <param name="path">만들 폴더 (Assets/ 아래)</param>
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            // 상위 폴더 경로
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        #endregion

        #region EnsureSettings - 활성 Localization 설정 확인, 없으면 만들기
        /// <param name="setup">셋업 값</param>
        /// <param name="log">실행 기록</param>
        /// <returns>활성 Localization 설정</returns>
        static LocalizationSettings EnsureSettings(LocalizationSetupSO setup, StringBuilder log)
        {
            // 지금 활성 설정 (없으면 null)
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
                return settings;

            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Default Localization Settings";
            // 새 설정 경로 (Project Settings의 Create 버튼과 같은 이름)
            var path = $"{setup.OutputFolder}/Localization Settings.asset";
            AssetDatabase.CreateAsset(settings, path);
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            log.AppendLine($"Localization 설정 생성: {path}");
            return settings;
        }
        #endregion

        #region EnsureFallbackEnabled - 번역이 없을 때 폴백 언어를 보게 켜기
        /// <param name="settings">활성 Localization 설정</param>
        /// <param name="log">실행 기록</param>
        static void EnsureFallbackEnabled(LocalizationSettings settings, StringBuilder log)
        {
            // 문자열 테이블 데이터베이스 (기본값은 폴백 꺼짐이라 Locale 폴백 메타데이터가 무시됨)
            var stringDatabase = settings.GetStringDatabase();
            // 에셋 테이블 데이터베이스 (문자열과 같은 규칙으로 맞춤)
            var assetDatabase = settings.GetAssetDatabase();
            if (stringDatabase.UseFallback && assetDatabase.UseFallback)
                return;

            stringDatabase.UseFallback = true;
            assetDatabase.UseFallback = true;
            EditorUtility.SetDirty(settings);
            log.AppendLine("폴백 켬: 번역이 없으면 Locale 폴백 순서대로 찾음");
        }
        #endregion

        #region EnsureLocales - 언어마다 Locale 만들기, 새 Locale 순서·표시 이름·폴백 맞추기
        /// <param name="setup">셋업 값</param>
        /// <param name="log">실행 기록</param>
        /// <returns>셋업 목록 순서의 Locale</returns>
        static List<Locale> EnsureLocales(LocalizationSetupSO setup, StringBuilder log)
        {
            // 셋업 목록 순서의 Locale
            var locales = new List<Locale>();
            // 이번 실행에서 새로 만든 Locale (순서는 이것만 정함. 기존 순서는 Project Settings에서 바꾼 대로 둠)
            var created = new HashSet<Locale>();
            foreach (var entry in setup.Locales)
            {
                // 이미 있는 이 코드의 Locale (없으면 null)
                var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(entry.Code));
                if (locale == null)
                {
                    locale = Locale.CreateLocale(entry.Code);
                    // 영어 이름 (모르는 코드면 코드 그대로)
                    var englishName = string.IsNullOrEmpty(locale.name) ? entry.Code : locale.name;
                    // Locale Generator와 같은 형식: 영어 이름 (코드)
                    locale.LocaleName = $"{englishName} ({entry.Code})";
                    // 새 Locale 에셋 경로
                    var path = $"{setup.OutputFolder}/Locales/{locale.LocaleName}.asset";
                    AssetDatabase.CreateAsset(locale, path);
                    LocalizationEditorSettings.AddLocale(locale);
                    created.Add(locale);
                    log.AppendLine($"Locale 생성: {path}");
                }
                locales.Add(locale);
            }

            // 새 Locale이 받을 순서 (기존 Locale 맨 뒤부터 셋업 목록 순서대로)
            var nextOrder = NextSortOrder(created);

            // 원문 언어 Locale (최종 폴백)
            var source = locales[setup.IndexOf(setup.SourceLocaleCode)];
            // 번역 없는 언어가 먼저 볼 Locale
            var fallback = locales[setup.IndexOf(setup.FallbackLocaleCode)];
            for (var i = 0; i < locales.Count; i++)
            {
                // 이 줄의 Locale
                var locale = locales[i];
                // 이 줄의 셋업 값
                var entry = setup.Locales[i];
                // 폴백 대상: 원문은 없음, 폴백 언어는 원문, 나머지는 폴백 언어
                var target = locale == source ? null : locale == fallback ? source : fallback;

                // 이번에 바뀐 게 있는지
                var changed = false;
                if (created.Contains(locale))
                {
                    locale.SortOrder = nextOrder++;
                    changed = true;
                }
                changed |= SetNativeName(locale, entry.NativeName);
                changed |= SetFallback(locale, target);

                if (!changed)
                    continue;
                EditorUtility.SetDirty(locale);
                log.AppendLine($"Locale 갱신: {entry.Code} (순서 {locale.SortOrder}, 표시 이름 {entry.NativeName}, 폴백 {(target != null ? target.Identifier.Code : "없음")})");
            }
            return locales;
        }
        #endregion

        #region NextSortOrder - 새 Locale이 받을 첫 순서
        /// <param name="created">이번 실행에서 새로 만든 Locale</param>
        /// <returns>기존 Locale의 가장 뒤 순서 + 1. 기존 Locale이 없으면 0</returns>
        static ushort NextSortOrder(HashSet<Locale> created)
        {
            // 기존 Locale 중 가장 뒤 순서 (없으면 -1)
            var last = -1;
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                if (!created.Contains(locale) && locale.SortOrder > last)
                    last = locale.SortOrder;
            }
            return (ushort)Math.Min(last + 1, ushort.MaxValue);
        }
        #endregion

        #region SetNativeName - Locale에 그 언어 글자 이름 메타데이터 맞추기
        /// <param name="locale">대상 Locale</param>
        /// <param name="nativeName">그 언어 글자 이름</param>
        /// <returns>바꿨으면 true</returns>
        static bool SetNativeName(Locale locale, string nativeName)
        {
            if (string.IsNullOrEmpty(nativeName))
                return false;

            // 이미 붙은 표시 이름 메타데이터 (없으면 null)
            var metadata = locale.Metadata.GetMetadata<LocaleNativeNameEntry>();
            if (metadata == null)
            {
                locale.Metadata.AddMetadata(new LocaleNativeNameEntry(nativeName));
                return true;
            }
            if (metadata.Name == nativeName)
                return false;

            metadata.Name = nativeName;
            return true;
        }
        #endregion

        #region SetFallback - Locale의 폴백 메타데이터 맞추기
        /// <param name="locale">대상 Locale</param>
        /// <param name="target">번역이 없을 때 볼 Locale. null이면 폴백 없음</param>
        /// <returns>바꿨으면 true</returns>
        static bool SetFallback(Locale locale, Locale target)
        {
            // 이미 붙은 폴백 메타데이터 (없으면 null)
            var metadata = locale.Metadata.GetMetadata<FallbackLocale>();
            if (target == null)
            {
                if (metadata == null)
                    return false;
                locale.Metadata.RemoveMetadata(metadata);
                return true;
            }

            if (metadata == null)
            {
                locale.Metadata.AddMetadata(new FallbackLocale(target));
                return true;
            }
            if (metadata.Locale == target)
                return false;

            metadata.Locale = target;
            return true;
        }
        #endregion

        #region EnsureSelectors - 시작 언어 고르는 순서 맞추기
        /// <param name="settings">활성 Localization 설정</param>
        /// <param name="setup">셋업 값</param>
        /// <param name="log">실행 기록</param>
        static void EnsureSelectors(LocalizationSettings settings, LocalizationSetupSO setup, StringBuilder log)
        {
            // 시작 언어 셀렉터 목록 (위에서부터 물어봄)
            var selectors = settings.GetStartupLocaleSelectors();
            if (IsSelectorOrderCorrect(selectors, setup.FallbackLocaleCode))
                return;

            selectors.Clear();
            selectors.Add(new CommandLineLocaleSelector());
            selectors.Add(new PlayerPrefLocaleSelector());
            selectors.Add(new SystemLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(setup.FallbackLocaleCode) });
            EditorUtility.SetDirty(settings);
            log.AppendLine($"셀렉터 순서: 명령줄 → 마지막 선택 → OS 언어 → {setup.FallbackLocaleCode}");
        }
        #endregion

        #region IsSelectorOrderCorrect - 셀렉터가 이미 정한 순서인지
        /// <param name="selectors">지금 셀렉터 목록</param>
        /// <param name="fallbackCode">마지막 셀렉터가 고를 언어 코드</param>
        static bool IsSelectorOrderCorrect(List<IStartupLocaleSelector> selectors, string fallbackCode)
        {
            return selectors.Count == 4
                && selectors[0] is CommandLineLocaleSelector
                && selectors[1] is PlayerPrefLocaleSelector
                && selectors[2] is SystemLocaleSelector
                && selectors[3] is SpecificLocaleSelector specific
                && specific.LocaleId.Code == fallbackCode;
        }
        #endregion

        #region EnsureStringTable - String Table 만들기, 빠진 언어 테이블 추가
        /// <param name="setup">셋업 값</param>
        /// <param name="locales">셋업 목록 순서의 Locale</param>
        /// <param name="log">실행 기록</param>
        static void EnsureStringTable(LocalizationSetupSO setup, List<Locale> locales, StringBuilder log)
        {
            // 이미 있는 테이블 묶음 (없으면 null)
            var collection = LocalizationEditorSettings.GetStringTableCollection(setup.StringTableName);
            if (collection == null)
            {
                // 테이블 폴더
                var folder = $"{setup.OutputFolder}/Tables";
                LocalizationEditorSettings.CreateStringTableCollection(setup.StringTableName, folder, locales);
                log.AppendLine($"String Table 생성: {folder}/{setup.StringTableName}");
                return;
            }

            foreach (var locale in locales)
            {
                if (collection.ContainsTable(locale.Identifier))
                    continue;
                collection.AddNewTable(locale.Identifier);
                log.AppendLine($"String Table 언어 추가: {setup.StringTableName}_{locale.Identifier.Code}");
            }
        }
        #endregion

        #region EnsureFontAssets - 원본 폰트마다 TMP 폰트 에셋 만들기
        /// <param name="setup">셋업 값</param>
        /// <param name="log">실행 기록</param>
        /// <returns>원본 폰트 → TMP 폰트 에셋 (만들지 못한 폰트는 빠짐)</returns>
        static Dictionary<Font, TMP_FontAsset> EnsureFontAssets(LocalizationSetupSO setup, StringBuilder log)
        {
            // 원본 폰트 → TMP 폰트 에셋
            var fontAssets = new Dictionary<Font, TMP_FontAsset>();
            foreach (var entry in setup.Locales)
            {
                if (fontAssets.ContainsKey(entry.Font))
                    continue;

                // 폰트 에셋 이름 (TMP 메뉴와 같은 규칙: 원본 파일 이름 + " SDF")
                var assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(entry.Font)) + " SDF";
                // 폰트 에셋 경로
                var path = $"{setup.OutputFolder}/Fonts/{assetName}.asset";
                // 이미 있는 폰트 에셋 (없으면 null)
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null)
                {
                    fontAsset = CreateFontAsset(entry.Font, assetName, path);
                    if (fontAsset == null)
                        continue;
                    log.AppendLine($"폰트 에셋 생성: {path}");
                }
                fontAssets[entry.Font] = fontAsset;
            }
            return fontAssets;
        }
        #endregion

        #region CreateFontAsset - Dynamic TMP 폰트 에셋 만들어 저장
        /// <param name="font">원본 폰트 파일</param>
        /// <param name="assetName">폰트 에셋 이름</param>
        /// <param name="path">저장할 경로</param>
        /// <returns>만든 폰트 에셋. 실패하면 null</returns>
        static TMP_FontAsset CreateFontAsset(Font font, string assetName, string path)
        {
            // 새 Dynamic 폰트 에셋 (2048 아틀라스, Multi Atlas)
            var fontAsset = TMP_FontAsset.CreateFontAsset(font, k_SamplingPointSize, k_AtlasPadding, GlyphRenderMode.SDFAA,
                k_AtlasSize, k_AtlasSize, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"[Localization Kit] 폰트 에셋을 못 만듦: {font.name} (임포트 설정의 Include Font Data 확인)", font);
                return null;
            }

            // 런타임 API는 모바일 셰이더를 써서, TMP 메뉴로 만든 것과 같은 셰이더로 바꿈
            var shader = Shader.Find(k_FontShaderName);
            if (shader != null)
                fontAsset.material.shader = shader;

            fontAsset.name = assetName;
            fontAsset.atlasTextures[0].name = assetName + " Atlas";
            fontAsset.material.name = assetName + " Atlas Material";
            AssetDatabase.CreateAsset(fontAsset, path);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            // 빌드엔 에디터에서 쌓인 글자를 빼고 빈 아틀라스로 시작
            var serialized = new SerializedObject(fontAsset);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }
        #endregion

        #region EnsureFontSet - 본문 FontSet 만들기, 언어별 폰트 맞추기
        /// <param name="setup">셋업 값</param>
        /// <param name="fontAssets">원본 폰트 → TMP 폰트 에셋</param>
        /// <param name="log">실행 기록</param>
        /// <returns>본문 FontSet</returns>
        static LocaleFontSetSO EnsureFontSet(LocalizationSetupSO setup, Dictionary<Font, TMP_FontAsset> fontAssets, StringBuilder log)
        {
            // FontSet 경로
            var path = $"{setup.OutputFolder}/FontSets/{k_BodyFontSetName}.asset";
            // 이미 있는 FontSet (없으면 null)
            var fontSet = AssetDatabase.LoadAssetAtPath<LocaleFontSetSO>(path);
            if (fontSet == null)
            {
                fontSet = ScriptableObject.CreateInstance<LocaleFontSetSO>();
                AssetDatabase.CreateAsset(fontSet, path);
                log.AppendLine($"FontSet 생성: {path}");
            }

            // 이번에 바뀐 게 있는지
            var changed = false;
            // 원문 언어의 원본 폰트 (이 폰트 에셋이 기본 폰트)
            var sourceFont = setup.Locales[setup.IndexOf(setup.SourceLocaleCode)].Font;
            if (fontAssets.TryGetValue(sourceFont, out var defaultFont) && fontSet.DefaultFont != defaultFont)
            {
                fontSet.DefaultFont = defaultFont;
                changed = true;
                log.AppendLine($"FontSet {k_BodyFontSetName}: 기본 폰트 → {defaultFont.name}");
            }

            foreach (var entry in setup.Locales)
            {
                if (!fontAssets.TryGetValue(entry.Font, out var fontAsset))
                    continue;

                // 이미 있는 이 언어 항목 (없으면 null)
                var existing = fontSet.Find(entry.Code);
                // 폰트가 같으면 그대로 둠 (항목에 넣어 둔 머티리얼 프리셋을 지키려고)
                if (existing != null && existing.Font == fontAsset)
                    continue;

                fontSet.Set(new LocaleFontEntry(entry.Code, fontAsset));
                changed = true;
                log.AppendLine($"FontSet {k_BodyFontSetName}: {entry.Code} → {fontAsset.name}");
            }

            if (changed)
                EditorUtility.SetDirty(fontSet);
            return fontSet;
        }
        #endregion

        #region EnsureTmpSettings - TMP 기본 폰트, 전역 폴백, 한국어 어절 줄바꿈
        /// <param name="setup">셋업 값</param>
        /// <param name="fontAssets">원본 폰트 → TMP 폰트 에셋</param>
        /// <param name="log">실행 기록</param>
        static void EnsureTmpSettings(LocalizationSetupSO setup, Dictionary<Font, TMP_FontAsset> fontAssets, StringBuilder log)
        {
            // 원문 언어의 원본 폰트 (이 폰트 에셋이 TMP 기본 폰트)
            var sourceFont = setup.Locales[setup.IndexOf(setup.SourceLocaleCode)].Font;
            if (!fontAssets.TryGetValue(sourceFont, out var defaultFont))
                return;

            // TMP Settings 편집용
            var serialized = new SerializedObject(TMP_Settings.instance);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = defaultFont;
            serialized.FindProperty("m_UseModernHangulLineBreakingRules").boolValue = true;

            // 전역 폴백: 기본 폰트 말고 모든 폰트 에셋 (언어 바뀌는 순간, 플레이어 입력, 언어 이름 표시용)
            var fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            foreach (var entry in setup.Locales)
            {
                if (!fontAssets.TryGetValue(entry.Font, out var fontAsset) || fontAsset == defaultFont || Contains(fallbacks, fontAsset))
                    continue;
                fallbacks.arraySize++;
                fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = fontAsset;
            }

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
                log.AppendLine("TMP Settings: 기본 폰트, 전역 폴백, 한국어 어절 줄바꿈");
        }
        #endregion

        #region Contains - 오브젝트 배열 속성에 이 오브젝트가 있는지
        /// <param name="array">오브젝트 배열 속성</param>
        /// <param name="target">찾을 오브젝트</param>
        static bool Contains(SerializedProperty array, UnityEngine.Object target)
        {
            for (var i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == target)
                    return true;
            }
            return false;
        }
        #endregion

        #region EnsureTextPrefab - 기본 텍스트 프리팹 만들기 (TMP + 문자열 연결 + 폰트 바인더)
        /// <param name="setup">셋업 값</param>
        /// <param name="fontSet">본문 FontSet</param>
        /// <param name="log">실행 기록</param>
        static void EnsureTextPrefab(LocalizationSetupSO setup, LocaleFontSetSO fontSet, StringBuilder log)
        {
            // 프리팹 경로
            var path = $"{setup.OutputFolder}/Prefabs/{k_TextPrefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            // 열린 씬을 안 건드리려고 미리보기 씬에서 만듦
            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                // 프리팹으로 저장할 오브젝트
                var root = new GameObject(k_TextPrefabName, typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(root, previewScene);

                // TMP 텍스트
                var text = root.AddComponent<TextMeshProUGUI>();
                text.font = fontSet.DefaultFont;
                text.text = k_TextPrefabName;

                // 문자열 연결: Update String 이벤트 → TMP text (Localization의 Localize 메뉴와 같은 방식)
                var localize = root.AddComponent<LocalizeStringEvent>();
                // TMP text 속성의 set 메서드
                var setter = typeof(TMP_Text).GetProperty(nameof(TMP_Text.text)).GetSetMethod();
                // set 메서드를 이 텍스트에 묶은 이벤트 대상
                var action = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, setter);
                UnityEventTools.AddPersistentListener(localize.OnUpdateString, action);
                localize.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

                // 언어별 폰트 교체
                var binder = root.AddComponent<LocaleFontUIBinder>();
                var serialized = new SerializedObject(binder);
                serialized.FindProperty("m_FontSet").objectReferenceValue = fontSet;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.AppendLine($"텍스트 프리팹 생성: {path}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
        #endregion
    }
}
