using Content.Shared.EntityEffects;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnEntityEffects(Entity<MetaDataComponent> entity, ref AdminOperationEvent<EntityEffectsOperation> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }
}

public sealed partial class EntityEffectsOperation : AdminOperationBase<EntityEffectsOperation>
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}
