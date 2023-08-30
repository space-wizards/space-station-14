using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages <see cref="DeathMatchRuleComponent"/>
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem<DeathMatchRuleComponent>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageChangedEvent>(OnHealthChanged);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    protected override void Started(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-death-match-added-announcement"));

    }

    protected override void Ended(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        component.DeadCheckTimer = null;
        component.RestartTimer = null;

    }

    private void OnHealthChanged(DamageChangedEvent _)
    {
        RunDelayedCheck();
    }

    private void OnPlayerStatusChanged(object? ojb, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            RunDelayedCheck();
        }
    }

    private void RunDelayedCheck()
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var deathMatch, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule) || deathMatch.DeadCheckTimer != null)
                continue;

            deathMatch.DeadCheckTimer = deathMatch.DeadCheckDelay;
        }
    }

    protected override void ActiveTick(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        // If the restart timer is active, that means the round is ending soon, no need to check for winners.
        // TODO: We probably want a sane, centralized round end thingie in GameTicker, RoundEndSystem is no good...
        if (component.RestartTimer != null)
        {
            component.RestartTimer -= frameTime;

            if (component.RestartTimer > 0f)
                return;

            GameTicker.EndRound();
            GameTicker.RestartRound();
            return;
        }

        if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin) || component.DeadCheckTimer == null)
            return;

        component.DeadCheckTimer -= frameTime;

        if (component.DeadCheckTimer > 0)
            return;

        component.DeadCheckTimer = null;

        IPlayerSession? winner = null;
        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not { Valid: true } playerEntity
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
            : Loc.GetString("rule-death-match-check-winner", ("winner", winner)));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",
            ("seconds", component.RestartDelay)));
        component.RestartTimer = component.RestartDelay;
    }
}
