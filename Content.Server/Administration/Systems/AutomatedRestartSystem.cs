using Content.Server.Communications;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.Administration.Systems;

/// <summary>
/// This handles automatically initiating round restart in various unrecoverable situations.
/// </summary>
public sealed class AutomatedRestartSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly MindTrackerSystem _mindTrackerSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private bool _autoRestartBlocked;
    private bool _callOnCommsDown;
    private float _percentDeadToCall;
    private float _percentDeadToRestart;
    private int _minPlayers;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CommunicationsConsoleComponent, PowerChangedEvent>(OnCommsPowerChanged);
        SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentShutdown>(OnCommsShutdown);

        _configurationManager.OnValueChanged(CCVars.GameAutoRestartCallOnPercentDead, v => _percentDeadToCall = v, true);
        _configurationManager.OnValueChanged(CCVars.GameAutoRestartRestartOnPercentDead, v => _percentDeadToRestart = v, true);
        _configurationManager.OnValueChanged(CCVars.GameAutoRestartMinPlayers, v => _minPlayers = v, true);
        _configurationManager.OnValueChanged(CCVars.GameAutoRestartCallOnCommsDown, v => _callOnCommsDown = v, true);
    }

    public bool Enabled()
    {
        return !_autoRestartBlocked && _playerManager.PlayerCount > _minPlayers;
    }

    private void OnCommsShutdown(EntityUid uid, CommunicationsConsoleComponent component, ComponentShutdown args)
    {
        Logger.Debug($"{TallyWorkingCommsConsoles()}");
        if (TallyWorkingCommsConsoles() == 0 && Enabled() && _callOnCommsDown && _gameTicker.RunLevel == GameRunLevel.InRound)
        {
            _roundEndSystem.RequestRoundEnd(_roundEndSystem.DefaultCountdownDuration, null, false);
            Logger.Debug("Called...");
        }
    }

    private void OnCommsPowerChanged(EntityUid uid, CommunicationsConsoleComponent component, PowerChangedEvent args)
    {
        if (TallyWorkingCommsConsoles() == 0 && Enabled() && _callOnCommsDown && _gameTicker.RunLevel == GameRunLevel.InRound)
        {
            _roundEndSystem.RequestRoundEnd(_roundEndSystem.DefaultCountdownDuration, null, false);
        }
    }

    private int TallyWorkingCommsConsoles()
    {
        var total = 0;
        foreach (var (_, power) in EntityQuery<CommunicationsConsoleComponent, ApcPowerReceiverComponent>())
        {
            if (power.Powered && !Terminating(power.Owner))
                total += 1;
        }

        return total;
    }

    public override void Update(float frameTime)
    {
        if (!Enabled() || _gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var totalOriginals = _mindTrackerSystem.OriginalMind.Count;
        var totalDead = 0;
        foreach (var (_, mind) in _mindTrackerSystem.OriginalMind)
        {
            if (mind.CharacterDeadPhysically && mind.CurrentEntity != null)
                totalDead += 1;
        }

        if (totalOriginals == 0 || totalDead == 0)
            return;

        // This won't call unless there's at least 14 players by default, because at that point there's so few people it's usually better to simply end-round if this many die.
        if ((float) totalDead / totalOriginals >= _percentDeadToCall
            && totalOriginals * (1 - _percentDeadToCall) / 2 > 1
            && _roundEndSystem.CanCall())
        {
            _roundEndSystem.RequestRoundEnd(_roundEndSystem.DefaultCountdownDuration, null, false, true);
        }

        // Below 20 players, this won't end the round unless absolutely everyone is dead, by default at least.
        if ((float) totalDead / totalOriginals >= _percentDeadToRestart)
        {
            _roundEndSystem.EndRound();
        }
    }
}
