using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Transform;

public sealed partial class SpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntity>
{
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntity> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);

        for (var i = 0; i < quantity; i++)
        {
            PredictedSpawnNextToOrDrop(args.Effect.Entity, entity, entity.Comp);

            // TODO figure out how to properly spawn inside of containers
            // e.g. cheese:
            // if the user is holding a bowl milk & enzyme, should drop to floor, not attached to the user.
            // if reaction happens in a backpack, should insert cheese into backpack.
            // --> if it doesn't fit, iterate through parent storage until it attaches to the grid (again, DON'T attach to players).
            // if the reaction happens INSIDE a stomach? the bloodstream? I have no idea how to handle that.
            // presumably having cheese materialize inside of your blood would have "disadvantages".
        }
    }
}

public sealed class SpawnEntity : EntityEffectBase<SpawnEntity>
{
    /// <summary>
    /// Amount of entities we're spawning
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// Prototype of the entity we're spawning
    /// </summary>
    [DataField (required: true)]
    public EntProtoId Entity;
}
