using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Prototypes;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

/// <summary>
/// System for count different stuff and show it in round end summary.
/// </summary>
public sealed class RoundEndStatisticsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    List<RoundStatisticPrototype>? _statistics;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeStatsValueEvent>(ChangeValue);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);

        _statistics = _prototypeManager.EnumeratePrototypes<RoundStatisticPrototype>().ToList();
    }

    // Change the value by given int
    private void ChangeValue(ref ChangeStatsValueEvent args)
    {
        var key = args.Key;
        var stat = _statistics?.FirstOrDefault(s => s.ID == key);
        if (stat != null)
        {
            stat.StatCount += args.Amount;
            return;
        }

        DebugTools.Assert(false);
    }

    // Set all ints to zero
    private void OnRoundStart(RoundStartingEvent args)
    {
        if (_statistics == null)
            return;

        _statistics.ForEach(stat => stat.StatCount = 0);
    }

    // Format and send all statistic
    private void OnRoundEndText(RoundStatisticsAppendEvent args)
    {
        if (_statistics == null)
            return;

        foreach (var stat in _statistics.Where(s => s.StatCount > 0))
        {
            var text = Loc.GetString(stat.StatString, ("count", stat.StatCount));
            args.AddLine(text);
        }

    }
}
