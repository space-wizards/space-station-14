using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class CanHibernateComponent : Component
{
    [DataField]
    public EntProtoId EepyAction = "ActionEepy";

    [DataField]
    public EntityUid? EepyActionEntity;

    [DataField]
    public string NotEnoughNutrientsMessage = "";

    [DataField]
    public string TooFarFromHibernationSpot = "";

    [DataField]
    public string SpriteStateId = "";
}

public sealed partial class EepyActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class EntityHasHibernated(NetEntity hibernator, string spriteStateId) : EntityEventArgs
{
    public NetEntity Hibernator = hibernator;
    public string SpriteStateId = spriteStateId;
}
