using Content.Server.StationEvents.Components;
﻿using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceArtifactRule : StationEventSystem<BluespaceArtifactRuleComponent>
{
    protected override void Added(EntityUid uid, BluespaceArtifactRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var str = Loc.GetString("bluespace-artifact-event-announcement",
            ("sighting", Loc.GetString(RobustRandom.Pick(component.PossibleSighting))));
        stationEvent.StartAnnouncement = str;

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, BluespaceArtifactRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var amountToSpawn = 1;
        for (var i = 0; i < amountToSpawn; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                return;

            Spawn(component.ArtifactSpawnerPrototype, coords);
            Spawn(component.ArtifactFlashPrototype, coords);

            Sawmill.Info($"Spawning random artifact at {coords}");
        }
    }
}
