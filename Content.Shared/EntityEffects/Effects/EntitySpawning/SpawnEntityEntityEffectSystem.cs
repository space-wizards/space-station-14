using Robust.Shared.Network;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a number of entities of a given prototype at the coordinates of this entity.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntity>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> entity, SpawnEntity effect, float scale, EntityUid? user)
    {
        var quantity = effect.Number * (int)Math.Floor(scale);
        var proto = effect.Entity;

        if (effect.Predicted)
        {
            for (var i = 0; i < quantity; i++)
            {
                PredictedSpawnNextToOrDrop(proto, entity, entity.Comp);
            }
        }
        else if (_net.IsServer)
        {
            for (var i = 0; i < quantity; i++)
            {
                SpawnNextToOrDrop(proto, entity, entity.Comp);
            }
        }
    }
}

/// <inheritdoc cref="BaseSpawnEntityEntityEffect{T}"/>
public sealed partial class SpawnEntity : BaseSpawnEntityEntityEffect<SpawnEntity>;
