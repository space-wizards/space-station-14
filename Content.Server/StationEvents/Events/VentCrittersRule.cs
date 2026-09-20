using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn random reagent foam around a vent.
/// </summary>
/// <remarks>
/// Do NOT copy paste this class to make a new mob event, create a new game rule entity using <see cref="VentCrittersRuleComponent"/>.
/// </remarks>
public sealed partial class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    protected override void Started(Entity<VentCrittersRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var critterRule = ent.Comp1;

        var validLocations = Station.GetEntitiesWithComponentOnStation<VentCritterSpawnLocationComponent>(true);

        if (validLocations.Count == 0)
        {
            return;
        }

        // guaranteed spawn
        if (critterRule.SpecialEntries.Count > 0)
        {
            var specialEntry = RobustRandom.Pick(critterRule.SpecialEntries);
            var specialSpawn = Transform(RobustRandom.Pick(validLocations)).Coordinates;
            Spawn(specialEntry.PrototypeId, specialSpawn);
        }

        foreach (var location in validLocations)
        {
            var spawns = EntitySpawnCollection.GetSpawns(critterRule.Entries, RobustRandom);
            foreach (var spawn in spawns)
            {
                Spawn(spawn, Transform(location).Coordinates);
            }

            var specialSpawns = EntitySpawnCollection.GetSpawns(critterRule.Entries, RobustRandom)
                .Concat(EntitySpawnCollection.GetSpawns(critterRule.SpecialEntries, RobustRandom));
            foreach (var spawn in specialSpawns)
            {
                Spawn(spawn, Transform(location).Coordinates);
            }
        }
    }
}
