using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Headset;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using static Content.Server.Interfaces.Chat.IChatManager;

namespace Content.Server.Chat
{
    /// <summary>
    ///     Dispatches chat messages to clients.
    /// </summary>
    internal sealed class ChatManager : IChatManager
    {
        private static readonly Dictionary<string, string> PatronOocColors = new()
        {
            // I had plans for multiple colors and those went nowhere so...
            { "nuclear_operative", "#aa00ff" },
            { "syndicate_agent", "#aa00ff" },
            { "revolutionary", "#aa00ff" }
        };

        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMoMMILink _mommiLink = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        /// <summary>
        /// The maximum length a player-sent message can be sent
        /// </summary>
        public const int MaxMessageLength = 1000;

        private const int VoiceRange = 7; // how far voice goes in world units

        /// <summary>
        /// The message displayed to the player when it exceeds the chat character limit
        /// </summary>
        private const string MaxLengthExceededMessage = "Your message exceeded {0} character limit";

        //TODO: make prio based?
        private readonly List<TransformChat> _chatTransformHandlers = new();
        private bool _oocEnabled = true;
        private bool _adminOocEnabled = true;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME);
            _netManager.RegisterNetMessage<ChatMaxMsgLengthMessage>(ChatMaxMsgLengthMessage.NAME, OnMaxLengthRequest);

            // Tell all the connected players the chat's character limit
            var msg = _netManager.CreateNetMessage<ChatMaxMsgLengthMessage>();
            msg.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendToAll(msg);

            _configurationManager.OnValueChanged(CCVars.OocEnabled, OnOocEnabledChanged, true);
            _configurationManager.OnValueChanged(CCVars.AdminOocEnabled, OnAdminOocEnabledChanged, true);
        }

        private void OnOocEnabledChanged(bool val)
        {
            _oocEnabled = val;
            DispatchServerAnnouncement(val ? "OOC chat has been enabled." : "OOC chat has been disabled.");
        }

        private void OnAdminOocEnabledChanged(bool val)
        {
            _adminOocEnabled = val;
            DispatchServerAnnouncement(val ? "Admin OOC chat has been enabled." : "Admin OOC chat has been disabled.");
        }

        public void DispatchServerAnnouncement(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = "SERVER: {0}";
            _netManager.ServerSendToAll(msg);
            Logger.InfoS("SERVER", message);
        }

        public void DispatchStationAnnouncement(string message, string sender = "CentComm")
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Radio;
            msg.Message = message;
            msg.MessageWrap = $"{sender} Announcement:\n{{0}}";
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

            // Check if message exceeds the character limit if the sender is a player
            if (source.TryGetComponent(out IActorComponent? actor) &&
                message.Length > MaxMessageLength)
            {
                var feedback = Loc.GetString(MaxLengthExceededMessage, MaxMessageLength);

                DispatchServerMessage(actor.playerSession, feedback);

                return;
            }

            foreach (var handler in _chatTransformHandlers)
            {
                //TODO: rather return a bool and use a out var?
                message = handler(source, message);
            }

            message = message.Trim();

            var mapPos = source.Transform.MapPosition;

            var clients = _playerManager.GetPlayersBy((x) => x.AttachedEntity != null
                    && (x.AttachedEntity.HasComponent<GhostComponent>()
                    || mapPos.InRange(x.AttachedEntity.Transform.MapPosition, VoiceRange)))
                .Select(p => p.ConnectedClient).ToList();

            if (message.StartsWith(';'))
            {
                // Remove semicolon
                message = message.Substring(1).TrimStart();

                // Capitalize first letter
                message = message[0].ToString().ToUpper() +
                          message.Remove(0, 1);

                if (source.TryGetComponent(out InventoryComponent? inventory) &&
                    inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.EARS, out ItemComponent? item) &&
                    item.Owner.TryGetComponent(out HeadsetComponent? headset))
                {
                    headset.RadioRequested = true;
                }
                else
                {
                    source.PopupMessage(Loc.GetString("You don't have a headset on!"));
                }
            }
            else
            {
                // Capitalize first letter
                message = message[0].ToString().ToUpper() +
                          message.Remove(0, 1);
            }

            var listeners = EntitySystem.Get<ListeningSystem>();
            listeners.PingListeners(source, message);

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Local;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("{0} says, \"{{0}}\"", source.Name);
            msg.SenderEntity = source.Uid;
            _netManager.ServerSendToMany(msg, clients);
        }

        public void EntityMe(IEntity source, string action)
        {
            if (!ActionBlockerSystem.CanEmote(source))
            {
                return;
            }

            // Check if entity is a player
            if (!source.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            // Check if message exceeds the character limit
            if (action.Length > MaxMessageLength)
            {
                DispatchServerMessage(actor.playerSession, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            action = FormattedMessage.EscapeText(action);

            var pos = source.Transform.Coordinates;
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
            if (_adminManager.IsAdmin(player))
            {
                if (!_adminOocEnabled)
                {
                    return;
                }
            }
            else if (!_oocEnabled)
            {
                return;
            }

            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: {player.Name}: {{0}}";
            if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            {
                var prefs = _preferencesManager.GetPreferences(player.UserId);
                msg.MessageColorOverride = prefs.AdminOOCColor;
            }
            if (player.ConnectedClient.UserData.PatronTier is { } patron &&
                     PatronOocColors.TryGetValue(patron, out var patronColor))
            {
                msg.MessageWrap = $"OOC: [color={patronColor}]{player.Name}[/color]: {{0}}";
            }

            //TODO: player.Name color, this will need to change the structure of the MsgChatMessage
            _netManager.ServerSendToAll(msg);

            _mommiLink.SendOOCMessage(player.Name, message);
        }

        public void SendDeadChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = GetDeadChatClients();

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;
            msg.MessageWrap = $"{Loc.GetString("DEAD")}: {player.AttachedEntity?.Name}: {{0}}";
            msg.SenderEntity = player.AttachedEntityUid.GetValueOrDefault();
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendAdminDeadChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = GetDeadChatClients();

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Dead;
            msg.Message = message;
            msg.MessageWrap = $"{Loc.GetString("ADMIN")}:(${player.ConnectedClient.UserName}): {{0}}";
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        private IEnumerable<INetChannel> GetDeadChatClients()
        {
            return _playerManager
                .GetPlayersBy(x => x.AttachedEntity != null && x.AttachedEntity.HasComponent<GhostComponent>())
                .Select(p => p.ConnectedClient)
                .Union(_adminManager.ActiveAdmins.Select(p => p.ConnectedClient));
        }

        public void SendAdminChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString(MaxLengthExceededMessage, MaxMessageLength));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.AdminChat;
            msg.Message = message;
            msg.MessageWrap = $"{Loc.GetString("ADMIN")}: {player.Name}: {{0}}";
            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendAdminAnnouncement(string message)
        {
            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.AdminChat;
            msg.Message = message;
            msg.MessageWrap = $"{Loc.GetString("ADMIN")}: {{0}}";

            _netManager.ServerSendToMany(msg, clients.ToList());
        }

        public void SendHookOOC(string sender, string message)
        {
            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = $"OOC: (D){sender}: {{0}}";
            _netManager.ServerSendToAll(msg);
        }

        private void OnMaxLengthRequest(ChatMaxMsgLengthMessage msg)
        {
            var response = _netManager.CreateNetMessage<ChatMaxMsgLengthMessage>();
            response.MaxMessageLength = MaxMessageLength;
            _netManager.ServerSendMessage(response, msg.MsgChannel);
        }

        public void RegisterChatTransform(TransformChat handler)
        {
            _chatTransformHandlers.Add(handler);
        }
    }
}
