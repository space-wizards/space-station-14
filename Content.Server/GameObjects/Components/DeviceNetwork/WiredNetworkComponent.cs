
using Content.Server.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.DeviceNetwork
{
    [RegisterComponent]
    public class WiredNetworkComponent : BaseNetworkComponent
    {
        public override string Name => "WiredNetworkConnection";

        public override int DeviceNetID => _deviceNetID;

        [DataField("deviceNetID")]
        private int _deviceNetID = NetworkUtils.WIRED;

        public override int Frequency { get => 0; set { } }
    }
}
