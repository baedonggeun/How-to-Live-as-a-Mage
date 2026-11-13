using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit.Tests
{
    /// <summary>LocaleFontSetSO의 폰트·머티리얼 고르기 테스트</summary>
    public class LocaleFontSetSOTests
    {
        /// <summary>테스트할 폰트 세트</summary>
        LocaleFontSetSO m_FontSet;

        /// <summary>목록에 없는 언어용 기본 폰트 (빈 가짜)</summary>
        TMP_FontAsset m_DefaultFont;

        /// <summary>일본어 폰트 (빈 가짜)</summary>
        TMP_FontAsset m_JapaneseFont;

        /// <summary>일본어 외곽선 프리셋 (가짜)</summary>
        Material m_JapaneseOutline;

        #region SetUp - 가짜 폰트와 세트 만들기
        [SetUp]
        public void SetUp()
        {
            m_FontSet = ScriptableObject.CreateInstance<LocaleFontSetSO>();
            m_DefaultFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
            m_JapaneseFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
            m_JapaneseOutline = new Material(Shader.Find("Hidden/InternalErrorShader"));

            m_FontSet.DefaultFont = m_DefaultFont;
            m_FontSet.Set(new LocaleFontEntry("ja", m_JapaneseFont, m_JapaneseOutline));
        }
        #endregion

        #region TearDown - 만든 오브젝트 지우기
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_FontSet);
            Object.DestroyImmediate(m_DefaultFont);
            Object.DestroyImmediate(m_JapaneseFont);
            Object.DestroyImmediate(m_JapaneseOutline);
        }
        #endregion

        #region GetFont_ListedLocale_ReturnsEntryFont - 목록에 있는 언어는 그 폰트
        [Test]
        public void GetFont_ListedLocale_ReturnsEntryFont()
        {
            Assert.AreSame(m_JapaneseFont, m_FontSet.GetFont("ja"));
        }
        #endregion

        #region GetFont_UnlistedLocale_ReturnsDefaultFont - 목록에 없는 언어는 기본 폰트
        [Test]
        public void GetFont_UnlistedLocale_ReturnsDefaultFont()
        {
            Assert.AreSame(m_DefaultFont, m_FontSet.GetFont("de"));
        }
        #endregion

        #region GetMaterial_ListedLocale_ReturnsPreset - 목록에 있는 언어는 그 프리셋
        [Test]
        public void GetMaterial_ListedLocale_ReturnsPreset()
        {
            Assert.AreSame(m_JapaneseOutline, m_FontSet.GetMaterial("ja"));
        }
        #endregion

        #region GetMaterial_UnlistedLocale_ReturnsNull - 기본 폰트로 갈 땐 프리셋 없음
        [Test]
        public void GetMaterial_UnlistedLocale_ReturnsNull()
        {
            Assert.IsNull(m_FontSet.GetMaterial("de"));
        }
        #endregion

        #region Set_SameLocale_ReplacesEntry - 같은 언어를 다시 넣으면 교체
        [Test]
        public void Set_SameLocale_ReplacesEntry()
        {
            // 새로 넣을 일본어 폰트 (빈 가짜)
            var newFont = ScriptableObject.CreateInstance<TMP_FontAsset>();

            m_FontSet.Set(new LocaleFontEntry("ja", newFont));

            Assert.AreSame(newFont, m_FontSet.GetFont("ja"));
            Assert.IsNull(m_FontSet.GetMaterial("ja"));
            Object.DestroyImmediate(newFont);
        }
        #endregion
    }
}