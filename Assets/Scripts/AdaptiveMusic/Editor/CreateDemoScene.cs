using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace AdaptiveMusic.Editor
{
    /// <summary>
    ///     Editor utility to create a minimal Adaptive Music demo scene wired to MusicMixer.
    ///     Menu: Tools/Adaptive Music/Create Demo Scene
    /// </summary>
    public static class CreateDemoScene
    {
        private const string MixerPath = "Assets/MusicMixer.mixer";
        private const string ScenePath = "Assets/Scenes/AdaptiveMusicDemo.unity";

        [MenuItem("Tools/Adaptive Music/Create Demo Scene", priority = 10)]
        public static void CreateScene()
        {
            // Ensure Scenes folder exists
            var scenesDir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(scenesDir))
                Directory.CreateDirectory(scenesDir!);

            // New untitled scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ensure an AudioListener exists (DefaultGameObjects includes Main Camera)

            // Load mixer
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
                Debug.LogError(
                    $"[CreateDemoScene] Missing mixer at {MixerPath}. Create 'Assets/MusicMixer.mixer' with exposed Ambient/Tension/Combat params.");

            // Root: AdaptiveMusicSystem
            var root = new GameObject("AdaptiveMusicSystem");
            var system = root.AddComponent<AdaptiveMusicSystem>();

            // Create three child AudioSources
            var ambientGO = new GameObject("AmbientLayer");
            ambientGO.transform.SetParent(root.transform);
            var ambientSrc = ambientGO.AddComponent<AudioSource>();
            ambientSrc.loop = true;

            var tensionGO = new GameObject("TensionLayer");
            tensionGO.transform.SetParent(root.transform);
            var tensionSrc = tensionGO.AddComponent<AudioSource>();
            tensionSrc.loop = true;

            var combatGO = new GameObject("CombatLayer");
            combatGO.transform.SetParent(root.transform);
            var combatSrc = combatGO.AddComponent<AudioSource>();
            combatSrc.loop = true;

            // Assign private serialized fields via SerializedObject
            var so = new SerializedObject(system);
            if (mixer != null) so.FindProperty("mixer").objectReferenceValue = mixer;

            var layerSourcesProp = so.FindProperty("layerSources");
            if (layerSourcesProp != null)
            {
                layerSourcesProp.arraySize = 3;
                layerSourcesProp.GetArrayElementAtIndex(0).objectReferenceValue = ambientSrc;
                layerSourcesProp.GetArrayElementAtIndex(1).objectReferenceValue = tensionSrc;
                layerSourcesProp.GetArrayElementAtIndex(2).objectReferenceValue = combatSrc;
            }

            // Default server URL remains as in script; user can change in Inspector
            so.ApplyModifiedPropertiesWithoutUndo();

            // GameStateTracker
            var gst = new GameObject("GameStateTracker");
            gst.AddComponent<GameStateTracker>();

            // Optional: Local MIDI sample object
            var localPlayerGO = new GameObject("LocalMidiPlayer");
            var lpSrc = localPlayerGO.AddComponent<AudioSource>();
            lpSrc.playOnAwake = false;
            localPlayerGO.AddComponent<LocalMidiPlayer>();

            // Save the scene
            if (!EditorSceneManager.SaveScene(scene, ScenePath, true))
            {
                Debug.LogError($"[CreateDemoScene] Failed to save scene to {ScenePath}");
            }
            else
            {
                AssetDatabase.Refresh();
                Debug.Log(
                    $"[CreateDemoScene] Demo scene created at {ScenePath}.\n- Assign 'Assets/MusicMixer.mixer' groups to AudioSources if needed.\n- Place a .mid in 'Assets/StreamingAssets/Midi/' and set filename on LocalMidiPlayer.");
            }
        }
    }
}