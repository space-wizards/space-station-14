using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;

namespace Content.Server.StationEvents;

public sealed class BasicStationEventSchedulerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    #region Private API

    /// <summary>
    ///     Returns the number of times the event has been run this round.
    /// </summary>
    public int TimesEventRun(GameRulePrototype proto)
    {
        var acc = 0;
        foreach (var (_, rule) in _gameTicker.AllPreviousGameRules)
        {
            if (rule == proto)
                acc++;
        }

        return acc;
    }

    /// <summary>
    ///     Returns the time since the last specified even prototype was run. If no event prototype is specified,
    ///     returns the time since any event occurred. If the event has not occurred this round, returns null.
    /// </summary>
    public TimeSpan? TimeSinceLastEvent(GameRulePrototype? prototype)
    {
        foreach (var (time, rule) in _gameTicker.AllPreviousGameRules.Reverse())
        {
            if (rule.Configuration is not StationEventRuleConfiguration)
                continue;

            if (prototype == null || rule == prototype)
                return time;
        }

        return null;
    }

    #endregion
}
