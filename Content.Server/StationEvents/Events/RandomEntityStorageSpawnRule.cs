using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class RandomEntityStorageSpawnRule : StationEventSystem<RandomEntityStorageSpawnRuleComponent>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;

    protected override void Started(EntityUid uid, RandomEntityStorageSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var validLockers = new List<Entity<EntityStorageComponent>>();
        var spawn = Spawn(comp.Prototype, MapCoordinates.Nullspace);

        foreach (var ent in GetEntitiesWithComponentOnStation<EntityStorageComponent>(false))
        {
            if (!_entityStorage.CanInsert(spawn, ent, ent.Comp))
            {
                continue;
            }

            validLockers.Add(ent);
        }

        if (validLockers.Count == 0)
        {
            Del(spawn);
            return;
        }

        var locker = RobustRandom.Pick(validLockers);
        if (!_entityStorage.Insert(spawn, locker, locker.Comp))
        {
            Del(spawn);
        }
    }
}
