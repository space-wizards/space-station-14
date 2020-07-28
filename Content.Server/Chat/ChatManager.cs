using System.Linq;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.Chat;
using Content.Shared.GameObjects.EntitySystems;
using NFluidsynth;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
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
using Content.Server.GameObjects.EntitySystems;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.Interfaces.Map;

namespace Content.Server.Chat
{
    /// <summary>
    ///     Dispatches chat messages to clients.
    /// </summary>
    internal sealed class ChatManager : IChatManager
    {
        /// <summary>
        /// The maximum length a player-sent message can be sent
        /// </summary>
        public int MaxMessageLength = 1000;

        private const int VoiceRange = 7; // how far voice goes in world units

        /// <summary>
        /// The message displayed to the player when it exceeds the chat character limit
        /// </summary>
        private const string MaxLengthExceededMessage = "Your message exceeded {0} character limit";

#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IMoMMILink _mommiLink;
        [Dependency] private readonly IConGroupController _conGroupController;
#pragma warning restore 649

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME);
            _netManager.RegisterNetMessage<ChatMaxMsgLengthMessage>(ChatMaxMsgLengthMessage.NAME, _onMaxLengthRequest);

            // Tell all the connected players the chat's character limit
            var msg = _netManager.CreateNetMessage<ChatMaxMsgLengthMessage>();
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToAll(msg);
        }

        public void DispatchServerAnnouncement(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = "SERVER: {0}";
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToAll(msg);
        }

        public void DispatchServerMessage(IPlayerSession player, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = "SERVER: {0}";
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendMessage(msg, player.ConnectedClient);
        }

        public void EntitySay(IEntity source, string message)
        {
            if (!ActionBlockerSystem.CanSpeak(source))
            {
                return;
            }

            // Get entity's PlayerSession
            IPlayerSession playerSession = source.GetComponent<IActorComponent>().playerSession;

            // Check if message exceeds the character limit if the sender is a player
            if (playerSession != null)
                if (message.Length > MaxMessageLength)
                {
                    DispatchServerMessage(playerSession, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                    return;
                }

            var pos = source.Transform.GridPosition;
            var clients = _playerManager.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Local;
            msg.Message = message;
            msg.MessageWrap = $"{source.Name} says, \"{{0}}\"";
            msg.SenderEntity = source.Uid;
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToMany(msg, clients.ToList());

            var listeners = _entitySystemManager.GetEntitySystem<ListeningSystem>();
            listeners.PingListeners(source, pos, message);
        }

        public void EntityMe(IEntity source, string action)
        {
            if (!ActionBlockerSystem.CanEmote(source))
            {
                return;
            }

            // Check if entity is a player
            IPlayerSession playerSession = source.GetComponent<IActorComponent>().playerSession;

            // Check if message exceeds the character limit
            if (playerSession != null)
                if (action.Length > MaxMessageLength)
                {
                    DispatchServerMessage(playerSession, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                    return;
                }

            var pos = source.Transform.GridPosition;
            var clients = _playerManager.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Emotes;
            msg.Message = action;
            msg.MessageWrap = $"{source.Name} {{0}}";
            msg.SenderEntity = source.Uid;
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendOOC(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limi
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: {player.SessionId}: {{0}}";
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToAll(msg);

            _mommiLink.SendOOCMessage(player.SessionId.ToString(), message);
        }

        public void SendDeadChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            var clients = _playerManager.GetPlayersBy(x => x.AttachedEntity != null && x.AttachedEntity.HasComponent<GhostComponent>()).Select(p => p.ConnectedClient);;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;
            msg.MessageWrap = $"{_localizationManager.GetString("DEAD")}: {player.AttachedEntity.Name}: {{0}}";
            msg.SenderEntity = player.AttachedEntityUid.GetValueOrDefault();
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendAdminChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            if (!_conGroupController.CanCommand(player, "asay"))
            {
                SendOOC(player, message);
                return;
            }
            var clients = _playerManager.GetPlayersBy(x => _conGroupController.CanCommand(x, "asay")).Select(p => p.ConnectedClient);;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.AdminChat;
            msg.Message = message;
            msg.MessageWrap = $"{_localizationManager.GetString("ADMIN")}: {player.SessionId}: {{0}}";
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendHookOOC(string sender, string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: (D){sender}: {{0}}";
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToAll(msg);
        }

        private void _onMaxLengthRequest(ChatMaxMsgLengthMessage msg)
        {
            var response = _netManager.CreateNetMessage<ChatMaxMsgLengthMessage>();
            response.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendMessage(response, msg.MsgChannel);
        }
    }
}
