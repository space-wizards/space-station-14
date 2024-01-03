using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server.Geras;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class GerasComponent : Component
{
    [DataField("gerasId")] public ProtoId<EntityPrototype> GerasId = "MobSlimesGeras";

    [DataField("gerasAction")] public ProtoId<EntityPrototype> GerasAction = "ActionMorphGeras";

    [DataField("gerasActionEntity")] public EntityUid? GerasActionEntity;
}

public sealed partial class MorphIntoGeras : InstantActionEvent
{

}
