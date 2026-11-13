using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// String Table 전부를 CSV로 내보내고 가져옴. 테이블 하나 = 파일 하나 ({출력 폴더}/CSV/{테이블 이름}.csv).
    /// 열: Key, Comment, 언어 코드(ko, en, ja …). 엑셀·Google 시트에서 편집하고 다시 가져오는 용도
    /// </summary>
    public static class LocalizationCsvDirector
    {
        /// <summary>CSV 폴더 이름 (셋업 값의 출력 폴더 아래)</summary>
        const string k_CsvFolderName = "CSV";

        /// <summary>키 열 이름</summary>
        const string k_KeyColumn = "Key";

        /// <summary>설명(번역가용 맥락) 열 이름</summary>
        const string k_CommentColumn = "Comment";

        #region ExportAll - 메뉴: 테이블 전부를 CSV로 내보내기
        [MenuItem("Tools/PaulowniaSky/Localization Kit/Export Tables to CSV")]
        public static void ExportAll()
        {
            // CSV 폴더
            var folder = GetFolder();
            Directory.CreateDirectory(folder);
            // 내보낸 파일 수
            var count = 0;
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                // 이 테이블 CSV 경로
                var path = $"{folder}/{collection.TableCollectionName}.csv";
                // 엑셀이 한글·한자를 알아보게 BOM 붙인 UTF-8
                using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
                    Csv.Export(writer, collection, CreateColumns());
                count++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Localization Kit] CSV 내보내기: {count}개 → {folder}");
        }
        #endregion

        #region ImportAll - 메뉴: CSV를 테이블 전부에 가져오기
        [MenuItem("Tools/PaulowniaSky/Localization Kit/Import Tables from CSV")]
        public static void ImportAll()
        {
            // CSV 폴더
            var folder = GetFolder();
            // 가져온 파일 수
            var count = 0;
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                // 이 테이블 CSV 경로
                var path = $"{folder}/{collection.TableCollectionName}.csv";
                if (!File.Exists(path))
                    continue;

                // BOM 있든 없든 UTF-8로 읽음
                using (var reader = new StreamReader(path, Encoding.UTF8, true))
                    Csv.ImportInto(reader, collection, CreateColumns(), true);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log(count == 0
                ? $"[Localization Kit] 가져올 CSV 없음: {folder}/{{테이블 이름}}.csv"
                : $"[Localization Kit] CSV 가져오기: {count}개 ← {folder}");
        }
        #endregion

        #region CreateColumns - 열 구성 (Key, Comment, 언어 코드들)
        /// <returns>CSV 열 구성</returns>
        static List<CsvColumns> CreateColumns()
        {
            // 열 구성: 키·설명 다음에 프로젝트 언어 순서대로
            var columns = new List<CsvColumns>
            {
                new KeyIdColumns
                {
                    IncludeId = false,
                    IncludeSharedComments = true,
                    KeyFieldName = k_KeyColumn,
                    SharedCommentFieldName = k_CommentColumn,
                },
            };
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                columns.Add(new LocaleColumns
                {
                    LocaleIdentifier = locale.Identifier,
                    FieldName = locale.Identifier.Code,
                    IncludeComments = false,
                });
            }
            return columns;
        }
        #endregion

        #region GetFolder - CSV 폴더 경로
        /// <returns>{셋업 값의 출력 폴더}/CSV</returns>
        static string GetFolder()
        {
            return $"{LocalizationSetupDirector.FindOrCreateSetup().OutputFolder}/{k_CsvFolderName}";
        }
        #endregion
    }
}
