

using Content.Server.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.DeviceNetwork
{
    [RegisterComponent]
    public class DummyNetworkComponent : BaseNetworkComponent
    {
        public override string Name => "DummyNetworkConnection";

        public override int DeviceNetID => _deviceNetID;

        [DataField("deviceNetID")]
        private int _deviceNetID = NetworkUtils.PRIVATE;

        public override int Frequency { get => _frequency; set => _frequency = value; }

        [DataField("frequency")]
        private int _frequency;
    }
}
