using System.Linq;
using Content.Server.Interfaces.Chat;
using Content.Shared.Chat;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;

namespace Content.Server.Chat
{
    /// <summary>
    ///     Dispatches chat messages to clients.
    /// </summary>
    internal sealed class ChatManager : IChatManager
    {
        private const int VoiceRange = 7; // how far voice goes in world units

#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME);
        }

        public void DispatchServerAnnouncement(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = "SERVER: {0}";
            _netManager.ServerSendToAll(msg);
        }

        public void DispatchServerMessage(IPlayerSession player, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = "SERVER: {0}";
            _netManager.ServerSendMessage(msg, player.ConnectedClient);
        }

        public void EntitySay(IEntity source, string message)
        {
            var pos = source.Transform.GridPosition;
            var clients = _playerManager.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Local;
            msg.Message = message;
            msg.MessageWrap = $"{source.Name} says, \"{{0}}\"";
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendOOC(IPlayerSession player, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: {player.SessionId}: {{0}}";
            _netManager.ServerSendToAll(msg);
        }
    }
}
