using System.Numerics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a random amount of the same entity, picked from a
/// <see cref="WeightedRandomEntityPrototype"/>, at a random offset around this entity.
/// Optionally delays the spawn via temporary spawner entities.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class WeightedSpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, WeightedSpawnEntity>
{
    private static readonly EntProtoId TempEntityProtoId = "TemporaryEntityForTimedDespawnSpawners";

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpawnOnDespawnSystem _spawnOnDespawn = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<WeightedSpawnEntity> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        // Get the position at which to start initially spawning entities
        var position = _transformSystem.GetMapCoordinates(uid);
        // Helper function used to randomly get an offset to apply to the original position
        Vector2 GetRandomVector() => new(_random.NextFloat(-effect.SpawnOffset, effect.SpawnOffset), _random.NextFloat(-effect.SpawnOffset, effect.SpawnOffset));
        // Randomly pick the entity to spawn and randomly pick how many to spawn
        var proto = _prototypeManager.Index(effect.WeightedEntityTable);
        var entityProto = proto.Pick(_random);
        var amountToSpawn = _random.NextFloat(effect.MinSpawn, effect.MaxSpawn);

        // Different behaviors for delayed spawning and immediate spawning
        if (effect.SpawnAfter != 0)
        {
            // if it fails to get the spawner, this won't ever work so just return
            if (!_prototypeManager.Resolve(TempEntityProtoId, out var tempSpawnerProto))
                return;

            // spawn the spawner, assign it a lifetime, and assign the entity that it will spawn when despawned
            for (var i = 0; i < amountToSpawn; i++)
            {
                var spawner = Spawn(tempSpawnerProto.ID, position.Offset(GetRandomVector()));
                EnsureComp<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
                timedDespawnComponent.Lifetime = effect.SpawnAfter;
                EnsureComp<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
                _spawnOnDespawn.SetPrototype((spawner, spawnOnDespawnComponent), entityProto.Id);
            }
        }
        else
        {
            // directly spawn the desired entities
            for (var i = 0; i < amountToSpawn; i++)
            {
                Spawn(entityProto, position.Offset(GetRandomVector()));
            }
        }
    }
}
