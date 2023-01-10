using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking.Rules;

/// <summary>
///     Simple GameRule that will do a free-for-all death match.
///     Kill everybody else to win.
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem
{
    public override string Prototype => "DeathMatch";

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private const float RestartDelay = 10f;
    private const float DeadCheckDelay = 5f;

    private float? _deadCheckTimer = null;
    private float? _restartTimer = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageChangedEvent>(OnHealthChanged);
    }

    public override void Started()
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-death-match-added-announcement"));

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Ended()
    {
        _deadCheckTimer = null;
        _restartTimer = null;

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnHealthChanged(DamageChangedEvent _)
    {
        RunDelayedCheck();
    }

    private void OnPlayerStatusChanged(object? _, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            RunDelayedCheck();
        }
    }

    private void RunDelayedCheck()
    {
        if (!RuleAdded || _deadCheckTimer != null)
            return;

        _deadCheckTimer = DeadCheckDelay;
    }

    public override void Update(float frameTime)
    {
        if (!RuleAdded)
            return;

        // If the restart timer is active, that means the round is ending soon, no need to check for winners.
        // TODO: We probably want a sane, centralized round end thingie in GameTicker, RoundEndSystem is no good...
        if (_restartTimer != null)
        {
            _restartTimer -= frameTime;

            if (_restartTimer > 0f)
                return;

            GameTicker.EndRound();
            GameTicker.RestartRound();
            return;
        }

        if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin) || _deadCheckTimer == null)
            return;

        _deadCheckTimer -= frameTime;

        if (_deadCheckTimer > 0)
            return;

        _deadCheckTimer = null;

        IPlayerSession? winner = null;
        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not {Valid: true} playerEntity
                || !TryComp(playerEntity, out MobStateComponent? state))
                continue;

            if (!_mobStateSystem.IsAlive(playerEntity, state))
                continue;

            // Found a second person alive, nothing decided yet!
            if (winner != null)
                return;

            winner = playerSession;
        }

        _chatManager.DispatchServerAnnouncement(winner == null
            ? Loc.GetString("rule-death-match-check-winner-stalemate")
            : Loc.GetString("rule-death-match-check-winner",("winner", winner)));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", RestartDelay)));
        _restartTimer = RestartDelay;
    }
}
