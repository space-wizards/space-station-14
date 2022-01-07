#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration
{
    [UsedImplicitly]
    public partial class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        private ISawmill _sawmill = default!;
        private readonly Dictionary<NetUserId, List<BwoinkTextMessage>> _history = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<FetchBwoinkLogMessage>(OnFetchLogRequest);
            SubscribeNetworkEvent<BwoinkReadMessage>(OnReadMessage);
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");
            InitializeWebhook();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownWebhook();
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = (IPlayerSession) eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            if (!IsAuthorizedForChannel(senderSession, message.ChannelId))
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            var bwoinkText = _adminManager.GetAdminData(senderSession)?.Flags switch
            {
                AdminFlags.Adminhelp => $"[color=purple]{senderSession.Name}[/color]: {escapedText}",
                > AdminFlags.Adminhelp => $"[color=red]{senderSession.Name}[/color]: {escapedText}",
                _ => $"{senderSession.Name}: {escapedText}",
            };

            var msg = new BwoinkTextMessage(message.ChannelId, senderSession.UserId, bwoinkText);

            LogBwoink(msg);
            if (!_history.ContainsKey(message.ChannelId))
                _history[message.ChannelId] = new();

            _history[message.ChannelId].Add(msg);

            // Admins
            var targets = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient).ToList();

            // And involved player
            if (_playerManager.TryGetSessionById(message.ChannelId, out var session))
                if (!targets.Contains(session.ConnectedClient))
                    targets.Add(session.ConnectedClient);

            foreach (var channel in targets)
                RaiseNetworkEvent(msg, channel);

            var noReceivers = targets.Count == 1;

            var sendsWebhook = _webhookUrl != string.Empty;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.ChannelId))
                    _messageQueues[msg.ChannelId] = 0;

                var str = message.Text;
                var unameLength = senderSession.Name.Length;

                if (unameLength+str.Length+_maxAdditionalChars > MessageMax)
                {
                    str = str[..(MessageMax - _maxAdditionalChars - unameLength)];
                }
            }

            if (noReceivers)
            {
                var systemText = sendsWebhook ?
                    Loc.GetString("bwoink-system-starmute-message-no-other-users-webhook") :
                    Loc.GetString("bwoink-system-starmute-message-no-other-users");
                var starMuteMsg = new BwoinkTextMessage(message.ChannelId, SystemUserId, systemText);
                RaiseNetworkEvent(starMuteMsg, senderSession.ConnectedClient);
            } else {
                message.Status = SharedBwoinkSystem.Status.Delivered;
            }
        }

        private bool IsAuthorizedForChannel(IPlayerSession session, NetUserId channelId)
        {
            var personalChannel = session.UserId == channelId;
            var senderAdmin = _adminManager.GetAdminData(session);
            return personalChannel || senderAdmin != null;
        }

        private void OnFetchLogRequest(FetchBwoinkLogMessage message, EntitySessionEventArgs eventArgs)
        {
            if (!IsAuthorizedForChannel((IPlayerSession) eventArgs.SenderSession, message.ChannelId))
                return;

            if (!_history.ContainsKey(message.ChannelId))
                return;

            foreach (var m in _history[message.ChannelId])
                RaiseNetworkEvent(m, eventArgs.SenderSession.ConnectedClient);
        }

        private void OnReadMessage(BwoinkReadMessage message, EntitySessionEventArgs eventArgs)
        {
            if (!IsAuthorizedForChannel((IPlayerSession) eventArgs.SenderSession, message.ChannelId))
                return;

            if (!_history.ContainsKey(message.ChannelId))
                return;

            foreach (var m in _history[message.ChannelId])
                m.Status = Status.Read;
        }
    }
}

