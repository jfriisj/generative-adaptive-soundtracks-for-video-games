using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AdaptiveMusic.Tests
{
    public class AdaptiveMusicLayerMixTests
    {
        [UnityTest]
        public IEnumerator DangerLevelUpdatesLayerVolumes()
        {
            var root = new GameObject("AdaptiveMusicSystem_Test");
            var system = root.AddComponent<AdaptiveMusicSystem>();

            // Prevent Start()/Initialize() from running (no server calls in tests)
            system.enabled = false;

            var sources = new AudioSource[3];
            for (var i = 0; i < sources.Length; i++)
            {
                var child = new GameObject($"Layer_{i}");
                child.transform.SetParent(root.transform);
                sources[i] = child.AddComponent<AudioSource>();
            }

            var layerMixer = new LayerMixer(null, sources);

            SetPrivateField(system, "layerSources", sources);
            SetPrivateField(system, "layerMixer", layerMixer);
            SetPrivateField(system, "isInitialized", true);

            // Low danger => Ambient ~1, others ~0
            system.SetDangerLevel(0f);
            InvokePrivateMethod(system, "UpdateLayerMix");
            layerMixer.Update(10f);

            Assert.That(layerMixer.GetLayerVolume("Ambient"), Is.EqualTo(1f).Within(0.05f));
            Assert.That(layerMixer.GetLayerVolume("Tension"), Is.EqualTo(0f).Within(0.05f));
            Assert.That(layerMixer.GetLayerVolume("Combat"), Is.EqualTo(0f).Within(0.05f));

            // Mid danger (just above tension threshold) => Tension > 0
            system.SetDangerLevel(0.5f);
            InvokePrivateMethod(system, "UpdateLayerMix");
            layerMixer.Update(10f);

            Assert.That(layerMixer.GetLayerVolume("Ambient"), Is.LessThan(1f));
            Assert.That(layerMixer.GetLayerVolume("Tension"), Is.GreaterThan(0f));
            Assert.That(layerMixer.GetLayerVolume("Combat"), Is.EqualTo(0f).Within(0.05f));

            // High danger => Combat > 0 and Ambient ~0
            system.SetDangerLevel(1f);
            InvokePrivateMethod(system, "UpdateLayerMix");
            layerMixer.Update(10f);

            Assert.That(layerMixer.GetLayerVolume("Ambient"), Is.EqualTo(0f).Within(0.05f));
            Assert.That(layerMixer.GetLayerVolume("Tension"), Is.GreaterThan(0.1f));
            Assert.That(layerMixer.GetLayerVolume("Combat"), Is.GreaterThan(0.5f));

            Object.Destroy(root);
            yield return null;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected private method '{methodName}' on {target.GetType().Name}");
            method.Invoke(target, null);
        }
    }
}
