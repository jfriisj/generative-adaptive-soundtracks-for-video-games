using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Maps game state (danger level, health, enemies) to dynamic music generation parameters.
    ///     Creates adaptive chord progressions and melodies that respond to gameplay in real-time.
    /// </summary>
    public class DynamicMusicParameterMapper : MonoBehaviour
    {
        [Header("BPM Mapping")] [SerializeField] [Tooltip("BPM range for ambient layer (low danger)")]
        private Vector2Int ambientBPMRange = new(60, 80);

        [SerializeField] [Tooltip("BPM range for tension layer (medium danger)")]
        private Vector2Int tensionBPMRange = new(85, 110);

        [SerializeField] [Tooltip("BPM range for combat layer (high danger)")]
        private Vector2Int combatBPMRange = new(115, 140);

        [Header("Generation Events Mapping")] [SerializeField] [Tooltip("Base gen_events for ambient music")]
        private int ambientGenEvents = 256;

        [SerializeField] [Tooltip("Increase gen_events in high-danger scenarios")]
        private int tensionGenEventsBonus = 128;

        [SerializeField] [Tooltip("Maximum gen_events to prevent excessive latency")]
        private int maxGenEvents = 512;

        [Header("Instrument Selection")] [SerializeField] [Tooltip("Instruments for low danger (0.0 - 0.3)")]
        private string[] lowDangerInstruments = { "Acoustic Grand", "Flute", "Pad" };

        [SerializeField] [Tooltip("Instruments for medium danger (0.3 - 0.7)")]
        private string[] mediumDangerInstruments = { "Strings", "Piano", "Synth Lead" };

        [SerializeField] [Tooltip("Instruments for high danger (0.7 - 1.0)")]
        private string[] highDangerInstruments = { "Distortion Guitar", "Strings", "Synth Bass" };

        [Header("Drum Mapping")] [SerializeField] [Tooltip("Enable drums based on danger threshold")]
        private float drumThreshold = 0.5f;

        [SerializeField] private string[] drumKits = { "Standard", "Rock", "Electronic" };

        [Header("Health-Based Modulation")] [SerializeField] [Tooltip("Reduce BPM when health is critically low")]
        private bool applyHealthModulation = true;

        [SerializeField] [Tooltip("Health threshold for critical state (slower, more dramatic music)")]
        private float criticalHealthThreshold = 0.2f;

        [SerializeField] [Tooltip("BPM reduction percentage in critical health state")] [Range(0f, 0.5f)]
        private float criticalHealthBPMReduction = 0.15f;

        [Header("Enemy Proximity Influence")]
        [SerializeField]
        [Tooltip("Increase gen_events complexity based on nearby enemies")]
        private bool enemyProximityAffectsComplexity = true;

        [SerializeField] [Tooltip("Additional gen_events per nearby enemy")]
        private int genEventsPerEnemy = 32;

        public static DynamicMusicParameterMapper Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        ///     Generate dynamic music parameters based on current game state.
        /// </summary>
        public LayerConfig GenerateDynamicParameters(
            string layerName,
            float dangerLevel,
            float playerHealth,
            int nearbyEnemies,
            string zoneName)
        {
            var config = new LayerConfig
            {
                name = layerName,
                seed = GenerateSeed(dangerLevel, playerHealth, nearbyEnemies)
            };

            // Map danger level to BPM
            config.bpm = CalculateDynamicBPM(layerName, dangerLevel, playerHealth);

            // Map danger and enemies to generation complexity
            config.gen_events = CalculateGenEvents(dangerLevel, nearbyEnemies);

            // Select instruments based on danger level
            config.instruments = SelectInstruments(dangerLevel, layerName);

            // Determine drum kit
            config.drum_kit = SelectDrumKit(dangerLevel);

            // Time signature (could be dynamic in future)
            config.time_sig = "4/4";

            // Allow control changes for dynamic expression
            config.allow_cc = true;

            Debug.Log($"[DynamicMapper] {layerName} | Danger: {dangerLevel:F2} | BPM: {config.bpm} | " +
                      $"Events: {config.gen_events} | Instruments: {string.Join(", ", config.instruments)}");

            return config;
        }

        /// <summary>
        ///     Calculate dynamic BPM based on layer, danger, and health.
        /// </summary>
        private int CalculateDynamicBPM(string layerName, float dangerLevel, float playerHealth)
        {
            Vector2Int bpmRange;

            // Select base BPM range based on layer
            switch (layerName.ToLower())
            {
                case "ambient":
                    bpmRange = ambientBPMRange;
                    break;
                case "tension":
                    bpmRange = tensionBPMRange;
                    break;
                case "combat":
                    bpmRange = combatBPMRange;
                    break;
                default:
                    bpmRange = ambientBPMRange;
                    break;
            }

            // Interpolate within range based on danger level
            var baseBPM = Mathf.RoundToInt(Mathf.Lerp(bpmRange.x, bpmRange.y, dangerLevel));

            // Apply health modulation (slow down when critically wounded)
            if (applyHealthModulation && playerHealth <= criticalHealthThreshold)
            {
                var healthFactor = playerHealth / criticalHealthThreshold; // 0.0 - 1.0
                var reduction = criticalHealthBPMReduction * (1f - healthFactor);
                baseBPM = Mathf.RoundToInt(baseBPM * (1f - reduction));
            }

            // Quantize to musical values (multiples of 5)
            baseBPM = Mathf.RoundToInt(baseBPM / 5f) * 5;

            return Mathf.Clamp(baseBPM, 40, 200);
        }

        /// <summary>
        ///     Calculate generation events based on danger and enemy count.
        /// </summary>
        private int CalculateGenEvents(float dangerLevel, int nearbyEnemies)
        {
            var baseEvents = ambientGenEvents;

            // Add complexity based on danger level
            if (dangerLevel > 0.3f)
            {
                var dangerFactor = Mathf.InverseLerp(0.3f, 1.0f, dangerLevel);
                baseEvents += Mathf.RoundToInt(tensionGenEventsBonus * dangerFactor);
            }

            // Add complexity based on enemy proximity
            if (enemyProximityAffectsComplexity && nearbyEnemies > 0) baseEvents += nearbyEnemies * genEventsPerEnemy;

            // Clamp to prevent excessive latency
            return Mathf.Clamp(baseEvents, 128, maxGenEvents);
        }

        /// <summary>
        ///     Select instruments based on danger level and layer.
        /// </summary>
        private string[] SelectInstruments(float dangerLevel, string layerName)
        {
            var selectedInstruments = new List<string>();

            // Select primary instruments based on danger level
            if (dangerLevel < 0.3f)
            {
                // Low danger: peaceful, ambient instruments
                var count = Mathf.Min(2, lowDangerInstruments.Length);
                for (var i = 0; i < count; i++) selectedInstruments.Add(lowDangerInstruments[i]);
            }
            else if (dangerLevel < 0.7f)
            {
                // Medium danger: blend ambient and intense instruments
                var count = Mathf.Min(2, mediumDangerInstruments.Length);
                for (var i = 0; i < count; i++) selectedInstruments.Add(mediumDangerInstruments[i]);

                // Add a low danger instrument for transition
                if (lowDangerInstruments.Length > 0)
                    selectedInstruments.Add(lowDangerInstruments[Random.Range(0, lowDangerInstruments.Length)]);
            }
            else
            {
                // High danger: intense, aggressive instruments
                var count = Mathf.Min(3, highDangerInstruments.Length);
                for (var i = 0; i < count; i++) selectedInstruments.Add(highDangerInstruments[i]);
            }

            // Layer-specific overrides
            if (layerName.ToLower() == "ambient" && selectedInstruments.Count == 0)
                selectedInstruments.Add("Acoustic Grand");
            else if (layerName.ToLower() == "combat" && selectedInstruments.Count == 0)
                selectedInstruments.Add("Distortion Guitar");

            return selectedInstruments.Count > 0 ? selectedInstruments.ToArray() : new[] { "Acoustic Grand" };
        }

        /// <summary>
        ///     Select drum kit based on danger level.
        /// </summary>
        private string SelectDrumKit(float dangerLevel)
        {
            if (dangerLevel < drumThreshold) return "None";

            // Select drum kit based on danger intensity
            if (dangerLevel < 0.7f) return "Standard";

            if (dangerLevel < 0.9f) return "Rock";

            return "Electronic";
        }

        /// <summary>
        ///     Generate a seed based on game state for reproducible but contextual music.
        ///     Optional <paramref name="salt"/> helps differentiate seeds across zones/layers.
        /// </summary>
        private int GenerateSeed(float dangerLevel, float playerHealth, int nearbyEnemies, string salt = null)
        {
            // Create a deterministic but state-dependent seed
            var dangerComponent = Mathf.RoundToInt(dangerLevel * 1000f);
            var healthComponent = Mathf.RoundToInt(playerHealth * 1000f);
            var enemyComponent = nearbyEnemies * 100;
            var timeComponent = Mathf.RoundToInt(Time.time) % 10000;

            // Stable (cross-session) string hash; do NOT use string.GetHashCode().
            int saltHash = 0;
            if (!string.IsNullOrEmpty(salt))
                unchecked
                {
                    for (int i = 0; i < salt.Length; i++) saltHash = (saltHash * 31) + salt[i];
                }

            // Combine components with prime multipliers
            var seed = dangerComponent * 7919 + healthComponent * 6971 + enemyComponent * 5381 + timeComponent + saltHash * 1543;

            return Mathf.Abs(seed);
        }

        /// <summary>
        ///     Update existing layer config with dynamic parameters.
        /// </summary>
        public void UpdateLayerConfig(ref LayerConfig config, float dangerLevel, float playerHealth, int nearbyEnemies)
        {
            config.bpm = CalculateDynamicBPM(config.name, dangerLevel, playerHealth);
            config.gen_events = CalculateGenEvents(dangerLevel, nearbyEnemies);
            config.instruments = SelectInstruments(dangerLevel, config.name);
            config.drum_kit = SelectDrumKit(dangerLevel);
            config.seed = GenerateSeed(dangerLevel, playerHealth, nearbyEnemies, config.name);
        }

        /// <summary>
        ///     Apply dynamic modifiers while preserving the zone's musical identity.
        ///     This keeps instruments / drum kit / time signature / key signature / sampling values from the zone config,
        ///     and only adjusts BPM, length, and seed from gameplay state.
        /// </summary>
        public void ApplyDynamicModifiersPreserveIdentity(
            ref LayerConfig config,
            float dangerLevel,
            float playerHealth,
            int nearbyEnemies,
            string seedSalt = null)
        {
            config.bpm = CalculateDynamicBPM(config.name, dangerLevel, playerHealth);
            config.gen_events = CalculateGenEvents(dangerLevel, nearbyEnemies);
            config.seed = GenerateSeed(dangerLevel, playerHealth, nearbyEnemies, seedSalt ?? config.name);
        }

        /// <summary>
        ///     Get mapping statistics for evaluation.
        /// </summary>
        public string GetMappingStats()
        {
            return $"BPM Ranges: A[{ambientBPMRange.x}-{ambientBPMRange.y}] " +
                   $"T[{tensionBPMRange.x}-{tensionBPMRange.y}] " +
                   $"C[{combatBPMRange.x}-{combatBPMRange.y}] | " +
                   $"GenEvents: {ambientGenEvents}-{maxGenEvents} | " +
                   $"DrumThreshold: {drumThreshold:F2}";
        }

        /// <summary>
        ///     Calculate an intensity value (0.0 - 1.0) based on player health, combat state and danger.
        ///     Higher intensity when: low health, in combat, high danger.
        /// </summary>
        public float CalculateIntensity(float playerHealth01, bool combatActive, float dangerLevel01)
        {
            // Invert health (low health increases intensity)
            float healthComponent = 1f - Mathf.Clamp01(playerHealth01);          // 0 (full) -> 1 (empty)
            float combatComponent = combatActive ? 1f : 0f;                      // 1 if in combat
            float dangerComponent = Mathf.Clamp01(dangerLevel01);                // already 0-1

            // Weighted blend (tweakable): health 0.2, combat 0.4, danger 0.4
            float intensity = healthComponent * 0.2f + combatComponent * 0.4f + dangerComponent * 0.4f;
            return Mathf.Clamp01(intensity);
        }
    }
}