using Content.Shared.DeviceLinking;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed class TwoWayLeverComponent : Component
    {
        [DataField("state")]
        public TwoWayLeverState State;

        [DataField("nextSignalLeft")]
        public bool NextSignalLeft;

        [DataField("leftPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string LeftPort = "Left";

        [DataField("rightPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string RightPort = "Right";

        [DataField("middlePort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string MiddlePort = "Middle";
    }
}
