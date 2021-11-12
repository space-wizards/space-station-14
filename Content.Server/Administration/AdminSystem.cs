using System;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Administration
{
    public class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        [Dependency] private readonly AdminLogSystem _logs = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeNetworkEvent<RequestLogsMessage>(OnLogsMessageRequest);
        }

        private void OnAdminPermsChanged(AdminPermsChangedEventArgs obj)
        {
            if(!obj.IsAdmin) return;
            SendFullPlayerList(obj.Player);
        }

        private void OnPlayerDetached(PlayerDetachedEvent ev)
        {
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(GetChangedEvent(ev.Player), admin.ConnectedClient);
            }
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(GetChangedEvent(ev.Player), admin.ConnectedClient);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _adminManager.OnPermsChanged -= OnAdminPermsChanged;
        }

        private PlayerInfoChangedEvent GetChangedEvent(IPlayerSession session)
        {
            return new()
            {
                PlayerInfo = GetPlayerInfo(session),
            };
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            EntityEventArgs? args = null;
            switch (e.NewStatus)
            {
                case SessionStatus.InGame:
                case SessionStatus.Connected:
                    args = GetChangedEvent(e.Session);
                    break;
                case SessionStatus.Disconnected:
                    args = new PlayerInfoRemovalMessage {NetUserId = e.Session.UserId};
                    break;
            }

            if(args == null) return;

            foreach (var admin in _adminManager.AllAdmins)
            {
                RaiseNetworkEvent(args, admin.ConnectedClient);
            }
        }

        private void SendFullPlayerList(IPlayerSession playerSession)
        {
            var ev = new FullPlayerListEvent();
            ev.PlayersInfo.Clear();
            foreach (var session in _playerManager.GetAllPlayers())
            {
                ev.PlayersInfo.Add(GetPlayerInfo(session));
            }

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        private PlayerInfo GetPlayerInfo(IPlayerSession session)
        {
            var name = session.Name;
            var username = session.AttachedEntity?.Name ?? string.Empty;
            var antag = session.ContentData()?.Mind?.AllRoles.Any(r => r.Antagonist) ?? false;
            var uid = session.AttachedEntity?.Uid ?? EntityUid.Invalid;

            return new PlayerInfo(name, username, antag, uid, session.UserId);
        }

        private void OnLogsMessageRequest(RequestLogsMessage msg, EntitySessionEventArgs args)
        {
            if (!_adminManager.HasAdminFlag((IPlayerSession) args.SenderSession, AdminFlags.Logs))
            {
                Logger.Warning($"Player {args.SenderSession.Name} tried to access admin logs without permission.");
                return;
            }

            var logs = new LogsMessage(_logs.MessagesCurrentRound().ToList());
            RaiseNetworkEvent(logs);
        }
    }
}
