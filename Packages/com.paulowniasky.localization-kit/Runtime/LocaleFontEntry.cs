using System;
using TMPro;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 언어 하나에 쓸 폰트·머티리얼. LocaleFontSetSO 목록의 한 줄
    /// </summary>
    [Serializable]
    public class LocaleFontEntry
    {
        /// <summary>언어 코드 (ko, ja, zh-Hans 등). Locale 에셋의 코드와 같아야 함</summary>
        [SerializeField] string m_LocaleCode;

        /// <summary>이 언어에 쓸 TMP 폰트 에셋</summary>
        [SerializeField] TMP_FontAsset m_Font;

        /// <summary>이 언어에 쓸 머티리얼 프리셋 (외곽선 등). 비우면 폰트 기본 머티리얼</summary>
        [SerializeField] Material m_Material;

        /// <summary>언어 코드</summary>
        public string LocaleCode => m_LocaleCode;

        /// <summary>폰트 에셋</summary>
        public TMP_FontAsset Font => m_Font;

        /// <summary>머티리얼 프리셋. 없으면 null</summary>
        public Material Material => m_Material;

        #region LocaleFontEntry() - 빈 항목 (Unity 직렬화·인스펙터용)
        /// <summary>빈 항목을 만듦. Unity가 직렬화·인스펙터에서 씀</summary>
        public LocaleFontEntry() { }
        #endregion

        #region LocaleFontEntry(localeCode, font, material) - 값 넣어 만들기 (셋업 메뉴·테스트용)
        /// <summary>값을 넣어 항목을 만듦</summary>
        /// <param name="localeCode">언어 코드</param>
        /// <param name="font">폰트 에셋</param>
        /// <param name="material">머티리얼 프리셋. 없으면 null</param>
        public LocaleFontEntry(string localeCode, TMP_FontAsset font, Material material = null)
        {
            m_LocaleCode = localeCode;
            m_Font = font;
            m_Material = material;
        }
        #endregion
    }
}