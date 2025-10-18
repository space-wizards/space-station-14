using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a paradox clone.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ParadoxCloneRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// Name modifer applied to the player when they turn into a ghost.
    /// Needed to be able to keep the original and the clone apart in dead chat.
    /// </summary>
    [DataField]
    public LocId? NameModifier = "paradox-clone-ghost-name-modifier";
}
