using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents;
using Content.Shared.CCVar;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Dynamic.Systems;

/// <summary>
///     Its dynamic, baby!!!!
/// </summary>
public sealed partial class DynamicModeSystem : GameRuleSystem
{
    public override string Prototype => "Dynamic";

    // Cached so we can re-enable stuff if dynamic is disabled.
    private bool _stationEventsWereEnabled;
    private bool _storytellerWasVotable;

    private float _refundAccumulator;

    /// <summary>
    ///     Has dynamic actually started yet?
    /// </summary>
    public bool DynamicStarted { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRoundStarted);

        InitializeEvents();

        _proto.PrototypesReloaded += ReloadSchedulers;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _proto.PrototypesReloaded -= ReloadSchedulers;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!DynamicStarted) return;

        // Midround injection loop.
        ScheduleMidroundInjection(frameTime);

        _refundAccumulator += frameTime;

        // Check refunds every minute.
        if (_refundAccumulator > 60.0f)
        {
            CheckAvailableRefunds();
        }
    }

    public override void Started()
    {
        // Don't run station events while Dynamic is active.
        // We'll take care of that.
        if (_stationEvents.Enabled)
        {
            _stationEventsWereEnabled = true;
            _stationEvents.Enabled = false;
        }

        // Disable storyteller voting.
        if (_cfg.GetCVar(CCVars.VoteStorytellerEnabled))
        {
            _storytellerWasVotable = true;
            _cfg.SetCVar(CCVars.VoteStorytellerEnabled, false);
        }

        RebuildSchedulers();

        // Storyteller before budget, so that it can affect
        // the threat level
        PickStoryteller();

        // Calculate budget
        GenerateThreat();
        GenerateBudgets();
    }

    private void OnRoundStarted(RulePlayerJobsAssignedEvent ev)
    {
        if (!Enabled)
            return;

        DynamicStarted = true;
        RunRoundstartEvents(ev.Players);
    }

    public override void Ended()
    {
        _stationEvents.Enabled = _stationEventsWereEnabled;
        _cfg.SetCVar(CCVars.VoteStorytellerEnabled, _storytellerWasVotable);

        // Null the current storyteller at round end.
        // This means that if dynamic wasn't selected,
        // a vote from the previous round can select the storyteller for the next round,
        // which is ideal.
        CurrentStoryteller = null;

        TotalEvents.Clear();
        RefundableEvents.Clear();
        RoundstartBudget = 0;
        MidroundBudget = 0;
        ThreatLevel = 0;
    }

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationEventSystem _stationEvents = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
}
