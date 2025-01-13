using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedItemRecallSystem))]
public sealed partial class ItemRecallComponent : Component
{
    [DataField]
    public LocId? WhileMarkedName = "";

    [DataField]
    public LocId? WhileMarkedDescription = "";

    [DataField]
    public SpriteSpecifier? WhileMarkedSprite;

    [ViewVariables]
    public EntityUid? MarkedEntity;
}
