using Content.Server.Chat.Managers;
using Content.Server.Database.Migrations.Postgres;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
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
    [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_station.GetStations().FirstOrNull() is not { } station)
            return;

        foreach (var tracker in EntityQuery<RespawnTrackerComponent>())
        {
            foreach (var (player, time) in tracker.RespawnQueue)
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
        if (args.NewMobState != MobState.Dead)
            return;

        ActorComponent? actor = null;
        HumanoidAppearanceComponent? appearance = null;
        if (!Resolve(args.Target, ref actor, ref appearance, logMissing: false))
            return;

        var query = EntityQueryEnumerator<RespawnDeadRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var respawnRule, out  var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (respawnRule.AlwaysRespawnDead)
                AddToTracker(actor.PlayerSession.UserId, (uid, tracker));
            if (RespawnPlayer((args.Target, actor, appearance), (uid, tracker)))
                break;
        }
    }

    /// <summary>
    /// Attempts to directly respawn a player, skipping the lobby screen.
    /// </summary>
    public bool RespawnPlayer(Entity<ActorComponent, HumanoidAppearanceComponent> player, Entity<RespawnTrackerComponent> respawnTracker)
    {
        if (!respawnTracker.Comp.Players.Contains(player.Comp1.PlayerSession.UserId) || respawnTracker.Comp.RespawnQueue.ContainsKey(player.Comp1.PlayerSession.UserId))
            return false;

        if (respawnTracker.Comp.RespawnDelay == TimeSpan.Zero)
        {
            if (_station.GetStations().FirstOrNull() is not { } station)
                return false;
            // Respawn the player using the profile that the original character was created with
            var profile = _appearance.GetBaseProfile((player, player.Comp2));
            if (profile is null)
                return false;
            if (respawnTracker.Comp.DeleteBody)
                QueueDel(player);
            GameTicker.MakeJoinGame(player.Comp1.PlayerSession, profile, station, silent: true);
            return false;
        }

        var msg = Loc.GetString("rule-respawn-in-seconds", ("second", respawnTracker.Comp.RespawnDelay.TotalSeconds));
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, respawnTracker, false, player.Comp1.PlayerSession.Channel, Color.LimeGreen);

        respawnTracker.Comp.RespawnQueue[player.Comp1.PlayerSession.UserId] = _timing.CurTime + respawnTracker.Comp.RespawnDelay;

        return true;
    }

    /// <summary>
    /// Adds a given player to the respawn tracker, ensuring that they are respawned if they die.
    /// </summary>
    public void AddToTracker(Entity<ActorComponent?> player, Entity<RespawnTrackerComponent?> respawnTracker)
    {
        if (!Resolve(respawnTracker, ref respawnTracker.Comp) || !Resolve(player, ref player.Comp, false))
            return;

        AddToTracker(player.Comp.PlayerSession.UserId, (respawnTracker, respawnTracker.Comp));
    }

    /// <summary>
    /// Adds a given player to the respawn tracker, ensuring that they are respawned if they die.
    /// </summary>
    public void AddToTracker(NetUserId id, Entity<RespawnTrackerComponent> tracker)
    {
        tracker.Comp.Players.Add(id);
    }
}
