using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AdaptiveMusic.Editor
{
    /// <summary>
    ///     Editor utility for quickly setting up the Adaptive Music System.
    /// </summary>
    public class AdaptiveMusicSetup : EditorWindow
    {
        private AudioMixer mixer;
        private string serverUrl = "ws://localhost:8765";

        private void OnGUI()
        {
            GUILayout.Label("Adaptive Music System Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will help you set up the Adaptive Music System in your scene.\n\n" +
                "It will create:\n" +
                "- AdaptiveMusicSystem GameObject with AudioSources\n" +
                "- GameStateTracker GameObject\n" +
                "- DemoController GameObject",
                MessageType.Info);

            GUILayout.Space(10);

            mixer = (AudioMixer)EditorGUILayout.ObjectField("Audio Mixer", mixer, typeof(AudioMixer), false);
            serverUrl = EditorGUILayout.TextField("Server URL", serverUrl);

            GUILayout.Space(20);

            if (GUILayout.Button("Create Setup", GUILayout.Height(40))) CreateSetup();

            GUILayout.Space(10);

            if (GUILayout.Button("Create AudioMixer Template")) CreateAudioMixerTemplate();
        }

        [MenuItem("Tools/Adaptive Music/Setup Scene")]
        private static void Init()
        {
            var window = (AdaptiveMusicSetup)GetWindow(typeof(AdaptiveMusicSetup));
            window.titleContent = new GUIContent("Adaptive Music Setup");
            window.Show();
        }

        private void CreateSetup()
        {
            // Check if already exists
            if (FindFirstObjectByType<AdaptiveMusicSystem>() != null)
                if (!EditorUtility.DisplayDialog("Already Exists",
                        "AdaptiveMusicSystem already exists in scene. Create anyway?",
                        "Yes", "Cancel"))
                    return;

            // Create main system GameObject
            var mainSystem = new GameObject("AdaptiveMusicSystem");
            var musicSystem = mainSystem.AddComponent<AdaptiveMusicSystem>();

            // Create layer AudioSources as children
            var sources = new AudioSource[3];
            string[] layerNames = { "AmbientLayer", "TensionLayer", "CombatLayer" };

            for (var i = 0; i < 3; i++)
            {
                var layerObj = new GameObject(layerNames[i]);
                layerObj.transform.SetParent(mainSystem.transform);

                sources[i] = layerObj.AddComponent<AudioSource>();
                sources[i].loop = true;
                sources[i].playOnAwake = false;
                sources[i].volume = 1f;

                // Set output to mixer group if available
                if (mixer != null)
                {
                    // Note: This requires the mixer groups to exist
                    // You'll need to manually assign mixer groups in the Inspector
                }
            }

            // Set server URL via reflection (since it's private)
            var serverUrlField = typeof(AdaptiveMusicSystem).GetField("serverUrl",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (serverUrlField != null) serverUrlField.SetValue(musicSystem, serverUrl);

            // Set mixer and sources via reflection
            var mixerField = typeof(AdaptiveMusicSystem).GetField("mixer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (mixerField != null && mixer != null) mixerField.SetValue(musicSystem, mixer);

            var sourcesField = typeof(AdaptiveMusicSystem).GetField("layerSources",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourcesField != null) sourcesField.SetValue(musicSystem, sources);

            // Create GameStateTracker
            var trackerObj = new GameObject("GameStateTracker");
            trackerObj.AddComponent<GameStateTracker>();

            // Create DemoController
            var demoObj = new GameObject("DemoController");
            var demoController = demoObj.AddComponent<DemoController>();

            Debug.Log("[AdaptiveMusicSetup] Scene setup complete! " +
                      "Don't forget to:\n" +
                      "1. Assign zone configs to AdaptiveMusicSystem\n" +
                      "2. Assign mixer groups to AudioSources\n" +
                      "3. Assign references to DemoController");

            // Select the main system for easy configuration
            Selection.activeGameObject = mainSystem;
        }

        private void CreateAudioMixerTemplate()
        {
            EditorUtility.DisplayDialog("Create AudioMixer",
                "AudioMixer assets must be created using Unity's built-in menu:\n\n" +
                "1. Right-click in Project window\n" +
                "2. Select 'Create > Audio Mixer'\n" +
                "3. Name it 'MusicMixer'\n" +
                "4. Open the mixer\n" +
                "5. Create 3 groups: Ambient, Tension, Combat\n" +
                "6. Expose Volume parameters for each group\n" +
                "7. Rename exposed parameters: AmbientVolume, TensionVolume, CombatVolume",
                "OK");
        }
    }

    /// <summary>
    ///     Custom inspector for MusicConfigSO with helper buttons.
    /// </summary>
    [CustomEditor(typeof(MusicConfigSO))]
    public class MusicConfigSOEditor : UnityEditor.Editor
    {
        private static readonly string[] InstrumentNames =
        {
            "(empty)",
            "Acoustic Grand", "Bright Acoustic", "Electric Grand", "Honky-Tonk", "Electric Piano 1", "Electric Piano 2", "Harpsichord", "Clav",
            "Celesta", "Glockenspiel", "Music Box", "Vibraphone", "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
            "Drawbar Organ", "Percussive Organ", "Rock Organ", "Church Organ", "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
            "Acoustic Guitar(nylon)", "Acoustic Guitar(steel)", "Electric Guitar(jazz)", "Electric Guitar(clean)", "Electric Guitar(muted)",
            "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics",
            "Acoustic Bass", "Electric Bass(finger)", "Electric Bass(pick)", "Fretless Bass", "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2",
            "Violin", "Viola", "Cello", "Contrabass", "Tremolo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani",
            "String Ensemble 1", "String Ensemble 2", "SynthStrings 1", "SynthStrings 2", "Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit",
            "Trumpet", "Trombone", "Tuba", "Muted Trumpet", "French Horn", "Brass Section", "SynthBrass 1", "SynthBrass 2",
            "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax", "Oboe", "English Horn", "Bassoon", "Clarinet",
            "Piccolo", "Flute", "Recorder", "Pan Flute", "Blown Bottle", "Skakuhachi", "Whistle", "Ocarina",
            "Lead 1 (square)", "Lead 2 (sawtooth)", "Lead 3 (calliope)", "Lead 4 (chiff)", "Lead 5 (charang)", "Lead 6 (voice)", "Lead 7 (fifths)", "Lead 8 (bass+lead)",
            "Pad 1 (new age)", "Pad 2 (warm)", "Pad 3 (polysynth)", "Pad 4 (choir)", "Pad 5 (bowed)", "Pad 6 (metallic)", "Pad 7 (halo)", "Pad 8 (sweep)",
            "FX 1 (rain)", "FX 2 (soundtrack)", "FX 3 (crystal)", "FX 4 (atmosphere)", "FX 5 (brightness)", "FX 6 (goblins)", "FX 7 (echoes)", "FX 8 (sci-fi)",
            "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "Bagpipe", "Fiddle", "Shanai",
            "Tinkle Bell", "Agogo", "Steel Drums", "Woodblock", "Taiko Drum", "Melodic Tom", "Synth Drum", "Reverse Cymbal",
            "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet", "Telephone Ring", "Helicopter", "Applause", "Gunshot"
        };

        private static readonly string[] DrumKitNames =
        {
            "None",
            "Standard",
            "Room",
            "Power",
            "Electric",
            "TR-808",
            "Jazz",
            "Blush",
            "Orchestra"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("zoneName"));

            var layersProp = serializedObject.FindProperty("layers");
            if (layersProp != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

                for (var i = 0; i < layersProp.arraySize; i++)
                {
                    var layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (layerProp == null) continue;

                    var layerNameProp = layerProp.FindPropertyRelative("name");
                    var layerLabel = layerNameProp != null && !string.IsNullOrWhiteSpace(layerNameProp.stringValue)
                        ? layerNameProp.stringValue
                        : $"Layer {i}";

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField(layerLabel, EditorStyles.boldLabel);

                    DrawLayerFields(layerProp);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);
            var config = (MusicConfigSO)target;

            if (GUILayout.Button("Validate Configuration")) ValidateConfig(config);

            if (GUILayout.Button("Generate Test MIDI")) GenerateTestMIDI(config);
        }

        private void DrawLayerFields(SerializedProperty layerProp)
        {
            var seedProp = layerProp.FindPropertyRelative("seed");
            var genEventsProp = layerProp.FindPropertyRelative("gen_events");
            var bpmProp = layerProp.FindPropertyRelative("bpm");
            var timeSigProp = layerProp.FindPropertyRelative("time_sig");
            var keySigProp = layerProp.FindPropertyRelative("key_sig");
            var drumKitProp = layerProp.FindPropertyRelative("drum_kit");
            var allowCcProp = layerProp.FindPropertyRelative("allow_cc");
            var tempProp = layerProp.FindPropertyRelative("temp");
            var topPProp = layerProp.FindPropertyRelative("top_p");
            var topKProp = layerProp.FindPropertyRelative("top_k");
            var instrumentsProp = layerProp.FindPropertyRelative("instruments");

            EditorGUILayout.PropertyField(layerProp.FindPropertyRelative("name"));
            if (seedProp != null) EditorGUILayout.PropertyField(seedProp);
            if (genEventsProp != null) EditorGUILayout.PropertyField(genEventsProp);
            if (bpmProp != null) EditorGUILayout.PropertyField(bpmProp);
            if (timeSigProp != null) EditorGUILayout.PropertyField(timeSigProp);
            if (keySigProp != null) EditorGUILayout.PropertyField(keySigProp);

            if (drumKitProp != null)
            {
                var current = drumKitProp.stringValue ?? "None";
                var idx = Array.IndexOf(DrumKitNames, current);
                if (idx < 0) idx = 0;
                var nextIdx = EditorGUILayout.Popup("Drum Kit", idx, DrumKitNames);
                drumKitProp.stringValue = DrumKitNames[nextIdx];
            }

            if (allowCcProp != null) EditorGUILayout.PropertyField(allowCcProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Instruments (pick 3)", EditorStyles.boldLabel);
            DrawInstrumentsDropdowns(instrumentsProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Sampling Controls", EditorStyles.boldLabel);
            if (tempProp != null) EditorGUILayout.PropertyField(tempProp);
            if (topPProp != null) EditorGUILayout.PropertyField(topPProp);
            if (topKProp != null) EditorGUILayout.PropertyField(topKProp);
        }

        private static void DrawInstrumentsDropdowns(SerializedProperty instrumentsProp)
        {
            if (instrumentsProp == null || !instrumentsProp.isArray)
            {
                EditorGUILayout.HelpBox("Missing instruments array on layer.", MessageType.Warning);
                return;
            }

            const int desiredCount = 3;
            if (instrumentsProp.arraySize != desiredCount) instrumentsProp.arraySize = desiredCount;

            for (var i = 0; i < desiredCount; i++)
            {
                var element = instrumentsProp.GetArrayElementAtIndex(i);
                if (element == null) continue;

                var current = element.stringValue ?? "";
                var idx = Array.IndexOf(InstrumentNames, current);
                if (idx < 0) idx = string.IsNullOrWhiteSpace(current) ? 0 : 1;

                var nextIdx = EditorGUILayout.Popup($"Instrument {i + 1}", idx, InstrumentNames);
                element.stringValue = nextIdx == 0 ? string.Empty : InstrumentNames[nextIdx];
            }
        }

        private void ValidateConfig(MusicConfigSO config)
        {
            var valid = true;
            var issues = "";

            if (string.IsNullOrEmpty(config.zoneName))
            {
                issues += "- Zone name is empty\n";
                valid = false;
            }

            if (config.layers == null || config.layers.Length == 0)
            {
                issues += "- No layers defined\n";
                valid = false;
            }

            foreach (var layer in config.layers)
            {
                if (string.IsNullOrEmpty(layer.name))
                {
                    issues += "- Layer has no name\n";
                    valid = false;
                }

                if (layer.instruments == null || layer.instruments.Length == 0)
                {
                    issues += $"- Layer '{layer.name}' has no instruments\n";
                    valid = false;
                }

                if (layer.gen_events < 64 || layer.gen_events > 512)
                    issues += $"- Layer '{layer.name}' gen_events out of range (64-512)\n";

                if (layer.bpm < 40 || layer.bpm > 200) issues += $"- Layer '{layer.name}' BPM out of range (40-200)\n";
            }

            if (valid)
                EditorUtility.DisplayDialog("Validation Success",
                    "Configuration is valid!", "OK");
            else
                EditorUtility.DisplayDialog("Validation Failed",
                    "Configuration has issues:\n\n" + issues, "OK");
        }

        private async void GenerateTestMIDI(MusicConfigSO config)
        {
            if (config.layers == null || config.layers.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No layers to test", "OK");
                return;
            }

            // Show layer selection dialog
            var layerNames = new string[config.layers.Length];
            for (var i = 0; i < config.layers.Length; i++) layerNames[i] = config.layers[i].name;

            // For now, test the first layer (ambient)
            var testLayer = config.layers[0];

            EditorUtility.DisplayProgressBar("Generating MIDI",
                $"Connecting to server for layer '{testLayer.name}'...", 0.3f);

            try
            {
                // Create a temporary WebSocket client
                var serverUrl = "ws://localhost:8765";
                var client = new WebSocketMusicClient(serverUrl);

                var connected = await client.Connect();

                if (!connected || !client.IsConnected)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Connection Failed",
                        $"Could not connect to WebSocket server at {serverUrl}\n\n" +
                        "Make sure the server is running:\n" +
                        "python examples/ws_server_standalone.py", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Generating MIDI",
                    $"Requesting MIDI generation for '{testLayer.name}'...", 0.6f);

                // Generate MIDI
                var midiData = await client.RequestMIDI(testLayer.ToParams());

                if (midiData == null || midiData.Length == 0)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Generation Failed",
                        "Server returned no MIDI data. Check the Unity console for details.", "OK");
                    await client.Disconnect();
                    return;
                }

                EditorUtility.DisplayProgressBar("Generating MIDI",
                    "Saving test MIDI file...", 0.9f);

                // Save to a test file
                var outputDir = "Assets/StreamingAssets/TestMIDI";
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                var filename = $"{config.zoneName}_{testLayer.name}_test.mid";
                var filepath = $"{outputDir}/{filename}";
                File.WriteAllBytes(filepath, midiData);

                // Disconnect
                await client.Disconnect();

                EditorUtility.ClearProgressBar();

                if (EditorUtility.DisplayDialog("MIDI Generated",
                        "Test MIDI generated successfully!\n\n" +
                        $"Layer: {testLayer.name}\n" +
                        $"File: {filepath}\n" +
                        $"Size: {midiData.Length} bytes\n\n" +
                        "Would you like to open the folder?", "Open Folder", "Close"))
                    EditorUtility.RevealInFinder(filepath);

                // Refresh the asset database
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Generation Failed",
                    $"Error generating MIDI:\n\n{e.Message}\n\n" +
                    "Make sure the WebSocket server is running at ws://localhost:8765", "OK");
                Debug.LogError($"MIDI generation error: {e}");
            }
        }
    }
}