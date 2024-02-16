using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Add to grids that you do not want manually piloted under any circumstances.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventPilotComponent : Component
{

}
