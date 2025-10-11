using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a given number of entities of a given prototype in a specified container owned by this entity.
/// Returns if the prototype cannot spawn in the specified container.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
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

/// <inheritdoc cref="BaseSpawnEntityEntityEffect{T}"/>
public sealed partial class SpawnEntityInContainer : BaseSpawnEntityEntityEffect<SpawnEntityInContainer>
{
    /// <summary>
    /// Name of the container we're trying to spawn into.
    /// </summary>
    [DataField(required: true)]
    public string ContainerName;
}
