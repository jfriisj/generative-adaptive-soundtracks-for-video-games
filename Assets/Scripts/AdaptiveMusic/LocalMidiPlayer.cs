using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Minimal component to play a local .mid file via MidiRenderer and an AudioSource.
    ///     Place your MIDI in Assets/StreamingAssets/Midi/ and set the filename.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LocalMidiPlayer : MonoBehaviour
    {
        [Header("Local MIDI Settings")] [Tooltip("Relative filename under StreamingAssets/Midi/")]
        public string midiFileName = "example.mid";

        [Tooltip("Autoplay on Start if file found")]
        public bool autoPlay = true;

        private MidiRenderer _renderer;

        private AudioSource _source;

        private async void Start()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _renderer = new MidiRenderer();

            if (autoPlay) await LoadAndPlayAsync();
        }

        public async Task LoadAndPlayAsync()
        {
            var baseDir = Path.Combine(Application.streamingAssetsPath, "Midi");
            var path = Path.Combine(baseDir, midiFileName);

            if (!File.Exists(path))
            {
                Debug.LogError($"[LocalMidiPlayer] MIDI not found: {path}. Place a .mid under StreamingAssets/Midi/");
                return;
            }

            var bytes = File.ReadAllBytes(path);
            var clip = await _renderer.RenderToAudioClip(bytes, Path.GetFileNameWithoutExtension(midiFileName));
            if (clip == null)
            {
                Debug.LogError("[LocalMidiPlayer] Failed to render MIDI to AudioClip");
                return;
            }

            _source.clip = clip;
            _source.Play();
            Debug.Log($"[LocalMidiPlayer] Playing {midiFileName} ({clip.length:F2}s)");
        }
    }
}