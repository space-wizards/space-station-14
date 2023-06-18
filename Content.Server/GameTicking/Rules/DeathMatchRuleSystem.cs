using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages <see cref="DeathMatchRuleComponent"/>
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem<DeathMatchRuleComponent>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private ISawmill _sawmill = default!;
    int _deathMatchStartingBalance;
    bool _gameLobbyEnableWin;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageChangedEvent>(OnHealthChanged);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        _cfg.OnValueChanged(CCVars.GameLobbyEnableWin, SetGameLobbyEnableWin, true);
        _cfg.OnValueChanged(CCVars.TraitorDeathMatchStartingBalance, SetDeathMatchStartingBalance, true);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void SetGameLobbyEnableWin(bool value) => _gameLobbyEnableWin = value;

    private void SetDeathMatchStartingBalance(int value) => _deathMatchStartingBalance = value;

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.GameLobbyEnableWin, SetGameLobbyEnableWin);
        _cfg.UnsubValueChanged(CCVars.TraitorDeathMatchStartingBalance, SetDeathMatchStartingBalance);
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

        if (component.SelectionStatus == DeathMatchRuleComponent.SelectionState.ReadyToSelect)
        {
            foreach (var playerSession in _playerManager.ServerSessions)
                AddUplink(playerSession);

            component.SelectionStatus = DeathMatchRuleComponent.SelectionState.SelectionMade;
        }

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

        if (!_gameLobbyEnableWin || component.DeadCheckTimer == null)
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

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dmPlayer, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            dmPlayer.SelectionStatus = DeathMatchRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, GameRuleComponent>();

        // checking the component so as not to issue uplinks in other modes
        while (query.MoveNext(out var uid, out var dmPlayer, out var gameRule))
        {
            // since the DM mode allows players to join after the start of the round,
            // they also need to be given funds to fight
            if (ev.LateJoin)
                AddUplink(ev.Player);
        }
    }

    public void AddUplink(IPlayerSession session)
    {
        if (session?.AttachedEntity is not { } user) { return; }
        Logger.Debug(_entityManager.ToPrettyString(user));
        if (!_uplink.AddUplink(user, _deathMatchStartingBalance)) { }
    }
}
