using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed class RampingStationEventSchedulerSystem : GameRuleSystem
{
    public override string Prototype => "RampingStationEventScheduler";

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private float _endTime;
    private float _maxChaos;
    private float _startingChaos;
    private float _timeUntilNextEvent;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetSeverityModifierEvent>(OnGetSeverityModifier);
    }

    public override void Started()
    {
        var avgChaos = _cfg.GetCVar(CCVars.EventsRampingAverageChaos);
        var avgTime = _cfg.GetCVar(CCVars.EventsRampingAverageEndTime);

        // Worlds shittiest probability distribution
        _maxChaos = _random.NextFloat(avgChaos - avgChaos / 4, avgChaos + avgChaos / 4);
        _endTime = _random.NextFloat(avgTime - avgTime / 4, avgTime + avgTime / 4);
        _startingChaos = _maxChaos / 10;

        PickNextEventTime();
    }

    public override void Ended()
    {
        _endTime = 0f;
        _maxChaos = 0f;
        _startingChaos = 0f;
        _timeUntilNextEvent = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!RuleStarted || !_event.EventsEnabled)
            return;

        if (_timeUntilNextEvent > 0f)
        {
            _timeUntilNextEvent -= frameTime;
            return;
        }

        _event.RunRandomEvent();
    }

    private void OnGetSeverityModifier(GetSeverityModifierEvent ev)
    {
        if (!RuleStarted)
            return;

        ev.Modifier *= GetChaosModifier();
        Logger.Info($"Ramping set modifier to {ev.Modifier}");
    }

    private float GetChaosModifier()
    {
        var roundTime = (float) _gameTicker.RoundDuration().TotalSeconds;
        if (roundTime > _endTime)
            return _maxChaos;

        return (_maxChaos / _endTime) * roundTime + _startingChaos;
    }

    private void PickNextEventTime()
    {
        var mod = GetChaosModifier();

        // 5-15 minutes baseline. Will get faster over time as the chaos mod increases.
        _timeUntilNextEvent = _random.NextFloat(300f / mod, 900f / mod);
    }
}
