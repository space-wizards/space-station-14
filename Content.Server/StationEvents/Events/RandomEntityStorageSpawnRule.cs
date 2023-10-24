using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class RandomEntityStorageSpawnRule : StationEventSystem<RandomEntityStorageSpawnRuleComponent>
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    protected override void Started(EntityUid uid, RandomEntityStorageSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        var validLockers = new List<(EntityUid, EntityStorageComponent)>();
        var spawn = Spawn(comp.Prototype, MapCoordinates.Nullspace);

        var query = EntityQueryEnumerator<EntityStorageComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var storage, out var xform))
        {
            if (StationSystem.GetOwningStation(ent, xform) != station)
                continue;

            if (!_entityStorage.CanInsert(spawn, ent, storage))
                continue;

            validLockers.Add((ent, storage));
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
