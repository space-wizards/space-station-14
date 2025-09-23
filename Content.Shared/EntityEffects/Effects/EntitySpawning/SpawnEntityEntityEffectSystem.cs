namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

public sealed partial class SpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntity>
{
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntity> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;

        for (var i = 0; i < quantity; i++)
        {
            PredictedSpawnNextToOrDrop(proto, entity, entity.Comp);
        }
    }
}

public sealed partial class SpawnEntity : BaseSpawnEntityEntityEffect<SpawnEntity>;
