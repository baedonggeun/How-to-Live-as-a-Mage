using System;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 셋업할 언어 하나: 코드, 표시 이름, 원본 폰트 파일. LocalizationSetupSO 목록의 한 줄
    /// </summary>
    [Serializable]
    public class LocaleSetupEntry
    {
        /// <summary>언어 코드 (ko, en, zh-Hans 등)</summary>
        [SerializeField] string m_Code;

        /// <summary>언어 선택 UI에 보일 그 언어 글자 이름 (한국어, 日本語 등)</summary>
        [SerializeField] string m_NativeName;

        /// <summary>이 언어의 TMP 폰트 에셋을 만들 원본 폰트 파일. 게임 스타일 폰트로 바꿔 끼우는 곳</summary>
        [SerializeField] Font m_Font;

        /// <summary>언어 코드</summary>
        public string Code => m_Code;

        /// <summary>그 언어 글자 이름</summary>
        public string NativeName => m_NativeName;

        /// <summary>원본 폰트 파일</summary>
        public Font Font => m_Font;

        #region LocaleSetupEntry() - 빈 항목 (Unity 직렬화·인스펙터용)
        /// <summary>빈 항목을 만듦. Unity가 직렬화·인스펙터에서 씀</summary>
        public LocaleSetupEntry() { }
        #endregion

        #region LocaleSetupEntry(code, nativeName, font) - 값 넣어 만들기 (기본값 채우기용)
        /// <summary>값을 넣어 항목을 만듦</summary>
        /// <param name="code">언어 코드</param>
        /// <param name="nativeName">그 언어 글자 이름</param>
        /// <param name="font">원본 폰트 파일</param>
        public LocaleSetupEntry(string code, string nativeName, Font font)
        {
            m_Code = code;
            m_NativeName = nativeName;
            m_Font = font;
        }
        #endregion
    }
}
