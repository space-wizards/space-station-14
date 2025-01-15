using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedItemRecallSystem))]
public sealed partial class ItemRecallComponent : Component
{
    [DataField]
    public LocId? WhileMarkedName = "item-recall-marked-name";

    [DataField]
    public LocId? WhileMarkedDescription = "item-recall-marked-description";

    /// <summary>
    /// The entity currently marked to be recalled by this action.
    /// </summary>
    [ViewVariables]
    public EntityUid? MarkedEntity;
}
