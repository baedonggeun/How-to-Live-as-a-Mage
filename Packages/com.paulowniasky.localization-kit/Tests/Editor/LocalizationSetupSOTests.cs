using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit.Tests
{
    /// <summary>LocalizationSetupSO 기본값·검사 테스트</summary>
    public class LocalizationSetupSOTests
    {
        /// <summary>테스트할 셋업 값 (기본값 채움)</summary>
        LocalizationSetupSO m_Setup;

        #region SetUp - 기본값 채운 셋업 값 만들기
        [SetUp]
        public void SetUp()
        {
            m_Setup = ScriptableObject.CreateInstance<LocalizationSetupSO>();
            m_Setup.FillDefaults();
        }
        #endregion

        #region TearDown - 만든 오브젝트 지우기
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Setup);
        }
        #endregion

        #region FillDefaults_Has13Locales - 기본 언어 13개
        [Test]
        public void FillDefaults_Has13Locales()
        {
            Assert.AreEqual(13, m_Setup.Locales.Count);
        }
        #endregion

        #region FillDefaults_AllKitFontsLoaded - 기본 폰트가 전부 키트에서 불러와짐
        [Test]
        public void FillDefaults_AllKitFontsLoaded()
        {
            foreach (var entry in m_Setup.Locales)
                Assert.IsNotNull(entry.Font, entry.Code);
        }
        #endregion

        #region Validate_Defaults_ReturnsNull - 기본값은 문제 없음
        [Test]
        public void Validate_Defaults_ReturnsNull()
        {
            Assert.IsNull(m_Setup.Validate());
        }
        #endregion

        #region Validate_SourceNotInList_ReturnsError - 원문 언어가 목록에 없으면 문제
        [Test]
        public void Validate_SourceNotInList_ReturnsError()
        {
            // 셋업 값 편집용
            var serialized = new SerializedObject(m_Setup);
            serialized.FindProperty("m_SourceLocaleCode").stringValue = "xx";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsNotNull(m_Setup.Validate());
        }
        #endregion

        #region Validate_DuplicateCode_ReturnsError - 언어 코드가 겹치면 문제
        [Test]
        public void Validate_DuplicateCode_ReturnsError()
        {
            // 셋업 값 편집용
            var serialized = new SerializedObject(m_Setup);
            serialized.FindProperty("m_Locales").GetArrayElementAtIndex(1).FindPropertyRelative("m_Code").stringValue = "ko";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsNotNull(m_Setup.Validate());
        }
        #endregion

        #region IndexOf_ReturnsListPosition - 언어 코드의 목록 위치
        [Test]
        public void IndexOf_ReturnsListPosition()
        {
            Assert.AreEqual(2, m_Setup.IndexOf("ja"));
            Assert.AreEqual(-1, m_Setup.IndexOf("xx"));
        }
        #endregion
    }
}
