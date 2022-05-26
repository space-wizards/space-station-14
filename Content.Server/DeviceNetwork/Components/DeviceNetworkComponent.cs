using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [Friend(typeof(DeviceNetworkSystem), typeof(DeviceNet))]
    public sealed class DeviceNetworkComponent : Component
    {
        /// <summary>
        ///     Valid device network NetIDs. The netID is used to separate device networks that shouldn't interact with
        ///     each other e.g. wireless and wired.
        /// </summary>
        [Serializable]
        public enum ConnectionType
        {
            Private,
            Wired,
            Wireless,
            Apc
        }
        // TODO allow devices to join more than one network?

        // TODO if wireless/wired is determined by ConnectionType, what is the point of WirelessNetworkComponent & the
        // other network-type-specific components? Shouldn't DeviceNetId determine conectivity checks?

        [DataField("deviceNetId")]
        public ConnectionType DeviceNetId { get; set; } = ConnectionType.Private;

        /// <summary>
        ///     The frequency that this device is listening on.
        /// </summary>
        [DataField("receiveFrequency")]
        public uint? ReceiveFrequency;

        /// <summary>
        ///     frequency prototype. Used to select a default frequency to listen to on. Used when the map is
        ///     initialized.
        /// </summary>
        [DataField("receiveFrequencyId", customTypeSerializer: typeof(PrototypeIdSerializer<DeviceFrequencyPrototype>))]
        public string? ReceiveFrequencyId;

        /// <summary>
        ///     The frequency that this device going to try transmit on.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transmitFrequency")]
        public uint? TransmitFrequency;

        /// <summary>
        ///     frequency prototype. Used to select a default frequency to transmit on. Used when the map is
        ///     initialized.
        /// </summary>
        [DataField("transmitFrequencyId", customTypeSerializer: typeof(PrototypeIdSerializer<DeviceFrequencyPrototype>))]
        public string? TransmitFrequencyId;

        /// <summary>
        ///     The address of the device, either on the network it is currently connected to or whatever address it
        ///     most recently used.
        /// </summary>
        [DataField("address")]
        public string Address = string.Empty;

        /// <summary>
        ///     If true, the address was customized and should be preserved across networks. If false, a randomly
        ///     generated address will be created whenever this device connects to a network.
        /// </summary>
        [DataField("customAddress")]
        public bool CustomAddress = false;

        /// <summary>
        ///     Prefix to prepend to any automatically generated addresses. Helps players to identify devices. This gets
        ///     localized.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prefix")]
        public string? Prefix;

        /// <summary>
        ///     Whether the device should listen for all device messages, regardless of the intended recipient.
        /// </summary>
        [DataField("receiveAll")]
        public bool ReceiveAll;

        /// <summary>
        ///     Whether the device should attempt to join the network on map init.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoConnect")]
        public bool AutoConnect = true;
    }
}
