using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BureaucraticErrorRule : StationEventSystem<BureaucraticErrorRuleComponent>
{
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    protected override void Started(EntityUid uid, BureaucraticErrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation, HasComp<StationJobsComponent>))
            return;

        var jobList = _stationJobs.GetJobs(chosenStation.Value).Keys.ToList();

        if (jobList.Count == 0)
            return;

        var mod = GetSeverityModifier();

        // Low chance to completely change up the late-join landscape by closing all positions except infinite slots.
        // Lower chance than the /tg/ equivalent of this event.
        if (RobustRandom.Prob(Math.Min(0.25f * MathF.Sqrt(mod), 1.0f)))
        {
            var chosenJob = RobustRandom.PickAndTake(jobList);
            _stationJobs.MakeJobUnlimited(chosenStation.Value, chosenJob); // INFINITE chaos.
            foreach (var job in jobList)
            {
                if (_stationJobs.IsJobUnlimited(chosenStation.Value, job))
                    continue;
                _stationJobs.TrySetJobSlot(chosenStation.Value, job, 0);
            }
        }
        else
        {
            var lower = (int) (jobList.Count * Math.Min(1.0f, 0.20 * mod));
            var upper = (int) (jobList.Count * Math.Min(1.0f, 0.30 * mod));
            // Changing every role is maybe a bit too chaotic so instead change 20-30% of them.
            var num = RobustRandom.Next(lower, upper);
            for (var i = 0; i < num; i++)
            {
                var chosenJob = RobustRandom.PickAndTake(jobList);
                if (_stationJobs.IsJobUnlimited(chosenStation.Value, chosenJob))
                    continue;

                _stationJobs.TryAdjustJobSlot(chosenStation.Value, chosenJob, RobustRandom.Next(-3, 6), clamp: true);
            }
        }
    }
}
