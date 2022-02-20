using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Administration
{
    public sealed class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        private readonly Dictionary<NetUserId, PlayerInfo> _playerList = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoleAddedEvent>(OnRoleEvent);
            SubscribeLocalEvent<RoleRemovedEvent>(OnRoleEvent);
        }

        public void UpdatePlayerList(IPlayerSession player)
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

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _adminManager.OnPermsChanged -= OnAdminPermsChanged;
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
            var name = session.Name;
            var username = string.Empty;

            if (session.AttachedEntity != null)
                username = EntityManager.GetComponent<MetaDataComponent>(session.AttachedEntity.Value).EntityName;

            var mind = session.ContentData()?.Mind;

            var job = mind?.AllRoles.FirstOrDefault(role => role is Job);
            var startingRole = job != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.Name) : string.Empty;

            var antag = mind?.AllRoles.Any(r => r.Antagonist) ?? false;

            var connected = session.Status is SessionStatus.Connected or SessionStatus.InGame;

            return new PlayerInfo(name, username, startingRole, antag, session.AttachedEntity.GetValueOrDefault(), session.UserId,
                connected);
        }
    }
}
