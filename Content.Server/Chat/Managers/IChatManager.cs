using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager
    {
        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorOverride">Override the color of the message being sent.</param>
        void DispatchServerAnnouncement(string message, Color? colorOverride = null);

        /// <summary>
        ///     Station announcement to every player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        /// <param name="playDefaultSound">If the default 'PA' sound should be played.</param>
        /// <param name="colorOverride">Override the color of the message being sent.</param>
        void DispatchStationAnnouncement(string message, string sender = "CentComm", bool playDefaultSound = true,
            Color? colorOverride = null);

        void DispatchServerMessage(IPlayerSession player, string message);

        void TrySendOOCMessage(IPlayerSession player, string message, OOCChatType type);

        void SendHookOOC(string sender, string message);
        void SendAdminAnnouncement(string message);

        void ChatMessageToOne(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat,
            INetChannel client);
        void ChatMessageToMany(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat,
            List<INetChannel> clients);
        void ChatMessageToAll(ChatChannel channel, string message, string messageWrap, Color? colorOverride = null);

        bool MessageCharacterLimit(IPlayerSession player, string message);
    }
}
