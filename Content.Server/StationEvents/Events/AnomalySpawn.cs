using System.Linq;
using Content.Server.Anomaly;
using Content.Server.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class AnomalySpawn : StationEventSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override string Prototype => "AnomalySpawn";

    public readonly string AnomalySpawnerPrototype = "RandomAnomalySpawner";

    public override void Added()
    {
        base.Added();

        var str = Loc.GetString("anomaly-spawn-event-announcement",
            ("sighting", Loc.GetString($"anomaly-spawn-sighting-{_random.Next(1, 6)}")));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    public override void Started()
    {
        base.Started();

        if (StationSystem.Stations.Count == 0)
            return; // No stations
        var chosenStation = RobustRandom.Pick(StationSystem.Stations.ToList());
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
            _anomaly.SpawnOnRandomGridLocation(grid.Value, AnomalySpawnerPrototype);
        }
    }
}
