using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AdaptiveMusic.Tests
{
    /// <summary>
    /// Batch test for MIDI generation with different parameters, extended timeouts, and detailed logging.
    /// Tests various music styles, lengths, and instruments to validate the Named Pipes/WebSocket server.
    /// </summary>
    public class MidiBatchGenerationTest
    {
        private IMusicClient client;
        private string outputDirectory;
        private const int EXTENDED_TIMEOUT_MS = 180000; // 3 minutes for complex generations
        private const int SHORT_TIMEOUT_MS = 60000; // 1 minute for simple generations
        
        /// <summary>
        /// Test configuration for a single MIDI generation request.
        /// </summary>
        private class TestConfig
        {
            public string Name;
            public MidiParams Params;
            public int TimeoutMs;
            public string Description;
        }

        [SetUp]
        public void Setup()
        {
            // These are integration tests that require an external generation server.
            // Skip by default to avoid failing test runs when the server isn't running.
            if (!string.Equals(Environment.GetEnvironmentVariable("ADAPTIVE_MUSIC_RUN_INTEGRATION_TESTS"), "1",
                    StringComparison.Ordinal))
            {
                Assert.Ignore(
                    "MidiBatchGenerationTest requires a running MIDI generation server. Set ADAPTIVE_MUSIC_RUN_INTEGRATION_TESTS=1 to enable.");
            }

            // Create output directory for generated MIDI files
            outputDirectory = Path.Combine(Application.dataPath, "..", "MidiTestOutput");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            Debug.Log($"[MidiBatchTest] Output directory: {outputDirectory}");
            
            // Use Named Pipe client (change to WebSocketMusicClient if needed)
            client = new NamedPipeMusicClient("AdaptiveMusicPipe");
            Debug.Log("[MidiBatchTest] Client initialized");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[MidiBatchTest] Test completed");
        }

        /// <summary>
        /// Batch test: Generate multiple MIDI files with different configurations.
        /// Tests various music styles, lengths, tempos, and instrument combinations.
        /// </summary>
        [UnityTest]
        public IEnumerator BatchGenerateDifferentMusicStyles()
        {
            Debug.Log("========================================");
            Debug.Log("[MidiBatchTest] Starting Batch MIDI Generation Test");
            Debug.Log("========================================");

            // Define test configurations
            var testConfigs = new List<TestConfig>
            {
                // Short ambient piece
                new TestConfig
                {
                    Name = "01_short_ambient_piano",
                    Description = "Short ambient piano piece (64 events, slow tempo)",
                    TimeoutMs = SHORT_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 1001,
                        gen_events = 64,
                        bpm = 60,
                        time_sig = "4/4",
                        instruments = new[] { "Acoustic Grand Piano" },
                        drum_kit = "None",
                        allow_cc = true
                    }
                },
                
                // Medium tension strings
                new TestConfig
                {
                    Name = "02_medium_tension_strings",
                    Description = "Medium tension strings (128 events, moderate tempo)",
                    TimeoutMs = SHORT_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 2002,
                        gen_events = 128,
                        bpm = 100,
                        time_sig = "4/4",
                        instruments = new[] { "String Ensemble 1", "Violin" },
                        drum_kit = "None",
                        allow_cc = true
                    }
                },
                
                // Combat with drums
                new TestConfig
                {
                    Name = "03_combat_with_drums",
                    Description = "High-energy combat with drums (256 events, fast tempo)",
                    TimeoutMs = EXTENDED_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 3003,
                        gen_events = 256,
                        bpm = 140,
                        time_sig = "4/4",
                        instruments = new[] { "Distortion Guitar", "Electric Bass (finger)" },
                        drum_kit = "Standard",
                        allow_cc = true
                    }
                },
                
                // Orchestral epic
                new TestConfig
                {
                    Name = "04_orchestral_epic",
                    Description = "Epic orchestral piece (256 events, cinematic)",
                    TimeoutMs = EXTENDED_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 4004,
                        gen_events = 256,
                        bpm = 120,
                        time_sig = "4/4",
                        instruments = new[] { "String Ensemble 1", "French Horn", "Trumpet", "Timpani" },
                        drum_kit = "Orchestra",
                        allow_cc = true
                    }
                },
                
                // Jazz ensemble
                new TestConfig
                {
                    Name = "05_jazz_ensemble",
                    Description = "Jazz ensemble with swing (192 events, medium tempo)",
                    TimeoutMs = EXTENDED_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 5005,
                        gen_events = 192,
                        bpm = 110,
                        time_sig = "4/4",
                        instruments = new[] { "Acoustic Grand Piano", "Tenor Sax", "Acoustic Bass" },
                        drum_kit = "Jazz",
                        allow_cc = true
                    }
                },
                
                // Electronic ambient
                new TestConfig
                {
                    Name = "06_electronic_ambient",
                    Description = "Electronic ambient (128 events, slow build)",
                    TimeoutMs = SHORT_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 6006,
                        gen_events = 128,
                        bpm = 90,
                        time_sig = "4/4",
                        instruments = new[] { "Synth Pad", "Synth Bass 1" },
                        drum_kit = "Electronic",
                        allow_cc = true
                    }
                },
                
                // Long complex piece
                new TestConfig
                {
                    Name = "07_long_complex_multi_instrument",
                    Description = "Long complex multi-instrument piece (512 events, EXTENDED)",
                    TimeoutMs = EXTENDED_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 7007,
                        gen_events = 512,
                        bpm = 130,
                        time_sig = "4/4",
                        instruments = new[] { "Acoustic Grand Piano", "String Ensemble 1", "Flute", "Electric Guitar (clean)" },
                        drum_kit = "Standard",
                        allow_cc = true
                    }
                },
                
                // Minimal solo instrument
                new TestConfig
                {
                    Name = "08_minimal_solo_flute",
                    Description = "Minimal solo flute (64 events, sparse)",
                    TimeoutMs = SHORT_TIMEOUT_MS,
                    Params = new MidiParams
                    {
                        seed = 8008,
                        gen_events = 64,
                        bpm = 70,
                        time_sig = "3/4",
                        instruments = new[] { "Flute" },
                        drum_kit = "None",
                        allow_cc = false
                    }
                }
            };

            // Connect to server
            Debug.Log("[MidiBatchTest] Connecting to server...");
            Task<bool> connectTask = client.Connect();
            yield return new WaitUntil(() => connectTask.IsCompleted);
            
            if (!connectTask.Result)
            {
                Assert.Fail("[MidiBatchTest] Failed to connect to server. Is the pipe/websocket server running?");
                yield break;
            }
            
            Debug.Log("[MidiBatchTest] Connected successfully!");
            Debug.Log($"[MidiBatchTest] Starting generation of {testConfigs.Count} MIDI files...\n");

            // Generate each MIDI file
            int successCount = 0;
            int failCount = 0;
            var results = new List<string>();

            for (int i = 0; i < testConfigs.Count; i++)
            {
                var config = testConfigs[i];
                
                Debug.Log("----------------------------------------");
                Debug.Log($"[MidiBatchTest] Test {i + 1}/{testConfigs.Count}: {config.Name}");
                Debug.Log($"[MidiBatchTest] Description: {config.Description}");
                Debug.Log($"[MidiBatchTest] Parameters:");
                Debug.Log($"  - Seed: {config.Params.seed}");
                Debug.Log($"  - Events: {config.Params.gen_events}");
                Debug.Log($"  - BPM: {config.Params.bpm}");
                Debug.Log($"  - Time Signature: {config.Params.time_sig}");
                Debug.Log($"  - Instruments: {string.Join(", ", config.Params.instruments)}");
                Debug.Log($"  - Drum Kit: {config.Params.drum_kit}");
                Debug.Log($"  - Timeout: {config.TimeoutMs / 1000}s");
                
                DateTime startTime = DateTime.Now;
                Debug.Log($"[MidiBatchTest] Starting generation at {startTime:HH:mm:ss}...");

                // Request MIDI generation
                Task<byte[]> generateTask = client.RequestMIDI(config.Params);
                
                // Wait with timeout and progress logging
                float elapsedSeconds = 0f;
                int lastLoggedSecond = 0;
                
                while (!generateTask.IsCompleted && elapsedSeconds * 1000 < config.TimeoutMs)
                {
                    yield return new WaitForSeconds(1f);
                    elapsedSeconds++;
                    
                    // Log progress every 5 seconds
                    if ((int)elapsedSeconds % 5 == 0 && (int)elapsedSeconds != lastLoggedSecond)
                    {
                        lastLoggedSecond = (int)elapsedSeconds;
                        Debug.Log($"[MidiBatchTest] Still generating... {elapsedSeconds}s elapsed (timeout at {config.TimeoutMs / 1000}s)");
                    }
                }

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                // Check result
                if (!generateTask.IsCompleted)
                {
                    Debug.LogError($"[MidiBatchTest] ❌ TIMEOUT after {duration.TotalSeconds:F1}s");
                    results.Add($"❌ {config.Name}: TIMEOUT ({duration.TotalSeconds:F1}s)");
                    failCount++;
                    continue;
                }

                if (generateTask.IsFaulted)
                {
                    Debug.LogError($"[MidiBatchTest] ❌ FAILED: {generateTask.Exception?.GetBaseException().Message}");
                    results.Add($"❌ {config.Name}: ERROR - {generateTask.Exception?.GetBaseException().Message}");
                    failCount++;
                    continue;
                }

                byte[] midiData = generateTask.Result;
                
                if (midiData == null || midiData.Length == 0)
                {
                    Debug.LogError($"[MidiBatchTest] ❌ FAILED: No MIDI data received");
                    results.Add($"❌ {config.Name}: No data received");
                    failCount++;
                    continue;
                }

                // Save MIDI file
                string outputPath = Path.Combine(outputDirectory, $"{config.Name}.mid");
                try
                {
                    File.WriteAllBytes(outputPath, midiData);
                    Debug.Log($"[MidiBatchTest] ✅ SUCCESS in {duration.TotalSeconds:F1}s");
                    Debug.Log($"[MidiBatchTest] Saved to: {outputPath}");
                    Debug.Log($"[MidiBatchTest] File size: {midiData.Length / 1024.0:F2} KB");
                    
                    results.Add($"✅ {config.Name}: {duration.TotalSeconds:F1}s ({midiData.Length / 1024.0:F2} KB)");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MidiBatchTest] ❌ Failed to save file: {ex.Message}");
                    results.Add($"❌ {config.Name}: Save failed - {ex.Message}");
                    failCount++;
                }
                
                // Brief pause between requests to avoid overwhelming the server
                yield return new WaitForSeconds(0.5f);
            }

            // Print summary
            Debug.Log("\n========================================");
            Debug.Log("[MidiBatchTest] BATCH TEST COMPLETE");
            Debug.Log("========================================");
            Debug.Log($"Total Tests: {testConfigs.Count}");
            Debug.Log($"✅ Successful: {successCount}");
            Debug.Log($"❌ Failed: {failCount}");
            Debug.Log($"Success Rate: {(successCount * 100.0 / testConfigs.Count):F1}%");
            Debug.Log("\nDetailed Results:");
            
            foreach (var result in results)
            {
                Debug.Log($"  {result}");
            }
            
            Debug.Log($"\nOutput Directory: {outputDirectory}");
            Debug.Log("========================================\n");

            // Assert that at least 75% of tests passed (allowing for some server issues)
            float successRate = successCount * 100.0f / testConfigs.Count;
            Assert.GreaterOrEqual(successRate, 75f, 
                $"Batch test success rate too low: {successRate:F1}% (expected >= 75%)");
        }

        /// <summary>
        /// Quick smoke test: Generate a single short MIDI file to verify basic functionality.
        /// </summary>
        [UnityTest]
        public IEnumerator QuickSmokeTest()
        {
            Debug.Log("[MidiBatchTest] Running Quick Smoke Test...");

            var params_ = new MidiParams
            {
                seed = 9999,
                gen_events = 64,
                bpm = 80,
                time_sig = "4/4",
                instruments = new[] { "Acoustic Grand Piano" },
                drum_kit = "None",
                allow_cc = true
            };

            // Connect
            Task<bool> connectTask = client.Connect();
            yield return new WaitUntil(() => connectTask.IsCompleted);
            Assert.IsTrue(connectTask.Result, "Failed to connect to server");

            // Generate
            Debug.Log("[MidiBatchTest] Requesting simple MIDI...");
            DateTime startTime = DateTime.Now;
            Task<byte[]> generateTask = client.RequestMIDI(params_);
            
            // Wait with timeout
            float elapsed = 0f;
            while (!generateTask.IsCompleted && elapsed < SHORT_TIMEOUT_MS / 1000f)
            {
                yield return new WaitForSeconds(1f);
                elapsed++;
            }

            TimeSpan duration = DateTime.Now - startTime;
            
            Assert.IsTrue(generateTask.IsCompleted, "Request timed out");
            Assert.IsNotNull(generateTask.Result, "No MIDI data received");
            Assert.Greater(generateTask.Result.Length, 0, "Empty MIDI data");

            // Save
            string outputPath = Path.Combine(outputDirectory, "smoke_test.mid");
            File.WriteAllBytes(outputPath, generateTask.Result);
            
            Debug.Log($"[MidiBatchTest] ✅ Smoke test passed in {duration.TotalSeconds:F1}s");
            Debug.Log($"[MidiBatchTest] Output: {outputPath} ({generateTask.Result.Length} bytes)");
        }

        /// <summary>
        /// Stress test: Generate multiple MIDI files in parallel (if client supports it).
        /// Tests server's ability to handle concurrent requests.
        /// </summary>
        [UnityTest]
        public IEnumerator StressTestConcurrentRequests()
        {
            Debug.Log("[MidiBatchTest] Starting Concurrent Requests Stress Test...");
            Debug.Log("[MidiBatchTest] Note: Named Pipe client serializes requests internally");

            // Connect
            Task<bool> connectTask = client.Connect();
            yield return new WaitUntil(() => connectTask.IsCompleted);
            Assert.IsTrue(connectTask.Result, "Failed to connect to server");

            // Launch 3 concurrent requests with different parameters
            var tasks = new List<Task<byte[]>>
            {
                client.RequestMIDI(new MidiParams
                {
                    seed = 10001,
                    gen_events = 64,
                    bpm = 80,
                    time_sig = "4/4",
                    instruments = new[] { "Acoustic Grand Piano" },
                    drum_kit = "None",
                    allow_cc = true
                }),
                client.RequestMIDI(new MidiParams
                {
                    seed = 10002,
                    gen_events = 96,
                    bpm = 100,
                    time_sig = "4/4",
                    instruments = new[] { "String Ensemble 1" },
                    drum_kit = "None",
                    allow_cc = true
                }),
                client.RequestMIDI(new MidiParams
                {
                    seed = 10003,
                    gen_events = 128,
                    bpm = 120,
                    time_sig = "4/4",
                    instruments = new[] { "Electric Guitar (clean)" },
                    drum_kit = "Standard",
                    allow_cc = true
                })
            };

            Debug.Log($"[MidiBatchTest] Launched {tasks.Count} concurrent requests...");
            DateTime startTime = DateTime.Now;

            // Wait for all to complete
            float elapsed = 0f;
            while (tasks.Exists(t => !t.IsCompleted) && elapsed < EXTENDED_TIMEOUT_MS / 1000f)
            {
                yield return new WaitForSeconds(1f);
                elapsed++;
                
                if ((int)elapsed % 10 == 0)
                {
                    int completed = tasks.FindAll(t => t.IsCompleted).Count;
                    Debug.Log($"[MidiBatchTest] Progress: {completed}/{tasks.Count} completed ({elapsed}s elapsed)");
                }
            }

            TimeSpan totalDuration = DateTime.Now - startTime;
            int successCount = 0;

            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].IsCompleted && !tasks[i].IsFaulted && tasks[i].Result != null && tasks[i].Result.Length > 0)
                {
                    string outputPath = Path.Combine(outputDirectory, $"concurrent_test_{i + 1}.mid");
                    File.WriteAllBytes(outputPath, tasks[i].Result);
                    Debug.Log($"[MidiBatchTest] Request {i + 1}: ✅ Success ({tasks[i].Result.Length} bytes)");
                    successCount++;
                }
                else
                {
                    Debug.LogError($"[MidiBatchTest] Request {i + 1}: ❌ Failed");
                }
            }

            Debug.Log($"[MidiBatchTest] Stress test complete: {successCount}/{tasks.Count} succeeded in {totalDuration.TotalSeconds:F1}s");
            Assert.GreaterOrEqual(successCount, tasks.Count, "All concurrent requests should succeed");
        }
    }
}
