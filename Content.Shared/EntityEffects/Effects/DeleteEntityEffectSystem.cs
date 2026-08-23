namespace Content.Shared.EntityEffects.Effects;

public sealed partial class DeleteEntityEffectSystem : EntityEffectSystem<MetaDataComponent ,DeleteEntity>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<DeleteEntity> args)
    {
        PredictedQueueDel(entity.Owner);
    }
}
