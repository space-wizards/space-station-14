using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Lock;
using Robust.Server.Containers;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HauntedClosetRuleSystem : GameRuleSystem<HauntedClosetRuleComponent>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private LockSystem _lockSystem = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private ContainerSystem _containerSystem = default!;

    protected override void Started(Entity<HauntedClosetRuleComponent, GameRuleComponent> ruleEnt, ref GameRuleStartedEvent args)
    {
        base.Started(ruleEnt, ref args);

        var queryUnListed = EntityQueryEnumerator<HauntedClosetComponent>();
        var query = new List<Entity<HauntedClosetComponent>>();
        var sum = 0f;
        while (queryUnListed.MoveNext(out var ent, out var comp))
        {
            sum += comp.CloseChance;
            query.Add((ent, comp));
        }

        var count = (int)Math.Floor(query.Count * ruleEnt.Comp1.ClosetFraction);

        // do maths to find the desired closets
        for (var i = 0; i < count; i++)
        {
            var chance = RobustRandom.NextFloat() * sum;
            if (query.Count == 0)
            {
                Log.Warning("Gamerule " + Name(ruleEnt.Owner) + " ran out of haunted closets before reaching the desired proportion");
                break;
            }

            for (var j = 0; j < query.Count; j++)
            {
                chance -= query[j].Comp.CloseChance;
                if (chance < 0f)
                {
                    sum -= chance;
                    Haunt(query[j].Owner, query[j].Comp, ruleEnt.Comp1);
                    query.RemoveSwap(j);
                    break;
                }
            }
        }
    }

    private void Haunt(EntityUid ent, HauntedClosetComponent haunted, HauntedClosetRuleComponent rule)
    {
        if (_entityStorage.IsOpen(ent))
        {
            // close it
            _entityStorage.CloseStorage(ent);

            // maybe fill things inside
            if (haunted.FillLoot == null)
                return;

            var spawns = _entityTable.GetSpawns(haunted.FillLoot).ToList();
            var xform = Transform(ent);
            var container = _containerSystem.GetContainer(ent, EntityStorageSystem.ContainerName);
            // so lockers dont refill themselves forever
            if (container.ContainedEntities.Count() > haunted.FillEntityMax)
                return;

            foreach (var proto in spawns)
            {
                var spawn = Spawn(proto);
                if (!_containerSystem.Insert(spawn, container, containerXform: xform))
                {
                    // it didnt fit
                    QueueDel(spawn);
                    break;
                }
            }
        }
        else if (rule.Toggle)
        {
            // open it
            _lockSystem.Unlock(ent, null);
            _entityStorage.OpenStorage(ent);
        }
    }
}
