using System.Linq;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Station.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Server.GameTicking.Rules;

public sealed class AllCaptainsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Prototype => "AllCaptains";

    private EntityUid holdJobs; // dunno if there should only be one reference like this because I'm a ss14 noob but hey  it's only gotta work one day :P

    public override void Added()
    {
    }

    public override void Started()
    {
        // temporarily disable role timers -- super hacky way workaround for client to be aware that role timers aren't required
        // without having to set up some kind of replication
        _cfg.SetCVar(CCVars.GameRoleTimers, false);
    }

    public override void Ended()
    {
        _cfg.SetCVar(CCVars.GameRoleTimers, true);
    }

    public StationJobsComponent GetJobs(EntityUid station)
    {
        if (!holdJobs.IsValid() || !HasComp<StationJobsComponent>(holdJobs)) // this doesn't check station parameter since all captains mode is the same for all stations.
        {
            holdJobs = Spawn(null, new EntityCoordinates(station, Vector2.Zero));
            var stationJobs = AddComp<StationJobsComponent>(holdJobs);

            // Create captains-only specific job list
            var mapJobList = new Dictionary<string, List<int?>> {{"Captain", new List<int?>{int.MaxValue, int.MaxValue}}};

            stationJobs.RoundStartTotalJobs = mapJobList.Values.Where(x => x[0] is not null && x[0] > 0).Sum(x => x[0]!.Value);
            stationJobs.MidRoundTotalJobs = mapJobList.Values.Where(x => x[1] is not null && x[1] > 0).Sum(x => x[1]!.Value);
            stationJobs.TotalJobs = stationJobs.MidRoundTotalJobs;
            stationJobs.JobList = mapJobList.ToDictionary(x => x.Key, x =>
            {
                if (x.Value[1] <= -1)
                    return null;
                return (uint?) x.Value[1];
            });
            stationJobs.RoundStartJobList = mapJobList.ToDictionary(x => x.Key, x =>
            {
                if (x.Value[0] <= -1)
                    return null;
                return (uint?) x.Value[0];
            });
            stationJobs.OverflowJobs = new HashSet<string>{"Captain"}; //stationData.StationConfig.OverflowJobs.ToHashSet();
        }

        return Comp<StationJobsComponent>(holdJobs);
    }

}
