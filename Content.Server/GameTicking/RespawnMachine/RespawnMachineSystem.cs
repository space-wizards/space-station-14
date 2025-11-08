using System;
using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.RespawnMachine;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Content.Shared.Chat;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.RespawnMachine
{
    public sealed class RespawnMachineSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedJobSystem _jobSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var now = _timing.CurTime;

            var query = EntityQueryEnumerator<RespawnMachineComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var toRespawn = new List<NetUserId>();
                foreach (var kv in comp.Queue)
                {
                    if (now < kv.Value)
                        continue;

                    toRespawn.Add(kv.Key);
                }

                foreach (var user in toRespawn)
                {
                    comp.Queue.Remove(user);

                    if (!_playerManager.TryGetSessionById(user, out var session))
                        continue;

                    // Make the player join game and try to preserve the configured job for this machine.
                    var jobId = comp.Job?.Id;
                    _gameTicker.MakeJoinGame(session, EntityUid.Invalid, jobId, silent: true);
                }
            }
        }

        private void OnMobStateChanged(MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead)
                return;

            if (!TryComp<ActorComponent>(args.Target, out var actor))
                return;

            // Determine player's job
            string? playerJobId = null;
            if (actor.PlayerSession.GetMind() is { } mind && _jobSystem.MindTryGetJobId(mind, out var job))
                playerJobId = job?.Id;

            if (playerJobId == null)
                return;

            var query = AllEntityQuery<RespawnMachineComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (!comp.Enabled)
                    continue;

                if (comp.Job == null)
                    continue;

                // only accept players with matching job
                if (comp.Job != new ProtoId<JobPrototype>(playerJobId))
                    continue;

                // If player already queued or on tracker, skip
                var id = actor.PlayerSession.UserId;
                if (comp.Queue.ContainsKey(id))
                    continue;

                var respawnAt = _timing.CurTime + comp.RespawnDelay;
                comp.Queue[id] = respawnAt;

                // Inform the player via server message
                var seconds = comp.RespawnDelay.TotalSeconds;
                var msg = seconds > 0
                    ? Loc.GetString("respawn-machine-queued", ("seconds", seconds))
                    : Loc.GetString("respawn-machine-queued-immediate");

                var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
                _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrapped, uid, false, actor.PlayerSession.Channel, Color.LimeGreen);

                // Only queue at the first matching machine we find.
                return;
            }
        }
    }
}
