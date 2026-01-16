using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    /// Named Pipe client for requesting MIDI from the adaptive music server.
    /// High-performance IPC alternative to WebSockets with ~10x lower latency.
    /// Supports full GPU acceleration on Python server side.
    /// </summary>
    public class NamedPipeMusicClient : IMusicClient
    {
        private readonly string pipeName;
        private bool isConnected = false;
        private readonly SemaphoreSlim requestSemaphore = new SemaphoreSlim(1, 1);
        
        private const int CONNECTION_TIMEOUT_MS = 5000; // 5 seconds
        private const int MIDI_GENERATION_TIMEOUT_MS = 120000; // 2 minutes
        private const int MAX_BUFFER_SIZE = 10 * 1024 * 1024; // 10MB

        public bool IsConnected => isConnected;

        /// <summary>
        /// Response from the MIDI server.
        /// </summary>
        [Serializable]
        private class MidiResponse
        {
            public string midi_base64;  // Legacy field name
            public string midi_b64;     // New field name
            public string status;
            public string error;
            public string filename;
        }

        public NamedPipeMusicClient(string pipeName = "AdaptiveMusicPipe")
        {
            this.pipeName = pipeName;
            Debug.Log($"[NamedPipeClient] Initialized with pipe name: {pipeName}");
        }

        /// <summary>
        /// Test connection to the named pipe server.
        /// </summary>
        public async Task<bool> Connect()
        {
            try
            {
                Debug.Log($"[NamedPipeClient] Testing connection to pipe '{pipeName}'...");
                
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var cts = new CancellationTokenSource(CONNECTION_TIMEOUT_MS);
                    await client.ConnectAsync(cts.Token);
                    
                    if (client.IsConnected)
                    {
                        isConnected = true;
                        Debug.Log("[NamedPipeClient] Connection test successful");
                        return true;
                    }
                }
            }
            catch (TimeoutException)
            {
                Debug.LogError($"[NamedPipeClient] Connection timeout. Is the pipe server running?");
            }
            catch (IOException ex)
            {
                Debug.LogError($"[NamedPipeClient] Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NamedPipeClient] Unexpected error: {ex.Message}");
            }

            isConnected = false;
            return false;
        }

        /// <summary>
        /// Request MIDI generation from the server.
        /// Uses semaphore to serialize concurrent requests (multiple layers loading in parallel).
        /// </summary>
        public async Task<byte[]> RequestMIDI(MidiParams parameters)
        {
            // Serialize requests to prevent concurrent pipe operations
            await requestSemaphore.WaitAsync();
            
            try
            {
                Debug.Log($"[NamedPipeClient] Requesting MIDI: seed={parameters.seed}, bpm={parameters.bpm}, events={parameters.gen_events}");
                
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    // Connect with timeout
                    var connectCts = new CancellationTokenSource(CONNECTION_TIMEOUT_MS);
                    try
                    {
                        await client.ConnectAsync(connectCts.Token);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NamedPipeClient] Failed to connect: {ex.Message}");
                        isConnected = false;
                        return null;
                    }

                    isConnected = true;

                    // Prepare request (compatible with WebSocket format)
                    string paramJson = JsonUtility.ToJson(parameters);
                    string requestJson = $"{{\"action\":\"generate-midi\",\"params\":{paramJson}}}";
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

                    Debug.Log($"[NamedPipeClient] Sending request ({requestBytes.Length} bytes)");
                    var startTime = DateTime.Now;

                    // Send request
                    await client.WriteAsync(requestBytes, 0, requestBytes.Length);
                    await client.FlushAsync();

                    // Read response with timeout
                    byte[] responseBytes;
                    var readCts = new CancellationTokenSource(MIDI_GENERATION_TIMEOUT_MS);
                    
                    try
                    {
                        responseBytes = await ReadAllBytesAsync(client, readCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogError($"[NamedPipeClient] Read timeout ({MIDI_GENERATION_TIMEOUT_MS / 1000}s exceeded)");
                        return null;
                    }

                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    Debug.Log($"[NamedPipeClient] Response received in {elapsed:F2}s ({responseBytes.Length} bytes)");

                    // Parse response
                    string responseJson = Encoding.UTF8.GetString(responseBytes);
                    MidiResponse response = JsonUtility.FromJson<MidiResponse>(responseJson);

                    // Check for errors
                    if (!string.IsNullOrEmpty(response.error))
                    {
                        Debug.LogError($"[NamedPipeClient] Server error: {response.error}");
                        return null;
                    }

                    // Support both field names
                    string midiBase64 = response.midi_base64 ?? response.midi_b64;
                    
                    if (string.IsNullOrEmpty(midiBase64))
                    {
                        Debug.LogError("[NamedPipeClient] No MIDI data in response");
                        return null;
                    }

                    // Decode base64 MIDI
                    byte[] midiBytes = Convert.FromBase64String(midiBase64);
                    Debug.Log($"[NamedPipeClient] Received {midiBytes.Length} bytes of MIDI");
                    
                    return midiBytes;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NamedPipeClient] Request failed: {ex.Message}");
                Debug.LogException(ex);
                isConnected = false;
                return null;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        /// <summary>
        /// Read all available bytes from the pipe stream.
        /// For message mode pipes, reads until the complete message is received.
        /// For byte mode, reads until no more data available.
        /// </summary>
        private async Task<byte[]> ReadAllBytesAsync(PipeStream stream, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                // Read first chunk
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                // Check if we've exceeded max buffer size
                if (ms.Length > MAX_BUFFER_SIZE)
                {
                    throw new InvalidOperationException($"Response exceeds maximum size ({MAX_BUFFER_SIZE} bytes)");
                }

                // Try to check if pipe is in message mode
                bool isMessageMode = false;
                try
                {
                    // This will throw if not in message mode
                    isMessageMode = stream.ReadMode == PipeTransmissionMode.Message;
                }
                catch
                {
                    // Pipe is in byte mode
                    isMessageMode = false;
                }

                // If message mode, check IsMessageComplete
                if (isMessageMode)
                {
                    while (stream.IsMessageComplete == false && bytesRead > 0)
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }

                        if (ms.Length > MAX_BUFFER_SIZE)
                        {
                            throw new InvalidOperationException($"Response exceeds maximum size ({MAX_BUFFER_SIZE} bytes)");
                        }
                    }
                }
                // For byte mode, just return what we read (server should send complete message in one write)

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Disconnect from the server (cleanup).
        /// </summary>
        public Task Disconnect()
        {
            isConnected = false;
            requestSemaphore?.Dispose();
            Debug.Log("[NamedPipeClient] Disconnected");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get connection status string.
        /// </summary>
        public string GetStatus()
        {
            return $"NamedPipe '{pipeName}' - Connected: {isConnected}";
        }

        /// <summary>
        /// Update method (no-op for Named Pipes, but keeps interface compatible).
        /// </summary>
        public void Update()
        {
            // Named pipes don't need manual message dispatching
        }
    }
}
