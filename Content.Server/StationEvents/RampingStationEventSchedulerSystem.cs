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

    [ViewVariables(VVAccess.ReadWrite)]
    private float _endTime;
    [ViewVariables(VVAccess.ReadWrite)]
    private float _maxChaos;
    [ViewVariables(VVAccess.ReadWrite)]
    private float _startingChaos;
    [ViewVariables(VVAccess.ReadWrite)]
    private float _timeUntilNextEvent;

    [ViewVariables]
    public float ChaosModifier
    {
        get
        {
            var roundTime = (float) _gameTicker.RoundDuration().TotalSeconds;
            if (roundTime > _endTime)
                return _maxChaos;

            return (_maxChaos / _endTime) * roundTime + _startingChaos;
        }
    }

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
        // Got a complaint? Send them to
        _maxChaos = _random.NextFloat(avgChaos - avgChaos / 4, avgChaos + avgChaos / 4);
        // This is in minutes, so *60 for seconds (for the chaos calc)
        _endTime = _random.NextFloat(avgTime - avgTime / 4, avgTime + avgTime / 4) * 60f;
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

        PickNextEventTime();
        _event.RunRandomEvent();
    }

    private void OnGetSeverityModifier(GetSeverityModifierEvent ev)
    {
        if (!RuleStarted)
            return;

        ev.Modifier *= ChaosModifier;
        Logger.Info($"Ramping set modifier to {ev.Modifier}");
    }

    private void PickNextEventTime()
    {
        var mod = ChaosModifier;

        // 4-12 minutes baseline. Will get faster over time as the chaos mod increases.
        _timeUntilNextEvent = _random.NextFloat(240f / mod, 720f / mod);
    }
}
