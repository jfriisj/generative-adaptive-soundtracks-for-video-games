using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Tracks game state and calculates danger level for adaptive music.
    ///     Aggregates health, enemy proximity, and combat state into a 0.0-1.0 danger value.
    /// </summary>
    public class GameStateTracker : MonoBehaviour
    {
        [Header("Player State")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Player health percentage (1.0 = full health, 0.0 = dead)")]
        private float health = 1f;

        [SerializeField]
        [Tooltip("If enabled, reads PlayerStats (currentHP/MaxHP) from the Player-tagged object and updates health automatically.")]
        private bool autoSyncHealthFromPlayerStats = true;

        [SerializeField]
        [Tooltip("Tag used to locate the player GameObject.")]
        private string playerTag = "Player";

        [SerializeField]
        [Tooltip("If health is set externally, auto-sync is paused for this many seconds.")]
        private float externalHealthOverrideGraceSeconds = 0.25f;

        [SerializeField] [Tooltip("Is player currently in combat?")]
        private bool combatActive;

        [Header("Enemy Tracking")] [SerializeField] [Tooltip("Maximum distance to consider enemies as threats")]
        private float enemyDetectionRadius = 20f;

        [SerializeField] [Tooltip("Enemies within this distance contribute maximum danger")]
        private float criticalEnemyRadius = 5f;

        [SerializeField]
        [Tooltip("If enabled, finds enemies by tag. If none are found, falls back to scanning EnemyStats components.")]
        private bool preferEnemyTag = true;

        [SerializeField]
        [Tooltip("Tag used to find enemies when Prefer Enemy Tag is enabled.")]
        private string enemyTag = "Enemy";

        [SerializeField]
        [Tooltip("How often to rescan the scene for enemies (seconds). Lower = more responsive, higher = cheaper.")]
        private float enemyScanIntervalSeconds = 0.25f;

        [Header("Danger Calculation")] [SerializeField] [Range(0f, 1f)] [Tooltip("Current calculated danger level")]
        private float dangerLevel;

        [SerializeField] [Range(0f, 1f)] [Tooltip("Smoothing factor for danger changes (0 = instant, 1 = very smooth)")]
        private float dangerSmoothFactor = 0.1f;

        [SerializeField] [Tooltip("Danger contribution from active combat")]
        private float combatDangerBonus = 0.3f;

        [Header("Debug")] [SerializeField] private bool showDebugInfo = true;

        private readonly List<Transform> nearbyEnemies = new();
        private readonly List<Transform> externallyRegisteredEnemies = new();
        private Transform playerTransform;

        private Component cachedPlayerStatsComponent;
        private FieldInfo cachedPlayerStatsMaxHpField;
        private FieldInfo cachedPlayerStatsCurrentHpField;

        private float lastExternalHealthSetRealtime = -999f;
        private float lastEnemyScanTime = -999f;

        private float targetDangerLevel;
        public static GameStateTracker Instance { get; private set; }

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

        private void Start()
        {
            TryResolvePlayer();
        }

        private void Update()
        {
            AutoSyncHealthFromPlayerStats();

            // Update enemy tracking
            UpdateNearbyEnemies();

            // Calculate target danger level
            targetDangerLevel = CalculateDangerLevel();

            // Smooth danger level changes
            dangerLevel = Mathf.Lerp(dangerLevel, targetDangerLevel, dangerSmoothFactor);

            if (showDebugInfo) DrawDebugInfo();
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;

            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, enemyDetectionRadius);

            // Draw critical radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, criticalEnemyRadius);
        }

        /// <summary>
        ///     Calculate danger level from game state.
        /// </summary>
        private float CalculateDangerLevel()
        {
            var danger = 0f;

            // Health contribution (inverse: low health = high danger)
            danger += (1f - health) * 0.5f;

            // Enemy proximity contribution
            if (playerTransform != null)
            {
                var enemyDanger = CalculateEnemyDanger();
                danger += enemyDanger * 0.4f;
            }

            // Combat state contribution
            if (combatActive) danger += combatDangerBonus;

            // Clamp to valid range
            return Mathf.Clamp01(danger);
        }

        /// <summary>
        ///     Calculate danger contribution from nearby enemies.
        /// </summary>
        private float CalculateEnemyDanger()
        {
            if (nearbyEnemies.Count == 0)
                return 0f;

            var totalDanger = 0f;

            foreach (var enemy in nearbyEnemies)
            {
                if (enemy == null) continue;

                var distance = Vector3.Distance(playerTransform.position, enemy.position);

                // Calculate danger based on distance (closer = more dangerous)
                var normalizedDistance = Mathf.Clamp01((distance - criticalEnemyRadius) /
                                                       (enemyDetectionRadius - criticalEnemyRadius));
                var enemyDanger = 1f - normalizedDistance;

                totalDanger += enemyDanger;
            }

            // Normalize by number of enemies (3+ enemies = maximum danger)
            return Mathf.Min(totalDanger / 3f, 1f);
        }

        /// <summary>
        ///     Update list of nearby enemies.
        /// </summary>
        private void UpdateNearbyEnemies()
        {
            if (Time.time - lastEnemyScanTime < enemyScanIntervalSeconds)
                return;
            lastEnemyScanTime = Time.time;

            nearbyEnemies.Clear();

            // Always include externally registered enemies (e.g., spawned at runtime)
            for (var i = externallyRegisteredEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = externallyRegisteredEnemies[i];
                if (enemy == null)
                {
                    externallyRegisteredEnemies.RemoveAt(i);
                    continue;
                }

                nearbyEnemies.Add(enemy);
            }

            if (playerTransform == null)
                return;

            // Prefer tag-based lookup (fast), but fall back if the scene doesn't use the Enemy tag.
            GameObject[] enemiesByTag = null;
            if (preferEnemyTag && !string.IsNullOrWhiteSpace(enemyTag))
            {
                try
                {
                    enemiesByTag = GameObject.FindGameObjectsWithTag(enemyTag);
                }
                catch
                {
                    enemiesByTag = null;
                }
            }

            if (enemiesByTag != null && enemiesByTag.Length > 0)
            {
                foreach (var enemy in enemiesByTag)
                {
                    if (enemy == null) continue;
                    var enemyTransform = enemy.transform;
                    if (enemyTransform == null) continue;

                    var distance = Vector3.Distance(playerTransform.position, enemyTransform.position);
                    if (distance <= enemyDetectionRadius && !nearbyEnemies.Contains(enemyTransform))
                        nearbyEnemies.Add(enemyTransform);
                }

                return;
            }

            // Fallback: detect enemies by component (works even if they are Untagged)
            var behaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                if (behaviour.GetType().Name != "EnemyStats") continue;

                var enemyTransform = behaviour.transform;
                if (enemyTransform == null) continue;

                var distance = Vector3.Distance(playerTransform.position, enemyTransform.position);
                if (distance <= enemyDetectionRadius && !nearbyEnemies.Contains(enemyTransform))
                    nearbyEnemies.Add(enemyTransform);
            }
        }

        /// <summary>
        ///     Get current danger level (0.0 = safe, 1.0 = critical).
        /// </summary>
        public float GetDangerLevel()
        {
            return dangerLevel;
        }

        /// <summary>
        ///     Get current player health (0.0 - 1.0).
        /// </summary>
        public float GetHealth()
        {
            return health;
        }

        /// <summary>
        ///     Set player health (0.0 - 1.0).
        /// </summary>
        public void SetHealth(float healthValue)
        {
            lastExternalHealthSetRealtime = Time.realtimeSinceStartup;
            SetHealthInternal(healthValue);
        }

        private void SetHealthInternal(float healthValue)
        {
            health = Mathf.Clamp01(healthValue);
        }

        private void TryResolvePlayer()
        {
            GameObject player = null;

            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                try
                {
                    player = GameObject.FindGameObjectWithTag(playerTag);
                }
                catch
                {
                    player = null;
                }
            }

            if (player == null)
            {
                Debug.LogWarning($"[GameStateTracker] No GameObject with '{playerTag}' tag found");
                playerTransform = null;
                cachedPlayerStatsComponent = null;
                cachedPlayerStatsMaxHpField = null;
                cachedPlayerStatsCurrentHpField = null;
                return;
            }

            playerTransform = player.transform;
            cachedPlayerStatsComponent = player.GetComponent("PlayerStats");
            cachedPlayerStatsMaxHpField = null;
            cachedPlayerStatsCurrentHpField = null;
        }

        private void AutoSyncHealthFromPlayerStats()
        {
            if (!autoSyncHealthFromPlayerStats)
                return;

            if (Time.realtimeSinceStartup - lastExternalHealthSetRealtime < externalHealthOverrideGraceSeconds)
                return;

            if (playerTransform == null || cachedPlayerStatsComponent == null)
                TryResolvePlayer();

            if (cachedPlayerStatsComponent == null)
                return;

            try
            {
                var statsType = cachedPlayerStatsComponent.GetType();
                if (cachedPlayerStatsMaxHpField == null)
                    cachedPlayerStatsMaxHpField = statsType.GetField("MaxHP", BindingFlags.Instance | BindingFlags.Public);
                if (cachedPlayerStatsCurrentHpField == null)
                    cachedPlayerStatsCurrentHpField = statsType.GetField("currentHP", BindingFlags.Instance | BindingFlags.Public);

                if (cachedPlayerStatsMaxHpField == null || cachedPlayerStatsCurrentHpField == null)
                    return;

                var maxHpObj = cachedPlayerStatsMaxHpField.GetValue(cachedPlayerStatsComponent);
                var curHpObj = cachedPlayerStatsCurrentHpField.GetValue(cachedPlayerStatsComponent);
                if (maxHpObj == null || curHpObj == null)
                    return;

                var maxHp = (int)maxHpObj;
                var currentHp = (int)curHpObj;
                if (maxHp <= 0)
                    return;

                var normalizedHealth = Mathf.Clamp01(currentHp / (float)maxHp);
                SetHealthInternal(normalizedHealth);
            }
            catch
            {
                // If PlayerStats changes or is missing, just skip auto-sync.
            }
        }

        /// <summary>
        ///     Set combat state.
        /// </summary>
        public void SetCombatActive(bool active)
        {
            if (combatActive != active)
            {
                combatActive = active;
                Debug.Log($"[GameStateTracker] Combat {(active ? "started" : "ended")}");
            }
        }

        /// <summary>
        ///     Get number of nearby enemies.
        /// </summary>
        public int GetNearbyEnemyCount()
        {
            return nearbyEnemies.Count;
        }

        /// <summary>
        ///     Get detailed state information for debugging.
        /// </summary>
        public string GetStateInfo()
        {
            return
                $"Health: {health:P0}, Enemies: {nearbyEnemies.Count}, Combat: {combatActive}, Danger: {dangerLevel:F2}";
        }

        /// <summary>
        ///     Draw debug information in editor.
        /// </summary>
        private void DrawDebugInfo()
        {
            if (playerTransform == null) return;

            // Draw detection radius
            Debug.DrawRay(playerTransform.position, Vector3.forward * enemyDetectionRadius, Color.yellow);
            Debug.DrawRay(playerTransform.position, Vector3.right * enemyDetectionRadius, Color.yellow);

            // Draw critical radius
            Debug.DrawRay(playerTransform.position, Vector3.forward * criticalEnemyRadius, Color.red);
            Debug.DrawRay(playerTransform.position, Vector3.right * criticalEnemyRadius, Color.red);

            // Draw lines to nearby enemies
            foreach (var enemy in nearbyEnemies)
                if (enemy != null)
                    Debug.DrawLine(playerTransform.position, enemy.position, Color.red);
        }

        /// <summary>
        ///     Manually add an enemy transform for tracking.
        /// </summary>
        public void RegisterEnemy(Transform enemy)
        {
            if (enemy == null)
                return;

            if (!externallyRegisteredEnemies.Contains(enemy))
                externallyRegisteredEnemies.Add(enemy);
        }

        /// <summary>
        ///     Remove an enemy from tracking.
        /// </summary>
        public void UnregisterEnemy(Transform enemy)
        {
            externallyRegisteredEnemies.Remove(enemy);
            nearbyEnemies.Remove(enemy);
        }
        
    }
}