using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

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

public sealed partial class SpawnEntityInContainerOrDrop : BaseSpawnEntityEntityEffect<SpawnEntityInContainerOrDrop>
{
    /// <summary>
    /// Name of the container we're trying to spawn into.
    /// </summary>
    [DataField(required: true)]
    public string ContainerName;
}
