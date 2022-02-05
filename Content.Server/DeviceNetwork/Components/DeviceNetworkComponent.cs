using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("DeviceNetworkComponent")]
    public class DeviceNetworkComponent : Component
    {
        /// <summary>
        ///  Valid device network NetIDs.
        /// The netID is used to separate device networks that shouldn't interact with each other e.g. wireless and wired.
        /// </summary>
        [Serializable]
        public enum ConnectionType
        {
            Private,
            Wired,
            Wireless,
            Apc
        }

        [DataField("deviceNetId")]
        public ConnectionType DeviceNetId { get; set; } = ConnectionType.Private;

        [DataField("frequency")]
        public int Frequency { get; set; } = 0;

        [ViewVariables]
        public bool Open;

        [ViewVariables]
        public string Address = string.Empty;

        [DataField("receiveAll")]
        public bool ReceiveAll;
    }
}
