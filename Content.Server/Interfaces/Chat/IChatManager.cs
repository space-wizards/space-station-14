using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        void DispatchServerAnnouncement(string message);

        void DispatchServerMessage(IPlayerSession player, string message);

        void EntitySay(IEntity source, string message);

        void SendOOC(IPlayerSession player, string message);
    }
}
