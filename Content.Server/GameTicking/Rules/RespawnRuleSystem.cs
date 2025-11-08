using Content.Server.Chat.Managers;
using Content.Server.Database.Migrations.Postgres;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Spawners.Components;
using Content.Shared.Roles.Jobs;

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
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;

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

                // Try to preserve the job when respawning so that job-specific spawn points are respected.
                string? jobId = null;
                if (session.GetMind() is { } sessMind && _jobSystem.MindTryGetJobId(sessMind, out var job))
                    jobId = job?.Id;

                GameTicker.MakeJoinGame(session, station, jobId, silent: true);
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

        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        var query = EntityQueryEnumerator<RespawnDeadRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var respawnRule, out  var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (respawnRule.AlwaysRespawnDead)
                AddToTracker(actor.PlayerSession.UserId, (uid, tracker));
            if (RespawnPlayer((args.Target, actor), (uid, tracker)))
                break;
        }
    }

    /// <summary>
    /// Attempts to directly respawn a player, skipping the lobby screen.
    /// </summary>
    public bool RespawnPlayer(Entity<ActorComponent> player, Entity<RespawnTrackerComponent> respawnTracker)
    {
        if (!respawnTracker.Comp.Players.Contains(player.Comp.PlayerSession.UserId) || respawnTracker.Comp.RespawnQueue.ContainsKey(player.Comp.PlayerSession.UserId))
            return false;

        // Determine effective respawn delay. Prefer any per-spawn-point delay configured for the player's job.
        var effectiveDelay = respawnTracker.Comp.RespawnDelay;
        if (effectiveDelay == TimeSpan.Zero)
        {
            // attempt to find a per-job spawnpoint with a respawn delay
            if (player.Comp.PlayerSession.GetMind() is { } mind && _jobSystem.MindTryGetJobId(mind, out var jobProto))
            {
                var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
                TimeSpan best = TimeSpan.Zero;
                while (query.MoveNext(out _, out var sp, out var _))
                {
                    if (sp.SpawnType != SpawnPointType.Job)
                        continue;

                    if (sp.Job != jobProto)
                        continue;

                    if (sp.RespawnDelay != TimeSpan.Zero && (best == TimeSpan.Zero || sp.RespawnDelay < best))
                        best = sp.RespawnDelay;
                }

                if (best != TimeSpan.Zero)
                    effectiveDelay = best;
            }
        }

            if (effectiveDelay == TimeSpan.Zero)
            {
                if (_station.GetStations().FirstOrNull() is not { } station)
                    return false;

                if (respawnTracker.Comp.DeleteBody)
                    QueueDel(player);
                // preserve job on immediate respawn if possible
                string? jobId = null;
                if (player.Comp.PlayerSession.GetMind() is { } mind && _jobSystem.MindTryGetJobId(mind, out var job))
                    jobId = job?.Id;

                GameTicker.MakeJoinGame(player.Comp.PlayerSession, station, jobId, silent: true);
                return false;
            }

        var msg = Loc.GetString("rule-respawn-in-seconds", ("second", effectiveDelay.TotalSeconds));
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, respawnTracker, false, player.Comp.PlayerSession.Channel, Color.LimeGreen);

        respawnTracker.Comp.RespawnQueue[player.Comp.PlayerSession.UserId] = _timing.CurTime + effectiveDelay;

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
