using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
[Access(typeof(StationAnchorSystem))]
public sealed partial class StationAnchorComponent : Component
{
    [DataField("switchedOn")]
    public bool SwitchedOn { get; set; } = true;
}
