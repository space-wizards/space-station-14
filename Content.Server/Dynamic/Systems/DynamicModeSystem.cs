using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Dynamic.Systems;

/// <summary>
///     Its dynamic, baby!!!!
/// </summary>
public partial class DynamicModeSystem : GameRuleSystem
{
    public override string Prototype => "Dynamic";

    // Cached so we can re-enable random events if dynamic is disabled.
    private bool _stationEventsWereEnabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRoundStarted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled) return;

        // Midround injection loop.
        _midroundAccumulator += frameTime;

        // debug
        if (_midroundAccumulator > 30.0f)
        {

            // Run a midround event
        }
    }

    public override void Added()
    {
        // Don't run station events while Dynamic is active.
        // We'll take care of that.
        if (_stationEvents.Enabled)
        {
            _stationEventsWereEnabled = true;
            _stationEvents.Enabled = false;
        }

        // Calculate budget
        GenerateThreat();
        GenerateBudgets();
    }

    private void OnRoundStarted(RulePlayerJobsAssignedEvent ev)
    {
        if (!Enabled)
            return;

        RunRoundstartEvents(ev.Players);
    }

    public override void Removed()
    {
        _stationEvents.Enabled = _stationEventsWereEnabled;

        TotalEvents.Clear();
        ActiveEvents.Clear();
    }

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationEventSystem _stationEvents = default!;
}
