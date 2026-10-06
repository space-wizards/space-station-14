using Content.Server.StationEvents.Components;
using Content.Shared.StationRecords;
using Content.Shared.GameTicking.Components;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Random;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that remove a number of station records.
/// </summary>
/// <seealso cref="ClericalErrorRuleComponent"/>
public sealed partial class ClericalErrorRule : StationEventSystem<ClericalErrorRuleComponent>
{
    [Dependency] private StationRecordsSystem _stationRecords = default!;

    protected override void Started(Entity<ClericalErrorRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        if (!TryComp<StationRecordsComponent>(chosenStation, out var stationRecords))
            return;

        var recordCount = stationRecords.Records.Keys.Count;

        if (recordCount == 0)
            return;

        var min = (int)Math.Max(1, Math.Round(ent.Comp1.MinToRemove * recordCount));
        var max = (int)Math.Max(min, Math.Round(ent.Comp1.MaxToRemove * recordCount));
        var toRemove = RobustRandom.Next(min, max);
        var keys = new List<uint>();
        for (var i = 0; i < toRemove; i++)
        {
            keys.Add(RobustRandom.Pick(stationRecords.Records.Keys));
        }

        foreach (var id in keys)
        {
            var key = new StationRecordKey(id, chosenStation.Value);
            _stationRecords.RemoveRecord(key, stationRecords);
        }
    }
}
