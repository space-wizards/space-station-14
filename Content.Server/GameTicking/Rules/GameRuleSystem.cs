using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.CCCCVars;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] protected readonly IChatManager ChatManager = default!;
    [Dependency] protected readonly GameTicker GameTicker = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    // DS14-start
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    // DS14-end

    // Not protected, just to be used in utility methods
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<T, GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<T, GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<T, GameRuleEndedEvent>(OnGameRuleEnded);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
        // DS14-start
        SubscribeLocalEvent<RoundEndDiscordTextAppendEvent>(OnRoundEndDiscordTextAppend);
        SubscribeLocalEvent<T, CollectGameRuleAdminStatusEvent>(OnCollectAdminStatus);
        // DS14-end
    }

    private void OnStartAttempt(RoundStartAttemptEvent args)
    {
        if (args.Forced || args.Cancelled)
            return;

        var useTotalPlayers = _cfg.GetCVar(CCCCVars.GameModesUseTotalPlayers); // DS14

        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            var minPlayers = gameRule.MinPlayers;
            var name = ToPrettyString(uid);

            int playerCount = useTotalPlayers ? _playerManager.PlayerCount : args.Players.Length; // DS14

            if (playerCount >= minPlayers) // DS14-edit
                continue;

            if (gameRule.CancelPresetOnTooFewPlayers)
            {
                // DS14-edit-start
                if (useTotalPlayers)
                {
                    ChatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-current-players",
                        ("currentPlayers", playerCount),
                        ("minimumPlayers", minPlayers),
                        ("presetName", name)));
                }
                else
                {
                    ChatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-ready-players",
                        ("readyPlayersCount", playerCount),
                        ("minimumPlayers", minPlayers),
                        ("presetName", name)));
                }
                // DS14-edit-end
                args.Cancel();
                //TODO remove this once announcements are logged
                Log.Info($"Rule '{name}' requires {minPlayers} players, but only {args.Players.Length} are ready.");
            }
            else
            {
                ForceEndSelf(uid, gameRule);
            }
        }
    }

    private void OnGameRuleAdded(EntityUid uid, T component, ref GameRuleAddedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Added(uid, component, ruleData, args);
    }

    private void OnGameRuleStarted(EntityUid uid, T component, ref GameRuleStartedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Started(uid, component, ruleData, args);
    }

    private void OnGameRuleEnded(EntityUid uid, T component, ref GameRuleEndedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Ended(uid, component, ruleData, args);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var query = AllEntityQuery<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<GameRuleComponent>(uid, out var ruleData))
                continue;

            AppendRoundEndText(uid, comp, ruleData, ref ev);
        }
    }

    // DS14-start
    private void OnRoundEndDiscordTextAppend(RoundEndDiscordTextAppendEvent ev)
    {
        var query = AllEntityQuery<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<GameRuleComponent>(uid, out var ruleData))
                continue;

            AppendRoundEndDiscordText(uid, comp, ruleData, ref ev);
        }
    }
    // DS14-end

    // DS14-start
    private void OnCollectAdminStatus(EntityUid uid, T component, CollectGameRuleAdminStatusEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;

        AppendAdminStatus(uid, component, ruleData, args);
    }
    // DS14-end

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    protected virtual void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule ends
    /// </summary>
    protected virtual void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {

    }

    /// <summary>
    /// Called at the end of a round when text needs to be added for a game rule.
    /// </summary>
    protected virtual void AppendRoundEndText(EntityUid uid, T component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {

    }

    // DS14-start
    /// <summary>
    /// Called at the end of a round when Discord-only log text needs to be added for a game rule.
    /// </summary>
    protected virtual void AppendRoundEndDiscordText(EntityUid uid, T component, GameRuleComponent gameRule, ref RoundEndDiscordTextAppendEvent args)
    {

    }

    /// <summary>
    /// Adds a read-only section to the centralized periodic admin status report.
    /// </summary>
    protected virtual void AppendAdminStatus(EntityUid uid,
        T component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {

    }
    // DS14-end

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // DS14-start
        if (GameTicker.RunLevel == GameRunLevel.PostRound)
            return;
        // DS14-end

        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp2))
                continue;

            ActiveTick(uid, comp1, comp2, frameTime);
        }
    }
}
