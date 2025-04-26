using Robust.Shared.GameStates;

namespace Content.Shared.Ghost;

/// <summary>
/// This component modifies visibility masks & sprite visibility for entities.
/// This exists to avoid code duplication between revenants & observer ghosts.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostVisibilitySystem))]
[AutoGenerateComponentState(true)]
public sealed partial class GhostVisibilityComponent : Component
{
    /// <summary>
    /// Whether the ghost is currently visible.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool Visible;

    /// <summary>
    /// Optional override for normal ghost visibility rules. Can be used to make a ghost always visible.
    /// </summary>
    [DataField]
    public bool? VisibleOverride;

    /// <summary>
    /// Whether the ghost can be revealed by global visibility settings (e.g., wizard shenanigans).
    /// </summary>
    [DataField]
    public bool IgnoreGlobalVisibility = true;

    /// <summary>
    /// Whether the ghost will be revealed after the round ends.
    /// </summary>
    [DataField]
    public bool VisibleOnRoundEnd;
}
