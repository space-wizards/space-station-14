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
        void DispatchStationAnnouncement(string message, string sender = "CentComm");

        void DispatchServerMessage(IPlayerSession player, string message);

        void EntitySay(IEntity source, string message);
        void EntityMe(IEntity source, string action);

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
