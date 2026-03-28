using Robust.Shared.GameStates;

namespace Content.Shared.RussStation.Carrying.Components;

/// <summary>
/// Active marker on an entity currently being carried.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeingCarriedComponent : Component
{
    // Saved by the client CarryingSystem on startup so draw depth can be restored when carrying ends.
    [ViewVariables]
    public int? OriginalDrawDepth;
}
