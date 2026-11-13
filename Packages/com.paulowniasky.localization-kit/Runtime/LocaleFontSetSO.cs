using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 언어별 폰트 묶음. 폰트 역할(본문, 제목 등)마다 에셋 하나
    /// </summary>
    [CreateAssetMenu(menuName = "PaulowniaSky/Localization Kit/Locale Font Set", fileName = "LocaleFontSet")]
    public class LocaleFontSetSO : ScriptableObject
    {
        /// <summary>목록에 없는 언어에 쓸 폰트</summary>
        [SerializeField] TMP_FontAsset m_DefaultFont;

        /// <summary>언어별 폰트 목록</summary>
        [SerializeField] List<LocaleFontEntry> m_Entries = new List<LocaleFontEntry>();

        /// <summary>목록에 없는 언어에 쓸 폰트</summary>
        public TMP_FontAsset DefaultFont
        {
            get => m_DefaultFont;
            set => m_DefaultFont = value;
        }

        #region Find - 언어 코드로 항목 찾기
        /// <summary>언어 코드에 맞는 항목을 찾음. 없으면 null</summary>
        /// <param name="localeCode">찾을 언어 코드</param>
        public LocaleFontEntry Find(string localeCode)
        {
            foreach (var entry in m_Entries)
            {
                if (entry.LocaleCode == localeCode)
                    return entry;
            }
            return null;
        }
        #endregion

        #region GetFont - 언어에 쓸 폰트 고르기
        /// <summary>언어 코드에 쓸 폰트. 목록에 없거나 폰트가 비어 있으면 기본 폰트</summary>
        /// <param name="localeCode">언어 코드</param>
        public TMP_FontAsset GetFont(string localeCode)
        {
            // 이 언어 항목 (없으면 null)
            var entry = Find(localeCode);
            return entry != null && entry.Font != null ? entry.Font : m_DefaultFont;
        }
        #endregion

        #region GetMaterial - 언어에 쓸 머티리얼 고르기
        /// <summary>언어 코드에 쓸 머티리얼 프리셋. 항목이나 프리셋이 없거나 기본 폰트로 넘어가면 null</summary>
        /// <param name="localeCode">언어 코드</param>
        public Material GetMaterial(string localeCode)
        {
            // 이 언어 항목 (없으면 null)
            var entry = Find(localeCode);
            return entry != null && entry.Font != null ? entry.Material : null;
        }
        #endregion

        #region Set - 항목 추가·교체
        /// <summary>같은 언어 코드 항목이 있으면 바꾸고, 없으면 추가. 셋업 메뉴·테스트용</summary>
        /// <param name="entry">넣을 항목</param>
        public void Set(LocaleFontEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // 같은 언어 코드 항목 위치 (없으면 -1)
            var index = m_Entries.FindIndex(e => e.LocaleCode == entry.LocaleCode);
            if (index >= 0)
                m_Entries[index] = entry;
            else
                m_Entries.Add(entry);
        }
        #endregion
    }
}