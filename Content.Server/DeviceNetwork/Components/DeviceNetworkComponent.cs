using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    public class DeviceNetworkComponent : Component
    {
        public override string Name => "DeviceNetworkComponent";

        public int DeviceNetID => _deviceNetID;

        [DataField("frequency")]
        public int Frequency { get; set; } = 0;

        [ViewVariables]
        public bool Open;

        [ViewVariables]
        public string Address = string.Empty;

        [DataField("receiveAll")]
        public bool ReceiveAll;

        /// <summary>
        /// The device networks netID this DeviceNetworkComponent connects to.
        /// The netID is used to seperate device networks that shouldn't interact with each other e.g. wireless and wired.
        /// The default netID's are_
        /// 0 -> Private
        /// 1 -> Wired
        /// 2 -> Wireless
        /// </summary>
        [DataField("deviceNetID")]
        private int _deviceNetID = (int)DeviceNetworkConstants.ConnectionType.Private;
    }
}
