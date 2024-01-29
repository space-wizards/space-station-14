using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class DoorSignalControlComponent : Component
    {
        [DataField("openPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OpenPort = "Open";

        [DataField("closePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string ClosePort = "Close";

        [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string TogglePort = "Toggle";

        [DataField("boltPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string InBolt = "DoorBolt";

        [DataField("onOpenPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string OutOpen = "DoorStatus";
    }
}
