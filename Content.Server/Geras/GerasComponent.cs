using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Geras;

/// <summary>
/// This component assigns the entity with a polymorph action.
/// </summary>
[RegisterComponent]
public sealed partial class GerasComponent : Component
{
    [DataField] public ProtoId<PolymorphPrototype> GerasPolymorphId = "SlimeMorphGeras";

    [DataField] public ProtoId<EntityPrototype> GerasAction = "ActionMorphGeras";

    [DataField] public EntityUid? GerasActionEntity;
}
