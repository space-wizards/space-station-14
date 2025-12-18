using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;


/// <summary>
/// Component used as a marker for an item marked by the ItemRecall ability.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedItemRecallSystem))]
public sealed partial class RecallMarkerComponent : Component
{
    /// <summary>
    /// The action that marked this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MarkedByAction;
}
