using System.Threading.Tasks;

namespace AdaptiveMusic
{
    /// <summary>
    /// Common interface for music generation clients (WebSocket, Named Pipes, etc.).
    /// </summary>
    public interface IMusicClient
    {
        /// <summary>
        /// Check if client is connected to server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connect to the music generation server.
        /// </summary>
        Task<bool> Connect();

        /// <summary>
        /// Request MIDI generation from the server.
        /// </summary>
        Task<byte[]> RequestMIDI(MidiParams parameters);

        /// <summary>
        /// Update method (for clients that need per-frame updates).
        /// </summary>
        void Update();

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        Task Disconnect();

        /// <summary>
        /// Get connection status string.
        /// </summary>
        string GetStatus();
    }
}
