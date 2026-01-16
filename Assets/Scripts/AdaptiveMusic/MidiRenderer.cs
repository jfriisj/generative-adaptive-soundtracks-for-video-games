using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Renders MIDI byte data to Unity AudioClip using DryWetMIDI library.
    ///     Supports background rendering to avoid blocking the main thread.
    /// </summary>
    public class MidiRenderer
    {
        private const int SAMPLE_RATE = 44100;
        private const int CHANNELS = 2; // Stereo
        private readonly string soundFontPath;

        public MidiRenderer()
        {
            // Default SoundFont path in StreamingAssets
            soundFontPath = Path.Combine(Application.streamingAssetsPath, "SoundFonts", "GeneralUser_GS.sf2");

            if (!File.Exists(soundFontPath))
                Debug.LogWarning($"[MidiRenderer] SoundFont not found at {soundFontPath}. " +
                                 "MIDI rendering will use synthesized audio. " +
                                 "Download GeneralUser GS v1.471 for better quality.");
        }

        public MidiRenderer(string customSoundFontPath)
        {
            soundFontPath = customSoundFontPath;
        }

        /// <summary>
        ///     Render MIDI bytes to AudioClip.
        ///     This is a simplified version - for production, you'd need a proper MIDI synthesizer.
        /// </summary>
        public async Task<AudioClip> RenderToAudioClip(byte[] midiBytes, string clipName = "GeneratedMIDI")
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                Debug.LogError("[MidiRenderer] Cannot render null or empty MIDI data");
                return null;
            }

            try
            {
                // Parse MIDI file
                var midiFile = await Task.Run(() =>
                {
                    using (var stream = new MemoryStream(midiBytes))
                    {
                        return MidiFile.Read(stream);
                    }
                });

                Debug.Log($"[MidiRenderer] Parsed MIDI: {midiFile.GetTrackChunks().Count()} tracks, " +
                          $"{midiFile.GetDuration<MetricTimeSpan>().TotalMicroseconds / 1000000.0:F2}s duration");

                // For now, create a placeholder AudioClip with synthesized audio
                // In production, you would use a proper MIDI synthesizer library
                var clip = await SynthesizeMidiToAudioClip(midiFile, clipName);

                return clip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MidiRenderer] Failed to render MIDI: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     Synthesize MIDI file to AudioClip.
        ///     NOTE: This is a simplified implementation. For production quality:
        ///     1. Use CSharpSynthForUnity or similar MIDI synthesizer
        ///     2. Or use external FluidSynth process
        ///     3. Or integrate NAudio (Windows only)
        /// </summary>
        private async Task<AudioClip> SynthesizeMidiToAudioClip(MidiFile midiFile, string clipName)
        {
            // Calculate clip duration from MIDI tempo map
            var tempo = midiFile.GetTempoMap();
            var durationMetric = midiFile.GetDuration<MetricTimeSpan>();
            var durationSeconds = (float)(durationMetric.TotalMicroseconds / 1000000.0);

            // Ensure minimum duration
            if (durationSeconds < 1f) durationSeconds = 10f; // Default to 10 seconds for short MIDI

            var totalSamples = Mathf.CeilToInt(durationSeconds * SAMPLE_RATE);

            Debug.Log($"[MidiRenderer] Creating AudioClip: {durationSeconds:F2}s, {totalSamples} samples");

            // Ensure valid clip name
            var safeName = string.IsNullOrEmpty(clipName) ? "MidiClip" : clipName;

            // Create AudioClip on main thread
            AudioClip clip = null;
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                clip = AudioClip.Create(safeName, totalSamples, CHANNELS, SAMPLE_RATE, false);

                // Validate clip creation
                if (clip == null)
                    Debug.LogError("[MidiRenderer] AudioClip.Create() returned null!");
                else
                    Debug.Log(
                        $"[MidiRenderer] AudioClip created: name={clip.name}, length={clip.length}s, channels={clip.channels}, samples={clip.samples}, freq={clip.frequency}Hz");
            });

            // Verify clip is valid before proceeding
            if (clip == null || clip.samples == 0)
            {
                Debug.LogError($"[MidiRenderer] AudioClip creation failed! clip={(clip == null ? "null" : "invalid")}");
                return null;
            }

            // Generate simple waveform based on MIDI notes
            // This is a placeholder - real implementation would use proper synthesis
            Debug.Log("[MidiRenderer] Generating audio synthesis...");
            var audioData = await Task.Run(() => GenerateSimpleSynthesis(midiFile, totalSamples, tempo));
            Debug.Log($"[MidiRenderer] Generated {audioData.Length} audio samples");

            // Check if audio has any signal
            var maxAmplitude = audioData.Max(s => Mathf.Abs(s));
            Debug.Log($"[MidiRenderer] Max amplitude in audio: {maxAmplitude}");

            // Verify array size matches clip
            var expectedArraySize = totalSamples * CHANNELS;
            if (audioData.Length != expectedArraySize)
            {
                Debug.LogError(
                    $"[MidiRenderer] Audio data size mismatch! Expected {expectedArraySize}, got {audioData.Length}");
                return null;
            }

            // Set audio data on main thread
            var setDataSuccess = false;
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                try
                {
                    clip.SetData(audioData, 0);
                    setDataSuccess = true;
                    Debug.Log($"[MidiRenderer] Audio data set successfully on clip: {clip.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MidiRenderer] SetData failed: {ex.Message}");
                }
            });

            if (!setDataSuccess)
            {
                Debug.LogError("[MidiRenderer] Failed to set audio data on clip");
                return null;
            }

            Debug.Log("[MidiRenderer] AudioClip created successfully");
            return clip;
        }

        /// <summary>
        ///     Generate simplified synthesis from MIDI notes.
        ///     This is a basic placeholder - replace with proper synthesis library.
        /// </summary>
        private float[] GenerateSimpleSynthesis(MidiFile midiFile, int totalSamples, TempoMap tempoMap)
        {
            var samples = new float[totalSamples * CHANNELS];

            // Get all notes from MIDI file
            var notes = midiFile.GetNotes();

            Debug.Log($"[MidiRenderer] Synthesizing {notes.Count} notes");

            foreach (var note in notes)
            {
                // Convert MIDI time to sample position
                var timeMetric = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                var lengthMetric = LengthConverter.ConvertTo<MetricTimeSpan>(note.Length, note.Time, tempoMap);

                var startTime = (float)(timeMetric.TotalMicroseconds / 1000000.0);
                var duration = (float)(lengthMetric.TotalMicroseconds / 1000000.0);

                var startSample = Mathf.FloorToInt(startTime * SAMPLE_RATE);
                var endSample = Mathf.Min(startSample + Mathf.FloorToInt(duration * SAMPLE_RATE), totalSamples);

                // Calculate frequency from MIDI note number
                var frequency = 440f * Mathf.Pow(2f, (note.NoteNumber - 69f) / 12f);
                var velocity = note.Velocity / 127f;

                // Generate simple sine wave for this note
                for (var i = startSample; i < endSample; i++)
                {
                    var t = (i - startSample) / (float)SAMPLE_RATE;

                    // Simple ADSR envelope
                    var envelope = 1f;
                    var noteDuration = (endSample - startSample) / (float)SAMPLE_RATE;
                    if (t < 0.01f) // Attack
                        envelope = t / 0.01f;
                    else if (t > noteDuration - 0.05f) // Release
                        envelope = (noteDuration - t) / 0.05f;

                    // Generate sine wave
                    var sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * velocity * envelope * 0.5f;

                    // Stereo: copy to both channels
                    var sampleIndex = i * CHANNELS;
                    if (sampleIndex < samples.Length - 1)
                    {
                        samples[sampleIndex] += sample; // Left
                        samples[sampleIndex + 1] += sample; // Right
                    }
                }
            }

            // Normalize to prevent clipping
            var maxAmplitude = 0f;
            for (var i = 0; i < samples.Length; i++)
                if (Mathf.Abs(samples[i]) > maxAmplitude)
                    maxAmplitude = Mathf.Abs(samples[i]);

            if (maxAmplitude > 0.8f)
            {
                var normalizeFactor = 0.8f / maxAmplitude;
                for (var i = 0; i < samples.Length; i++) samples[i] *= normalizeFactor;
            }

            return samples;
        }

        /// <summary>
        ///     Validate MIDI data.
        /// </summary>
        public bool ValidateMidi(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length < 14)
                return false;

            try
            {
                using (var stream = new MemoryStream(midiBytes))
                {
                    MidiFile.Read(stream);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     Helper class to execute code on Unity main thread from background tasks.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher instance;
        private readonly ConcurrentQueue<Action> actionQueue = new();

        private void Update()
        {
            while (actionQueue.TryDequeue(out var action)) action?.Invoke();
        }

        public static UnityMainThreadDispatcher Instance()
        {
            if (instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }

            return instance;
        }

        public async Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            actionQueue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task;
        }
    }
}