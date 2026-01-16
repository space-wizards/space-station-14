using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStationAnchorSystem))]
public sealed partial class StationAnchorComponent : Component
{
    [DataField]
    public bool SwitchedOn { get; set; } = true;
}
