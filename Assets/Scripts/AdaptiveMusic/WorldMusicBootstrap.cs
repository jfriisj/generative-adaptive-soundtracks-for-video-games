using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Minimal bootstrapper to request MIDI from WebSocket and save to disk.
    ///     This simplified version bypasses AudioClip creation for debugging.
    /// </summary>
    public class WorldMusicBootstrap : MonoBehaviour
    {
        [Header("Config Source")] [Tooltip("Resources path (without extension) to the World zone JSON config")]
        public string resourcesJsonPath = "MusicConfigs/WorldZone";

        [Header("Output Settings")] [Tooltip("Where to save MIDI files relative to Assets folder")]
        public string midiOutputPath = "Audio/music";

        [Header("References")] [SerializeField]
        private AdaptiveMusicSystem musicSystem;

        private void Awake()
        {
            if (musicSystem == null)
            {
                musicSystem = FindAnyObjectByType<AdaptiveMusicSystem>();
                if (musicSystem == null)
                {
                    var go = new GameObject("AdaptiveMusicSystem");
                    musicSystem = go.AddComponent<AdaptiveMusicSystem>();
                }
            }
        }

        private async void Start()
        {
            Debug.Log("[WorldMusicBootstrap] Starting...");

            // Load JSON from Resources
            Debug.Log($"[WorldMusicBootstrap] Loading config from Resources: '{resourcesJsonPath}'");
            var text = Resources.Load<TextAsset>(resourcesJsonPath);
            if (text == null)
            {
                Debug.LogError($"[WorldMusicBootstrap] Could not find Resources '{resourcesJsonPath}.json'");
                return;
            }

            Debug.Log($"[WorldMusicBootstrap] Config loaded, parsing JSON ({text.text.Length} chars)...");

            // Parse JSON into DTO
            WorldZoneDto dto;
            try
            {
                dto = JsonUtility.FromJson<WorldZoneDto>(text.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMusicBootstrap] Failed to parse JSON: {ex.Message}");
                return;
            }

            if (dto == null || dto.layers == null || dto.layers.Length == 0)
            {
                Debug.LogError("[WorldMusicBootstrap] Invalid WorldZone JSON");
                return;
            }

            Debug.Log($"[WorldMusicBootstrap] Parsed zone '{dto.zoneName}' with {dto.layers.Length} layer(s)");

            // Wait for music system to initialize with longer timeout
            Debug.Log("[WorldMusicBootstrap] Waiting for AdaptiveMusicSystem to initialize...");
            var waitMs = 0;
            const int maxWaitMs = 10000; // 10 seconds
            while (musicSystem != null && !musicSystem.IsReady() && waitMs < maxWaitMs)
            {
                await Task.Delay(500);
                waitMs += 500;
                if (waitMs % 2000 == 0) Debug.Log($"[WorldMusicBootstrap] Still waiting... ({waitMs}ms elapsed)");
            }

            Debug.Log($"[WorldMusicBootstrap] AdaptiveMusicSystem ready after {waitMs}ms");

            // Check if music client is connected
            var client = musicSystem.GetMusicClient();
            if (client == null)
            {
                Debug.LogError("[WorldMusicBootstrap] Music client is null! Cannot proceed.");
                return;
            }

            Debug.Log("[WorldMusicBootstrap] Music client obtained, checking connection...");

            // Request MIDI directly without building full config
            Debug.Log("[WorldMusicBootstrap] Requesting MIDI from server...");
            await RequestAndSaveMidi(dto.layers[0]);
            Debug.Log("[WorldMusicBootstrap] MIDI saved to disk. You can now import it in Unity.");
        }

        private async Task RequestAndSaveMidi(LayerDto layerData)
        {
            Debug.Log($"[WorldMusicBootstrap] RequestAndSaveMidi called for layer: {layerData.name}");

            // Ensure output directory exists
            var fullOutputPath = Path.Combine(Application.dataPath, midiOutputPath);
            Debug.Log($"[WorldMusicBootstrap] Output path: {fullOutputPath}");

            if (!Directory.Exists(fullOutputPath))
            {
                Directory.CreateDirectory(fullOutputPath);
                Debug.Log($"[WorldMusicBootstrap] Created directory: {fullOutputPath}");
            }
            else
            {
                Debug.Log("[WorldMusicBootstrap] Directory already exists");
            }

            // Build request parameters
            var midiParams = new MidiParams
            {
                seed = layerData.seed,
                gen_events = Mathf.Clamp(layerData.gen_events, 64, 512),
                bpm = Mathf.Clamp(layerData.bpm, 40, 200),
                time_sig = string.IsNullOrEmpty(layerData.time_sig) ? "4/4" : layerData.time_sig,
                instruments = layerData.instruments != null ? layerData.instruments : new[] { "Acoustic Grand" },
                drum_kit = string.IsNullOrEmpty(layerData.drum_kit) ? "None" : layerData.drum_kit,
                allow_cc = layerData.allow_cc
            };

            Debug.Log(
                $"[WorldMusicBootstrap] MidiParams created: seed={midiParams.seed}, events={midiParams.gen_events}, bpm={midiParams.bpm}");

            // Get music client
            var client = musicSystem.GetMusicClient();
            if (client == null)
            {
                Debug.LogError("[WorldMusicBootstrap] Music client is null!");
                return;
            }

            Debug.Log($"[WorldMusicBootstrap] Music client obtained, IsConnected={client.IsConnected}");

            // Request MIDI from server
            Debug.Log("[WorldMusicBootstrap] Calling client.RequestMIDI()...");
            byte[] midiBytes = null;

            try
            {
                midiBytes = await client.RequestMIDI(midiParams);
                Debug.Log(
                    $"[WorldMusicBootstrap] RequestMIDI returned, midiBytes={(midiBytes == null ? "null" : midiBytes.Length + " bytes")}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMusicBootstrap] Exception during RequestMIDI: {ex.Message}\n{ex.StackTrace}");
                return;
            }

            if (midiBytes == null || midiBytes.Length == 0)
            {
                Debug.LogError("[WorldMusicBootstrap] Failed to receive MIDI data from server");
                return;
            }

            Debug.Log($"[WorldMusicBootstrap] Received {midiBytes.Length} bytes of MIDI data");

            // Save to file
            var fileName = $"{layerData.name}_seed{layerData.seed}.mid";
            var filePath = Path.Combine(fullOutputPath, fileName);

            try
            {
                File.WriteAllBytes(filePath, midiBytes);
                Debug.Log($"[WorldMusicBootstrap] ✓ MIDI file saved to: {filePath}");
                Debug.Log(
                    $"[WorldMusicBootstrap] Next step: In Unity, refresh Assets and check Assets/Audio/music/{fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMusicBootstrap] Failed to write file: {ex.Message}");
            }
        }

        [Serializable]
        private class WorldZoneDto
        {
            public string zoneName;
            public LayerDto[] layers;
        }

        [Serializable]
        private class LayerDto
        {
            public string name;
            public int seed;
            public int gen_events;
            public int bpm;
            public string time_sig;
            public string[] instruments;
            public string drum_kit;
            public bool allow_cc;
        }
    }
}