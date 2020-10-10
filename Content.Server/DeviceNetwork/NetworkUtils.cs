using Content.Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Handles responding to pings.
        /// </summary>
        public static void PingResponse<T>(T connection, string sender, IReadOnlyDictionary<string, string> payload, string message = "") where T : IDeviceNetworkConnection
        {
            if (payload.TryGetValue("command", out var command) && command == "ping")
            {
                var response = new Dictionary<string, string>
                {
                    {"command", "ping_response"},
                    {"message", message}
                };

                connection.Send(connection.Frequency, sender, response);
            }
        }
    }
}
