using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.DeviceNetwork.Components
{
    /// <summary>
    /// Sends and receives device network messages wirelessly. Devices sending and receiving need to be in range and on the same frequency.
    /// </summary>
    [RegisterComponent]
    public class WirelessNetworkComponent : BaseNetworkComponent
    {
        public override string Name => "WirelessNetworkConnection";

        public override int DeviceNetID => _deviceNetID;

        public override int Frequency { get => _frequency; set => _frequency = value; }

        [DataField("range")]
        public int Range { get; set; }

        [DataField("deviceNetID")]
        private int _deviceNetID = NetworkUtils.WIRELESS;

        [DataField("frequency")]
        private int _frequency;
    }
}
