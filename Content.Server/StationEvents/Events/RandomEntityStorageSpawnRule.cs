using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
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

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var station))
            return;

        var spawnRule = ent.Comp1;

        var validLockers = new List<(EntityUid, EntityStorageComponent)>();
        var spawn = Spawn(spawnRule.Prototype, MapCoordinates.Nullspace);

        var query = EntityQueryEnumerator<EntityStorageComponent, TransformComponent>();
        while (query.MoveNext(out var storageUid, out var storage, out var xform))
        {
            if (Station.GetOwningStation(ent, xform) != station.Value.Owner)
                continue;

            if (!_entityStorage.CanInsert(spawn, ent, storage))
                continue;

            validLockers.Add((storageUid, storage));
        }

        if (validLockers.Count == 0)
        {
            Del(spawn);
            return;
        }

        var (locker, storageComp) = RobustRandom.Pick(validLockers);
        if (!_entityStorage.Insert(spawn, locker, storageComp))
        {
            Del(spawn);
        }
    }
}
