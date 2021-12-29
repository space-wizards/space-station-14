namespace Content.Server.DeviceNetwork
{
    /// <summary>
    /// A collection of constants to help with using device networks
    /// </summary>
    public static class DeviceNetworkConstants
    {
        /// <summary>
        /// Invalid address used for broadcasting
        /// </summary>
        public const string NullAddress = "######";

        #region Commands

        /// <summary>
        /// The key for command names
        /// E.g. [DeviceNetworkConstants.Command] = "ping"
        /// </summary>
        public const string Command = "command";

        /// <summary>
        /// The command for setting a devices state
        /// E.g. to turn a light on or off
        /// </summary>
        public const string CmdSetState = "set_state";

        /// <summary>
        /// The command for a device that just updated its state
        /// E.g. suit sensors broadcasting owners vitals state
        /// </summary>
        public const string CmdUpdatedState = "updated_state";

        #endregion

        #region SetState

        /// <summary>
        /// Used with the <see cref="CmdSetState"/> command to turn a device on or off
        /// </summary>
        public const string StateEnabled = "state_enabled";

        #endregion
    }
}
