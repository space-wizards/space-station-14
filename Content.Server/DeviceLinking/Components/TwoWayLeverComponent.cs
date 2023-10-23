using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class TwoWayLeverComponent : Component
    {
        [DataField("state")]
        public TwoWayLeverState State;

        [DataField("nextSignalLeft")]
        public bool NextSignalLeft;

        [DataField("leftPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string LeftPort = "Left";

        [DataField("rightPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string RightPort = "Right";

        [DataField("middlePort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string MiddlePort = "Middle";
    }
}
