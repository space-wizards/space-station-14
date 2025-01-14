using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedItemRecallSystem))]
public sealed partial class RecallMarkerComponent : Component
{
    /// <summary>
    /// The entity that marked this item.
    /// </summary>
    [ViewVariables]
    public EntityUid? MarkedByEntity;

    /// <summary>
    /// The action that marked this item.
    /// </summary>
    [ViewVariables]
    public EntityUid? MarkedByAction;
}
