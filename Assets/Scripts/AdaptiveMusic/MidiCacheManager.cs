using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Manages caching of MIDI data with LRU eviction policy.
    ///     Persists cache to disk for reuse across sessions.
    /// </summary>
    public class MidiCacheManager
    {
        private const string CACHE_FILE_NAME = "midi_cache.json";
        private const long MAX_CACHE_SIZE_BYTES = 50 * 1024 * 1024; // 50MB default

        private readonly Dictionary<string, CacheEntry> cache = new();
        private readonly string cachePath;
        private long currentCacheSize;
        private readonly LinkedList<string> lruList = new();

        public MidiCacheManager()
        {
            cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
            LoadCache();
        }

        /// <summary>
        ///     Generate cache key from zone, layer, seed, and optional dynamic parameters.
        /// </summary>
        public static string GetCacheKey(string zone, string layer, int seed, string musicType = null, float? intensity = null)
        {
            var key = $"{zone}_{layer}_{seed}";
            
            // Include music type if specified (for death, victory, etc.)
            if (!string.IsNullOrEmpty(musicType))
            {
                key += $"_{musicType}";
            }
            
            // Include intensity bucket if specified (rounded to avoid too many cache entries)
            if (intensity.HasValue)
            {
                // Bucket intensity into 5 levels: 0.0-0.2, 0.2-0.4, 0.4-0.6, 0.6-0.8, 0.8-1.0
                int intensityBucket = Mathf.FloorToInt(intensity.Value * 5);
                key += $"_i{intensityBucket}";
            }
            
            return key;
        }

        /// <summary>
        ///     Retrieve MIDI data from cache. Returns null if not found.
        /// </summary>
        public byte[] Get(string key)
        {
            if (cache.TryGetValue(key, out var entry))
            {
                // Update LRU: move to front
                lruList.Remove(key);
                lruList.AddFirst(key);

                entry.accessCount++;
                entry.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Debug.Log($"[MidiCache] Cache HIT for key: {key}");
                return entry.midiData;
            }

            Debug.Log($"[MidiCache] Cache MISS for key: {key}");
            return null;
        }

        /// <summary>
        ///     Store MIDI data in cache with LRU eviction if needed.
        /// </summary>
        public void Set(string key, byte[] midiData)
        {
            if (midiData == null || midiData.Length == 0)
            {
                Debug.LogError($"[MidiCache] Cannot cache empty MIDI data for key: {key}");
                return;
            }

            // If key already exists, remove old entry
            if (cache.ContainsKey(key))
            {
                currentCacheSize -= cache[key].midiData.Length;
                lruList.Remove(key);
            }

            // Evict old entries if needed
            while (currentCacheSize + midiData.Length > MAX_CACHE_SIZE_BYTES && lruList.Count > 0)
            {
                var oldestKey = lruList.Last.Value;
                Evict(oldestKey);
            }

            // Add new entry
            var entry = new CacheEntry
            {
                key = key,
                midiData = midiData,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                accessCount = 1
            };

            cache[key] = entry;
            lruList.AddFirst(key);
            currentCacheSize += midiData.Length;

            Debug.Log(
                $"[MidiCache] Cached {midiData.Length} bytes for key: {key} (Total: {currentCacheSize / 1024}KB)");
        }

        /// <summary>
        ///     Remove a specific entry from cache.
        /// </summary>
        private void Evict(string key)
        {
            if (cache.TryGetValue(key, out var entry))
            {
                currentCacheSize -= entry.midiData.Length;
                cache.Remove(key);
                lruList.Remove(key);
                Debug.Log($"[MidiCache] Evicted key: {key}");
            }
        }

        /// <summary>
        ///     Clear all cached MIDI data.
        /// </summary>
        public void Clear()
        {
            cache.Clear();
            lruList.Clear();
            currentCacheSize = 0;
            SaveCache();
            Debug.Log("[MidiCache] Cache cleared");
        }

        /// <summary>
        ///     Delete the cache file from disk entirely.
        /// </summary>
        public void DeleteCache()
        {
            try
            {
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                    Debug.Log($"[MidiCache] Cache file deleted: {cachePath}");
                }
                else
                {
                    Debug.Log("[MidiCache] No cache file found to delete");
                }
                
                // Clear runtime cache too
                cache.Clear();
                lruList.Clear();
                currentCacheSize = 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MidiCache] Failed to delete cache file: {ex.Message}");
            }
        }

        /// <summary>
        ///     Save cache to disk (JSON format).
        /// </summary>
        public void SaveCache()
        {
            try
            {
                var cacheData = new CacheData();
                foreach (var entry in cache.Values) cacheData.entries.Add(entry);

                var json = JsonUtility.ToJson(cacheData, false);
                File.WriteAllText(cachePath, json);
                Debug.Log($"[MidiCache] Saved {cache.Count} entries to disk");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MidiCache] Failed to save cache: {ex.Message}");
            }
        }

        /// <summary>
        ///     Load cache from disk.
        /// </summary>
        private void LoadCache()
        {
            if (!File.Exists(cachePath))
            {
                Debug.Log("[MidiCache] No existing cache found");
                return;
            }

            try
            {
                var json = File.ReadAllText(cachePath);
                var cacheData = JsonUtility.FromJson<CacheData>(json);

                foreach (var entry in cacheData.entries)
                {
                    cache[entry.key] = entry;
                    lruList.AddLast(entry.key);
                    currentCacheSize += entry.midiData.Length;
                }

                Debug.Log($"[MidiCache] Loaded {cache.Count} entries from disk ({currentCacheSize / 1024}KB)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MidiCache] Failed to load cache: {ex.Message}");
            }
        }

        /// <summary>
        ///     Get cache statistics.
        /// </summary>
        public string GetStats()
        {
            return
                $"Cache: {cache.Count} entries, {currentCacheSize / 1024}KB / {MAX_CACHE_SIZE_BYTES / 1024 / 1024}MB";
        }

        /// <summary>
        ///     Cache entry containing MIDI data and metadata.
        /// </summary>
        [Serializable]
        private class CacheEntry
        {
            public string key;
            public byte[] midiData;
            public long timestamp;
            public int accessCount;
        }

        /// <summary>
        ///     Serializable wrapper for the cache dictionary.
        /// </summary>
        [Serializable]
        private class CacheData
        {
            public List<CacheEntry> entries = new();
        }
    }
}