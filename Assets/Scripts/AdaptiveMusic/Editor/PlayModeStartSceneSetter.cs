using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdaptiveMusic.Editor
{
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetter
    {
        private const string AutoSetKey = "AdaptiveMusic.AutoSetPlayScene";
        private const string ScenePath = "Assets/Scenes/AdaptiveMusic.unity";
        private const string MenuPathSet = "Tools/AdaptiveMusic/Set Play Mode Start Scene";
        private const string MenuPathAuto = "Tools/AdaptiveMusic/Auto Set Play Mode Start Scene";

        static PlayModeStartSceneSetter()
        {
            if (!EditorPrefs.GetBool(AutoSetKey, true))
            {
                return;
            }

            SetPlayModeStartScene(false);
        }

        [MenuItem(MenuPathSet)]
        private static void SetPlayModeStartSceneMenu()
        {
            SetPlayModeStartScene(true);
        }

        [MenuItem(MenuPathAuto, true)]
        private static bool ToggleAutoValidate()
        {
            Menu.SetChecked(MenuPathAuto, EditorPrefs.GetBool(AutoSetKey, true));
            return true;
        }

        [MenuItem(MenuPathAuto)]
        private static void ToggleAuto()
        {
            var newValue = !EditorPrefs.GetBool(AutoSetKey, true);
            EditorPrefs.SetBool(AutoSetKey, newValue);
            Menu.SetChecked(MenuPathAuto, newValue);

            if (newValue)
            {
                SetPlayModeStartScene(true);
            }
        }

        private static void SetPlayModeStartScene(bool log)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene == null)
            {
                if (log)
                {
                    Debug.LogWarning($"[AdaptiveMusic] Start scene not found at {ScenePath}.");
                }
                return;
            }

            EditorSceneManager.playModeStartScene = scene;
            EnsureSceneInBuildSettings(ScenePath);

            if (log)
            {
                Debug.Log($"[AdaptiveMusic] Play Mode start scene set to {ScenePath}.");
            }
        }

        private static void EnsureSceneInBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == path))
            {
                return;
            }

            var list = scenes.ToList();
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
