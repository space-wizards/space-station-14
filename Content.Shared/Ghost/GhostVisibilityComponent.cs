using Content.Shared.Eye;
using Robust.Shared.GameStates;

namespace Content.Shared.Ghost;

/// <summary>
/// This component modifies visibility masks & sprite visibility for entities.
/// This exists to avoid code duplication between ghosts, and ghost-like entities (e.g., revenants).
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
    /// <remarks>
    /// Admin ghosts should not be getting revealed by the wizard.
    /// Similarly, revenants should be able to continue functioning.
    /// </remarks>
    [DataField]
    public bool IgnoreGlobalVisibility = true;
    // This defaults to true / is opt-in because that's how it was before I refactored it. IMO it should be opt-out.

    /// <summary>
    /// Whether the ghost will be revealed after the round ends.
    /// </summary>
    /// <remarks>
    /// Admin ghosts should not be getting revealed at the end of the round.
    /// </remarks>
    [DataField]
    public bool VisibleOnRoundEnd;
    // This defaults to false / is opt-in because that's how it was before I refactored it. IMO it should be opt-out.

    /// <summary>
    /// Visibility layers to add or remove from this entity when ghost-visibility is toggled.
    /// </summary>
    [DataField]
    public VisibilityFlags Layer = VisibilityFlags.Ghost;

    /// <summary>
    /// Eye visibility mask to add to this entity..
    /// </summary>
    [DataField]
    public VisibilityFlags Mask = VisibilityFlags.Ghost;
}
