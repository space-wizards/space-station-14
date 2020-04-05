using System.Linq;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Observer;
using Content.Shared.Chat;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

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
        [Dependency] private readonly IMoMMILink _mommiLink;
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
            if (!ActionBlockerSystem.CanSpeak(source))
            {
                return;
            }

            var pos = source.Transform.GridPosition;
            var clients = _playerManager.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Local;
            msg.Message = message;
            msg.MessageWrap = $"{source.Name} says, \"{{0}}\"";
            msg.SenderEntity = source.Uid;
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void EntityMe(IEntity source, string action)
        {
            if (!ActionBlockerSystem.CanEmote(source))
            {
                return;
            }

            var pos = source.Transform.GridPosition;
            var clients = _playerManager.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Emotes;
            msg.Message = action;
            msg.MessageWrap = $"{source.Name} {{0}}";
            msg.SenderEntity = source.Uid;
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendOOC(IPlayerSession player, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: {player.SessionId}: {{0}}";
            _netManager.ServerSendToAll(msg);

            _mommiLink.SendOOCMessage(player.SessionId.ToString(), message);
        }

        public void SendDeadChat(IPlayerSession player, string message)
        {
            var clients = _playerManager.GetPlayersBy(x => x.AttachedEntity != null && x.AttachedEntity.HasComponent<GhostComponent>()).Select(p => p.ConnectedClient);;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;
            msg.MessageWrap = $"DEAD: {player.AttachedEntity.Name}: {{0}}";
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendHookOOC(string sender, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: (D){sender}: {{0}}";
            _netManager.ServerSendToAll(msg);
        }
    }
}
