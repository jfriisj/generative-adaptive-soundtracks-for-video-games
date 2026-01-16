using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AdaptiveMusic.Tests
{
    /// <summary>
    ///     Unit tests for the Adaptive Music System components.
    /// </summary>
    public class AdaptiveMusicTests
    {
        [Test]
        public void TestCacheKeyGeneration()
        {
            var key = MidiCacheManager.GetCacheKey("forest", "ambient", 1001);
            Assert.AreEqual("forest_ambient_1001", key);
        }

        [Test]
        public void TestCacheGetReturnsNullForMissingKey()
        {
            var cache = new MidiCacheManager();
            var result = cache.Get("nonexistent_key");
            Assert.IsNull(result);
        }

        [Test]
        public void TestCacheSetAndGet()
        {
            var cache = new MidiCacheManager();
            var testData = new byte[] { 1, 2, 3, 4, 5 };

            cache.Set("test_key", testData);
            var retrieved = cache.Get("test_key");

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(testData.Length, retrieved.Length);
            Assert.AreEqual(testData[0], retrieved[0]);
        }

        [Test]
        public void TestLayerConfigToParams()
        {
            var layer = new LayerConfig
            {
                name = "ambient",
                seed = 1001,
                gen_events = 256,
                bpm = 80,
                time_sig = "4/4",
                instruments = new[] { "Acoustic Grand" },
                drum_kit = "None",
                allow_cc = true
            };

            var params_ = layer.ToParams();

            Assert.AreEqual(1001, params_.seed);
            Assert.AreEqual(256, params_.gen_events);
            Assert.AreEqual(80, params_.bpm);
            Assert.AreEqual("4/4", params_.time_sig);
            Assert.AreEqual(1, params_.instruments.Length);
            Assert.AreEqual("Acoustic Grand", params_.instruments[0]);
        }

        [Test]
        public void TestMusicConfigGetLayer()
        {
            var config = ScriptableObject.CreateInstance<MusicConfigSO>();
            config.zoneName = "test";
            config.layers = new[]
            {
                new LayerConfig { name = "ambient" },
                new LayerConfig { name = "tension" },
                new LayerConfig { name = "combat" }
            };

            var ambient = config.GetLayer("ambient");
            Assert.IsNotNull(ambient);
            Assert.AreEqual("ambient", ambient.name);

            var tension = config.GetLayer("TENSION"); // Case insensitive
            Assert.IsNotNull(tension);
            Assert.AreEqual("tension", tension.name);

            var nonexistent = config.GetLayer("nonexistent");
            Assert.IsNotNull(nonexistent); // Should return first layer as fallback
            Assert.AreEqual("ambient", nonexistent.name);
        }

        [Test]
        public void TestMidiValidation()
        {
            var renderer = new MidiRenderer();

            // Invalid MIDI (too short)
            var invalidMidi = new byte[] { 1, 2, 3 };
            Assert.IsFalse(renderer.ValidateMidi(invalidMidi));

            // Null MIDI
            Assert.IsFalse(renderer.ValidateMidi(null));
        }
    }

    /// <summary>
    ///     Play mode tests for runtime behavior.
    /// </summary>
    public class AdaptiveMusicPlayModeTests
    {
        [UnityTest]
        public IEnumerator TestGameStateTrackerCreation()
        {
            if (GameStateTracker.Instance != null)
            {
                Object.Destroy(GameStateTracker.Instance.gameObject);
                yield return null;
            }

            var go = new GameObject("GameStateTracker");
            var tracker = go.AddComponent<GameStateTracker>();

            yield return null; // Wait one frame for Awake/Start

            Assert.IsNotNull(GameStateTracker.Instance);
            Assert.AreEqual(tracker, GameStateTracker.Instance);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator TestDangerLevelCalculation()
        {
            if (GameStateTracker.Instance != null)
            {
                Object.Destroy(GameStateTracker.Instance.gameObject);
                yield return null;
            }

            var go = new GameObject("GameStateTracker");
            var tracker = go.AddComponent<GameStateTracker>();

            yield return null;

            // Make smoothing deterministic for tests
            var smoothField = typeof(GameStateTracker).GetField("dangerSmoothFactor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            smoothField?.SetValue(tracker, 1f);

            // Test health contribution
            tracker.SetHealth(0.5f);
            yield return null; // allow Update() to recalc danger
            var danger = tracker.GetDangerLevel();

            // Should be around 0.25 (50% health loss * 0.5 weight)
            Assert.That(danger, Is.EqualTo(0.25f).Within(0.05f));

            // Test combat contribution
            tracker.SetCombatActive(true);
            yield return null;

            var dangerWithCombat = tracker.GetDangerLevel();
            Assert.Greater(dangerWithCombat, danger);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator TestLayerMixerVolumeControl()
        {
            // Create test setup
            var go = new GameObject("TestMixer");
            var sources = new AudioSource[3];

            for (var i = 0; i < 3; i++)
            {
                var child = new GameObject($"Layer{i}");
                child.transform.SetParent(go.transform);
                sources[i] = child.AddComponent<AudioSource>();
            }

            var mixer = new LayerMixer(null, sources);

            yield return null;

            // Test volume setting
            mixer.SetLayerVolume("Ambient", 0.5f);
            mixer.Update(1f);

            var ambientVolume = mixer.GetLayerVolume("Ambient");
            Assert.Greater(ambientVolume, 0f);

            // Test activation
            Assert.IsTrue(mixer.IsLayerActive("Ambient"));

            Object.Destroy(go);
        }
    }
}