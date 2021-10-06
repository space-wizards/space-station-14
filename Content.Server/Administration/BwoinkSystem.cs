#nullable enable
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    [UsedImplicitly]
    public class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = (IPlayerSession) eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            if ((senderSession.UserId != message.ChannelId) && (_adminManager.GetAdminData(senderSession) == null))
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var msg = new BwoinkTextMessage(message.ChannelId, senderSession.UserId, $"{senderSession.Name}: {message.Text}");

            LogBwoink(msg);

            var targets = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            // Admins
            foreach (var channel in targets)
                RaiseNetworkEvent(msg, channel);

            // And involved player
            if (_playerManager.TryGetSessionById(message.ChannelId, out var session))
                if (!targets.Contains(session.ConnectedClient))
                    RaiseNetworkEvent(msg, session.ConnectedClient);
        }
    }
}

