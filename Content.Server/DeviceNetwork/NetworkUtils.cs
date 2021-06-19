using Content.Server.Interfaces;
using System.Collections.Generic;

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

        /// <summary>
        /// The key for command names
        /// </summary>
        public const string COMMAND = "command";
    }
}
