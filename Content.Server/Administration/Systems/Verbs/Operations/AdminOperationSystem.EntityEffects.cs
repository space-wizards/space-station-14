using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnEntityEffects(Entity<MetaDataComponent> entity, ref AdminOperationEvent<EntityEffectsOperation> args)
    {
        _entityEffects.ApplyEffects(entity, args.Operation.Effects, user: args.User);
    }
}
