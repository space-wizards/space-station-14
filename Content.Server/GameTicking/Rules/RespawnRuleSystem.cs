using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles logic and interactions related to <see cref="RespawnDeadRuleComponent"/>
/// </summary>
public sealed class RespawnRuleSystem : GameRuleSystem<RespawnDeadRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnSuicide(SuicideEvent ev)
    {
        if (!TryComp<ActorComponent>(ev.Victim, out var actor))
           return;

        var query = EntityQueryEnumerator<RespawnTrackerComponent>();
        while (query.MoveNext(out _, out var respawn))
        {
            if (respawn.Players.Remove(actor.PlayerSession.UserId))
                QueueDel(ev.Victim);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        var query = EntityQueryEnumerator<RespawnDeadRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (RespawnPlayer(args.Target, uid, actor: actor))
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_station.GetStations().FirstOrNull() is not { } station)
            return;

        foreach (var tracker in EntityQuery<RespawnTrackerComponent>())
        {
            var queue = new Dictionary<NetUserId, TimeSpan>(tracker.RespawnQueue);
            foreach (var (player, time) in queue)
            {
                if (_timing.CurTime < time)
                    continue;

                if (!_playerManager.TryGetSessionById(player, out var session))
                    continue;

                if (session.GetMind() is { } mind && TryComp<MindComponent>(mind, out var mindComp) && mindComp.OwnedEntity.HasValue)
                    QueueDel(mindComp.OwnedEntity.Value);
                GameTicker.MakeJoinGame(session, station, silent: true);
                tracker.RespawnQueue.Remove(player);
            }
        }
    }

    /// <summary>
    /// Adds a given player to the respawn tracker, ensuring that they are respawned if they die.
    /// </summary>
    public void AddToTracker(EntityUid player, EntityUid tracker, RespawnTrackerComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(tracker, ref component) || !Resolve(player, ref actor, false))
            return;

        AddToTracker(actor.PlayerSession.UserId, tracker, component);
    }

    /// <summary>
    /// Adds a given player to the respawn tracker, ensuring that they are respawned if they die.
    /// </summary>
    public void AddToTracker(NetUserId id, EntityUid tracker, RespawnTrackerComponent? component = null)
    {
        if (!Resolve(tracker, ref component))
            return;

        component.Players.Add(id);
    }

    /// <summary>
    /// Attempts to directly respawn a player, skipping the lobby screen.
    /// </summary>
    public bool RespawnPlayer(EntityUid player, EntityUid respawnTracker, RespawnTrackerComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(respawnTracker, ref component) || !Resolve(player, ref actor, false))
            return false;

        if (!component.Players.Contains(actor.PlayerSession.UserId) || component.RespawnQueue.ContainsKey(actor.PlayerSession.UserId))
            return false;

        if (component.RespawnDelay == TimeSpan.Zero)
        {
            if (_station.GetStations().FirstOrNull() is not { } station)
                return false;

            QueueDel(player);
            GameTicker.MakeJoinGame(actor.PlayerSession, station, silent: true);
            return false;
        }

        var msg = Loc.GetString("rule-respawn-in-seconds", ("second", component.RespawnDelay.TotalSeconds));
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, respawnTracker, false, actor.PlayerSession.Channel, Color.LimeGreen);
        component.RespawnQueue[actor.PlayerSession.UserId] = _timing.CurTime + component.RespawnDelay;
        return true;
    }
}
