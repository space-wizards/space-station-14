using Content.Server.Interfaces;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
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
        public static void PingResponse<T>(T connection, string sender, IReadOnlyDictionary<string, string> payload, string message = "") where T : IDeviceNetworkConnection
        {
            if (payload.TryGetValue(COMMAND, out var command) && command == PING)
            {
                var response = new Dictionary<string, string>
                {
                    {COMMAND, "ping_response"},
                    {MESSAGE, message}
                };

                connection.Send(connection.Frequency, sender, response);
            }
        }
    }
}
