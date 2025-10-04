using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

public sealed partial class SpawnEntityInContainerEntityEffectSystem : EntityEffectSystem<ContainerManagerComponent, SpawnEntityInContainer>
{
    protected override void Effect(Entity<ContainerManagerComponent> entity, ref EntityEffectEvent<SpawnEntityInContainer> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;
        var container = args.Effect.ContainerName;

        for (var i = 0; i < quantity; i++)
        {
            // Stop trying to spawn if it fails
            if (!PredictedTrySpawnInContainer(proto, entity, container, out _, entity.Comp))
                return;
        }
    }
}

public sealed partial class SpawnEntityInContainer : BaseSpawnEntityEntityEffect<SpawnEntityInContainer>
{
    /// <summary>
    /// Name of the container we're trying to spawn into.
    /// </summary>
    [DataField(required: true)]
    public string ContainerName;
}
