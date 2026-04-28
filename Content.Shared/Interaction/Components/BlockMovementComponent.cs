using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// This is used for entities which cannot move or interact in any way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockMovementComponent : Component
{
    /// <summary>
    /// Blocks generic interactions such as container insertion, pick up, drop and such.
    /// </summary>
    [DataField]
    public bool BlockInteraction = true;

    /// <summary>
    /// Blocks being able to use entities.
    /// </summary>
    [DataField]
    public bool BlockUse = true;
}
