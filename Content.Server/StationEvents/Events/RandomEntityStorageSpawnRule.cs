using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn an entity inside of another random entity with <see cref="EntityStorageComponent"/>.
/// </summary>
/// <seealso cref="RandomEntityStorageSpawnRuleComponent"/>
public sealed partial class RandomEntityStorageSpawnRule : StationEventSystem<RandomEntityStorageSpawnRuleComponent>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;

    protected override void Started(Entity<RandomEntityStorageSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var spawnRule = ent.Comp1;

        var validLockers = new List<Entity<EntityStorageComponent>>();
        var spawn = Spawn(spawnRule.Prototype, MapCoordinates.Nullspace);

        foreach (var storageEnt in Station.GetEntitiesWithComponentOnStation<EntityStorageComponent>(false))
        {
            if (!_entityStorage.CanInsert(spawn, storageEnt, storageEnt.Comp))
            {
                continue;
            }

            validLockers.Add(storageEnt);
        }

        if (validLockers.Count == 0)
        {
            Del(spawn);
            return;
        }

        var locker = RobustRandom.Pick(validLockers);
        if (!_entityStorage.Insert(spawn, locker, locker.Comp))
            Del(spawn);
    }
}
