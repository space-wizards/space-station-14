using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Players;
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

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Connected && e.NewStatus != SessionStatus.Disconnected) return;

            var ev = new PlayerListChangedEvent();
            ev.PlayersInfo.Clear();
            foreach (var session in _playerManager.GetAllPlayers())
            {
                var name = session.Name;
                var username = session.AttachedEntity?.Name ?? string.Empty;
                var antag = session.ContentData()?.Mind?.AllRoles.Any(r => r.Antagonist) ?? false;
                var uid = session.AttachedEntity?.Uid ?? EntityUid.Invalid;

                ev.PlayersInfo.Add(new PlayerListChangedEvent.PlayerInfo(name, username, antag, uid, session));
            }

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(ev, admin.ConnectedClient);
            }
        }
    }
}
