using System.Linq;
using Content.Server.Station;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BureaucraticError : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override string? StartAnnouncement =>
        Loc.GetString("station-event-bureaucratic-error-announcement");
    public override string Name => "BureaucraticError";

    public override string? StartAudio => "/Audio/Announcements/announce.ogg";

    public override int MinimumPlayers => 25;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    protected override float EndAfter => 1f;

    public override void Startup()
    {
        base.Startup();
        var chosenStation = _random.Pick(EntitySystem.Get<StationSystem>().StationInfo.Values.ToList());
        var jobList = chosenStation.JobList.Keys.Where(x => !_prototypeManager.Index<JobPrototype>(x).IsHead).ToList();

        // Low chance to completely change up the late-join landscape by closing all positions except infinite slots.
        // Lower chance than the /tg/ equivalent of this event.
        if (_random.Prob(0.25f))
        {
            var chosenJob = _random.PickAndTake(jobList);
            chosenStation.AdjustJobAmount(chosenJob, -1); // INFINITE chaos.
            foreach (var job in jobList)
            {
                if (chosenStation.JobList[job] == -1)
                    continue;
                chosenStation.AdjustJobAmount(job, 0);
            }
        }
        else
        {
            // Changing every role is maybe a bit too chaotic so instead change 20-30% of them.
            for (var i = 0; i < _random.Next((int)(jobList.Count * 0.20), (int)(jobList.Count * 0.30)); i++)
            {
                var chosenJob = _random.PickAndTake(jobList);
                if (chosenStation.JobList[chosenJob] == -1)
                    continue;

                var adj = Math.Max(chosenStation.JobList[chosenJob] + _random.Next(-3, 6), 0);
                chosenStation.AdjustJobAmount(chosenJob, adj);
            }
        }
    }

}
