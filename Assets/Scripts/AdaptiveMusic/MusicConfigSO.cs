using System;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     ScriptableObject that defines music configuration for a specific zone.
    ///     Contains layer definitions with MIDI generation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicConfig", menuName = "Adaptive Music/Zone Config")]
    public class MusicConfigSO : ScriptableObject
    {
        [Header("Zone Identity")] [Tooltip("Name of the zone (e.g., 'forest', 'dungeon', 'boss')")]
        public string zoneName = "forest";

        [Header("Layer Definitions")] [Tooltip("Array of music layers for this zone (ambient, tension, combat, etc.)")]
        public LayerConfig[] layers =
        {
            new() { name = "ambient" },
            new() { name = "tension" },
            new() { name = "combat" }
        };

        /// <summary>
        ///     Get a layer configuration by name.
        /// </summary>
        public LayerConfig GetLayer(string layerName)
        {
            foreach (var layer in layers)
                if (layer.name.Equals(layerName, StringComparison.OrdinalIgnoreCase))
                    return layer;

            Debug.LogWarning($"Layer '{layerName}' not found in zone '{zoneName}'");
            return layers.Length > 0 ? layers[0] : null;
        }
    }

    /// <summary>
    ///     Configuration for a single music layer.
    ///     Maps to MIDI generation parameters sent to the server.
    /// </summary>
    [Serializable]
    public class LayerConfig
    {
        [Header("Layer Identity")] [Tooltip("Layer name (e.g., 'ambient', 'tension', 'combat')")]
        public string name = "ambient";

        [Header("Generation Parameters")] [Tooltip("Random seed for reproducible generation")]
        public int seed = 1001;

        [Tooltip("Number of MIDI events to generate (higher = longer)")] [Range(64, 512)]
        public int gen_events = 256;

        [Tooltip("Beats per minute")] [Range(40, 200)]
        public int bpm = 80;

        [Tooltip("Time signature (e.g., '4/4', '3/4', '6/8')")]
        public string time_sig = "4/4";

        [Tooltip("Optional key signature hint (tokenizer v2 only). Examples: 'C', 'G', 'Am', 'Em'. Use 'auto' or empty to omit.")]
        public string key_sig = "auto";

        [Tooltip("MIDI instruments to use (e.g., 'Acoustic Grand', 'Flute', 'Strings')")]
        public string[] instruments = { "Acoustic Grand" };

        [Tooltip("Drum kit to use ('None' for no drums)")]
        public string drum_kit = "None";

        [Tooltip("Allow MIDI control change messages")]
        public bool allow_cc = true;

        [Header("Sampling Controls")]
        [Tooltip("Sampling temperature (lower = more stable, higher = more random)")]
        [Range(0.1f, 1.2f)]
        public float temp = 0.85f;

        [Tooltip("Top-p nucleus sampling (lower = safer, higher = more varied)")]
        [Range(0.1f, 1f)]
        public float top_p = 0.95f;

        [Tooltip("Top-k sampling (lower = safer, higher = more varied)")]
        [Range(1, 128)]
        public int top_k = 50;

        /// <summary>
        ///     Convert this layer config to MIDI generation parameters.
        /// </summary>
        public MidiParams ToParams()
        {
            return new MidiParams
            {
                seed = seed,
                gen_events = gen_events,
                max_len = gen_events,
                bpm = bpm,
                time_sig = time_sig,
                key_sig = key_sig,
                instruments = instruments,
                drum_kit = drum_kit,
                allow_cc = allow_cc,

                temp = temp,
                top_p = top_p,
                top_k = top_k
            };
        }
    }

    /// <summary>
    ///     MIDI generation parameters sent to the WebSocket server.
    /// </summary>
    [Serializable]
    public class MidiParams
    {
        public int seed;
        public int gen_events;
        public int max_len;
        public int bpm;
        public string time_sig;
        public string key_sig;
        public string[] instruments;
        public string drum_kit;
        public bool allow_cc;

        // Sampling controls
        public float temp = 0.85f;
        public float top_p = 0.95f;
        public int top_k = 50;

        // Advanced decoding constraints (optional)
        public bool disable_patch_change;
        public bool disable_control_change;
        public int[] disable_channels;

        // Tokenization / MIDI pre-processing (used by some servers/modes)
        public bool optimise_midi;
        public int cc_eps;
        public int tempo_eps;
        public bool remap_track_channel;

        // Dynamic intensity (0.0 - 1.0) mapped from gameplay state.
        public float intensity;
        // Optional music type marker (e.g., "death", "victory") for specialized generation.
        public string music_type;
    }
}