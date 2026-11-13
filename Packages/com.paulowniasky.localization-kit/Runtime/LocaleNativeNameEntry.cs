using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// Locale에 붙이는 메타데이터. 언어 선택 UI에 보일 그 언어 글자 이름 (한국어, 日本語 등).
    /// Locale 이름(LocaleName)은 Addressables 그룹 이름에 쓰여서 영어 그대로 두고, 표시 이름만 여기 둠
    /// </summary>
    [Serializable]
    [Metadata(AllowedTypes = MetadataType.Locale, AllowMultiple = false, MenuItem = "PaulowniaSky/Native Name")]
    public class LocaleNativeNameEntry : IMetadata
    {
        /// <summary>그 언어 글자로 쓴 언어 이름</summary>
        [SerializeField] string m_Name;

        /// <summary>그 언어 글자로 쓴 언어 이름</summary>
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        #region LocaleNativeNameEntry() - 빈 메타데이터 (Unity 직렬화·인스펙터용)
        /// <summary>빈 메타데이터를 만듦. Unity가 직렬화·인스펙터에서 씀</summary>
        public LocaleNativeNameEntry() { }
        #endregion

        #region LocaleNativeNameEntry(name) - 이름 넣어 만들기 (셋업 메뉴용)
        /// <summary>이름을 넣어 메타데이터를 만듦</summary>
        /// <param name="name">그 언어 글자로 쓴 언어 이름</param>
        public LocaleNativeNameEntry(string name)
        {
            m_Name = name;
        }
        #endregion
    }
}
