using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.RetractableItemAction;

/// <summary>
/// Component used as a marker for items summoned by the RetractableItemAction system.
/// Used for keeping track of items summoned by said action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(RetractableItemActionSystem))]
public sealed partial class ActionRetractableItemComponent : Component
{
    /// <summary>
    /// The action that marked this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SummoningAction;
}
