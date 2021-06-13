using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components
{
    public abstract class BaseNetworkComponent : Component
    {
        public abstract int DeviceNetID { get; }
        public abstract int Frequency { get; set; }

        [ViewVariables]
        //[DataField("open", true)]
        public bool Open;

        [ViewVariables]
        public string Address = string.Empty;

        [DataField("receiveAll")]
        public bool ReceiveAll;

        [DataField("handlePings")]
        public bool HandlePings;

        [DataField("pingResponse")]
        public string PingResponse = string.Empty;

    }
}
