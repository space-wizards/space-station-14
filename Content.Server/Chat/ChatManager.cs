using System.Linq;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.Chat;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using Content.Server.GameObjects.Components;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;

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
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IMoMMILink _mommiLink;
        [Dependency] private readonly IConGroupController _conGroupController;
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

            var entities = source.EntityManager.GetEntitiesInRange(pos, VoiceRange);
            if (entities.Count() > 0)
            {
                foreach (var entity in entities)
                {
                    if (entity.TryGetComponent<ListeningComponent>(out ListeningComponent listener)
                        && !source.HasComponent<RadioComponent>())
                    {
                        listener.HeardSpeech(message);
                    }
                        
                }
            }
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
            msg.MessageWrap = $"{_localizationManager.GetString("DEAD")}: {player.AttachedEntity.Name}: {{0}}";
            msg.SenderEntity = player.AttachedEntityUid.GetValueOrDefault();
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendAdminChat(IPlayerSession player, string message)
        {
            if(!_conGroupController.CanCommand(player, "asay"))
            {
                SendOOC(player, message);
                return;
            }
            var clients = _playerManager.GetPlayersBy(x => _conGroupController.CanCommand(x, "asay")).Select(p => p.ConnectedClient);;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.AdminChat;
            msg.Message = message;
            msg.MessageWrap = $"{_localizationManager.GetString("ADMIN")}: {player.SessionId}: {{0}}";
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
