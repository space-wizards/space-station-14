using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

/// <summary>
/// Manages and displays round-end statistics, counting events and formatting results for the round summary.
/// </summary>
public sealed class RoundEndStatisticsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Dictionary<ProtoId<RoundStatisticPrototype>, (RoundStatisticPrototype stat, int statCount)> _statistics = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeStatsValueEvent>(ChangeValue);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);

        _statistics = _prototypeManager
            .EnumeratePrototypes<RoundStatisticPrototype>()
            .ToDictionary(stat => new ProtoId<RoundStatisticPrototype>(stat.ID), stat => (stat, 0));
    }

    // Change the value by the given int
    private void ChangeValue(ref ChangeStatsValueEvent args)
    {
        var key = new ProtoId<RoundStatisticPrototype>(args.Key);

        if (_statistics.TryGetValue(key, out var entry))
        {
            _statistics[key] = (entry.stat, entry.statCount + args.Amount);
        }
        else
        {
            DebugTools.Assert(false);
        }
    }

    // Set all ints to zero on roundstart
    private void OnRoundStart(RoundStartingEvent args)
    {
        foreach (var key in _statistics.Keys.ToList())
        {
            var stat = _statistics[key];
            _statistics[key] = (stat.stat, 0);
        }
    }

    // Format and send all statistics on roundend
    private void OnRoundEndText(RoundStatisticsAppendEvent args)
    {
        foreach (var stat in _statistics.Values.Where(s => s.statCount > 0))
        {
            var text = Loc.GetString(stat.stat.StatString, ("count", stat.statCount));
            args.AddLine(text);
        }
    }
}
