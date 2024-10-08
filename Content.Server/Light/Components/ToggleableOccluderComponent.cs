using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Allows entities with OccluderComponent to toggle that component on and off.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ToggleableOccluderComponent : Component
    {
        [DataField]
        public ProtoId<SinkPortPrototype> OnPort = "On";

        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OffPort = "Off";

        [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string TogglePort = "Toggle";
    }
}
