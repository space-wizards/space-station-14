using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events spawning an artifact on station with a bluespace flash.
/// </summary>
/// <seealso cref="BluespaceArtifactRuleComponent"/>
public sealed partial class BluespaceArtifactRule : StationEventSystem<BluespaceArtifactRuleComponent>
{
    protected override void Added(Entity<BluespaceArtifactRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(ent, out var stationEvent))
            return;

        var str = Loc.GetString("bluespace-artifact-event-announcement",
            ("sighting", Loc.GetString(RobustRandom.Pick(ent.Comp1.PossibleSightings))));
        stationEvent.StartAnnouncement = str;

        base.Added(ent, ref args);
    }

    protected override void Started(Entity<BluespaceArtifactRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var amountToSpawn = 1;
        for (var i = 0; i < amountToSpawn; i++)
        {
            if (!Station.TryFindRandomTile(out _, out _, out _, out var coords))
                return;

            Spawn(ent.Comp1.ArtifactSpawnerPrototype, coords);
            Spawn(ent.Comp1.ArtifactFlashPrototype, coords);

            Sawmill.Info($"Spawning random artifact at {coords}");
        }
    }
}
