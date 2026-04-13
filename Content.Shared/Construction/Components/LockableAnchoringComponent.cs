using Robust.Shared.GameStates;

namespace Content.Shared.Construction.Components;

/// <summary>
/// Makes so a entity can only be anchored/unanchored if locked or unlocked
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LockableAnchoringComponent : Component
{
    /// <summary>
    /// Anchorable flags to set when locked
    /// </summary>
    public AnchorableFlags LockedFlags = AnchorableFlags.Anchorable;

    /// <summary>
    /// Anchorable flags to set when unlocked
    /// </summary>
    public AnchorableFlags UnockedFlags = AnchorableFlags.Anchorable | AnchorableFlags.Unanchorable;
}
