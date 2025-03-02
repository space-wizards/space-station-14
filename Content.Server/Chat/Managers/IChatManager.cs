using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        public void SendChannelMessage(
            string message,
            ProtoId<CommunicationChannelPrototype> communicationChannel,
            ICommonSession? senderSession,
            EntityUid? senderEntity,
            HashSet<ICommonSession>? targetSessions = null,
            bool escapeText = true,
            ChatMessageContext? channelParameters = null,
            bool logMessage = true
        );

        public void SendChannelMessage(
            FormattedMessage message,
            string communicationChannel,
            ICommonSession? senderSession,
            EntityUid? senderEntity,
            HashSet<ICommonSession>? targetSessions = null,
            ChatMessageContext? channelParameters = null,
            bool logMessage = true
        );

        public void SendChannelMessage(
            FormattedMessage message,
            string communicationChannel,
            ICommonSession? senderSession,
            EntityUid? senderEntity,
            List<CommunicationChannelPrototype> usedCommsTypes,
            HashSet<ICommonSession>? targetSessions = null,
            ChatMessageContext? channelParameters = null,
            bool logMessage = true
        );

        public void SendChannelMessage(
            FormattedMessage message,
            CommunicationChannelPrototype communicationChannel,
            ICommonSession? senderSession,
            EntityUid? senderEntity,
            List<CommunicationChannelPrototype> usedCommsChannels,
            HashSet<ICommonSession>? targetSessions = null,
            ChatMessageContext? channelParameters = null,
            bool logMessage = true);

        void SendAdminAnnouncement(string message, AdminFlags? requiredFlags = null);
        void SendAdminAnnouncementMessage(ICommonSession player, string message, bool suppressLog = true);

        void SendHookOOC(string sender, string message);

        void DispatchServerAnnouncement(string message);

        void DispatchServerMessage(ICommonSession player, string message, bool suppressLog = false);

        void SendAdminAlert(string message);

        void SendAdminAlert(EntityUid player, string message);

        void ChatFormattedMessageToHashset(FormattedMessage message, CommunicationChannelPrototype channel, IEnumerable<INetChannel> clients, EntityUid? source, bool hideChat, bool recordReplay, NetUserId? author = null);

        bool MessageCharacterLimit(ICommonSession player, string message);

        void DeleteMessagesBy(NetUserId uid);

        [return: NotNullIfNotNull(nameof(author))]
        ChatUser? EnsurePlayer(NetUserId? author);

        /// <summary>
        /// Called when a player sends a chat message to handle rate limits.
        /// Will update counts and do necessary actions if breached.
        /// </summary>
        /// <param name="player">The player sending a chat message.</param>
        /// <returns>False if the player has violated rate limits and should be blocked from sending further messages.</returns>
        RateLimitStatus HandleRateLimit(ICommonSession player);
    }
}
