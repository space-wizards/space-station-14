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
using Robust.Shared.Localization;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

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
            var senderPersonalChannel = senderSession.UserId == message.ChannelId;
            var senderAdmin = _adminManager.GetAdminData(senderSession) != null;
            var authorized = senderPersonalChannel || senderAdmin;
            if (!authorized)
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            var bwoinkText = senderAdmin
                ? $"[color=red]{senderSession.Name}[/color]: {escapedText}"
                : $"{senderSession.Name}: {escapedText}";
            var msg = new BwoinkTextMessage(message.ChannelId, senderSession.UserId, bwoinkText);

            LogBwoink(msg);

            // Admins
            var targets = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient).ToList();

            // And involved player
            if (_playerManager.TryGetSessionById(message.ChannelId, out var session))
                if (!targets.Contains(session.ConnectedClient))
                    targets.Add(session.ConnectedClient);

            foreach (var channel in targets)
                RaiseNetworkEvent(msg, channel);

            if (targets.Count == 1)
            {
                var systemText = senderPersonalChannel ?
                    Loc.GetString("bwoink-system-starmute-message-no-other-users-primary") :
                    Loc.GetString("bwoink-system-starmute-message-no-other-users-secondary");
                var starMuteMsg = new BwoinkTextMessage(message.ChannelId, SystemUserId, systemText);
                RaiseNetworkEvent(starMuteMsg, senderSession.ConnectedClient);
            }
        }
    }
}

