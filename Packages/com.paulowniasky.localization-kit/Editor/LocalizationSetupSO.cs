using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 게임별 다국어 셋업 값. 셋업 메뉴(LocalizationSetupDirector)가 이걸 읽고 프로젝트를 맞춤
    /// </summary>
    [CreateAssetMenu(menuName = "PaulowniaSky/Localization Kit/Localization Setup", fileName = "LocalizationSetup")]
    public class LocalizationSetupSO : ScriptableObject
    {
        /// <summary>키트 원본 폰트 폴더</summary>
        public const string KitFontFolder = "Packages/com.paulowniasky.localization-kit/Fonts";

        /// <summary>원문 언어 코드. 번역 기준이고 최종 폴백</summary>
        [SerializeField] string m_SourceLocaleCode = "ko";

        /// <summary>번역 없는 언어가 먼저 볼 언어 코드. OS 언어로 못 정할 때 시작 언어도 이것</summary>
        [SerializeField] string m_FallbackLocaleCode = "en";

        /// <summary>처음 실행 때(고른 언어가 없을 때) OS 언어로 시작할지. 끄면 폴백 언어로 시작</summary>
        [SerializeField] bool m_UseSystemLocale = true;

        /// <summary>셋업할 언어 목록. 새로 만드는 언어는 이 순서로 붙음 (이후 순서는 Project Settings > Localization에서 바꿈)</summary>
        [SerializeField] List<LocaleSetupEntry> m_Locales = new List<LocaleSetupEntry>();

        /// <summary>설정·Locale·테이블·폰트 에셋을 만들 폴더 (Assets/ 아래)</summary>
        [SerializeField] string m_OutputFolder = "Assets/Localization";

        /// <summary>만들 String Table 이름</summary>
        [SerializeField] string m_StringTableName = "UI";

        /// <summary>원문 언어 코드</summary>
        public string SourceLocaleCode => m_SourceLocaleCode;

        /// <summary>폴백 언어 코드</summary>
        public string FallbackLocaleCode => m_FallbackLocaleCode;

        /// <summary>처음 실행 때 OS 언어로 시작할지</summary>
        public bool UseSystemLocale => m_UseSystemLocale;

        /// <summary>셋업할 언어 목록</summary>
        public IReadOnlyList<LocaleSetupEntry> Locales => m_Locales;

        /// <summary>출력 폴더</summary>
        public string OutputFolder => m_OutputFolder;

        /// <summary>String Table 이름</summary>
        public string StringTableName => m_StringTableName;

        #region Reset - 인스펙터 Reset이나 Create 메뉴로 만들 때 기본값 채우기
        void Reset()
        {
            FillDefaults();
        }
        #endregion

        #region FillDefaults - 13개 언어 프리셋과 키트 폰트로 채우기
        /// <summary>원문 ko, 폴백 en, 13개 언어와 키트 Noto 폰트로 채움</summary>
        public void FillDefaults()
        {
            m_SourceLocaleCode = "ko";
            m_FallbackLocaleCode = "en";
            m_UseSystemLocale = true;
            m_OutputFolder = "Assets/Localization";
            m_StringTableName = "UI";

            // 라틴·키릴 문자 언어가 같이 쓰는 폰트
            var latin = LoadKitFont("NotoSans-Regular.ttf");
            m_Locales = new List<LocaleSetupEntry>
            {
                new LocaleSetupEntry("ko", "한국어", LoadKitFont("NotoSansKR-Regular.otf")),
                new LocaleSetupEntry("en", "English", latin),
                new LocaleSetupEntry("ja", "日本語", LoadKitFont("NotoSansJP-Regular.otf")),
                new LocaleSetupEntry("zh-Hans", "简体中文", LoadKitFont("NotoSansSC-Regular.otf")),
                new LocaleSetupEntry("zh-Hant", "繁體中文", LoadKitFont("NotoSansTC-Regular.otf")),
                new LocaleSetupEntry("ru", "Русский", latin),
                new LocaleSetupEntry("de", "Deutsch", latin),
                new LocaleSetupEntry("fr", "Français", latin),
                new LocaleSetupEntry("es", "Español", latin),
                new LocaleSetupEntry("pt-BR", "Português (Brasil)", latin),
                new LocaleSetupEntry("it", "Italiano", latin),
                new LocaleSetupEntry("pl", "Polski", latin),
                new LocaleSetupEntry("tr", "Türkçe", latin),
            };
        }
        #endregion

        #region IndexOf - 언어 코드의 목록 위치
        /// <summary>언어 코드가 목록 몇 번째인지. 없으면 -1</summary>
        /// <param name="code">찾을 언어 코드</param>
        public int IndexOf(string code)
        {
            for (var i = 0; i < m_Locales.Count; i++)
            {
                if (m_Locales[i].Code == code)
                    return i;
            }
            return -1;
        }
        #endregion

        #region Validate - 셋업 전에 값 검사
        /// <summary>셋업에 쓸 수 있는 값인지 검사</summary>
        /// <returns>문제 없으면 null, 있으면 이유</returns>
        public string Validate()
        {
            if (m_Locales == null || m_Locales.Count == 0)
                return "언어 목록이 비어 있음";

            // 이미 본 언어 코드
            var codes = new HashSet<string>();
            foreach (var entry in m_Locales)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Code))
                    return "언어 코드가 빈 항목이 있음";
                if (!codes.Add(entry.Code))
                    return $"언어 코드가 겹침: {entry.Code}";
                if (entry.Font == null)
                    return $"{entry.Code}의 폰트가 비어 있음";
            }

            if (!codes.Contains(m_SourceLocaleCode))
                return $"원문 언어 {m_SourceLocaleCode}가 목록에 없음";
            if (!codes.Contains(m_FallbackLocaleCode))
                return $"폴백 언어 {m_FallbackLocaleCode}가 목록에 없음";
            if (string.IsNullOrWhiteSpace(m_OutputFolder) || !m_OutputFolder.StartsWith("Assets/", StringComparison.Ordinal))
                return "출력 폴더는 Assets/ 아래여야 함";
            if (string.IsNullOrWhiteSpace(m_StringTableName))
                return "String Table 이름이 비어 있음";
            return null;
        }
        #endregion

        #region LoadKitFont - 키트 원본 폰트 불러오기
        /// <param name="fileName">Fonts 폴더 안 파일 이름</param>
        static Font LoadKitFont(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Font>($"{KitFontFolder}/{fileName}");
        }
        #endregion
    }
}
