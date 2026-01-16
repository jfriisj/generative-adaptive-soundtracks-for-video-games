# MIDI Batch Generation Test Suite

Comprehensive test suite for validating MIDI generation with different parameters, extended timeouts, and detailed logging.

## Overview

The `MidiBatchGenerationTest` class provides three test scenarios:

1. **BatchGenerateDifferentMusicStyles** - Full batch test with 8 different music configurations
2. **QuickSmokeTest** - Fast single MIDI generation to verify basic functionality  
3. **StressTestConcurrentRequests** - Concurrent request handling test

## Test Configurations

The batch test generates these music styles:

| # | Name | Description | Events | BPM | Timeout |
|---|------|-------------|--------|-----|---------|
| 1 | Short Ambient Piano | Slow, minimal piano | 64 | 60 | 60s |
| 2 | Medium Tension Strings | Moderate strings | 128 | 100 | 60s |
| 3 | Combat with Drums | Fast combat music | 256 | 140 | 180s |
| 4 | Orchestral Epic | Cinematic orchestra | 256 | 120 | 180s |
| 5 | Jazz Ensemble | Jazz with swing | 192 | 110 | 180s |
| 6 | Electronic Ambient | Electronic synth pad | 128 | 90 | 60s |
| 7 | Long Complex Multi-Instrument | Extended piece | 512 | 130 | 180s |
| 8 | Minimal Solo Flute | Sparse solo | 64 | 70 | 60s |

## How to Run

### Prerequisites

1. **Start the MIDI generation server** (Named Pipes or WebSocket):
   ```bash
   cd examples
   ./start_pipe_server.sh
   # OR
   python ws_server_standalone.py
   ```

2. **Verify server is running** - Check for:
   ```
   [SERVER] Waiting for Unity connection...
   ```

### Running the Tests

1. **In Unity Editor:**
   - Open `Window > General > Test Runner`
   - Click `PlayMode` tab
   - Expand `AdaptiveMusic.Tests > MidiBatchGenerationTest`
   - Select the test you want to run:
     - **QuickSmokeTest** - Fast single test (~10-20s)
     - **BatchGenerateDifferentMusicStyles** - Full suite (~5-20 minutes depending on CPU/GPU)
     - **StressTestConcurrentRequests** - Concurrent test (~2-5 minutes)
   - Click `Run Selected`

2. **Via Command Line:**
   ```bash
   # Run all PlayMode tests
   Unity.exe -runTests -testPlatform PlayMode -testResults results.xml
   
   # Run specific test
   Unity.exe -runTests -testPlatform PlayMode -testFilter MidiBatchGenerationTest.QuickSmokeTest
   ```

## Output

### Console Logs

The test provides detailed progress logging:

```
========================================
[MidiBatchTest] Starting Batch MIDI Generation Test
========================================
[MidiBatchTest] Test 1/8: 01_short_ambient_piano
[MidiBatchTest] Description: Short ambient piano piece (64 events, slow tempo)
[MidiBatchTest] Parameters:
  - Seed: 1001
  - Events: 64
  - BPM: 60
  - Time Signature: 4/4
  - Instruments: Acoustic Grand Piano
  - Drum Kit: None
  - Timeout: 60s
[MidiBatchTest] Starting generation at 14:30:25...
[MidiBatchTest] Still generating... 5s elapsed (timeout at 60s)
[MidiBatchTest] ✅ SUCCESS in 8.3s
[MidiBatchTest] Saved to: C:/github/BclPro_AI_Ed/MidiTestOutput/01_short_ambient_piano.mid
[MidiBatchTest] File size: 2.45 KB
```

### Generated MIDI Files

All generated MIDI files are saved to:
```
<ProjectRoot>/MidiTestOutput/
```

Files are named by test configuration:
- `01_short_ambient_piano.mid`
- `02_medium_tension_strings.mid`
- `03_combat_with_drums.mid`
- etc.

You can play these files in any MIDI player (e.g., Windows Media Player, VLC, MuseScore).

### Test Summary

At the end of the batch test, you'll see a summary:

```
========================================
[MidiBatchTest] BATCH TEST COMPLETE
========================================
Total Tests: 8
✅ Successful: 7
❌ Failed: 1
Success Rate: 87.5%

Detailed Results:
  ✅ 01_short_ambient_piano: 8.3s (2.45 KB)
  ✅ 02_medium_tension_strings: 12.1s (4.72 KB)
  ✅ 03_combat_with_drums: 24.5s (9.83 KB)
  ✅ 04_orchestral_epic: 28.7s (11.24 KB)
  ✅ 05_jazz_ensemble: 19.3s (7.65 KB)
  ❌ 06_electronic_ambient: TIMEOUT (60.0s)
  ✅ 07_long_complex_multi_instrument: 45.2s (18.91 KB)
  ✅ 08_minimal_solo_flute: 6.7s (1.89 KB)

Output Directory: C:/github/BclPro_AI_Ed/MidiTestOutput
========================================
```

## Timeout Configuration

Timeouts are configured per test complexity:

- **SHORT_TIMEOUT_MS** = 60,000ms (1 minute) - For simple/short generations
- **EXTENDED_TIMEOUT_MS** = 180,000ms (3 minutes) - For complex/long generations

### Adjusting Timeouts

If you're running on CPU without GPU acceleration, increase timeouts:

```csharp
private const int EXTENDED_TIMEOUT_MS = 300000; // 5 minutes
private const int SHORT_TIMEOUT_MS = 120000; // 2 minutes
```

## Customizing Tests

### Add New Test Configurations

Edit the `testConfigs` list in `BatchGenerateDifferentMusicStyles()`:

```csharp
new TestConfig
{
    Name = "09_my_custom_test",
    Description = "My custom MIDI test",
    TimeoutMs = SHORT_TIMEOUT_MS,
    Params = new MidiParams
    {
        seed = 9009,
        gen_events = 128,
        bpm = 95,
        time_sig = "4/4",
        instruments = new[] { "Cello", "French Horn" },
        drum_kit = "None",
        allow_cc = true
    }
}
```

### Change Client Type

By default, the test uses Named Pipes. To use WebSocket:

```csharp
// In Setup() method
client = new WebSocketMusicClient("ws://127.0.0.1:8765");
```

## Troubleshooting

### Server Not Running
```
[MidiBatchTest] Failed to connect to server. Is the pipe/websocket server running?
```
**Solution:** Start the server before running tests.

### Timeouts
```
[MidiBatchTest] ❌ TIMEOUT after 60.0s
```
**Solutions:**
- Install CUDA 12 + cuDNN 9 for GPU acceleration
- Increase timeout values
- Reduce `gen_events` parameter
- Check server logs for errors

### Connection Errors
```
[NamedPipeClient] Connection failed: Access denied
```
**Solutions:**
- Kill existing Python processes: `taskkill /F /IM python.exe`
- Restart server with clean pipe
- Check Windows permissions for named pipes

### Generation Errors
```
[SERVER] MIDI generation failed: AttributeError
```
**Solutions:**
- Check server console for full stack trace
- Verify ONNX models are properly loaded
- Check tokenizer configuration
- Try with simpler parameters (fewer instruments, fewer events)

## Performance Benchmarks

Expected generation times (varies by hardware):

| Hardware | 64 events | 256 events | 512 events |
|----------|-----------|------------|------------|
| GPU (CUDA 12) | 0.5-2s | 2-5s | 5-10s |
| CPU (fallback) | 3-8s | 10-25s | 30-60s |

Named Pipes IPC overhead: ~0.5-2ms (negligible compared to generation time)

## Success Criteria

The batch test passes if:
- Success rate ≥ 75%
- All files saved correctly
- No server crashes
- MIDI data is valid (>0 bytes)

## Integration with CI/CD

Example GitHub Actions workflow:

```yaml
- name: Run MIDI Generation Tests
  run: |
    # Start server in background
    cd examples
    python pipe_server_standalone.py --pipe-name AdaptiveMusicPipe &
    SERVER_PID=$!
    
    # Run Unity tests
    Unity -runTests -testPlatform PlayMode -testFilter MidiBatchGenerationTest
    
    # Cleanup
    kill $SERVER_PID
```

## Related Files

- `NamedPipeMusicClient.cs` - Named Pipes client implementation
- `WebSocketMusicClient.cs` - WebSocket client implementation  
- `MidiParams.cs` - MIDI generation parameters
- `examples/pipe_server_standalone.py` - Python server (Named Pipes)
- `examples/ws_server_standalone.py` - Python server (WebSocket)

## Support

For issues or questions:
1. Check server logs in `examples/pipe_server.log`
2. Enable Unity Debug logs in Test Runner
3. Review `NAMED_PIPES_READY.md` for setup instructions
4. Check `INTEGRATION_GUIDE.md` for architecture details
