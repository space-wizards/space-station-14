using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemAction;


/// <summary>
/// Component used as a marker for items summoned by the ItemAction system.
/// Used for keeping track of items summoned by said action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedItemActionSystem))]
public sealed partial class ActionSummonedItemComponent : Component
{
    /// <summary>
    /// The action that marked this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SummoningAction;
}
