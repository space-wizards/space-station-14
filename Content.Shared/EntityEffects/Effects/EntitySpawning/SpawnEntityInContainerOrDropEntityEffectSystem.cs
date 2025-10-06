using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a given number of entities of a given prototype in a specified container owned by this entity.
/// Acts like <see cref="SpawnEntityEntityEffectSystem"/> if it cannot spawn the prototype in the specified container.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntityInContainerOrDropEntityEffectSystem : EntityEffectSystem<ContainerManagerComponent, SpawnEntityInContainerOrDrop>
{
    protected override void Effect(Entity<ContainerManagerComponent> entity, ref EntityEffectEvent<SpawnEntityInContainerOrDrop> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;
        var container = args.Effect.ContainerName;

        var xform = Transform(entity);
        for (var i = 0; i < quantity; i++)
        {
            PredictedSpawnInContainerOrDrop(proto, entity, container, xform, entity.Comp);
        }
    }
}

/// <inheritdoc cref="BaseSpawnEntityEntityEffect{T}"/>
public sealed partial class SpawnEntityInContainerOrDrop : BaseSpawnEntityEntityEffect<SpawnEntityInContainerOrDrop>
{
    /// <summary>
    /// Name of the container we're trying to spawn into.
    /// </summary>
    [DataField(required: true)]
    public string ContainerName;
}
