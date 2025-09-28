using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// This is used for entities which cannot move or interact in any way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockMovementComponent : Component
{
    [DataField]
    public bool BlockUse = true;

    [DataField]
    public bool BlockInteraction = true;

    [DataField]
    public bool BlockDrop = true;

    [DataField]
    public bool BlockPickup = true;

    [DataField]
    public bool BlockChangeDirection = true;
}
