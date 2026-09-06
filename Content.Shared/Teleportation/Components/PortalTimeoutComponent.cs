using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Blocks portal use until the target leaves the bounds of its exit trigger.
/// Also protects the creator of a hand-teleporter portal until they step out.
/// Attached to the target; no component means no portal exit block.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PortalTimeoutComponent : Component
{
    /// <summary>
    /// The portal whose trigger bounds must be left before another portal can be used.
    /// For a hand teleporter, this is the portal just created beneath the user.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public EntityUid ExitPortal;
}
