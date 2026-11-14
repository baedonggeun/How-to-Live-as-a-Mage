using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>에디터 Play 시작 씬을 정함. 흐름 씬이면 Boot부터 띄우고, 편집 중이던 씬 이름을 GameManager에 넘김</summary>
    [InitializeOnLoad]
    static class PlayModeRouter
    {
        /// <summary>Boot 씬 경로</summary>
        const string BootScenePath = "Assets/Scenes/Boot.unity";

        #region PlayModeRouter - 에디터가 켜지거나 스크립트를 다시 읽을 때 구독하고 지금 씬 기준으로 맞춤
        static PlayModeRouter()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UpdateStartScene();
        }
        #endregion

        #region OnActiveSceneChanged - 편집 중인 씬이 바뀌면 시작 씬을 다시 맞춤
        /// <param name="previous">이전 씬</param>
        /// <param name="next">새로 편집하는 씬</param>
        static void OnActiveSceneChanged(Scene previous, Scene next) => UpdateStartScene();
        #endregion

        #region OnPlayModeStateChanged - Play 직전에 시작 씬과 편집 중이던 씬 이름을 다시 맞춤
        /// <param name="change">플레이 모드 변화</param>
        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) UpdateStartScene();
        }
        #endregion

        #region UpdateStartScene - 흐름 씬(빌드 목록에 있는 씬)이면 Boot부터, 아니면 그 씬 그대로 Play
        static void UpdateStartScene()
        {
            // 지금 편집 중인 씬
            Scene active = SceneManager.GetActiveScene();
            if (!IsInBuildList(active.path))
            {
                EditorSceneManager.playModeStartScene = null;
                SessionState.EraseString(GameManager.EditorStartSceneKey);
                return;
            }

            // Boot 씬 에셋
            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (boot == null)
            {
                Debug.LogError($"[PlayModeRouter] {BootScenePath} 없음");
                return;
            }
            EditorSceneManager.playModeStartScene = boot;

            // Boot에서 Play하면 Title부터
            string startScene = string.Equals(active.path, BootScenePath, System.StringComparison.Ordinal)
                ? TitleState.Scene
                : active.name;
            SessionState.SetString(GameManager.EditorStartSceneKey, startScene);
        }
        #endregion

        #region IsInBuildList - 씬 경로가 빌드 목록에 켜져 있는지
        /// <param name="scenePath">확인할 씬 경로</param>
        static bool IsInBuildList(string scenePath)
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && string.Equals(scene.path, scenePath, System.StringComparison.Ordinal))
                    return true;
            return false;
        }
        #endregion
    }
}
