using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace AdaptiveMusic
{
    [Serializable]
    public class GenerationDebugOverrides
    {
        public bool enabled;

        [Header("Cache/Reload")]
        [Tooltip("If true, bypasses the persistent MIDI cache for requests using overrides.")]
        public bool bypassMidiCache = true;

        [Header("Seed")]
        public bool overrideSeed;
        public int seed = 1234;

        [Header("Structure")]
        public bool overrideBpm;
        [Range(40, 200)]
        public int bpm = 80;
        public bool overrideGenEvents;
        [Range(64, 512)]
        public int gen_events = 256;
        public bool overrideTimeSig;
        public string time_sig = "4/4";
        public bool overrideKeySig;
        public string key_sig = "auto";

        [Header("Sampling")]
        public bool overrideSampling;
        [Range(0.1f, 1.2f)]
        public float temp = 0.85f;
        [Range(0.1f, 1f)]
        public float top_p = 0.95f;
        [Range(1, 128)]
        public int top_k = 50;

        [Header("Orchestration")]
        public bool overrideInstruments;
        [Tooltip("General MIDI instrument names (must match backend patch list). Leave empty for auto.")]
        public string instrument1;
        public string instrument2;
        public string instrument3;

        public bool overrideDrumKit;
        [Tooltip("Drum kit name (e.g., None, Standard, Room, Power, Electric, TR-808, Jazz, Blush, Orchestra)")]
        public string drum_kit = "None";
    }

    /// <summary>
    /// Client type for MIDI generation server communication.
    /// </summary>
    public enum ClientType
    {
        WebSocket,   // Network-based (ws://localhost:8765)
        NamedPipe    // IPC-based (faster, Windows/Unix sockets)
    }

    /// <summary>
    ///     Main manager for the adaptive music system.
    ///     Coordinates all components: client, cache, renderer, mixer, and game state tracking.
    /// </summary>
    public class AdaptiveMusicSystem : MonoBehaviour
    {
        // Singleton instance for easy access (used by MetricsCollector)
        public static AdaptiveMusicSystem Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Client type: WebSocket (network) or NamedPipe (IPC, ~10x faster)")]
        private ClientType clientType = ClientType.WebSocket;
        
        [SerializeField] [Tooltip("WebSocket server URL (if using WebSocket client)")]
        private string serverUrl = "ws://localhost:8766";
        
        [SerializeField] [Tooltip("Named pipe name (if using NamedPipe client)")]
        private string pipeName = "AdaptiveMusicPipe";

        [SerializeField] [Tooltip("Current zone music configuration")]
        private MusicConfigSO currentZoneConfig;

        [SerializeField] [Tooltip("AudioMixer with exposed layer volume parameters")]
        private AudioMixer mixer;

        [SerializeField] [Tooltip("AudioSources for each layer (Ambient, Tension, Combat)")]
        private AudioSource[] layerSources = new AudioSource[3];

        [Header("Layer Management")] [SerializeField] [Range(0f, 1f)] [Tooltip("Current danger level from game state")]
        private float dangerLevel;

        [SerializeField] [Tooltip("Use manual danger level instead of GameStateTracker")]
        private bool useManualDangerLevel;

        [SerializeField] [Tooltip("Danger thresholds for layer transitions")]
        private float tensionThreshold = 0.3f;

        [SerializeField] [Tooltip("Danger threshold for combat layer")]
        private float combatThreshold = 0.7f;

        [Header("Runtime Status")] [SerializeField]
        private bool isInitialized;

        [SerializeField] private bool isConnectedToServer;

        [SerializeField] private string currentZoneName = "";
        
        [SerializeField] private bool playerIsDead = false;
        
        [SerializeField] private bool deathMusicRequested = false;

        [Header("Debug Overrides")]
        [SerializeField]
        private GenerationDebugOverrides debugOverrides = new();

        private MidiCacheManager cache;

        // Core components
        private IMusicClient client;
        private LayerMixer layerMixer;

        // State tracking
        private readonly Dictionary<string, AudioClip> loadedClips = new();
        private readonly HashSet<string> pendingRequests = new();
        private new MidiRenderer renderer; // 'new' keyword to hide Component.renderer

        private sealed class MidiRequestDebug
        {
            public string zoneName;
            public string layerName;
            public string cacheKey;
            public MidiParams parameters;
            public bool usedDynamicMapping;
            public float timeSinceStartup;
            public bool isSpecial;
        }

        private readonly MidiRequestDebug[] lastLayerRequests = new MidiRequestDebug[3];
        private MidiRequestDebug lastSpecialRequest;

        private const float ZONE_TRANSITION_FADE_SECONDS = 2f;

        public GenerationDebugOverrides DebugOverrides => debugOverrides;

        private static int StableHash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            unchecked
            {
                int hash = 0;
                for (int i = 0; i < input.Length; i++)
                    hash = (hash * 31) + input[i];
                return hash;
            }
        }

        public MusicConfigSO GetCurrentZoneConfigAsset()
        {
            return currentZoneConfig;
        }

        /// <summary>
        /// Force reload the current zone (even if it is already active).
        /// Useful for development-time parameter tweaking.
        /// </summary>
        public async void ForceReloadCurrentZone()
        {
            if (currentZoneConfig == null)
            {
                Debug.LogWarning("[AdaptiveMusicSystem] Cannot reload: currentZoneConfig is null");
                return;
            }

            Debug.Log($"[AdaptiveMusicSystem] === FORCE RELOAD ZONE === Zone: {currentZoneConfig.zoneName}");

            var nextClips = await PreloadZoneClips(currentZoneConfig);
            layerMixer?.StopAll(false);
            await Task.Delay(TimeSpan.FromSeconds(ZONE_TRANSITION_FADE_SECONDS));
            ApplyZoneClips(currentZoneConfig, nextClips);
            StartAmbientIfReady();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            await Initialize();
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Update WebSocket message queue
            client?.Update();

            // Get current danger level from game state (unless manual mode is enabled)
            if (!useManualDangerLevel && GameStateTracker.Instance != null)
            {
                dangerLevel = GameStateTracker.Instance.GetDangerLevel();
                
                // Check for player death
                float health = GameStateTracker.Instance.GetHealth();
                if (health <= 0f && !playerIsDead)
                {
                    playerIsDead = true;
                    deathMusicRequested = false; // Reset flag for new death music request
                    Debug.Log("[AdaptiveMusicSystem] Player death detected");
                    _ = HandlePlayerDeath();
                }
            }

            // Update layer volumes based on danger level (skip if death music is playing)
            if (!playerIsDead)
            {
                UpdateLayerMix();
            }

            // Update mixer
            layerMixer?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            cache?.DeleteCache();

            if (Instance == this)
                Instance = null;
        }

        private async void OnApplicationQuit()
        {
            // Delete cache to force fresh generation next runtime
            cache?.DeleteCache();

            // Disconnect from server
            if (client != null) await client.Disconnect();
        }

        /// <summary>
        ///     Get the music client for direct MIDI requests.
        /// </summary>
        public IMusicClient GetMusicClient()
        {
            return client;
        }
        
        /// <summary>
        ///     Get current client type.
        /// </summary>
        public ClientType GetClientType()
        {
            return clientType;
        }

        /// <summary>
        ///     Initialize all system components.
        /// </summary>
        private async Task Initialize()
        {
            Debug.Log($"[AdaptiveMusicSystem] Initializing (client: {clientType}, url: {serverUrl}, pipe: {pipeName})...");

            // Ensure default layer sources exist if not assigned in the inspector
            EnsureLayerSourcesExist();

            // Validate layer sources
            if (!ValidateLayerSources())
            {
                Debug.LogError("[AdaptiveMusicSystem] Invalid layer source configuration");
                return;
            }

            // Initialize components (after AudioSources are ready)
            // Create client based on selected type
            if (clientType == ClientType.NamedPipe)
            {
                Debug.Log($"[AdaptiveMusicSystem] Using Named Pipe client (pipe: {pipeName})");
                client = new NamedPipeMusicClient(pipeName);
            }
            else
            {
                Debug.Log($"[AdaptiveMusicSystem] Using WebSocket client (url: {serverUrl})");
                client = new WebSocketMusicClient(serverUrl);
            }
            
            cache = new MidiCacheManager();
            renderer = new MidiRenderer();
            layerMixer = new LayerMixer(mixer, layerSources);

            // Connect to server
            isConnectedToServer = await client.Connect();
            if (!isConnectedToServer)
            {
                var errorMsg = $"Failed to connect to server ({clientType}), will use cached layers only";
                Debug.LogWarning($"[AdaptiveMusicSystem] {errorMsg}");
                MusicErrorNotification.ShowError(errorMsg);
            }

            // Load initial zone if configured
            if (currentZoneConfig != null) await LoadZone(currentZoneConfig);

            isInitialized = true;
            Debug.Log("[AdaptiveMusicSystem] Initialization complete");
        }

        /// <summary>
        ///     Ensure we have three AudioSources (Ambient/Tension/Combat). If not assigned,
        ///     create child GameObjects with AudioSources and assign them.
        /// </summary>
        private void EnsureLayerSourcesExist()
        {
            if (layerSources == null || layerSources.Length < 3) layerSources = new AudioSource[3];

            string[] names = { "AmbientLayer", "TensionLayer", "CombatLayer" };
            for (var i = 0; i < 3; i++)
                if (layerSources[i] == null)
                {
                    var child = transform.Find(names[i]);
                    GameObject go;
                    if (child == null)
                    {
                        go = new GameObject(names[i]);
                        go.transform.SetParent(transform);
                    }
                    else
                    {
                        go = child.gameObject;
                    }

                    var src = go.GetComponent<AudioSource>();
                    if (src == null) src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = true;
                    layerSources[i] = src;
                    Debug.Log($"[AdaptiveMusicSystem] Ensured AudioSource[{i}]: {names[i]}");
                }
        }

        /// <summary>
        ///     Validate that layer audio sources are properly configured.
        /// </summary>
        private bool ValidateLayerSources()
        {
            if (layerSources == null || layerSources.Length < 3)
            {
                Debug.LogError("[AdaptiveMusicSystem] Need at least 3 AudioSources for layers");
                return false;
            }

            for (var i = 0; i < 3; i++)
            {
                if (layerSources[i] == null)
                {
                    Debug.LogError($"[AdaptiveMusicSystem] Layer source {i} is null");
                    return false;
                }

                // Configure for looping
                layerSources[i].loop = true;
            }

            return true;
        }

        /// <summary>
        ///     Load music layers for a specific zone.
        /// </summary>
        public async Task LoadZone(MusicConfigSO zoneConfig)
        {
            if (zoneConfig == null)
            {
                Debug.LogError("[AdaptiveMusicSystem] Cannot load null zone config");
                return;
            }

            currentZoneConfig = zoneConfig;
            currentZoneName = zoneConfig.zoneName;
            Debug.Log($"[AdaptiveMusicSystem] Loading zone: '{currentZoneName}', layers={zoneConfig.layers.Length}");

            // Preload and then assign each layer
            var clips = await PreloadZoneClips(zoneConfig);
            ApplyZoneClips(zoneConfig, clips);

            StartAmbientIfReady();

            Debug.Log($"[AdaptiveMusicSystem] Zone '{currentZoneName}' loaded successfully");
        }

        /// <summary>
        ///     Preload (generate/cache/render) all clips for a zone without changing the currently playing zone.
        /// </summary>
        private async Task<AudioClip[]> PreloadZoneClips(MusicConfigSO zoneConfig)
        {
            var clips = new AudioClip[3];
            var tasks = new List<Task<AudioClip>>();

            for (var i = 0; i < zoneConfig.layers.Length && i < 3; i++)
            {
                var layerConfig = zoneConfig.layers[i];
                Debug.Log($"[AdaptiveMusicSystem] Preloading layer {i}: '{layerConfig.name}' (zone='{zoneConfig.zoneName}')");
                tasks.Add(GetOrCreateClipForLayer(layerConfig, i, zoneConfig.zoneName));
            }

            Debug.Log($"[AdaptiveMusicSystem] Waiting for {tasks.Count} layer(s) to preload...");
            await Task.WhenAll(tasks);

            for (var i = 0; i < tasks.Count && i < clips.Length; i++)
                clips[i] = tasks[i].Result;

            Debug.Log("[AdaptiveMusicSystem] Zone preload complete");

            return clips;
        }

        /// <summary>
        ///     Assign preloaded clips to layer sources.
        /// </summary>
        private void ApplyZoneClips(MusicConfigSO zoneConfig, AudioClip[] clips)
        {
            for (var i = 0; i < clips.Length && i < layerSources.Length; i++)
            {
                if (clips[i] == null) continue;
                layerSources[i].clip = clips[i];
                Debug.Log($"[AdaptiveMusicSystem] Applied clip to source[{i}]: {clips[i].name}");
            }

            // Set initial layer volumes (ambient active, others silent)
            layerMixer.SetLayerVolume("Ambient", 1f);
            layerMixer.SetLayerVolume("Tension", 0f);
            layerMixer.SetLayerVolume("Combat", 0f);
        }

        private void StartAmbientIfReady()
        {
            if (layerSources[0].clip != null)
            {
                Debug.Log(
                    $"[AdaptiveMusicSystem] Starting ambient layer playback (clip={layerSources[0].clip.name}, length={layerSources[0].clip.length}s)");
                layerSources[0].volume = 1f;
                layerSources[0].Play();
                Debug.Log($"[AdaptiveMusicSystem] Ambient layer playing: {layerSources[0].isPlaying}");

                // Check for AudioListener and create one if missing
                var listener = FindAnyObjectByType<AudioListener>();
                if (listener == null)
                {
                    Debug.LogWarning("[AdaptiveMusicSystem] No AudioListener found. Creating one on Main Camera...");
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        listener = camera.gameObject.AddComponent<AudioListener>();
                        Debug.Log($"[AdaptiveMusicSystem] AudioListener added to '{camera.gameObject.name}'");
                    }
                    else
                    {
                        var listenerGO = new GameObject("AudioListener");
                        listener = listenerGO.AddComponent<AudioListener>();
                        Debug.Log("[AdaptiveMusicSystem] AudioListener added to new GameObject");
                    }
                }
                else
                {
                    Debug.Log($"[AdaptiveMusicSystem] AudioListener found on '{listener.gameObject.name}'");
                }
            }
            else
            {
                Debug.LogWarning("[AdaptiveMusicSystem] Ambient layer clip not ready yet, will start when assigned");
            }
        }

        /// <summary>
        ///     Load a single music layer.
        /// </summary>
        private async Task<AudioClip> GetOrCreateClipForLayer(LayerConfig layer, int layerIndex, string zoneName)
        {
            Debug.Log(
            $"[AdaptiveMusicSystem] GetOrCreateClipForLayer: layer='{layer.name}', index={layerIndex}, zone='{zoneName}'");
            
            // Prepare cache key parameters
            float? intensityForCache = null;
            string musicTypeForCache = null;
            
            // Dynamic parameter mapping (if available)
            LayerConfig dynamicConfig = null;
            MidiParams requestParams = layer.ToParams();
            var tracker = GameStateTracker.Instance;
            
            if (DynamicMusicParameterMapper.Instance != null && tracker != null)
            {
                float danger = tracker.GetDangerLevel();
                float health = tracker.GetHealth();
                int enemies = tracker.GetNearbyEnemyCount();

                // Preserve per-zone identity (instruments/time_sig/key_sig/sampling) from the layer asset,
                // and only apply gameplay-driven modifiers (BPM/length/seed).
                dynamicConfig = new LayerConfig
                {
                    name = layer.name,
                    seed = layer.seed,
                    gen_events = layer.gen_events,
                    bpm = layer.bpm,
                    time_sig = layer.time_sig,
                    key_sig = layer.key_sig,
                    instruments = layer.instruments,
                    drum_kit = layer.drum_kit,
                    allow_cc = layer.allow_cc,
                    temp = layer.temp,
                    top_p = layer.top_p,
                    top_k = layer.top_k
                };

                DynamicMusicParameterMapper.Instance.ApplyDynamicModifiersPreserveIdentity(
                    ref dynamicConfig, danger, health, enemies, seedSalt: $"{zoneName}:{layer.name}");
                requestParams = dynamicConfig.ToParams();
                
                // Derive intensity for cache key
                bool combatActive = danger >= combatThreshold;
                intensityForCache = DynamicMusicParameterMapper.Instance.CalculateIntensity(health, combatActive, danger);
                requestParams.intensity = intensityForCache.Value;
            }

            // Optional development-time overrides (used by DemoController)
            if (debugOverrides != null && debugOverrides.enabled)
            {
                if (debugOverrides.overrideSeed)
                    requestParams.seed = debugOverrides.seed;

                if (debugOverrides.overrideBpm)
                    requestParams.bpm = debugOverrides.bpm;

                if (debugOverrides.overrideGenEvents)
                {
                    requestParams.gen_events = debugOverrides.gen_events;
                    requestParams.max_len = debugOverrides.gen_events;
                }

                if (debugOverrides.overrideTimeSig)
                    requestParams.time_sig = debugOverrides.time_sig;

                if (debugOverrides.overrideKeySig)
                    requestParams.key_sig = debugOverrides.key_sig;

                if (debugOverrides.overrideSampling)
                {
                    requestParams.temp = debugOverrides.temp;
                    requestParams.top_p = debugOverrides.top_p;
                    requestParams.top_k = debugOverrides.top_k;
                }

                if (debugOverrides.overrideDrumKit)
                    requestParams.drum_kit = debugOverrides.drum_kit;

                if (debugOverrides.overrideInstruments)
                {
                    var instruments = new List<string>(capacity: 3);
                    void Add(string name)
                    {
                        if (string.IsNullOrWhiteSpace(name)) return;
                        var trimmed = name.Trim();
                        if (!string.IsNullOrEmpty(trimmed)) instruments.Add(trimmed);
                    }

                    Add(debugOverrides.instrument1);
                    Add(debugOverrides.instrument2);
                    Add(debugOverrides.instrument3);

                    requestParams.instruments = instruments.Count > 0 ? instruments.ToArray() : Array.Empty<string>();
                }
            }
            
            var cacheKey = MidiCacheManager.GetCacheKey(zoneName, layer.name, requestParams.seed, musicTypeForCache, intensityForCache);
            if (debugOverrides != null && debugOverrides.enabled)
            {
                string instrumentsJoined = requestParams.instruments != null && requestParams.instruments.Length > 0
                    ? string.Join("|", requestParams.instruments)
                    : "";
                int instrumentsHash = StableHash(instrumentsJoined);

                cacheKey += $"_dbg_b{requestParams.bpm}_e{requestParams.gen_events}_ts{(requestParams.time_sig ?? "")}_ks{(requestParams.key_sig ?? "")}" +
                            $"_t{requestParams.temp:F2}_p{requestParams.top_p:F2}_k{requestParams.top_k}" +
                            $"_dk{(requestParams.drum_kit ?? "")}_ih{instrumentsHash}";
            }
            Debug.Log($"[AdaptiveMusicSystem] Cache key: {cacheKey}");

            // Check if already loaded
            if (loadedClips.ContainsKey(cacheKey))
            {
                Debug.Log($"[AdaptiveMusicSystem] Layer '{layer.name}' already loaded (clip exists in loadedClips)");
                return loadedClips[cacheKey];
            }

            // Check if request is already pending
            if (pendingRequests.Contains(cacheKey))
            {
                Debug.Log($"[AdaptiveMusicSystem] Layer '{layer.name}' request already pending");
                return null;
            }

            pendingRequests.Add(cacheKey);
            Debug.Log($"[AdaptiveMusicSystem] Added '{cacheKey}' to pending requests");

            try
            {
                // Try cache first
                Debug.Log($"[AdaptiveMusicSystem] Checking cache for '{cacheKey}'...");
                byte[] midiBytes = null;
                bool bypassMidiCache = debugOverrides != null && debugOverrides.enabled && debugOverrides.bypassMidiCache;
                if (!bypassMidiCache)
                    midiBytes = cache.Get(cacheKey);

                // If not cached, request from server
                if (midiBytes == null)
                {
                    Debug.Log(
                        $"[AdaptiveMusicSystem] Cache miss. Preparing request for layer '{layer.name}' (connected={isConnectedToServer})...");

                    if (dynamicConfig != null)
                    {
                        Debug.Log($"[AdaptiveMusicSystem] Using dynamic parameters for layer '{layer.name}' (seed={dynamicConfig.seed}, bpm={dynamicConfig.bpm}, events={dynamicConfig.gen_events})");
                    }

                    // Record metrics start
                    float genStart = Time.time;

                    RecordLayerMidiRequest(layerIndex, zoneName, layer.name, requestParams, cacheKey,
                        usedDynamicMapping: dynamicConfig != null);

                    midiBytes = await client.RequestMIDI(requestParams);
                    float genEnd = Time.time;

                    if (midiBytes != null)
                    {
                        if (!bypassMidiCache)
                            cache.Set(cacheKey, midiBytes);
                        // Metrics recording
                        if (MetricsCollector.Instance != null)
                        {
                            var usedConfig = dynamicConfig ?? layer;
                            MetricsCollector.Instance.RecordGeneration(
                                layer.name,
                                currentZoneName,
                                tracker?.GetDangerLevel() ?? 0f,
                                tracker?.GetHealth() ?? 1f,
                                tracker?.GetNearbyEnemyCount() ?? 0,
                                usedConfig.seed,
                                usedConfig.gen_events,
                                usedConfig.bpm,
                                usedConfig.instruments,
                                genStart,
                                genEnd,
                                true,
                                midiBytes.Length,
                                null);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[AdaptiveMusicSystem] Failed to get MIDI for layer '{layer.name}'");
                        MusicErrorNotification.ShowError($"Failed to generate layer '{layer.name}'");
                        if (MetricsCollector.Instance != null && tracker != null)
                        {
                            var usedConfig = dynamicConfig ?? layer;
                            MetricsCollector.Instance.RecordGeneration(
                                layer.name,
                                currentZoneName,
                                tracker.GetDangerLevel(),
                                tracker.GetHealth(),
                                tracker.GetNearbyEnemyCount(),
                                usedConfig.seed,
                                usedConfig.gen_events,
                                usedConfig.bpm,
                                usedConfig.instruments,
                                genStart,
                                genEnd,
                                false,
                                0,
                                "MIDI request failed");
                        }
                        return null;
                    }
                }

                // Render MIDI to AudioClip
                Debug.Log(
                    $"[AdaptiveMusicSystem] Rendering MIDI to AudioClip for layer '{layer.name}' ({midiBytes.Length} bytes)");
                var clip = await renderer.RenderToAudioClip(midiBytes, $"{zoneName}_{layer.name}");

                if (clip != null)
                {
                    Debug.Log(
                        $"[AdaptiveMusicSystem] AudioClip created: name={clip.name}, length={clip.length}s, channels={clip.channels}, samples={clip.samples}");
                    loadedClips[cacheKey] = clip;
                    Debug.Log($"[AdaptiveMusicSystem] Layer '{layer.name}' loaded successfully (clip cached)");
                    return clip;
                }
                else
                {
                    Debug.LogError($"[AdaptiveMusicSystem] Failed to render audio for layer '{layer.name}'");
                    MusicErrorNotification.ShowError($"Failed to render audio for '{layer.name}'");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdaptiveMusicSystem] Error loading layer '{layer.name}': {ex.Message}");
                MusicErrorNotification.ShowError($"Error loading '{layer.name}': {ex.Message}");
                return null;
            }
            finally
            {
                pendingRequests.Remove(cacheKey);
            }
        }

        /// <summary>
        ///     Update layer mix based on current danger level.
        /// </summary>
        private void UpdateLayerMix()
        {
            // Ambient: full at low danger, fades out as danger increases
            var ambientVolume = 1f - dangerLevel;

            // Tension: kicks in at tensionThreshold, peaks around 0.6
            var tensionVolume = 0f;
            if (dangerLevel > tensionThreshold)
                tensionVolume = Mathf.Min((dangerLevel - tensionThreshold) / (combatThreshold - tensionThreshold), 1f) *
                                0.6f;

            // Combat: kicks in at combatThreshold, dominates at high danger
            var combatVolume = 0f;
            if (dangerLevel > combatThreshold)
                combatVolume = (dangerLevel - combatThreshold) / (1f - combatThreshold) * 1.2f;

            layerMixer.SetLayerVolume("Ambient", ambientVolume);
            layerMixer.SetLayerVolume("Tension", tensionVolume);
            layerMixer.SetLayerVolume("Combat", combatVolume);
        }

        /// <summary>
        ///     Change to a different zone with smooth transition.
        /// </summary>
        public async void ChangeZone(MusicConfigSO newZoneConfig)
        {
            Debug.Log($"[AdaptiveMusicSystem] === ZONE CHANGE REQUEST === New Zone: {(newZoneConfig != null ? newZoneConfig.zoneName : "NULL")}, Current: {currentZoneName}");
            
            if (newZoneConfig == null)
            {
                Debug.LogError("[AdaptiveMusicSystem] Cannot change to null zone config");
                return;
            }
                
            if (newZoneConfig == currentZoneConfig)
            {
                Debug.Log($"[AdaptiveMusicSystem] Zone change ignored - already in zone '{currentZoneName}'");
                return;
            }

            Debug.Log($"[AdaptiveMusicSystem] Proceeding with zone change from '{currentZoneName}' to '{newZoneConfig.zoneName}'");
            Debug.Log($"[AdaptiveMusicSystem] New zone has {newZoneConfig.layers.Length} layers");

            // Preload next zone while current zone continues playing.
            Debug.Log("[AdaptiveMusicSystem] Preloading next zone clips...");
            var nextClips = await PreloadZoneClips(newZoneConfig);

            // Begin fade-out on zone change (not tied to clip end)
            Debug.Log("[AdaptiveMusicSystem] Fading out current zone layers...");
            layerMixer?.StopAll(false);

            // Wait for fade-out to reach near-silence before swapping clips.
            await Task.Delay(TimeSpan.FromSeconds(ZONE_TRANSITION_FADE_SECONDS));

            currentZoneConfig = newZoneConfig;
            currentZoneName = newZoneConfig.zoneName;

            ApplyZoneClips(newZoneConfig, nextClips);
            StartAmbientIfReady();

            Debug.Log("[AdaptiveMusicSystem] Zone change complete");
        }

        /// <summary>
        ///     Handle player death by requesting and playing death music.
        /// </summary>
        private async Task HandlePlayerDeath()
        {
            if (deathMusicRequested)
            {
                Debug.Log("[AdaptiveMusicSystem] Death music already requested");
                return;
            }
            
            deathMusicRequested = true;
            Debug.Log("[AdaptiveMusicSystem] Requesting death music...");
            
            // Fade out all current layers
            layerMixer?.StopAll();
            
            // Create death music parameters
            var deathParams = new MidiParams
            {
                seed = UnityEngine.Random.Range(1000, 9999),
                gen_events = 256, // Moderate length for death sequence
                bpm = 60, // Slow, somber tempo
                time_sig = "4/4",
                instruments = new[] { "Strings", "Piano", "Pad" }, // Melancholic instruments
                drum_kit = "None",
                allow_cc = true,
                intensity = 0.8f, // High intensity for dramatic effect
                music_type = "death" // Special marker for server
            };
            
            try
            {
                // Request death music from server
                RecordSpecialMidiRequest("death", deathParams);
                var midiBytes = await client.RequestMIDI(deathParams);
                
                if (midiBytes != null && midiBytes.Length > 0)
                {
                    Debug.Log($"[AdaptiveMusicSystem] Death music received ({midiBytes.Length} bytes)");
                    
                    // Render to AudioClip
                    var deathClip = await renderer.RenderToAudioClip(midiBytes, "death_music");
                    
                    if (deathClip != null)
                    {
                        Debug.Log("[AdaptiveMusicSystem] Playing death music");
                        
                        // Play on ambient layer (index 0)
                        layerSources[0].clip = deathClip;
                        layerSources[0].loop = false; // Play once
                        layerSources[0].volume = 1f;
                        layerSources[0].Play();
                        
                        layerMixer.SetLayerVolume("Ambient", 1f);
                    }
                    else
                    {
                        Debug.LogError("[AdaptiveMusicSystem] Failed to render death music");
                    }
                }
                else
                {
                    Debug.LogError("[AdaptiveMusicSystem] Failed to receive death music from server");
                    MusicErrorNotification.ShowError("Failed to generate death music");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdaptiveMusicSystem] Error handling player death: {ex.Message}");
                MusicErrorNotification.ShowError($"Death music error: {ex.Message}");
            }
        }

        /// <summary>
        ///     Reset player death state (call when respawning).
        /// </summary>
        public void ResetDeathState()
        {
            playerIsDead = false;
            deathMusicRequested = false;
            Debug.Log("[AdaptiveMusicSystem] Death state reset");
        }

        /// <summary>
        ///     Clear the MIDI cache.
        /// </summary>
        public void ClearCache()
        {
            cache?.Clear();
            loadedClips.Clear();
            Debug.Log("[AdaptiveMusicSystem] Cache cleared");
        }

        /// <summary>
        ///     Get system status for debugging.
        /// </summary>
        public string GetStatus()
        {
            var status = "Adaptive Music System Status:\n";
            status += $"  Initialized: {isInitialized}\n";
            status += $"  Server: {(isConnectedToServer ? "Connected" : "Disconnected")}\n";
            status += $"  Zone: {currentZoneName}\n";
            status += $"  Danger Level: {dangerLevel:F2}\n";
            status += $"  Loaded Clips: {loadedClips.Count}\n";

            if (cache != null)
                status += $"  {cache.GetStats()}\n";

            if (layerMixer != null)
                status += $"\n{layerMixer.GetStatus()}";

            return status;
        }

        // Public API for external control

        /// <summary>
        ///     Manually set danger level (enables manual mode, overrides game state tracking).
        /// </summary>
        public void SetDangerLevel(float danger)
        {
            float previousDanger = dangerLevel;
            bool wasManual = useManualDangerLevel;
            
            useManualDangerLevel = true;
            dangerLevel = Mathf.Clamp01(danger);
            
            // Only log significant changes to avoid spam
            if (!wasManual || Mathf.Abs(dangerLevel - previousDanger) > 0.05f)
            {
                Debug.Log($"[AdaptiveMusicSystem] === DANGER LEVEL CHANGE === {previousDanger:F3} -> {dangerLevel:F3} (manual mode: {!wasManual} -> {useManualDangerLevel})");
            }
        }

        /// <summary>
        ///     Disable manual danger level and return to using GameStateTracker.
        /// </summary>
        public void UseAutomaticDangerLevel()
        {
            useManualDangerLevel = false;
        }

        /// <summary>
        ///     Check if system is ready for playback.
        /// </summary>
        public bool IsReady()
        {
            return isInitialized && loadedClips.Count > 0;
        }

        /// <summary>
        ///     Get current zone name.
        /// </summary>
        public string GetCurrentZone()
        {
            return currentZoneName;
        }

        /// <summary>
        /// Returns a compact summary of the last MIDI request(s) actually sent to the client.
        /// Intended for in-game debug UI.
        /// </summary>
        public string GetLastMidiRequestDebugString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Last MIDI requests (sent):");

            bool any = false;
            for (var i = 0; i < lastLayerRequests.Length; i++)
            {
                var entry = lastLayerRequests[i];
                if (entry == null || entry.parameters == null) continue;
                any = true;

                sb.AppendLine(FormatMidiRequestEntry(entry, prefix: $"  Layer[{i}]"));
            }

            if (lastSpecialRequest != null && lastSpecialRequest.parameters != null)
            {
                any = true;
                sb.AppendLine(FormatMidiRequestEntry(lastSpecialRequest, prefix: "  Special"));
            }

            if (!any)
                sb.AppendLine("  (none yet)");

            return sb.ToString();
        }

        /// <summary>
        /// Returns layer playback/mix state (active flags, volumes, clips, and isPlaying).
        /// </summary>
        public string GetLayerPlaybackDebugString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Layer playback:");

            string[] names = { "Ambient", "Tension", "Combat" };
            float bestVolume = -1f;
            string dominant = null;

            for (var i = 0; i < names.Length && i < layerSources.Length; i++)
            {
                var src = layerSources[i];
                var layerName = names[i];
                float mixVol = layerMixer != null ? layerMixer.GetLayerVolume(layerName) : 0f;
                bool active = layerMixer != null && layerMixer.IsLayerActive(layerName);

                if (mixVol > bestVolume)
                {
                    bestVolume = mixVol;
                    dominant = layerName;
                }

                var clipName = src != null && src.clip != null ? src.clip.name : "<none>";
                var isPlaying = src != null && src.isPlaying;

                sb.AppendLine(
                    $"  [{i}] {layerName}: mixVol={mixVol:F2} active={active} isPlaying={isPlaying} clip={clipName}");
            }

            if (dominant != null)
                sb.AppendLine($"Dominant (by mixer volume): {dominant} ({bestVolume:F2})");

            return sb.ToString();
        }

        private void RecordLayerMidiRequest(int layerIndex, string zoneName, string layerName, MidiParams parameters,
            string cacheKey, bool usedDynamicMapping)
        {
            if (layerIndex < 0 || layerIndex >= lastLayerRequests.Length) return;

            lastLayerRequests[layerIndex] = new MidiRequestDebug
            {
                zoneName = zoneName,
                layerName = layerName,
                cacheKey = cacheKey,
                parameters = CloneMidiParams(parameters),
                usedDynamicMapping = usedDynamicMapping,
                timeSinceStartup = Time.realtimeSinceStartup,
                isSpecial = false
            };
        }

        private void RecordSpecialMidiRequest(string specialName, MidiParams parameters)
        {
            lastSpecialRequest = new MidiRequestDebug
            {
                zoneName = currentZoneName,
                layerName = specialName,
                cacheKey = null,
                parameters = CloneMidiParams(parameters),
                usedDynamicMapping = false,
                timeSinceStartup = Time.realtimeSinceStartup,
                isSpecial = true
            };
        }

        private static MidiParams CloneMidiParams(MidiParams src)
        {
            if (src == null) return null;
            return new MidiParams
            {
                seed = src.seed,
                gen_events = src.gen_events,
                bpm = src.bpm,
                time_sig = src.time_sig,
                key_sig = src.key_sig,
                instruments = src.instruments != null ? (string[])src.instruments.Clone() : null,
                drum_kit = src.drum_kit,
                allow_cc = src.allow_cc,
                temp = src.temp,
                top_p = src.top_p,
                top_k = src.top_k,
                intensity = src.intensity,
                music_type = src.music_type
            };
        }

        private static string FormatMidiRequestEntry(MidiRequestDebug entry, string prefix)
        {
            var p = entry.parameters;
            var instruments = p.instruments != null ? string.Join(", ", p.instruments) : "";
            var intensity = p.intensity;
            var musicType = string.IsNullOrEmpty(p.music_type) ? "" : p.music_type;
            var cacheKeyPart = string.IsNullOrEmpty(entry.cacheKey) ? "" : $" cacheKey={entry.cacheKey}";

            return
                $"{prefix} zone={entry.zoneName} layer={entry.layerName} seed={p.seed} bpm={p.bpm} events={p.gen_events} ts={p.time_sig} ks={p.key_sig} " +
                $"temp={p.temp:F2} top_p={p.top_p:F2} top_k={p.top_k} " +
                $"drums={p.drum_kit} intensity={intensity:F2} music_type={musicType} dynamic={entry.usedDynamicMapping}" +
                $"{cacheKeyPart} t={entry.timeSinceStartup:F1}s\n    instruments=[{instruments}]";
        }
    }
}