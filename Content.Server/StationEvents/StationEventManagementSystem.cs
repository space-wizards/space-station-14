using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents;

/// <summary>
///     This system handles adding, removing, starting, stopping, announcing, etc station events.
///     This system does not handle how these events are started or stopped. That is up to game rules
///     (like <see cref="BasicStationEventSchedulerSystem"/>)
///
///     This is more or less a proxy to <see cref="GameTicker"/> and its handling of <see cref="GameRulePrototype"/>
///     management, since station events are inheritors of game rules.
/// </summary>
[PublicAPI]
public sealed class StationEventManagementSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    #region Active Events API

    /// <summary>
    ///     Adds a station event, but does not start it.
    /// </summary>
    public void AddStationEvent(StationEventPrototype proto)
    {
        _gameTicker.AddGameRule(proto);
    }

    /// <summary>
    ///     Adds and starts a station event.
    /// </summary>
    public void StartStationEvent(StationEventPrototype proto)
    {
        _gameTicker.StartGameRule(proto);
    }

    /// <summary>
    ///     Forcibly ends a station event.
    /// </summary>
    public void EndStationEvent(StationEventPrototype proto)
    {
        _gameTicker.EndGameRule(proto);
    }

    /// <summary>
    ///     Returns true if the event has been started yet.
    /// </summary>
    public bool IsStationEventStarted(StationEventPrototype proto)
    {
        return _gameTicker.IsGameRuleStarted(proto);
    }

    #endregion

    #region All Events API

    /// <summary>
    ///     Returns the number of times the event has been run this round.
    /// </summary>
    public int TimesEventRun(StationEventPrototype proto)
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
    public TimeSpan? TimeSinceLastEvent(StationEventPrototype? prototype)
    {
        foreach (var (time, rule) in _gameTicker.AllPreviousGameRules.Reverse())
        {
            if (rule is not StationEventPrototype)
                continue;

            if (prototype == null || rule == prototype)
                return time;
        }

        return null;
    }

    #endregion
}
