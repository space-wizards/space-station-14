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

    [ViewVariables]
    public EntityUid? MarkedEntity;
}
