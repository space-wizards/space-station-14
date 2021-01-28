using Content.Server.Interfaces;

namespace Content.Server.DeviceNetwork
{ 
    /// <summary>
    /// A collection of utilities to help with using device networks
    /// </summary>
    public static class NetworkUtils
    {
        public const int PRIVATE = 0;
        public const int WIRED = 1;
        public const int WIRELESS = 2;

        public const string COMMAND = "command";
        public const string MESSAGE = "message";
        public const string PING = "ping";

        /// <summary>
        /// Handles responding to pings.
        /// </summary>
        public static void PingResponse(int frequency, DeviceNetworkConnection connection, string sender, NetworkPayload payload, string message = "")
        {
            if (payload.TryGetValue(COMMAND, out var command) && command == PING)
            {
                var response = NetworkPayload.Create(
                    (COMMAND, "ping_response"),
                    (MESSAGE, message)
                );
                connection.Send(frequency, sender, response);
            }
        }
    }
}
