using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Manages dynamic layer mixing with smooth volume transitions and crossfades.
    ///     Controls AudioMixer parameters based on game state.
    /// </summary>
    public class LayerMixer
    {
        private const float TRANSITION_SPEED = 2f; // Seconds for volume transition
        private const float MIN_VOLUME_DB = -80f;
        private const float MAX_VOLUME_DB = 0f;
        private readonly AudioSource[] layerSources;
        private readonly Dictionary<string, LayerState> layerStates = new();
        private readonly AudioMixer mixer;

        public LayerMixer(AudioMixer mixer, AudioSource[] layerSources)
        {
            this.mixer = mixer;
            this.layerSources = layerSources;

            InitializeLayers();
        }

        /// <summary>
        ///     Initialize layer states.
        /// </summary>
        private void InitializeLayers()
        {
            string[] layerNames = { "Ambient", "Tension", "Combat" };

            for (var i = 0; i < layerNames.Length && i < layerSources.Length; i++)
            {
                layerStates[layerNames[i]] = new LayerState
                {
                    name = layerNames[i],
                    sourceIndex = i,
                    targetVolume = 0f,
                    currentVolume = 0f,
                    isActive = false
                };

                // Initialize mixer parameter
                SetMixerVolume(layerNames[i], 0f);
            }

            Debug.Log($"[LayerMixer] Initialized {layerStates.Count} layers");
        }

        /// <summary>
        ///     Set target volume for a layer (0.0 = silent, 1.0 = full volume).
        ///     Volume will smoothly transition over time.
        /// </summary>
        public void SetLayerVolume(string layerName, float volume)
        {
            if (!layerStates.ContainsKey(layerName))
            {
                Debug.LogWarning($"[LayerMixer] Layer '{layerName}' not found");
                return;
            }

            volume = Mathf.Clamp01(volume);
            var state = layerStates[layerName];
            float previousTarget = state.targetVolume;
            state.targetVolume = volume;

            // Log significant volume changes
            if (Mathf.Abs(volume - previousTarget) > 0.05f)
            {
                Debug.Log($"[LayerMixer] === LAYER VOLUME CHANGE === {layerName}: {previousTarget:F3} -> {volume:F3} (current: {state.currentVolume:F3}, active: {state.isActive})");
            }

            // Activate/deactivate based on target volume
            if (volume > 0.01f && !state.isActive)
            {
                Debug.Log($"[LayerMixer] Activating layer '{layerName}' (volume: {volume:F3})");
                ActivateLayer(layerName);
            }
            else if (volume <= 0.01f && state.isActive)
            {
                // Will deactivate when current volume reaches 0
                Debug.Log($"[LayerMixer] Layer '{layerName}' will deactivate when volume reaches 0 (target: {volume:F3})");
            }
        }

        /// <summary>
        ///     Update layer volumes (call from MonoBehaviour Update).
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var layer in layerStates.Values)
            {
                if (Mathf.Approximately(layer.currentVolume, layer.targetVolume))
                    continue;

                // Smooth transition
                var delta = (layer.targetVolume - layer.currentVolume) * deltaTime / TRANSITION_SPEED;
                layer.currentVolume = Mathf.MoveTowards(layer.currentVolume, layer.targetVolume,
                    Mathf.Abs(delta) + deltaTime * 2f); // Minimum speed

                // Update mixer
                SetMixerVolume(layer.name, layer.currentVolume);

                // Deactivate if reached zero
                if (layer.currentVolume <= 0.01f && layer.isActive && layer.targetVolume <= 0.01f)
                    DeactivateLayer(layer.name);
            }
        }

        /// <summary>
        ///     Activate a layer (start playback if not already playing).
        /// </summary>
        private void ActivateLayer(string layerName)
        {
            var state = layerStates[layerName];
            var source = layerSources[state.sourceIndex];

            if (!source.isPlaying && source.clip != null)
            {
                source.Play();
                Debug.Log($"[LayerMixer] Activated layer: {layerName}");
            }

            state.isActive = true;
        }

        /// <summary>
        ///     Deactivate a layer (pause playback).
        /// </summary>
        private void DeactivateLayer(string layerName)
        {
            var state = layerStates[layerName];
            var source = layerSources[state.sourceIndex];

            if (source.isPlaying)
            {
                source.Pause();
                Debug.Log($"[LayerMixer] Deactivated layer: {layerName}");
            }

            state.isActive = false;
        }

        /// <summary>
        ///     Crossfade from one layer to another.
        /// </summary>
        public void Crossfade(string fromLayer, string toLayer, float duration = 2f)
        {
            Debug.Log($"[LayerMixer] Crossfading from {fromLayer} to {toLayer} over {duration}s");

            // Fade out old layer
            if (layerStates.ContainsKey(fromLayer)) SetLayerVolume(fromLayer, 0f);

            // Fade in new layer
            if (layerStates.ContainsKey(toLayer)) SetLayerVolume(toLayer, 1f);
        }

        /// <summary>
        ///     Set mixer volume in decibels (converts linear 0-1 to dB scale).
        /// </summary>
        private void SetMixerVolume(string layerName, float linearVolume)
        {
            // Convert linear volume to decibels
            var volumeDb = linearVolume > 0.0001f
                ? Mathf.Log10(linearVolume) * 20f
                : MIN_VOLUME_DB;

            volumeDb = Mathf.Clamp(volumeDb, MIN_VOLUME_DB, MAX_VOLUME_DB);

            // Set mixer parameter (assumes exposed parameters named "AmbientVolume", "TensionVolume", etc.)
            var paramName = $"{layerName}Volume";
            if (mixer != null) mixer.SetFloat(paramName, volumeDb);
        }

        /// <summary>
        ///     Stop all layers immediately.
        /// </summary>
        public void StopAll()
        {
            StopAll(true);
        }

        /// <summary>
        ///     Stop all layers.
        ///     If immediate=false, layers fade to silence via the normal smoothing logic and will auto-deactivate
        ///     when they reach zero.
        /// </summary>
        public void StopAll(bool immediate)
        {
            foreach (var layer in layerStates.Values)
            {
                SetLayerVolume(layer.name, 0f);

                if (immediate)
                {
                    layer.currentVolume = 0f;
                    DeactivateLayer(layer.name);
                }
            }

            Debug.Log(immediate ? "[LayerMixer] Stopped all layers (immediate)" : "[LayerMixer] Stopping all layers (fade-out)");
        }

        /// <summary>
        ///     Get current volume for a layer.
        /// </summary>
        public float GetLayerVolume(string layerName)
        {
            return layerStates.ContainsKey(layerName) ? layerStates[layerName].currentVolume : 0f;
        }

        /// <summary>
        ///     Check if a layer is currently active.
        /// </summary>
        public bool IsLayerActive(string layerName)
        {
            return layerStates.ContainsKey(layerName) && layerStates[layerName].isActive;
        }

        /// <summary>
        ///     Synchronize all active layers to the same playback position.
        ///     Useful when switching zones to keep beat alignment.
        /// </summary>
        public void SynchronizeLayers()
        {
            var referenceTime = 0f;
            AudioSource referenceSource = null;

            // Find a playing source as reference
            foreach (var source in layerSources)
                if (source.isPlaying)
                {
                    referenceSource = source;
                    referenceTime = source.time;
                    break;
                }

            if (referenceSource == null)
                return;

            // Sync all other sources
            foreach (var source in layerSources)
                if (source != referenceSource && source.clip != null)
                    source.time = referenceTime % source.clip.length;

            Debug.Log($"[LayerMixer] Synchronized layers to {referenceTime:F2}s");
        }

        /// <summary>
        ///     Get mixer status for debugging.
        /// </summary>
        public string GetStatus()
        {
            var status = "LayerMixer Status:\n";
            foreach (var layer in layerStates.Values)
                status +=
                    $"  {layer.name}: Vol={layer.currentVolume:F2} Target={layer.targetVolume:F2} Active={layer.isActive}\n";
            return status;
        }

        /// <summary>
        ///     State tracking for each layer.
        /// </summary>
        private class LayerState
        {
            public float currentVolume;
            public bool isActive;
            public string name;
            public int sourceIndex;
            public float targetVolume;
        }
    }
}