using System;
using TMPro;
using UnityEditor;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 저장 직전에 Dynamic 폰트 에셋의 아틀라스(에디터에서 그린 글자 캐시)를 비움.
    /// 그린 글자가 파일에 남지 않아서 Play나 편집 뒤 저장해도 git diff가 생기지 않음.
    /// 대상: Dynamic(또는 DynamicOS)이고 Clear Dynamic Data On Build가 켜진 폰트 에셋. 빌드 때 버릴 데이터는 저장할 때도 버림
    /// </summary>
    public class DynamicFontSaveHandler : AssetModificationProcessor
    {
        #region OnWillSaveAssets - 저장할 에셋 중 Dynamic 폰트 에셋의 아틀라스 비우기
        /// <summary>Unity가 에셋을 저장하기 직전에 부름</summary>
        /// <param name="paths">저장할 에셋 경로</param>
        /// <returns>저장할 경로 (목록은 그대로)</returns>
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!path.EndsWith(".asset", StringComparison.Ordinal))
                    continue;

                // 이 경로의 TMP 폰트 에셋 (폰트 에셋이 아니면 null)
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null || !ShouldClear(fontAsset))
                    continue;

                fontAsset.ClearFontAssetData(true);
            }
            return paths;
        }
        #endregion

        #region ShouldClear - 비울 대상인지
        /// <param name="fontAsset">검사할 폰트 에셋</param>
        /// <returns>Dynamic이고, 빌드 때 비우게 돼 있고, 그린 글자가 남아 있으면 true</returns>
        static bool ShouldClear(TMP_FontAsset fontAsset)
        {
            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic
                && fontAsset.atlasPopulationMode != AtlasPopulationMode.DynamicOS)
                return false;

            // 빌드 때 아틀라스를 비우는지 (TMP에 공개 속성이 없어서 직렬화 값으로 읽음)
            var clearOnBuild = new SerializedObject(fontAsset).FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild == null || !clearOnBuild.boolValue)
                return false;

            // 이미 비어 있으면 안 건드림 (비운 상태는 글리프 0개, 아틀라스 1×1)
            return fontAsset.glyphTable.Count > 0 || (fontAsset.atlasTexture != null && fontAsset.atlasTexture.width > 1);
        }
        #endregion
    }
}
