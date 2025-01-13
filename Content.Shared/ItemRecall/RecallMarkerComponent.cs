using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedItemRecallSystem))]
public sealed partial class RecallMarkerComponent : Component
{
    /// <summary>
    ///     Does this spell require Wizard Robes & Hat?
    /// </summary>
    [ViewVariables]
    public EntityUid MarkedByEntity;

    [ViewVariables]
    public EntityUid MarkedByAction;
}
