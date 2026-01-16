using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Collects quantitative metrics for adaptive music system evaluation.
    ///     Tracks generation latency, parameter correlations, and performance benchmarks.
    /// </summary>
    public class MetricsCollector : MonoBehaviour
    {
        [Header("Metrics Settings")] [SerializeField] [Tooltip("Enable metric collection")]
        private bool collectMetrics = true;

        [SerializeField] [Tooltip("Log metrics to console")]
        private bool logToConsole;

        [SerializeField] [Tooltip("Output directory for metric CSV files")]
        private string outputDirectory = "AdaptiveMusicMetrics";

        private int failedGenerations;
        private readonly List<GameStateSnapshot> gameStateSnapshots = new();

        // Metric storage
        private readonly List<GenerationMetric> generationMetrics = new();

        // Performance tracking
        private float lastFrameTime;
        private float lastUpdateTime;
        private readonly List<ParameterCorrelation> parameterCorrelations = new();
        private readonly List<PerformanceMetric> performanceMetrics = new();

        private float sessionStartTime;
        private int successfulGenerations;
        private int totalGenerations;
        public static MetricsCollector Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                sessionStartTime = Time.time;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Create output directory
            var fullPath = Path.Combine(Application.persistentDataPath, outputDirectory);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[MetricsCollector] Created metrics directory: {fullPath}");
            }
        }

        private void Update()
        {
            if (!collectMetrics) return;

            // Track frame time
            var currentFrameTime = Time.deltaTime * 1000f; // Convert to ms
            lastFrameTime = currentFrameTime;

            // Periodically capture game state snapshots (every 0.5 seconds)
            if (Time.time - lastUpdateTime >= 0.5f)
            {
                CaptureGameStateSnapshot();
                CapturePerformanceMetric();
                lastUpdateTime = Time.time;
            }
        }

        private void OnApplicationQuit()
        {
            if (collectMetrics && totalGenerations > 0) ExportMetrics();
        }

        /// <summary>
        ///     Record a MIDI generation attempt.
        /// </summary>
        public void RecordGeneration(
            string layerName,
            string zoneName,
            float dangerLevel,
            float playerHealth,
            int nearbyEnemies,
            int seed,
            int genEvents,
            int bpm,
            string[] instruments,
            float requestStartTime,
            float requestEndTime,
            bool success,
            int midiBytes = 0,
            string errorMessage = null)
        {
            if (!collectMetrics) return;

            var latency = (requestEndTime - requestStartTime) * 1000f; // Convert to ms

            var metric = new GenerationMetric
            {
                timestamp = Time.time - sessionStartTime,
                layerName = layerName,
                zoneName = zoneName,
                dangerLevel = dangerLevel,
                playerHealth = playerHealth,
                nearbyEnemies = nearbyEnemies,
                seed = seed,
                genEvents = genEvents,
                bpm = bpm,
                instruments = string.Join("|", instruments),
                requestStartTime = requestStartTime - sessionStartTime,
                requestEndTime = requestEndTime - sessionStartTime,
                latencyMs = latency,
                success = success,
                midiBytes = midiBytes,
                errorMessage = errorMessage ?? ""
            };

            generationMetrics.Add(metric);

            if (success)
            {
                successfulGenerations++;
                RecordParameterCorrelation(dangerLevel, bpm, genEvents, instruments, playerHealth, nearbyEnemies);
            }
            else
            {
                failedGenerations++;
            }

            totalGenerations++;

            if (logToConsole)
                Debug.Log($"[MetricsCollector] Generation: {layerName} | Latency: {latency:F2}ms | Success: {success}");
        }

        /// <summary>
        ///     Record parameter correlation data.
        /// </summary>
        private void RecordParameterCorrelation(
            float dangerLevel,
            int bpm,
            int genEvents,
            string[] instruments,
            float health,
            int enemies)
        {
            var correlation = new ParameterCorrelation
            {
                timestamp = Time.time - sessionStartTime,
                dangerLevel = dangerLevel,
                bpmGenerated = bpm,
                genEvents = genEvents,
                instrumentsUsed = string.Join("|", instruments),
                healthAtGeneration = health,
                enemyCountAtGeneration = enemies
            };

            parameterCorrelations.Add(correlation);
        }

        /// <summary>
        ///     Capture current game state snapshot.
        /// </summary>
        private void CaptureGameStateSnapshot()
        {
            if (GameStateTracker.Instance == null || AdaptiveMusicSystem.Instance == null)
                return;

            var tracker = GameStateTracker.Instance;
            var musicSystem = AdaptiveMusicSystem.Instance;

            var snapshot = new GameStateSnapshot
            {
                timestamp = Time.time - sessionStartTime,
                dangerLevel = tracker.GetDangerLevel(),
                playerHealth = tracker.GetType().GetField("health",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(tracker) as float? ?? 0f,
                nearbyEnemies = tracker.GetNearbyEnemyCount(),
                inCombat = (bool)(tracker.GetType().GetField("combatActive",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(tracker) ?? false),
                playerPosition = GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero,
                currentZone = musicSystem.GetType().GetField("currentZoneName",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(musicSystem) as string ?? "unknown",
                ambientVolume = 0f, // Will be populated by LayerMixer if available
                tensionVolume = 0f,
                combatVolume = 0f
            };

            gameStateSnapshots.Add(snapshot);
        }

        /// <summary>
        ///     Capture performance metrics.
        /// </summary>
        private void CapturePerformanceMetric()
        {
            var metric = new PerformanceMetric
            {
                timestamp = Time.time - sessionStartTime,
                fps = 1f / Time.deltaTime,
                frameTimeMs = lastFrameTime,
                updateTimeMs = Time.deltaTime * 1000f,
                activeAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,
                memoryUsageMB = GC.GetTotalMemory(false) / (1024f * 1024f),
                cacheHitRate = 0, // Will be populated by MidiCacheManager if available
                cacheMissRate = 0
            };

            performanceMetrics.Add(metric);
        }

        /// <summary>
        ///     Export all collected metrics to CSV files.
        /// </summary>
        public void ExportMetrics()
        {
            var basePath = Path.Combine(Application.persistentDataPath, outputDirectory);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            ExportGenerationMetrics(Path.Combine(basePath, $"generation_metrics_{timestamp}.csv"));
            ExportGameStateSnapshots(Path.Combine(basePath, $"gamestate_snapshots_{timestamp}.csv"));
            ExportParameterCorrelations(Path.Combine(basePath, $"parameter_correlations_{timestamp}.csv"));
            ExportPerformanceMetrics(Path.Combine(basePath, $"performance_metrics_{timestamp}.csv"));
            ExportSummary(Path.Combine(basePath, $"session_summary_{timestamp}.txt"));

            Debug.Log($"[MetricsCollector] Metrics exported to: {basePath}");
        }

        private void ExportGenerationMetrics(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "Timestamp,LayerName,ZoneName,DangerLevel,PlayerHealth,NearbyEnemies,Seed,GenEvents,BPM,Instruments,RequestStart,RequestEnd,LatencyMs,Success,MidiBytes,ErrorMessage");

            foreach (var metric in generationMetrics)
                sb.AppendLine($"{metric.timestamp:F3},{metric.layerName},{metric.zoneName},{metric.dangerLevel:F3}," +
                              $"{metric.playerHealth:F3},{metric.nearbyEnemies},{metric.seed},{metric.genEvents},{metric.bpm}," +
                              $"\"{metric.instruments}\",{metric.requestStartTime:F3},{metric.requestEndTime:F3}," +
                              $"{metric.latencyMs:F2},{metric.success},{metric.midiBytes},\"{metric.errorMessage}\"");

            File.WriteAllText(path, sb.ToString());
        }

        private void ExportGameStateSnapshots(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "Timestamp,DangerLevel,PlayerHealth,NearbyEnemies,InCombat,PlayerPosX,PlayerPosY,PlayerPosZ,CurrentZone,AmbientVolume,TensionVolume,CombatVolume");

            foreach (var snapshot in gameStateSnapshots)
                sb.AppendLine($"{snapshot.timestamp:F3},{snapshot.dangerLevel:F3},{snapshot.playerHealth:F3}," +
                              $"{snapshot.nearbyEnemies},{snapshot.inCombat},{snapshot.playerPosition.x:F2}," +
                              $"{snapshot.playerPosition.y:F2},{snapshot.playerPosition.z:F2},{snapshot.currentZone}," +
                              $"{snapshot.ambientVolume:F3},{snapshot.tensionVolume:F3},{snapshot.combatVolume:F3}");

            File.WriteAllText(path, sb.ToString());
        }

        private void ExportParameterCorrelations(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,DangerLevel,BPM,GenEvents,Instruments,HealthAtGeneration,EnemyCountAtGeneration");

            foreach (var corr in parameterCorrelations)
                sb.AppendLine($"{corr.timestamp:F3},{corr.dangerLevel:F3},{corr.bpmGenerated}," +
                              $"{corr.genEvents},\"{corr.instrumentsUsed}\",{corr.healthAtGeneration:F3}," +
                              $"{corr.enemyCountAtGeneration}");

            File.WriteAllText(path, sb.ToString());
        }

        private void ExportPerformanceMetrics(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "Timestamp,FPS,FrameTimeMs,UpdateTimeMs,ActiveAudioSources,MemoryUsageMB,CacheHitRate,CacheMissRate");

            foreach (var metric in performanceMetrics)
                sb.AppendLine($"{metric.timestamp:F3},{metric.fps:F2},{metric.frameTimeMs:F2}," +
                              $"{metric.updateTimeMs:F2},{metric.activeAudioSources},{metric.memoryUsageMB:F2}," +
                              $"{metric.cacheHitRate},{metric.cacheMissRate}");

            File.WriteAllText(path, sb.ToString());
        }

        private void ExportSummary(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Adaptive Music System - Session Summary ===");
            sb.AppendLine($"Session Duration: {Time.time - sessionStartTime:F2} seconds");
            sb.AppendLine($"Total Generations: {totalGenerations}");
            sb.AppendLine(
                $"Successful: {successfulGenerations} ({(float)successfulGenerations / totalGenerations * 100:F1}%)");
            sb.AppendLine($"Failed: {failedGenerations} ({(float)failedGenerations / totalGenerations * 100:F1}%)");
            sb.AppendLine();

            if (generationMetrics.Count > 0)
            {
                var avgLatency = 0f;
                var minLatency = float.MaxValue;
                var maxLatency = float.MinValue;

                foreach (var metric in generationMetrics)
                    if (metric.success)
                    {
                        avgLatency += metric.latencyMs;
                        minLatency = Mathf.Min(minLatency, metric.latencyMs);
                        maxLatency = Mathf.Max(maxLatency, metric.latencyMs);
                    }

                avgLatency /= successfulGenerations;

                sb.AppendLine("=== Generation Latency ===");
                sb.AppendLine($"Average: {avgLatency:F2}ms");
                sb.AppendLine($"Min: {minLatency:F2}ms");
                sb.AppendLine($"Max: {maxLatency:F2}ms");
                sb.AppendLine();
            }

            if (performanceMetrics.Count > 0)
            {
                var avgFps = 0f;
                var avgMemory = 0f;

                foreach (var metric in performanceMetrics)
                {
                    avgFps += metric.fps;
                    avgMemory += metric.memoryUsageMB;
                }

                avgFps /= performanceMetrics.Count;
                avgMemory /= performanceMetrics.Count;

                sb.AppendLine("=== Performance ===");
                sb.AppendLine($"Average FPS: {avgFps:F2}");
                sb.AppendLine($"Average Memory: {avgMemory:F2}MB");
                sb.AppendLine();
            }

            sb.AppendLine("=== Data Files Generated ===");
            sb.AppendLine($"- generation_metrics: {generationMetrics.Count} records");
            sb.AppendLine($"- gamestate_snapshots: {gameStateSnapshots.Count} records");
            sb.AppendLine($"- parameter_correlations: {parameterCorrelations.Count} records");
            sb.AppendLine($"- performance_metrics: {performanceMetrics.Count} records");

            File.WriteAllText(path, sb.ToString());
            Debug.Log(sb.ToString());
        }

        /// <summary>
        ///     Clear all collected metrics.
        /// </summary>
        public void ClearMetrics()
        {
            generationMetrics.Clear();
            gameStateSnapshots.Clear();
            parameterCorrelations.Clear();
            performanceMetrics.Clear();
            totalGenerations = 0;
            successfulGenerations = 0;
            failedGenerations = 0;
            sessionStartTime = Time.time;
            Debug.Log("[MetricsCollector] Metrics cleared");
        }

        /// <summary>
        ///     Get current session statistics.
        /// </summary>
        public string GetSessionStats()
        {
            var successRate = totalGenerations > 0 ? (float)successfulGenerations / totalGenerations * 100f : 0f;
            return $"Generations: {totalGenerations} | Success: {successRate:F1}% | Metrics: {generationMetrics.Count}";
        }

        #region Metric Data Structures

        [Serializable]
        public struct GenerationMetric
        {
            public float timestamp;
            public string layerName;
            public string zoneName;
            public float dangerLevel;
            public float playerHealth;
            public int nearbyEnemies;
            public int seed;
            public int genEvents;
            public int bpm;
            public string instruments;
            public float requestStartTime;
            public float requestEndTime;
            public float latencyMs;
            public bool success;
            public int midiBytes;
            public string errorMessage;
        }

        [Serializable]
        public struct GameStateSnapshot
        {
            public float timestamp;
            public float dangerLevel;
            public float playerHealth;
            public int nearbyEnemies;
            public bool inCombat;
            public Vector3 playerPosition;
            public string currentZone;
            public float ambientVolume;
            public float tensionVolume;
            public float combatVolume;
        }

        [Serializable]
        public struct ParameterCorrelation
        {
            public float timestamp;
            public float dangerLevel;
            public int bpmGenerated;
            public int genEvents;
            public string instrumentsUsed;
            public float healthAtGeneration;
            public int enemyCountAtGeneration;
        }

        [Serializable]
        public struct PerformanceMetric
        {
            public float timestamp;
            public float fps;
            public float frameTimeMs;
            public float updateTimeMs;
            public int activeAudioSources;
            public float memoryUsageMB;
            public int cacheHitRate;
            public int cacheMissRate;
        }

        #endregion
    }
}