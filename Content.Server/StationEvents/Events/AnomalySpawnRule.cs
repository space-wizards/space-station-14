using System.Linq;
using Content.Server.Anomaly;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class AnomalySpawnRule : StationEventSystem<AnomalySpawnRuleComponent>
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    protected override void Added(EntityUid uid, AnomalySpawnRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString("anomaly-spawn-event-announcement",
            ("sighting", Loc.GetString($"anomaly-spawn-sighting-{RobustRandom.Next(1, 6)}")));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    protected override void Started(EntityUid uid, AnomalySpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        EntityUid? grid = null;
        foreach (var g in stationData.Grids.Where(HasComp<BecomesStationComponent>))
        {
            grid = g;
        }

        if (grid is not { })
            return;

        var amountToSpawn = Math.Max(1, (int) MathF.Round(GetSeverityModifier() / 2));
        for (var i = 0; i < amountToSpawn; i++)
        {
            _anomaly.SpawnOnRandomGridLocation(grid.Value, component.AnomalySpawnerPrototype);
        }
    }
}
