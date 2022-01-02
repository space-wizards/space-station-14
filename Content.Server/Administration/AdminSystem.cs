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

        private void UpdatePlayerList(IPlayerSession player)
        {
            _playerList[player.UserId] = GetPlayerInfo(player);

            var playerInfoChangedEvent = new PlayerInfoChangedEvent
            {
                PlayerInfo = _playerList[player.UserId]
            };

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(playerInfoChangedEvent, admin.ConnectedClient);
            }
        }

        private void OnDisconnect(object? sender, NetDisconnectedArgs args)
        {
            _disconnections[args.Channel.UserId] = new PlayerDisconnect(DateTime.Now, args.Reason);
        }

        private void OnRoleEvent(RoleEvent ev)
        {
            if (!ev.Role.Antagonist || ev.Role.Mind.Session == null)
                return;

            UpdatePlayerList(ev.Role.Mind.Session);
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

            UpdatePlayerList(ev.Player);
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            UpdatePlayerList(ev.Player);
        }

        private void OnMobState(MobStateChangedEvent ev)
        {
            if (TryComp<MindComponent>(ev.Entity, out var mind)
                && mind.Mind is not null
                && mind.Mind.TryGetSession(out var session))
                    UpdatePlayerList(session);
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
            UpdatePlayerList(e.Session);
        }

        private void SendFullPlayerList(IPlayerSession playerSession)
        {
            var ev = new FullPlayerListEvent();

            ev.PlayersInfo = _playerList.Values.ToList();

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        private PlayerInfo GetPlayerInfo(IPlayerSession session)
        {
            var username = session.Name;
            var name = string.Empty;
            MobStateFlags msf = MobStateFlags.Unknown;

            if (session.AttachedEntity != null)
            {
                name = Comp<MetaDataComponent>(session.AttachedEntity.Value).EntityName;
                if (TryComp<MobStateComponent>(session.AttachedEntity.Value, out var mobState))
                    msf = mobState.CurrentState?.ToFlags() ?? msf;
            }

            var mind = session.ContentData()?.Mind;
            var antag = mind?.AllRoles.Any(r => r.Antagonist) ?? false;
            var roles = mind?.AllRoles.Select(r => r.Name).ToArray() ?? Array.Empty<string>();

            return new PlayerInfo
            (
                Username: username,
                SessionId: session.UserId,
                Ping: session.Ping,
                EntityUid: session.AttachedEntity.GetValueOrDefault(),
                Connected: session.Status,
                Disconnected: _disconnections.GetValueOrDefault(session.UserId),
                Afk: _afkManager.IsAfk(session),

                CharacterName: name,
                Antag: antag,
                Roles: roles,
                DeadIC: mind?.CharacterDeadIC ?? true,
                DeadPhysically: mind?.CharacterDeadPhysically ?? true,
                TimeOfDeath: mind?.TimeOfDeath,
                MobState: msf
            );
        }
    }
}
