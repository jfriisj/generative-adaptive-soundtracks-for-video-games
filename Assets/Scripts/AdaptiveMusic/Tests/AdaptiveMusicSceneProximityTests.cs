using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AdaptiveMusic.Tests
{
    public class AdaptiveMusicSceneProximityTests
    {
        [UnityTest]
        public IEnumerator AdaptiveMusicScene_EnemyProximityRaisesDanger_AndDrivesMix()
        {
            // Clean any stale singleton from previous tests
            if (GameStateTracker.Instance != null)
            {
                Object.Destroy(GameStateTracker.Instance.gameObject);
                yield return null;
            }

            // Load the demo scene under test.
            // Use EditorSceneManager so this test doesn't depend on Build Profiles / Build Settings.
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Scenes/AdaptiveMusic.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("AdaptiveMusicSceneProximityTests requires UNITY_EDITOR to load scenes by path in PlayMode tests.");
            yield break;
#endif

            // Ensure a Player exists (GameStateTracker relies on the tag lookup)
            GameObject player;
            try
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            catch
            {
                player = null;
            }

            var createdPlayer = false;
            if (player == null)
            {
                player = new GameObject("TestPlayer");
                player.tag = "Player";
                createdPlayer = true;
            }

            // Place player far away for a clean baseline (avoid existing scene enemies affecting baseline).
            player.transform.position = new Vector3(1000f, 0f, 0f);

            // Ensure a fresh GameStateTracker that will find Player in Start()
            if (GameStateTracker.Instance != null)
            {
                Object.Destroy(GameStateTracker.Instance.gameObject);
                yield return null;
            }

            var trackerGo = new GameObject("GameStateTracker_Test");
            var tracker = trackerGo.AddComponent<GameStateTracker>();

            // Make calculations deterministic and fast
            var smoothField = typeof(GameStateTracker).GetField("dangerSmoothFactor",
                BindingFlags.NonPublic | BindingFlags.Instance);
            smoothField?.SetValue(tracker, 1f);

            // Baseline: full health, not in combat
            tracker.SetHealth(1f);
            tracker.SetCombatActive(false);

            // Let Start() and at least one Update() run
            yield return null;
            yield return null;

            // Wait a few frames for danger to settle (scene scripts may still be initializing).
            yield return WaitForDangerAtMost(tracker, 0.05f, 30);

            var baselineDanger = tracker.GetDangerLevel();
            Assert.That(baselineDanger, Is.LessThanOrEqualTo(0.05f), tracker.GetStateInfo());

            // Move player back near enemies for the proximity portion.
            player.transform.position = Vector3.zero;

            // Spawn 3 enemies within critical radius to push proximityDanger to ~1.0
            var enemies = new GameObject[3];
            for (var i = 0; i < enemies.Length; i++)
            {
                enemies[i] = new GameObject($"TestEnemy_{i}");
                enemies[i].tag = "Enemy";
                enemies[i].transform.position = new Vector3(1f + i * 0.25f, 0f, 0f);
            }

            // Let tracker Update() pick up tagged enemies
            yield return null;
            yield return null;

            // Wait until proximity danger is clearly above the tension threshold.
            yield return WaitForDangerAtLeast(tracker, 0.35f, 30);

            var proximityDanger = tracker.GetDangerLevel();
            Assert.That(proximityDanger, Is.GreaterThan(0.3f));

            // Now verify the AdaptiveMusic mix logic reacts to the computed danger
            var mixRoot = new GameObject("AdaptiveMusicSystem_MixHarness");
            var system = mixRoot.AddComponent<AdaptiveMusicSystem>();
            system.enabled = false; // do not run Initialize() / connect to server

            var sources = new AudioSource[3];
            for (var i = 0; i < sources.Length; i++)
            {
                var child = new GameObject($"Layer_{i}");
                child.transform.SetParent(mixRoot.transform);
                sources[i] = child.AddComponent<AudioSource>();
            }

            var layerMixer = new LayerMixer(null, sources);
            SetPrivateField(system, "layerSources", sources);
            SetPrivateField(system, "layerMixer", layerMixer);
            SetPrivateField(system, "isInitialized", true);
            // Avoid borderline float comparisons around 0.70 by nudging the threshold slightly lower for this harness.
            SetPrivateField(system, "combatThreshold", 0.65f);

            SetPrivateField(system, "dangerLevel", proximityDanger);
            Assert.That(GetPrivateFloat(system, "dangerLevel"), Is.EqualTo(proximityDanger).Within(0.0001f));
            InvokePrivateMethod(system, "UpdateLayerMix");
            layerMixer.Update(10f);

            Assert.That(layerMixer.GetLayerVolume("Tension"), Is.GreaterThan(0f));
            Assert.That(layerMixer.GetLayerVolume("Combat"), Is.EqualTo(0f).Within(0.05f));

            // Turning on combat should push danger high enough to trigger combat layer
            tracker.SetCombatActive(true);
            tracker.SetHealth(0f); // ensure danger is well above combat threshold
            yield return null;
            yield return null;

            // Wait for danger to settle well into the combat range.
            // With health=0, combatActive=true, and 3 close enemies, danger should clamp near 1.0.
            yield return WaitForDangerAtLeast(tracker, 0.95f, 30);

            var combatDanger = tracker.GetDangerLevel();
            Assert.That(combatDanger, Is.GreaterThanOrEqualTo(0.95f), tracker.GetStateInfo());

            SetPrivateField(system, "dangerLevel", combatDanger);
            Assert.That(combatDanger, Is.GreaterThan(GetPrivateFloat(system, "combatThreshold") + 0.05f));
            InvokePrivateMethod(system, "UpdateLayerMix");
            // Run a few update steps to let the mixer converge.
            for (var i = 0; i < 5; i++)
                layerMixer.Update(0.5f);

            Assert.That(layerMixer.GetLayerVolume("Combat"), Is.GreaterThan(0.8f));

            // Cleanup
            Object.Destroy(mixRoot);
            Object.Destroy(trackerGo);
            for (var i = 0; i < enemies.Length; i++)
                Object.Destroy(enemies[i]);
            if (createdPlayer)
                Object.Destroy(player);

            yield return null;
        }

        private static IEnumerator WaitForDangerAtLeast(GameStateTracker tracker, float minimum, int maxFrames)
        {
            for (var i = 0; i < maxFrames; i++)
            {
                if (tracker.GetDangerLevel() >= minimum)
                    yield break;
                yield return null;
            }

            Assert.Fail($"Danger did not reach >= {minimum:F2} within {maxFrames} frames. {tracker.GetStateInfo()}");
        }

        private static IEnumerator WaitForDangerAtMost(GameStateTracker tracker, float maximum, int maxFrames)
        {
            for (var i = 0; i < maxFrames; i++)
            {
                if (tracker.GetDangerLevel() <= maximum)
                    yield break;
                yield return null;
            }

            Assert.Fail($"Danger did not drop to <= {maximum:F2} within {maxFrames} frames. {tracker.GetStateInfo()}");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static float GetPrivateFloat(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}");
            return (float)field.GetValue(target);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected private method '{methodName}' on {target.GetType().Name}");
            method.Invoke(target, null);
        }
    }
}
