namespace Content.Server.DeviceNetwork
{
    /// <summary>
    /// A collection of utilities to help with using device networks
    /// </summary>
    public static class DeviceNetworkConstants
    {
        public enum ConnectionType
        {
            Private,
            Wired,
            Wireless
        }

        /// <summary>
        /// The key for command names
        /// E.g. [DeviceNetworkConstants.Command] = "ping"
        /// </summary>
        public const string Command = "command";
    }
}
