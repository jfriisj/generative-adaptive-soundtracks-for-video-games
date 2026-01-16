using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AdaptiveMusic
{
    /// <summary>
    /// WebSocket client for requesting MIDI from the adaptive music server.
    /// Handles connection lifecycle, retries, and error recovery.
    /// Uses System.Net.WebSockets.ClientWebSocket for cross-platform compatibility.
    /// </summary>
    public class WebSocketMusicClient : IMusicClient
    {
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const float BASE_RETRY_DELAY = 1f; // Exponential backoff base
        private const int MIDI_GENERATION_TIMEOUT_MS = 120000; // 2 minutes for MIDI generation
        private readonly SemaphoreSlim requestSemaphore = new(1, 1); // Serialize concurrent requests
        private CancellationTokenSource cancellationTokenSource;
        private bool isConnected;
        private readonly byte[] receiveBuffer = new byte[1024 * 1024]; // 1MB buffer
        private readonly string serverUrl;
        private ClientWebSocket webSocket;

        public WebSocketMusicClient(string url)
        {
            serverUrl = url;
        }

        public bool IsConnected => isConnected && webSocket?.State == WebSocketState.Open;

        /// <summary>
        ///     Connect to the WebSocket server with exponential backoff retry.
        /// </summary>
        public async Task<bool> Connect()
        {
            if (IsConnected)
            {
                Debug.Log("[WebSocketClient] Already connected");
                return true;
            }

            for (var attempt = 0; attempt < MAX_RECONNECT_ATTEMPTS; attempt++)
            {
                try
                {
                    Debug.Log(
                        $"[WebSocketClient] Connecting to {serverUrl} (attempt {attempt + 1}/{MAX_RECONNECT_ATTEMPTS})");

                    webSocket = new ClientWebSocket();
                    // Helps prevent idle disconnects (server/proxy timeouts).
                    webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                    cancellationTokenSource = new CancellationTokenSource();

                    var uri = new Uri(serverUrl);
                    await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);

                    if (webSocket.State == WebSocketState.Open)
                    {
                        isConnected = true;
                        Debug.Log("[WebSocketClient] Connected successfully");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebSocketClient] Connection attempt {attempt + 1} failed: {ex.Message}");
                    webSocket?.Dispose();
                    webSocket = null;
                }

                // Exponential backoff
                if (attempt < MAX_RECONNECT_ATTEMPTS - 1)
                {
                    var delay = BASE_RETRY_DELAY * Mathf.Pow(2, attempt);
                    Debug.Log($"[WebSocketClient] Retrying in {delay}s...");
                    await Task.Delay((int)(delay * 1000));
                }
            }

            Debug.LogError("[WebSocketClient] Failed to connect after maximum attempts");
            return false;
        }

        /// <summary>
        ///     Request MIDI generation from the server.
        ///     Uses semaphore to serialize concurrent requests (multiple layers loading in parallel).
        /// </summary>
        public async Task<byte[]> RequestMIDI(MidiParams parameters)
        {
            Debug.Log($"[WebSocketClient] === MIDI REQUEST START === BPM: {parameters.bpm}, Events: {parameters.gen_events}, Seed: {parameters.seed}");
            Debug.Log($"[WebSocketClient] Parameters - Instruments: [{string.Join(", ", parameters.instruments ?? new string[0])}], DrumKit: {parameters.drum_kit}, Intensity: {parameters.intensity}");
            
            // Serialize requests to prevent concurrent WebSocket operations
            await requestSemaphore.WaitAsync();
            try
            {
                // If the remote closes unexpectedly, the socket can remain "Open" until the next IO.
                // We retry once on WebSocketException after resetting the connection.
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    if (!IsConnected)
                    {
                        Debug.LogWarning("[WebSocketClient] Not connected, attempting to reconnect...");
                        var connected = await Connect();
                        if (!connected)
                        {
                            Debug.LogError("[WebSocketClient] Cannot request MIDI: not connected");
                            return null;
                        }
                    }

                    try
                    {
                        return await RequestMIDIInternal(parameters);
                    }
                    catch (WebSocketException wse)
                    {
                        Debug.LogError($"[WebSocketClient] WebSocket error: {wse.Message}");
                        ResetConnection("WebSocketException during request");

                        if (attempt == 0)
                        {
                            Debug.LogWarning("[WebSocketClient] Retrying request once after reconnect...");
                            continue;
                        }

                        return null;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketClient] Request failed: {ex.Message}");
                ResetConnection("Unhandled exception during request");
                return null;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        private async Task<byte[]> RequestMIDIInternal(MidiParams parameters)
        {
            // Serialize request
            // The standalone server expects wrapper payload: { "action": "generate-midi", "params": { ... } }
            var paramJson = JsonUtility.ToJson(parameters);
            var json = $"{{\"action\":\"generate-midi\",\"params\":{paramJson}}}";
            Debug.Log($"[WebSocketClient] Sending request (wrapped): {json}");

            // Send request
            var sendBuffer = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true,
                cancellationTokenSource.Token);
            Debug.Log(
                $"[WebSocketClient] Request sent, waiting for response (timeout: {MIDI_GENERATION_TIMEOUT_MS / 1000}s)...");

            // Wait for response (configurable timeout for MIDI generation)
            var startTime = DateTime.Now;
            var responseData = await ReceiveMessage(MIDI_GENERATION_TIMEOUT_MS);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Debug.Log($"[WebSocketClient] === RESPONSE RECEIVED === Elapsed time: {elapsed:F2}s, Data size: {(responseData?.Length ?? 0)} bytes");

            if (responseData == null)
            {
                Debug.LogError("[WebSocketClient] No response received");
                return null;
            }

            // Parse response
            var responseJson = Encoding.UTF8.GetString(responseData);
            Debug.Log(
                $"[WebSocketClient] Raw response preview: {responseJson.Substring(0, Mathf.Min(200, responseJson.Length))}...");

            MidiResponse response;
            try
            {
                response = JsonUtility.FromJson<MidiResponse>(responseJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketClient] Failed to parse JSON response: {ex.Message}");
                return null;
            }

            if (!string.IsNullOrEmpty(response.error))
            {
                Debug.LogError($"[WebSocketClient] Server error: {response.error}");
                return null;
            }

            // Support both field names: midi_base64 (old) and midi_b64 (new standalone server)
            var midiBase64 = response.midi_base64 ?? response.midi_b64;

            if (string.IsNullOrEmpty(midiBase64))
            {
                Debug.LogError(
                    "[WebSocketClient] No MIDI data in response (checked both midi_base64 and midi_b64 fields)");
                return null;
            }

            // Decode base64 MIDI
            var midiBytes = Convert.FromBase64String(midiBase64);
            Debug.Log($"[WebSocketClient] Received {midiBytes.Length} bytes of MIDI");
            return midiBytes;
        }

        private void ResetConnection(string reason)
        {
            Debug.LogWarning($"[WebSocketClient] Resetting connection: {reason}");
            isConnected = false;

            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch
            {
                // Ignore
            }

            try
            {
                webSocket?.Dispose();
            }
            catch
            {
                // Ignore
            }

            try
            {
                cancellationTokenSource?.Dispose();
            }
            catch
            {
                // Ignore
            }

            webSocket = null;
            cancellationTokenSource = null;
        }

        /// <summary>
        /// Receive a complete message from the WebSocket with timeout.
        /// </summary>
        private async Task<byte[]> ReceiveMessage(int timeoutMs)
        {
            try
            {
                using (var timeoutCts = new CancellationTokenSource(timeoutMs))
                using (var linkedCts =
                       CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token,
                           timeoutCts.Token))
                {
                    var buffer = new ArraySegment<byte>(receiveBuffer);
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await webSocket.ReceiveAsync(buffer, linkedCts.Token);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Debug.LogWarning("[WebSocketClient] Server initiated close");
                                try
                                {
                                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                }
                                catch
                                {
                                    // Ignore close failures
                                }
                                ResetConnection("Server closed connection");
                                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely,
                                    "Server closed WebSocket while awaiting response");
                            }

                            ms.Write(receiveBuffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType != WebSocketMessageType.Text)
                        {
                            Debug.LogError($"[WebSocketClient] Unexpected message type: {result.MessageType}");
                            ResetConnection("Unexpected message type");
                            throw new WebSocketException(WebSocketError.InvalidMessageType,
                                $"Unexpected WebSocket message type: {result.MessageType}");
                        }

                        return ms.ToArray();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Debug.LogError($"[WebSocketClient] Receive timeout ({timeoutMs / 1000}s exceeded). Server may be slow or offline.");
                ResetConnection("Receive timeout");
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely,
                    $"Receive timeout after {timeoutMs}ms");
            }
            catch (WebSocketException)
            {
                ResetConnection("WebSocket receive failure");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketClient] Receive error: {ex.Message}");
                ResetConnection("Receive error");
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ex.Message);
            }
        }

        /// <summary>
        /// Update method to dispatch WebSocket messages (call from MonoBehaviour Update).
        /// </summary>
        public void Update()
        {
            // ClientWebSocket doesn't need manual message dispatching
        }

        /// <summary>
        /// Close the WebSocket connection.
        /// </summary>
        public async Task Disconnect()
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
                try
                {
                    cancellationTokenSource?.Cancel();
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing",
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebSocketClient] Error during disconnect: {ex.Message}");
                }
                finally
                {
                    webSocket?.Dispose();
                    cancellationTokenSource?.Dispose();
                    requestSemaphore?.Dispose();
                    webSocket = null;
                    isConnected = false;
                    Debug.Log("[WebSocketClient] Disconnected");
                }
        }

        /// <summary>
        /// Get connection status string.
        /// </summary>
        public string GetStatus()
        {
            if (webSocket == null) return "Not initialized";
            return $"{webSocket.State} - Connected: {IsConnected}";
        }

        /// <summary>
        ///     Response from the MIDI server.
        ///     Supports both midi_base64 (old) and midi_b64 (new standalone server).
        /// </summary>
        [Serializable]
        private class MidiResponse
        {
            public string midi_base64; // Old server field name
            public string midi_b64; // New standalone server field name
            public string status;
            public string error;
        }
    }
}