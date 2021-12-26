using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager
    {
        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        void DispatchServerAnnouncement(string message);

        /// <summary>
        ///     Station announcement to every player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        /// <param name="playDefaultSound">If the default 'PA' sound should be played.</param>
        void DispatchStationAnnouncement(string message, string sender = "CentComm", bool playDefaultSound = true);

        void DispatchServerMessage(IPlayerSession player, string message);

        /// <param name="hideChat">If true, message will not be logged to chat boxes but will still produce a speech bubble.</param>
        void EntitySay(EntityUid source, string message, bool hideChat=false);
        void EntityMe(EntityUid source, string action);

        void SendOOC(IPlayerSession player, string message);
        void SendAdminChat(IPlayerSession player, string message);
        void SendDeadChat(IPlayerSession player, string message);
        void SendAdminDeadChat(IPlayerSession player, string message);

        void SendHookOOC(string sender, string message);

        delegate string TransformChat(EntityUid speaker, string message);
        void RegisterChatTransform(TransformChat handler);
        void SendAdminAnnouncement(string message);
    }
}
