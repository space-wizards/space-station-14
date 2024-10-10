using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Light.Components;

/// <summary>
///     Allows entities with OccluderComponent to toggle that component on and off.
/// </summary>
[RegisterComponent]
public sealed partial class ToggleableOccluderComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
}
