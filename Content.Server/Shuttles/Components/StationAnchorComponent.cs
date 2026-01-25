using Content.Server.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StationAnchorSystem))]
public sealed partial class StationAnchorComponent : Component
{
    [DataField]
    public bool SwitchedOn { get; set; } = true;
}
