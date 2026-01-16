using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Demo controller for testing the adaptive music system.
    ///     Provides UI controls for zone switching and danger level adjustment.
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private AdaptiveMusicSystem musicSystem;

        [SerializeField] private GameStateTracker gameStateTracker;

        [Header("Zone Configs")] [SerializeField]
        private MusicConfigSO forestConfig;

        [SerializeField] private MusicConfigSO mountainConfig;

        [SerializeField] private MusicConfigSO oceanConfig;

        [SerializeField] private MusicConfigSO cherryConfig;

        [SerializeField] private MusicConfigSO safeConfig;

        [SerializeField] private MusicConfigSO victoryConfig;

        [Header("Demo Controls")] [SerializeField] [Range(0f, 1f)]
        private float manualDangerLevel = 0.5f;

        [SerializeField] private bool useManualDanger = true;

        [SerializeField] [Range(0f, 1f)] private float manualHealth = 1f;

        [SerializeField] private bool showDebugUI = true;

        [Header("Feedback")]
        [SerializeField] private float statusUpdateInterval = 0.5f;
        
        // Real-time feedback
        private string lastAction = "Starting up...";
        private float lastStatusUpdate = 0f;
        private string currentStatus = "Initializing...";

        [Header("Mode Selection")]
        [SerializeField] private bool useAutoZoneSwitching = false;
        
        private bool lastAutoMode = false;

        [Header("Generation Overrides (Dev)")]
        [SerializeField]
        private bool showGenerationOverrides = true;

        private Vector2 debugScrollPosition;

        private int instrumentPickerTarget = -1;
        private Vector2 instrumentPickerScroll;

        private static readonly string[] DrumKitChoices =
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

        private static readonly string[] InstrumentChoices =
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

        private void Start()
        {
            Debug.Log("[DemoController] ==================== DEMO CONTROLLER STARTING ====================");
            Debug.Log("[DemoController] Checking for system references...");
            
            // Find references if not assigned
            if (musicSystem == null) 
            {
                Debug.Log("[DemoController] MusicSystem not assigned, searching for AdaptiveMusicSystem...");
                musicSystem = FindAnyObjectByType<AdaptiveMusicSystem>();
                Debug.Log($"[DemoController] Found AdaptiveMusicSystem: {musicSystem != null}");
            }
            else
            {
                Debug.Log("[DemoController] MusicSystem already assigned");
            }

            if (gameStateTracker == null) 
            {
                Debug.Log("[DemoController] GameStateTracker not assigned, getting Instance...");
                gameStateTracker = GameStateTracker.Instance;
                Debug.Log($"[DemoController] Found GameStateTracker: {gameStateTracker != null}");
            }
            else
            {
                Debug.Log("[DemoController] GameStateTracker already assigned");
            }
            
            // Find AudioManager for auto mode (disabled for Iteration 1)
            // audioManager = FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);

            Debug.Log("[DemoController] Loading music configs from Resources...");
            
            // Load zone configs from Resources if not assigned in Inspector
            if (forestConfig == null)
            {
                forestConfig = Resources.Load<MusicConfigSO>("MusicConfigs/ForestMusicConfig");
                Debug.Log($"[DemoController] Loaded ForestConfig: {forestConfig != null} {(forestConfig != null ? $"(name: {forestConfig.zoneName})" : "")}");
            }
            if (mountainConfig == null)
            {
                mountainConfig = Resources.Load<MusicConfigSO>("MusicConfigs/MountainMusicConfig");
                Debug.Log($"[DemoController] Loaded MountainConfig: {mountainConfig != null} {(mountainConfig != null ? $"(name: {mountainConfig.zoneName})" : "")}");
            }
            if (oceanConfig == null)
            {
                oceanConfig = Resources.Load<MusicConfigSO>("MusicConfigs/OceanMusicConfig");
                Debug.Log($"[DemoController] Loaded OceanConfig: {oceanConfig != null} {(oceanConfig != null ? $"(name: {oceanConfig.zoneName})" : "")}");
            }
            if (cherryConfig == null)
            {
                cherryConfig = Resources.Load<MusicConfigSO>("MusicConfigs/CherryMusicConfig");
                Debug.Log($"[DemoController] Loaded CherryConfig: {cherryConfig != null} {(cherryConfig != null ? $"(name: {cherryConfig.zoneName})" : "")}");
            }
            if (safeConfig == null)
            {
                safeConfig = Resources.Load<MusicConfigSO>("MusicConfigs/SafeZoneMusicConfig");
                Debug.Log($"[DemoController] Loaded SafeConfig: {safeConfig != null} {(safeConfig != null ? $"(name: {safeConfig.zoneName})" : "")}");
            }
            if (victoryConfig == null)
            {
                victoryConfig = Resources.Load<MusicConfigSO>("MusicConfigs/VictoryMusicConfig");
                Debug.Log($"[DemoController] Loaded VictoryConfig: {victoryConfig != null} {(victoryConfig != null ? $"(name: {victoryConfig.zoneName})" : "")}");
            }

            Debug.Log($"[DemoController] Demo controls ready. Configs: Forest={forestConfig != null}, Mountain={mountainConfig != null}, Ocean={oceanConfig != null}, Cherry={cherryConfig != null}, Safe={safeConfig != null}, Victory={victoryConfig != null}");
            
            Debug.Log("[DemoController] Applying initial mode...");
            // Apply initial mode
            ApplyModeChange();
            
            Debug.Log($"[DemoController] === INITIALIZATION COMPLETE === Manual danger: {useManualDanger}, Show UI: {showDebugUI}");
            Debug.Log($"[DemoController] Ready for input: Tab (mode), 1-6 (zones), Up/Down (danger)");
            
            lastAction = "Initialized - Ready for input!";
            currentStatus = "System ready";
        }

        private void Update()
        {
            // Update status periodically
            if (Time.time - lastStatusUpdate > statusUpdateInterval)
            {
                UpdateSystemStatus();
                lastStatusUpdate = Time.time;
            }
            
            // Check for mode change
            if (lastAutoMode != useAutoZoneSwitching)
            {
                Debug.Log($"[DemoController] Mode change detected: {lastAutoMode} -> {useAutoZoneSwitching}");
                lastAction = $"Mode changed to {(useAutoZoneSwitching ? "AUTO" : "MANUAL")}";
                ApplyModeChange();
                lastAutoMode = useAutoZoneSwitching;
            }
            
            // Toggle mode with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log($"[DemoController] Tab pressed - toggling mode from {(useAutoZoneSwitching ? "AUTO" : "MANUAL")} to {(!useAutoZoneSwitching ? "AUTO" : "MANUAL")}");
                useAutoZoneSwitching = !useAutoZoneSwitching;
                lastAction = $"Tab pressed - switching to {(!useAutoZoneSwitching ? "AUTO" : "MANUAL")} mode";
                ApplyModeChange();
            }

            // Only process manual zone shortcuts in MANUAL mode
            if (!useAutoZoneSwitching)
            {
                // Keyboard shortcuts for zones
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Debug.Log("[DemoController] Key '1' pressed - Loading Forest zone");
                    lastAction = "Key 1: Loading Forest zone...";
                    LoadForestZone();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log("[DemoController] Key '2' pressed - Loading Mountain zone");
                    lastAction = "Key 2: Loading Mountain zone...";
                    LoadMountainZone();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log("[DemoController] Key '3' pressed - Loading Ocean zone");
                    lastAction = "Key 3: Loading Ocean zone...";
                    LoadOceanZone();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    Debug.Log("[DemoController] Key '4' pressed - Loading Cherry zone");
                    lastAction = "Key 4: Loading Cherry zone...";
                    LoadCherryZone();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    Debug.Log("[DemoController] Key '5' pressed - Loading Safe zone");
                    lastAction = "Key 5: Loading Safe zone...";
                    LoadSafeZone();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    Debug.Log("[DemoController] Key '6' pressed - Loading Victory zone");
                    lastAction = "Key 6: Loading Victory zone...";
                    LoadVictoryZone();
                }
            }

            // Danger level control with keys
            float previousDanger = manualDangerLevel;
            bool dangerChanged = false;
            if (Input.GetKey(KeyCode.UpArrow))
            {
                manualDangerLevel = Mathf.Min(manualDangerLevel + Time.deltaTime * 0.2f, 1f);
                if (Mathf.Abs(manualDangerLevel - previousDanger) > 0.01f)
                {
                    Debug.Log($"[DemoController] Up Arrow pressed - Danger level: {manualDangerLevel:F3} (was {previousDanger:F3})");
                    lastAction = $"↑ Danger: {manualDangerLevel:F2}";
                    dangerChanged = true;
                }
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                manualDangerLevel = Mathf.Max(manualDangerLevel - Time.deltaTime * 0.2f, 0f);
                if (Mathf.Abs(manualDangerLevel - previousDanger) > 0.01f)
                {
                    Debug.Log($"[DemoController] Down Arrow pressed - Danger level: {manualDangerLevel:F3} (was {previousDanger:F3})");
                    lastAction = $"↓ Danger: {manualDangerLevel:F2}";
                    dangerChanged = true;
                }
            }

            // Apply manual controls
            if (!useAutoZoneSwitching && useManualDanger && musicSystem != null) 
            {
                musicSystem.SetDangerLevel(manualDangerLevel);
            }

            // Only override health in MANUAL mode. In AUTO mode, GameStateTracker syncs from PlayerStats.
            if (!useAutoZoneSwitching && gameStateTracker != null)
                gameStateTracker.SetHealth(manualHealth);
        }

        private void OnGUI()
        {
            if (!showDebugUI)
                return;

            float panelHeight = Mathf.Max(200f, Screen.height - 20f);
            GUILayout.BeginArea(new Rect(10, 10, 450, panelHeight));
            GUILayout.BeginVertical("box");

            debugScrollPosition = GUILayout.BeginScrollView(
                debugScrollPosition,
                false,
                true,
                GUILayout.Width(440),
                GUILayout.Height(panelHeight - 10f)
            );

            GUILayout.Label("=== ADAPTIVE MUSIC DEMO ===", GUI.skin.box);
            
            // Real-time feedback at the top
            GUI.backgroundColor = Color.yellow;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"🎵 LAST ACTION: {lastAction}", GUILayout.Height(25));
            GUILayout.Label($"📊 STATUS: {currentStatus}", GUILayout.Height(20));
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);

            // Mode selection
            GUILayout.Label("=== Mode ===", GUI.skin.box);
            bool newAutoMode = GUILayout.Toggle(useAutoZoneSwitching, "Auto Zone Switching (via BiomeChanger)");
            if (newAutoMode != useAutoZoneSwitching)
            {
                Debug.Log($"[DemoController] Checkbox toggled: {useAutoZoneSwitching} -> {newAutoMode}");
                useAutoZoneSwitching = newAutoMode;
                lastAction = $"Mode toggled to {(newAutoMode ? "AUTO" : "MANUAL")}";
                ApplyModeChange();
            }
            
            // Clear mode indicator with color coding
            GUI.backgroundColor = useAutoZoneSwitching ? Color.yellow : Color.cyan;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"🎛️ Current Mode: {(useAutoZoneSwitching ? "AUTO" : "MANUAL")}");
            if (useAutoZoneSwitching)
                GUILayout.Label("✅ AUTO: Zones via BiomeChanger/AudioManager; danger via GameStateTracker");
            else
                GUILayout.Label("✅ MANUAL: Use buttons/keys 1-6 to switch zones");
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            // Zone selection
            GUI.enabled = !useAutoZoneSwitching;
            GUILayout.Label(useAutoZoneSwitching
                ? "🎯 Zone Selection (keys 1-6) - Disabled in AUTO mode"
                : "🎯 Zone Selection (keys 1-6):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🌲 Forest (1)")) { lastAction = "Button: Loading Forest"; LoadForestZone(); }
            if (GUILayout.Button("⛰️ Mountain (2)")) { lastAction = "Button: Loading Mountain"; LoadMountainZone(); }
            if (GUILayout.Button("🌊 Ocean (3)")) { lastAction = "Button: Loading Ocean"; LoadOceanZone(); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🌸 Cherry (4)")) { lastAction = "Button: Loading Cherry"; LoadCherryZone(); }
            if (GUILayout.Button("🏠 Safe (5)")) { lastAction = "Button: Loading Safe"; LoadSafeZone(); }
            if (GUILayout.Button("🏆 Victory (6)")) { lastAction = "Button: Loading Victory"; LoadVictoryZone(); }
            GUILayout.EndHorizontal();
            GUI.enabled = true; // Re-enable GUI
            GUILayout.Space(10);

            // Manual danger control
            GUI.enabled = !useAutoZoneSwitching;
            useManualDanger = GUILayout.Toggle(useManualDanger, "🎚️ Use Manual Danger Control");
            if (!useAutoZoneSwitching && useManualDanger)
            {
                GUILayout.Label($"⚡ Danger Level: {manualDangerLevel:F2} (Use Up/Down arrows)");
                float newDanger = GUILayout.HorizontalSlider(manualDangerLevel, 0f, 1f);
                if (Mathf.Abs(newDanger - manualDangerLevel) > 0.01f)
                {
                    lastAction = $"Slider: Danger {newDanger:F2}";
                    manualDangerLevel = newDanger;
                }
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            // Health control
            if (useAutoZoneSwitching)
            {
                var liveHealth = gameStateTracker != null ? gameStateTracker.GetHealth() : manualHealth;
                GUILayout.Label($"❤️ Player Health (AUTO): {liveHealth:P0}");
            }
            else
            {
                GUILayout.Label($"❤️ Player Health: {manualHealth:P0}");
                float newHealth = GUILayout.HorizontalSlider(manualHealth, 0f, 1f);
                if (Mathf.Abs(newHealth - manualHealth) > 0.01f)
                {
                    lastAction = $"Health changed to {newHealth:P0}";
                    manualHealth = newHealth;
                }
            }
            GUILayout.Space(10);

            // System status
            if (musicSystem != null)
            {
                GUILayout.Label("=== 🎵 SYSTEM STATUS ===", GUI.skin.box);
                
                // Color-coded current zone
                string currentZone = musicSystem.GetCurrentZone() ?? "None";
                GUI.backgroundColor = GetZoneColor(currentZone);
                GUILayout.BeginVertical("box");
                GUILayout.Label($"🎯 Current Zone: {currentZone.ToUpper()}");
                GUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
                
                GUILayout.Label($"🔧 System Ready: {(musicSystem.IsReady() ? "✅ YES" : "❌ NO")}");

                if (gameStateTracker != null)
                {
                    float dangerLevel = gameStateTracker.GetDangerLevel();
                    string dangerEmoji = dangerLevel < 0.3f ? "😌" : dangerLevel < 0.7f ? "😰" : "💀";
                    GUILayout.Label($"{dangerEmoji} Danger Level: {dangerLevel:F2}");
                    
                    // Layer status indicators
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = dangerLevel >= 0.0f ? Color.green : Color.gray;
                    GUILayout.Label("🎵 Ambient", GUILayout.Width(80));
                    GUI.backgroundColor = dangerLevel >= 0.3f ? Color.yellow : Color.gray;
                    GUILayout.Label("⚡ Tension", GUILayout.Width(80));
                    GUI.backgroundColor = dangerLevel >= 0.7f ? Color.red : Color.gray;
                    GUILayout.Label("⚔️ Combat", GUILayout.Width(80));
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Label($"📊 {gameStateTracker.GetStateInfo()}");
                }

                GUILayout.Space(8);
                GUILayout.Label("=== 🧩 GENERATION DEBUG ===", GUI.skin.box);
                GUILayout.Label(musicSystem.GetLastMidiRequestDebugString());
                GUILayout.Space(4);
                GUILayout.Label(musicSystem.GetLayerPlaybackDebugString());

                if (showGenerationOverrides)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("=== 🛠️ GENERATION OVERRIDES (DEV) ===", GUI.skin.box);

                    var overrides = musicSystem.DebugOverrides;
                    overrides.enabled = GUILayout.Toggle(overrides.enabled, "Enable overrides (development-time)");

                    GUI.enabled = overrides.enabled;
                    overrides.bypassMidiCache = GUILayout.Toggle(overrides.bypassMidiCache, "Bypass MIDI cache while overrides are active");

                    GUILayout.Space(4);
                    overrides.overrideSampling = GUILayout.Toggle(overrides.overrideSampling, "Override sampling (temp/top-p/top-k)");
                    if (overrides.overrideSampling)
                    {
                        GUILayout.Label($"Temp: {overrides.temp:F2}");
                        overrides.temp = GUILayout.HorizontalSlider(overrides.temp, 0.1f, 1.2f);
                        GUILayout.Label($"Top-p: {overrides.top_p:F2}");
                        overrides.top_p = GUILayout.HorizontalSlider(overrides.top_p, 0.1f, 1.0f);
                        GUILayout.Label($"Top-k: {overrides.top_k}");
                        overrides.top_k = Mathf.RoundToInt(GUILayout.HorizontalSlider(overrides.top_k, 1f, 128f));
                    }

                    GUILayout.Space(4);
                    overrides.overrideBpm = GUILayout.Toggle(overrides.overrideBpm, "Override BPM");
                    if (overrides.overrideBpm)
                    {
                        GUILayout.Label($"BPM: {overrides.bpm}");
                        overrides.bpm = Mathf.RoundToInt(GUILayout.HorizontalSlider(overrides.bpm, 40f, 200f));
                    }

                    overrides.overrideGenEvents = GUILayout.Toggle(overrides.overrideGenEvents, "Override length (gen_events)");
                    if (overrides.overrideGenEvents)
                    {
                        GUILayout.Label($"Events: {overrides.gen_events}");
                        overrides.gen_events = Mathf.RoundToInt(GUILayout.HorizontalSlider(overrides.gen_events, 64f, 512f));
                    }

                    GUILayout.Space(4);
                    overrides.overrideTimeSig = GUILayout.Toggle(overrides.overrideTimeSig, "Override time signature");
                    if (overrides.overrideTimeSig)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Time Sig", GUILayout.Width(70));
                        overrides.time_sig = GUILayout.TextField(overrides.time_sig ?? "4/4", GUILayout.Width(120));
                        GUILayout.EndHorizontal();
                    }

                    overrides.overrideKeySig = GUILayout.Toggle(overrides.overrideKeySig, "Override key signature");
                    if (overrides.overrideKeySig)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Key Sig", GUILayout.Width(70));
                        overrides.key_sig = GUILayout.TextField(overrides.key_sig ?? "auto", GUILayout.Width(120));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(4);
                    overrides.overrideSeed = GUILayout.Toggle(overrides.overrideSeed, "Override seed (repro)");
                    if (overrides.overrideSeed)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Seed", GUILayout.Width(70));
                        string seedStr = GUILayout.TextField(overrides.seed.ToString(), GUILayout.Width(120));
                        if (int.TryParse(seedStr, out int parsedSeed))
                            overrides.seed = parsedSeed;
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(4);
                    overrides.overrideInstruments = GUILayout.Toggle(overrides.overrideInstruments, "Override instruments (GM names)");
                    if (overrides.overrideInstruments)
                    {
                        DrawInstrumentPickerRow("Instr 1", 0, ref overrides.instrument1);
                        DrawInstrumentPickerRow("Instr 2", 1, ref overrides.instrument2);
                        DrawInstrumentPickerRow("Instr 3", 2, ref overrides.instrument3);

                        if (instrumentPickerTarget >= 0)
                        {
                            GUILayout.Space(4);
                            GUILayout.BeginVertical(GUI.skin.box);

                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"Select Instrument {instrumentPickerTarget + 1}");
                            if (GUILayout.Button("Close", GUILayout.Width(70)))
                                instrumentPickerTarget = -1;
                            GUILayout.EndHorizontal();

                            instrumentPickerScroll = GUILayout.BeginScrollView(instrumentPickerScroll, GUILayout.Height(220));
                            for (var i = 0; i < InstrumentChoices.Length; i++)
                            {
                                if (GUILayout.Button(InstrumentChoices[i]))
                                {
                                    var selected = i == 0 ? string.Empty : InstrumentChoices[i];
                                    switch (instrumentPickerTarget)
                                    {
                                        case 0:
                                            overrides.instrument1 = selected;
                                            break;
                                        case 1:
                                            overrides.instrument2 = selected;
                                            break;
                                        case 2:
                                            overrides.instrument3 = selected;
                                            break;
                                    }

                                    instrumentPickerTarget = -1;
                                }
                            }

                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                    }
                    else
                    {
                        instrumentPickerTarget = -1;
                    }

                    overrides.overrideDrumKit = GUILayout.Toggle(overrides.overrideDrumKit, "Override drum kit");
                    if (overrides.overrideDrumKit)
                    {
                        int currentIndex = 0;
                        if (!string.IsNullOrEmpty(overrides.drum_kit))
                        {
                            for (int i = 0; i < DrumKitChoices.Length; i++)
                                if (string.Equals(DrumKitChoices[i], overrides.drum_kit, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    currentIndex = i;
                                    break;
                                }
                        }

                        GUILayout.Label("Drum Kit");
                        int newIndex = GUILayout.SelectionGrid(currentIndex, DrumKitChoices, 3);
                        overrides.drum_kit = DrumKitChoices[Mathf.Clamp(newIndex, 0, DrumKitChoices.Length - 1)];
                    }

                    GUILayout.Space(6);
                    GUI.backgroundColor = new Color(0.85f, 1f, 0.85f);
                    if (GUILayout.Button("🔁 Regenerate Current Zone"))
                    {
                        lastAction = "Regenerating current zone...";
                        musicSystem.ForceReloadCurrentZone();
                    }
                    GUI.backgroundColor = Color.white;
                    GUI.enabled = true;
                }
            }

            GUILayout.Space(10);

            // Cache control
            if (GUILayout.Button("🗑️ Clear Cache"))
            {
                musicSystem?.ClearCache();
                lastAction = "Cache cleared successfully";
                Debug.Log("[DemoController] Cache cleared");
            }

            GUILayout.Label("\n⌨️ Keyboard Shortcuts:");
            GUILayout.Label("• Tab - Toggle Auto/Manual mode");
            GUILayout.Label("• 1-6 - Switch zones (Manual mode only)");
            GUILayout.Label("• ↑/↓ - Adjust danger level");
            
            GUILayout.Space(5);
            GUILayout.Label("💡 Tip: Watch the colored status indicators above!");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private Color GetZoneColor(string zoneName)
        {
            return zoneName?.ToLower() switch
            {
                "forest" => Color.green,
                "mountain" => Color.gray,
                "ocean" => Color.cyan,
                "cherry" => new Color(1f, 0.7f, 0.8f), // Pink
                "safe" => Color.white,
                "victory" => Color.yellow,
                _ => Color.white
            };
        }

        private void DrawInstrumentPickerRow(string label, int slotIndex, ref string current)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70));

            var display = string.IsNullOrEmpty(current) ? "(empty)" : current;
            if (GUILayout.Button(display, GUILayout.Width(240)))
            {
                instrumentPickerTarget = instrumentPickerTarget == slotIndex ? -1 : slotIndex;
                instrumentPickerScroll = Vector2.zero;
            }

            GUILayout.EndHorizontal();
        }
        
        private void UpdateSystemStatus()
        {
            if (musicSystem != null && gameStateTracker != null)
            {
                string zone = musicSystem.GetCurrentZone() ?? "None";
                bool ready = musicSystem.IsReady();
                float danger = gameStateTracker.GetDangerLevel();
                
                currentStatus = $"Zone: {zone} | Ready: {(ready ? "Yes" : "No")} | Danger: {danger:F2}";
                
                // Update layer status
                if (ready)
                {
                    string layerStatus = "";
                    if (danger >= 0.7f) layerStatus = "All layers active";
                    else if (danger >= 0.3f) layerStatus = "Ambient + Tension";
                    else layerStatus = "Ambient only";
                    
                    currentStatus += $" | {layerStatus}";
                }
            }
            else
            {
                currentStatus = "System not available";
            }
        }

        public void LoadForestZone()
        {
            Debug.Log($"[DemoController] === LOADING FOREST ZONE === MusicSystem: {musicSystem != null}, Config: {forestConfig != null}");
            if (forestConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with ForestConfig (name: {forestConfig.zoneName})");
                musicSystem.ChangeZone(forestConfig);
                lastAction = "✅ Forest zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Forest - missing references";
                Debug.LogWarning($"[DemoController] Forest config or music system not assigned - Forest: {forestConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        public void LoadMountainZone()
        {
            Debug.Log($"[DemoController] === LOADING MOUNTAIN ZONE === MusicSystem: {musicSystem != null}, Config: {mountainConfig != null}");
            if (mountainConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with MountainConfig (name: {mountainConfig.zoneName})");
                musicSystem.ChangeZone(mountainConfig);
                lastAction = "✅ Mountain zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Mountain - missing references";
                Debug.LogWarning($"[DemoController] Mountain config or music system not assigned - Mountain: {mountainConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        public void LoadOceanZone()
        {
            Debug.Log($"[DemoController] === LOADING OCEAN ZONE === MusicSystem: {musicSystem != null}, Config: {oceanConfig != null}");
            if (oceanConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with OceanConfig (name: {oceanConfig.zoneName})");
                musicSystem.ChangeZone(oceanConfig);
                lastAction = "✅ Ocean zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Ocean - missing references";
                Debug.LogWarning($"[DemoController] Ocean config or music system not assigned - Ocean: {oceanConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        public void LoadCherryZone()
        {
            Debug.Log($"[DemoController] === LOADING CHERRY ZONE === MusicSystem: {musicSystem != null}, Config: {cherryConfig != null}");
            if (cherryConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with CherryConfig (name: {cherryConfig.zoneName})");
                musicSystem.ChangeZone(cherryConfig);
                lastAction = "✅ Cherry zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Cherry - missing references";
                Debug.LogWarning($"[DemoController] Cherry config or music system not assigned - Cherry: {cherryConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        public void LoadSafeZone()
        {
            Debug.Log($"[DemoController] === LOADING SAFE ZONE === MusicSystem: {musicSystem != null}, Config: {safeConfig != null}");
            if (safeConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with SafeConfig (name: {safeConfig.zoneName})");
                musicSystem.ChangeZone(safeConfig);
                lastAction = "✅ Safe zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Safe - missing references";
                Debug.LogWarning($"[DemoController] Safe config or music system not assigned - Safe: {safeConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        public void LoadVictoryZone()
        {
            Debug.Log($"[DemoController] === LOADING VICTORY ZONE === MusicSystem: {musicSystem != null}, Config: {victoryConfig != null}");
            if (victoryConfig != null && musicSystem != null)
            {
                Debug.Log($"[DemoController] Calling musicSystem.ChangeZone() with VictoryConfig (name: {victoryConfig.zoneName})");
                musicSystem.ChangeZone(victoryConfig);
                lastAction = "✅ Victory zone loaded successfully";
                Debug.Log("[DemoController] ChangeZone call completed");
            }
            else
            {
                lastAction = "❌ Failed to load Victory - missing references";
                Debug.LogWarning($"[DemoController] Victory config or music system not assigned - Victory: {victoryConfig != null}, MusicSystem: {musicSystem != null}");
            }
        }

        /// <summary>
        ///     Toggle combat state for testing.
        /// </summary>
        public void ToggleCombat()
        {
            if (gameStateTracker != null)
            {
                var currentState = gameStateTracker.GetDangerLevel() > 0.7f;
                gameStateTracker.SetCombatActive(!currentState);
            }
        }

        /// <summary>
        ///     Simulate taking damage.
        /// </summary>
        public void SimulateDamage(float amount)
        {
            manualHealth = Mathf.Max(0f, manualHealth - amount);
            gameStateTracker?.SetHealth(manualHealth);
        }

        /// <summary>
        ///     Simulate healing.
        /// </summary>
        public void SimulateHeal(float amount)
        {
            manualHealth = Mathf.Min(1f, manualHealth + amount);
            gameStateTracker?.SetHealth(manualHealth);
        }

        /// <summary>
        ///     Apply mode change - manual mode only for Iteration 1.
        /// </summary>
        private void ApplyModeChange()
        {
            Debug.Log($"[DemoController] === MODE CHANGE === User requested: {(useAutoZoneSwitching ? "AUTO" : "MANUAL")}");
            Debug.Log($"[DemoController] GameStateTracker available: {gameStateTracker != null}");
            Debug.Log($"[DemoController] MusicSystem available: {musicSystem != null}");
            
            if (useAutoZoneSwitching)
            {
                Debug.Log("[DemoController] AUTO mode enabled");
                lastAction = "✅ AUTO mode active";
                // Ensure we stop applying manual danger when in AUTO mode
                useManualDanger = false;
            }
            else
            {
                Debug.Log("[DemoController] MANUAL mode selected");
                lastAction = "✅ Manual mode active";
            }
            
            lastAutoMode = useAutoZoneSwitching;
            Debug.Log($"[DemoController] Mode change complete. Manual danger enabled: {useManualDanger}");
        }

        /// <summary>
        ///     Get current mode state.
        /// </summary>
        public bool IsAutoMode => useAutoZoneSwitching;
    }
}