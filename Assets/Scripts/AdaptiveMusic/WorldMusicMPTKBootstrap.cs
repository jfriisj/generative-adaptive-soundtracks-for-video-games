using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Bootstrap for world music using MPTK for direct MIDI playback.
    ///     This bypasses AudioClip.Create() issues by using Maestro MIDI Player.
    ///     No custom components required - uses MPTK MidiFilePlayer directly.
    /// </summary>
    public class WorldMusicMPTKBootstrap : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private string midiFilePath = "Assets/Audio/music/ambient_seed100.mid";

        [Header("MPTK Layer Players")] [SerializeField]
        private MPTKLayerPlayer ambientLayer;

        [SerializeField] private MPTKLayerPlayer tensionLayer;
        [SerializeField] private MPTKLayerPlayer combatLayer;

        [Header("WebSocket Settings")] [SerializeField]
        private string serverUrl = "ws://localhost:8765";

        [SerializeField] private bool requestFromServer = true;
        [SerializeField] private int seed = 100;
        [SerializeField] private int genEvents = 64;
        [SerializeField] private int bpm = 80;

        private WebSocketMusicClient wsClient;

        private async void Start()
        {
            Debug.Log("[WorldMusicMPTKBootstrap] Starting...");

            // Option 1: Load existing MIDI file
            if (!requestFromServer && File.Exists(midiFilePath))
            {
                Debug.Log($"[WorldMusicMPTKBootstrap] Loading existing MIDI: {midiFilePath}");
                var midiBytes = File.ReadAllBytes(midiFilePath);
                await PlayMidi(midiBytes);
                return;
            }

            // Option 2: Request from WebSocket server
            Debug.Log("[WorldMusicMPTKBootstrap] Requesting MIDI from WebSocket server...");
            wsClient = new WebSocketMusicClient(serverUrl);

            var connected = await wsClient.Connect();
            if (!connected)
            {
                Debug.LogError("[WorldMusicMPTKBootstrap] Failed to connect to WebSocket server");
                return;
            }

            Debug.Log("[WorldMusicMPTKBootstrap] Connected! Requesting MIDI...");

            var midiParams = new MidiParams
            {
                seed = seed,
                gen_events = genEvents,
                bpm = bpm,
                time_sig = "4/4",
                instruments = new[] { "Acoustic Grand", "Pad 2 (warm)", "Pizzicato Strings", "Flute" },
                drum_kit = "None",
                allow_cc = false
            };

            var midi = await wsClient.RequestMIDI(midiParams);

            if (midi == null || midi.Length == 0)
            {
                Debug.LogError("[WorldMusicMPTKBootstrap] Failed to receive MIDI data");
                return;
            }

            Debug.Log($"[WorldMusicMPTKBootstrap] Received {midi.Length} bytes of MIDI");

            // Save for future use
            var outputPath = Path.Combine(Application.dataPath, "Audio", "music", $"ambient_seed{seed}.mid");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, midi);
            Debug.Log($"[WorldMusicMPTKBootstrap] Saved to: {outputPath}");

            // Play it
            await PlayMidi(midi);
        }

        private void OnDestroy()
        {
            if (wsClient != null) _ = wsClient.Disconnect();
        }

        private async Task PlayMidi(byte[] midiBytes)
        {
            if (ambientLayer == null)
            {
                Debug.LogError("[WorldMusicMPTKBootstrap] Ambient layer not assigned!");
                return;
            }

            Debug.Log("[WorldMusicMPTKBootstrap] Starting ambient layer playback...");

            // Use MPTK to play MIDI directly
            ambientLayer.PlayMidiBytes(midiBytes);

            // Set volume
            ambientLayer.SetVolume(1.0f);

            // Wait a bit to verify playback started
            await Task.Delay(1000);

            if (ambientLayer.IsPlaying())
                Debug.Log("[WorldMusicMPTKBootstrap] ✓ Ambient layer is playing!");
            else
                Debug.LogWarning("[WorldMusicMPTKBootstrap] Ambient layer is not playing yet...");
        }

        // Public methods for testing
        public void SetAmbientVolume(float volume)
        {
            if (ambientLayer != null) ambientLayer.SetVolume(volume);
        }

        public void SetTensionVolume(float volume)
        {
            if (tensionLayer != null) tensionLayer.SetVolume(volume);
        }

        public void SetCombatVolume(float volume)
        {
            if (combatLayer != null) combatLayer.SetVolume(volume);
        }
    }
}