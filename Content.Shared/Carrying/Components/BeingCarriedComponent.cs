using Robust.Shared.GameStates;

namespace Content.Shared.Carrying.Components;

/// <summary>
/// Active marker on an entity currently being carried.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeingCarriedComponent : Component
{
    // Saved on ComponentStartup so draw depth can be restored on ComponentRemove (client-side only).
    [ViewVariables]
    public int? OriginalDrawDepth;
}
