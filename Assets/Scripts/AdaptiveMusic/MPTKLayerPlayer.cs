using System;
using System.IO;
using System.Threading.Tasks;
using MidiPlayerTK;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Plays a single adaptive music layer using MPTK MidiFilePlayer.
    ///     This bypasses the AudioClip.Create() issues by using MPTK's native MIDI rendering.
    /// </summary>
    [RequireComponent(typeof(MidiFilePlayer))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AudioReverbFilter))]
    [RequireComponent(typeof(AudioChorusFilter))]
    public class MPTKLayerPlayer : MonoBehaviour
    {
        [Header("Layer Configuration")] [SerializeField]
        private string layerName = "ambient";

        [SerializeField] private float targetVolume = 1.0f;
        [SerializeField] private float fadeSpeed = 2.0f;
        private byte[] currentMidiBytes;
        private float currentVolume;
        private bool isPlaying;

        private MidiFilePlayer midiPlayer;

        private void Awake()
        {
            midiPlayer = GetComponent<MidiFilePlayer>();
            if (midiPlayer == null)
            {
                Debug.LogError($"[MPTKLayerPlayer] No MidiFilePlayer component found on {gameObject.name}");
                return;
            }

            // Configure MPTK player
            midiPlayer.MPTK_Loop = true;
            midiPlayer.MPTK_PlayOnStart = false;
            midiPlayer.MPTK_Volume = 0f; // Start silent

            Debug.Log($"[MPTKLayerPlayer] '{layerName}' initialized");
        }

        private void Update()
        {
            // Smooth volume transitions
            if (Mathf.Abs(currentVolume - targetVolume) > 0.01f)
            {
                currentVolume = Mathf.Lerp(currentVolume, targetVolume, fadeSpeed * Time.deltaTime);
                if (midiPlayer != null) midiPlayer.MPTK_Volume = currentVolume;
            }
        }

        private void OnDestroy()
        {
            Stop();

            // Clean up temp file if it exists
            try
            {
                var tempPath = Path.Combine(Application.temporaryCachePath, $"{layerName}_temp.mid");
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MPTKLayerPlayer] Failed to delete temp file: {ex.Message}");
            }
        }

        /// <summary>
        ///     Load and play MIDI bytes on this layer.
        /// </summary>
        public async Task LoadAndPlay(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                Debug.LogError($"[MPTKLayerPlayer] '{layerName}' received null/empty MIDI bytes");
                return;
            }

            currentMidiBytes = midiBytes;
            Debug.Log($"[MPTKLayerPlayer] '{layerName}' loading {midiBytes.Length} bytes of MIDI");

            // Save MIDI to a temporary file for MPTK to load
            var tempPath = Path.Combine(Application.temporaryCachePath, $"{layerName}_temp.mid");
            File.WriteAllBytes(tempPath, midiBytes);
            Debug.Log($"[MPTKLayerPlayer] '{layerName}' saved to temp: {tempPath}");

            // Load the MIDI file into MPTK
            await Task.Run(() =>
            {
                // MPTK requires MIDI files to be in the MidiDB or loaded from file path
                // We'll use the direct byte loading approach
            });

            // Load MIDI from bytes directly (MPTK Pro feature) or use file path
            // For now, we'll use the temp file approach
            midiPlayer.MPTK_MidiName = tempPath;

            // Start playing with volume at 0 (will fade in via SetVolume)
            midiPlayer.MPTK_Play();
            isPlaying = true;

            Debug.Log($"[MPTKLayerPlayer] '{layerName}' playback started");
        }

        /// <summary>
        ///     Play MIDI bytes directly without loading from MidiDB.
        /// </summary>
        public void PlayMidiBytes(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                Debug.LogError($"[MPTKLayerPlayer] '{layerName}' received null/empty MIDI bytes");
                return;
            }

            currentMidiBytes = midiBytes;
            Debug.Log($"[MPTKLayerPlayer] '{layerName}' playing {midiBytes.Length} bytes directly");

            // Stop current playback
            if (isPlaying) midiPlayer.MPTK_Stop();

            // Load MIDI from bytes into midiLoaded
            if (midiPlayer.MPTK_MidiLoaded == null)
            {
                // Create new MidiLoad if it doesn't exist
                // MPTK will create it automatically when we start the coroutine
            }

            // Use the legacy thread player which accepts byte arrays
            // This is the proper MPTK way to play from bytes
            midiPlayer.StartCoroutine(midiPlayer.ThreadLegacyPlay(midiBytes));
            isPlaying = true;

            Debug.Log($"[MPTKLayerPlayer] '{layerName}' playback started from byte array");
        }

        /// <summary>
        ///     Set target volume for this layer (0.0-1.0).
        ///     Volume will smoothly transition to this value.
        /// </summary>
        public void SetVolume(float volume)
        {
            targetVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        ///     Stop playback immediately.
        /// </summary>
        public void Stop()
        {
            if (midiPlayer != null)
            {
                midiPlayer.MPTK_Stop();
                isPlaying = false;
                currentVolume = 0f;
                midiPlayer.MPTK_Volume = 0f;
                Debug.Log($"[MPTKLayerPlayer] '{layerName}' stopped");
            }
        }

        /// <summary>
        ///     Pause playback.
        /// </summary>
        public void Pause()
        {
            if (midiPlayer != null && isPlaying)
            {
                midiPlayer.MPTK_Pause();
                Debug.Log($"[MPTKLayerPlayer] '{layerName}' paused");
            }
        }

        /// <summary>
        ///     Resume playback.
        /// </summary>
        public void Resume()
        {
            if (midiPlayer != null && !midiPlayer.MPTK_IsPlaying)
            {
                midiPlayer.MPTK_UnPause();
                Debug.Log($"[MPTKLayerPlayer] '{layerName}' resumed");
            }
        }

        /// <summary>
        ///     Check if this layer is currently playing.
        /// </summary>
        public bool IsPlaying()
        {
            return isPlaying && midiPlayer != null && midiPlayer.MPTK_IsPlaying;
        }

        /// <summary>
        ///     Get current playback position in seconds.
        /// </summary>
        public float GetPosition()
        {
            return midiPlayer != null ? (float)midiPlayer.MPTK_Position : 0f;
        }
    }
}