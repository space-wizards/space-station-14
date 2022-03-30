using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Headset;
using Content.Server.MoMMI;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Shared.Disease.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using static Content.Server.Chat.Managers.IChatManager;

namespace Content.Server.Chat.Managers
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

        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMoMMILink _mommiLink = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private AdminLogSystem _logs = default!;

        /// <summary>
        /// The maximum length a player-sent message can be sent
        /// </summary>
        public int MaxMessageLength => _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        //TODO: make prio based?
        private readonly List<TransformChat> _chatTransformHandlers = new();
        private bool _oocEnabled = true;
        private bool _adminOocEnabled = true;

        public void Initialize()
        {
            _logs = EntitySystem.Get<AdminLogSystem>();
            _netManager.RegisterNetMessage<MsgChatMessage>();

            _configurationManager.OnValueChanged(CCVars.OocEnabled, OnOocEnabledChanged, true);
            _configurationManager.OnValueChanged(CCVars.AdminOocEnabled, OnAdminOocEnabledChanged, true);
        }

        private void OnOocEnabledChanged(bool val)
        {
            if (_oocEnabled == val) return;

            _oocEnabled = val;
            DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-ooc-chat-enabled-message" : "chat-manager-ooc-chat-disabled-message"));
        }

        private void OnAdminOocEnabledChanged(bool val)
        {
            if (_adminOocEnabled == val) return;

            _adminOocEnabled = val;
            DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-admin-ooc-chat-enabled-message" : "chat-manager-admin-ooc-chat-disabled-message"));
        }

        public void DispatchServerAnnouncement(string message, Color? colorOverride = null)
        {
            var messageWrap = Loc.GetString("chat-manager-server-wrap-message");
            ChatMessageToAll(ChatChannel.Server, message, messageWrap, colorOverride);
            Logger.InfoS("SERVER", message);

            _logs.Add(LogType.Chat, LogImpact.Low, $"Server announcement: {message}");
        }

        public void DispatchStationAnnouncement(string message, string sender = "Central Command", bool playDefaultSound = true, Color? colorOverride = null)
        {
            var messageWrap = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender));
            ChatMessageToAll(ChatChannel.Radio, message, messageWrap, colorOverride);
            if (playDefaultSound)
            {
                SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/announce.ogg", AudioParams.Default.WithVolume(-2f));
            }

            _logs.Add(LogType.Chat, LogImpact.Low, $"Station Announcement from {sender}: {message}");
        }

        public void DispatchServerMessage(IPlayerSession player, string message)
        {
            var messageWrap = Loc.GetString("chat-manager-server-wrap-message");
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            _netManager.ServerSendMessage(msg, player.ConnectedClient);

            _logs.Add(LogType.Chat, LogImpact.Low, $"Server message from {player:Player}: {message}");
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
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.OOC;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-ooc-wrap-message", ("playerName",player.Name));
            if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            {
                var prefs = _preferencesManager.GetPreferences(player.UserId);
                msg.MessageColorOverride = prefs.AdminOOCColor;
            }
            if (player.ConnectedClient.UserData.PatronTier is { } patron &&
                     PatronOocColors.TryGetValue(patron, out var patronColor))
            {
                msg.MessageWrap = Loc.GetString("chat-manager-send-ooc-patron-wrap-message", ("patronColor", patronColor),("playerName", player.Name));
            }

            //TODO: player.Name color, this will need to change the structure of the MsgChatMessage
            _netManager.ServerSendToAll(msg);

            _mommiLink.SendOOCMessage(player.Name, message);
            _logs.Add(LogType.Chat, LogImpact.Low, $"OOC from {player:Player}: {message}");
        }

        public void SendAdminChat(IPlayerSession player, string message)
        {
            // Check if message exceeds the character limit
            if (message.Length > MaxMessageLength)
            {
                DispatchServerMessage(player, Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength)));
                return;
            }

            message = FormattedMessage.EscapeText(message);

            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Admin;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-admin-chat-wrap-message",
                                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                                            ("playerName", player.Name));
            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, $"Admin chat from {player:Player}: {message}");
        }

        public void SendAdminAnnouncement(string message)
        {
            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            message = FormattedMessage.EscapeText(message);

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Admin;
            msg.Message = message;
            msg.MessageWrap = Loc.GetString("chat-manager-send-admin-announcement-wrap-message",
                                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")));

            _netManager.ServerSendToMany(msg, clients.ToList());

            _logs.Add(LogType.Chat, LogImpact.Low, $"Admin announcement from {message}: {message}");
        }

        public void SendHookOOC(string sender, string message)
        {
            message = FormattedMessage.EscapeText(message);
            var messageWrap = Loc.GetString("chat-manager-send-hook-ooc-wrap-message", ("senderName", sender));
            ChatMessageToAll(ChatChannel.OOC, message, messageWrap);
            _logs.Add(LogType.Chat, LogImpact.Low, $"Hook OOC from {sender}: {message}");
        }

        public void RegisterChatTransform(TransformChat handler)
        {
            // TODO: Literally just make this an event...
            _chatTransformHandlers.Add(handler);
        }

        public void ChatMessageToOne(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat, INetChannel client)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = channel;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            msg.SenderEntity = source;
            msg.HideChat = hideChat;
            _netManager.ServerSendMessage(msg, client);
        }

        public void ChatMessageToMany(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat, List<INetChannel> clients)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = channel;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            msg.SenderEntity = source;
            msg.HideChat = hideChat;
            _netManager.ServerSendToMany(msg, clients);
        }

        public void ChatMessageToAll(ChatChannel channel, string message, string messageWrap, Color? colorOverride = null)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = channel;
            msg.Message = message;
            msg.MessageWrap = messageWrap;
            if (colorOverride != null)
            {
                msg.MessageColorOverride = colorOverride.Value;
            }
            _netManager.ServerSendToAll(msg);
        }

        public bool MessageCharacterLimit(EntityUid source, string message)
        {
            var isOverLength = false;

            // Check if message exceeds the character limit if the sender is a player
            if (_entManager.TryGetComponent(source, out ActorComponent? actor) &&
                message.Length > MaxMessageLength)
            {
                var feedback = Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength));

                DispatchServerMessage(actor.PlayerSession, feedback);

                isOverLength = true;
            }

            return isOverLength;
        }
    }
}
