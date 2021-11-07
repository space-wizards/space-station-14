using System;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    public class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            EntityEventArgs? args = null;
            switch (e.NewStatus)
            {
                case SessionStatus.InGame:
                case SessionStatus.Connected:
                    args = new PlayerInfoChangedEvent
                    {
                        PlayerInfo = new PlayerInfo(
                            e.Session.Name, e.Session.AttachedEntity?.Name ?? string.Empty,
                            e.Session.ContentData()?.Mind?.AllRoles.Any(r => r.Antagonist) ?? false,
                            e.Session.AttachedEntity?.Uid ?? EntityUid.Invalid,
                            e.Session.UserId),
                    };
                    break;
                case SessionStatus.Disconnected:
                    args = new PlayerInfoRemovalMessage {NetUserId = e.Session.UserId};
                    break;
            }

            if(args == null) return;

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(args, admin.ConnectedClient);
            }

            if (e.NewStatus != SessionStatus.Disconnected && _adminManager.IsAdmin(e.Session))
            {
                SendFullPlayerList(e.Session);
            }
        }

        private void SendFullPlayerList(IPlayerSession playerSession)
        {
            var ev = new FullPlayerListEvent();
            ev.PlayersInfo.Clear();
            foreach (var session in _playerManager.GetAllPlayers())
            {
                var name = session.Name;
                var username = session.AttachedEntity?.Name ?? string.Empty;
                var antag = session.ContentData()?.Mind?.AllRoles.Any(r => r.Antagonist) ?? false;
                var uid = session.AttachedEntity?.Uid ?? EntityUid.Invalid;

                ev.PlayersInfo.Add(new PlayerInfo(name, username, antag, uid, session.UserId));
            }

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }
    }
}
