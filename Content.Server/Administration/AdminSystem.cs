using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Server.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Administration
{
    public class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IAfkManager _afkManager = default!;

        private readonly Dictionary<NetUserId, PlayerInfo> _playerList = new();
        private readonly Dictionary<NetUserId, PlayerDisconnect> _disconnections = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;
            _netManager.Disconnect += OnDisconnect;
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoleAddedEvent>(OnRoleEvent);
            SubscribeLocalEvent<RoleRemovedEvent>(OnRoleEvent);
            SubscribeLocalEvent<MobStateChangedEvent>(OnMobState);
        }

        private void UpdatePlayerList(NetUserId uid, IPlayerSession? player = null, Mind.Mind? mind = null)
        {
            _playerList[uid] = GetPlayerInfo(uid, player, mind);

            var playerInfoChangedEvent = new PlayerInfoChangedEvent
            {
                PlayerInfo = _playerList[uid]
            };

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(playerInfoChangedEvent, admin.ConnectedClient);
            }
        }

        private void OnDisconnect(object? sender, NetDisconnectedArgs args)
        {
            _disconnections[args.Channel.UserId] = new PlayerDisconnect(DateTime.Now, args.Reason);
            UpdatePlayerList(args.Channel.UserId);
        }

        private void OnRoleEvent(RoleEvent ev)
        {
            if (!ev.Role.Antagonist || ev.Role.Mind.Session == null)
                return;

            UpdatePlayerList(ev.Role.Mind.OriginalOwnerUserId, ev.Role.Mind.Session, ev.Role.Mind);
        }

        private void OnAdminPermsChanged(AdminPermsChangedEventArgs obj)
        {
            if(!obj.IsAdmin)
            {
                RaiseNetworkEvent(new FullPlayerListEvent(), obj.Player.ConnectedClient);
                return;
            }

            SendFullPlayerList(obj.Player);
        }

        private void OnPlayerDetached(PlayerDetachedEvent ev)
        {
            // If disconnected then the player won't have a connected entity to get character name from.
            // The disconnected state gets sent by OnPlayerStatusChanged.
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            UpdatePlayerList(ev.Player.UserId, ev.Player);
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            UpdatePlayerList(ev.Player.UserId, ev.Player);
        }

        private void OnMobState(MobStateChangedEvent ev)
        {
            if (TryComp<MindComponent>(ev.Entity, out var mind)
                && mind.Mind is not null)
            {
                UpdatePlayerList(mind.Mind.OriginalOwnerUserId, mind.Mind.Session, mind.Mind);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _adminManager.OnPermsChanged -= OnAdminPermsChanged;
            _netManager.Disconnect -= OnDisconnect;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            UpdatePlayerList(e.Session.UserId, e.Session);
        }

        private void SendFullPlayerList(IPlayerSession playerSession)
        {
            var ev = new FullPlayerListEvent();

            ev.PlayersInfo = _playerList.Values.ToList();

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        private PlayerInfo GetPlayerInfo(NetUserId uid, IPlayerSession? session = null, Mind.Mind? mind = null)
        {
            var last = _playerList.GetValueOrDefault(uid) ?? new PlayerInfo();

            mind ??= session?.ContentData()?.Mind;

            last.SessionId = uid;

            var ent = mind?.OwnedEntity
                ?? session?.AttachedEntity
                ?? null;

            if (ent is not null && !Deleted(ent))
            {
                last.EntityUid = ent.Value;
                if (TryComp<MetaDataComponent>(ent, out var meta))
                    last!.CharacterName = meta.EntityName;

                if (TryComp<MobStateComponent>(ent, out var mobState))
                    last!.MobState = mobState.CurrentState!.ToFlags();
            }

            if (mind is not null)
            {
                last.Antag = mind.AllRoles.Any(r => r.Antagonist);
                last.Roles = mind.AllRoles.Select(r => r.Name).ToArray();
                last.DeadIC = mind.CharacterDeadIC;
                last.DeadPhysically = mind.CharacterDeadPhysically;
                last.TimeOfDeath = mind.TimeOfDeath;
            }

            var dc = _disconnections.GetValueOrDefault(uid);
            if (dc is not null)
                last.Disconnected=dc;

            if (session is not null)
            {
                last = last with
                {
                    Username=session.Name,
                    Ping=session.Ping,
                    Connected=session.Status,
                    Afk=_afkManager.IsAfk(session),
                };
            }

            return last;
        }
    }
}
