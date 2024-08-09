using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Allows entities with OccluderComponent to toggle that component on and off.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ToggleableOccluderComponent : Component
    {
        [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OnPort = "On";

        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OffPort = "Off";

        [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string TogglePort = "Toggle";
    }
}
