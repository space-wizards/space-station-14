using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class EntityEffectsOperation : AdminOperationBase<EntityEffectsOperation>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}
