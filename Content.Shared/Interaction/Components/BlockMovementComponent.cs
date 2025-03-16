using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// This is used for entities which cannot move or interact in any way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockMovementComponent : Component
{
    [DataField]
    public bool BlockInteraction = true;
}
