using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PaulowniaSky.LocalizationKit.Tests
{
    /// <summary>DynamicFontSaveHandler가 저장 직전에 비울 폰트 에셋을 고르는지 테스트</summary>
    public class DynamicFontSaveHandlerTests
    {
        /// <summary>테스트할 Dynamic 폰트 에셋 (글자 0개, 원본 폰트 없음)</summary>
        TMP_FontAsset m_FontAsset;

        #region TearDown - 만든 오브젝트 지우기
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_FontAsset);
        }
        #endregion

        #region ShouldClear_OnlyKerningLeft_ReturnsTrue - 글자표가 비어도 커닝 기록이 남으면 비움
        [Test]
        public void ShouldClear_OnlyKerningLeft_ReturnsTrue()
        {
            m_FontAsset = CreateDynamicFontAsset(true);
            m_FontAsset.fontFeatureTable.glyphPairAdjustmentRecords.Add(new GlyphPairAdjustmentRecord());

            Assert.IsTrue(DynamicFontSaveHandler.ShouldClear(m_FontAsset));
        }
        #endregion

        #region ShouldClear_ClearOnBuildOff_ReturnsFalse - Clear Dynamic Data On Build가 꺼져 있으면 안 비움
        [Test]
        public void ShouldClear_ClearOnBuildOff_ReturnsFalse()
        {
            m_FontAsset = CreateDynamicFontAsset(false);
            m_FontAsset.fontFeatureTable.glyphPairAdjustmentRecords.Add(new GlyphPairAdjustmentRecord());

            Assert.IsFalse(DynamicFontSaveHandler.ShouldClear(m_FontAsset));
        }
        #endregion

        #region CreateDynamicFontAsset - 글자 0개인 Dynamic 폰트 에셋 만들기
        /// <param name="clearOnBuild">Clear Dynamic Data On Build를 켤지</param>
        /// <returns>만든 폰트 에셋</returns>
        static TMP_FontAsset CreateDynamicFontAsset(bool clearOnBuild)
        {
            // 만들 폰트 에셋
            var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();

            // Clear Dynamic Data On Build는 TMP에 internal 속성뿐이라 리플렉션으로 넣음.
            // SerializedObject로 적용하면 OnValidate가 돌아서, 머티리얼 없는 가짜 에셋에서 TMP가 예외를 냄
            typeof(TMP_FontAsset)
                .GetProperty("clearDynamicDataOnBuild", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fontAsset, clearOnBuild);

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            return fontAsset;
        }
        #endregion
    }
}
