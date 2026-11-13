using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Database;
using Content.Server.Discord.DiscordLink;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Players.RateLimiting;
using JetBrains.Annotations;
using NetCord.Gateway;
using NetCord.Rest;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed partial class BwoinkSystem : SharedBwoinkSystem
    {
        private const string RateLimitKey = "AdminHelp";

        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IAdminManager _adminManager = default!;
        [Dependency] private IConfigurationManager _config = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private IPlayerLocator _playerLocator = default!;
        [Dependency] private ServerGameTicker _gameTicker = default!;
        [Dependency] private SharedMindSystem _minds = default!;
        [Dependency] private IAfkManager _afkManager = default!;
        [Dependency] private IServerDbManager _dbManager = default!;
        [Dependency] private PlayerRateLimitManager _rateLimit = default!;
        [Dependency] private DiscordLink _discordLink = default!;
        [Dependency] private ITaskManager _taskManager = default!;

        private ulong? _forumChannelId;
        private ulong? _onCallRoleId;

        private ISawmill _sawmill = default!;

        private string _serverName = string.Empty;

        /// <summary>
        ///     Discord threads of the current round, by the player they belong to.
        /// </summary>
        private readonly Dictionary<NetUserId, DiscordRelayInteraction> _relayInteractions = new();

        /// <summary>
        ///     Players the Discord threads of the current round belong to, by thread ID.
        ///     Used to relay messages sent in a thread back to the player.
        /// </summary>
        private readonly Dictionary<ulong, NetUserId> _relayThreadOwners = new();

        private Dictionary<NetUserId, ulong> _oldThreadIds = new();
        private readonly Dictionary<NetUserId, Queue<DiscordRelayedData>> _messageQueues = new();
        private readonly HashSet<NetUserId> _processingChannels = new();
        private readonly Dictionary<NetUserId, (TimeSpan Timestamp, bool Typing)> _typingUpdateTimestamps = new();
        private string _overrideClientName = string.Empty;

        // Max message length is 2000: https://discord.com/developers/docs/resources/message#create-message-jsonform-params
        private const ushort DiscordMessageMax = 2000;

        // Max thread name length is 100: https://discord.com/developers/docs/resources/channel#start-thread-in-forum-or-media-channel-jsonform-params
        private const ushort ThreadNameMax = 100;

        // Maximum length a message can be before it is cut off
        // Should be shorter than DiscordMessageMax, to leave space for the on-call mention
        private const ushort MessageLengthCap = 1800;

        // Text to be used to cut off messages that are too long. Should be shorter than MessageLengthCap
        private const string TooLongText = "... **(too long)**";

        private readonly Dictionary<NetUserId, DateTime> _activeConversations = new();

        /// <summary>
        ///     Whether ahelp messages are currently relayed to Discord.
        /// </summary>
        private bool DiscordRelayEnabled => _forumChannelId != null && _discordLink.IsConnected;

        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_config, CCVars.DiscordAHelpForumChannelId, OnForumChannelIdChanged, true);
            Subs.CVar(_config, CCVars.DiscordAhelpMention, OnOnCallRoleIdChanged, true);
            Subs.CVar(_config, CVars.GameHostName, OnServerNameChanged, true);
            Subs.CVar(_config, CCVars.AdminAhelpOverrideClientName, OnOverrideChanged, true);
            Subs.CVar(_config, CCVars.AhelpQuickInfoStartWordSize, v => _startWordMinSize = v, true);
            _sawmill = LogManager.GetSawmill("AHELP");

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _discordLink.OnMessageReceived += OnDiscordMessageReceived;

            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
            SubscribeNetworkEvent<BwoinkClientTypingUpdated>(OnClientTypingUpdated);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _activeConversations.Clear());

        	_rateLimit.Register(
                RateLimitKey,
                new RateLimitRegistration(CCVars.AhelpRateLimitPeriod,
                    CCVars.AhelpRateLimitCount,
                    PlayerRateLimitedAction)
                );
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _discordLink.OnMessageReceived -= OnDiscordMessageReceived;
        }

        private void OnOnCallRoleIdChanged(string roleId)
        {
            _onCallRoleId = ulong.TryParse(roleId, out var id) ? id : null;
        }

        private void PlayerRateLimitedAction(ICommonSession obj)
        {
            RaiseNetworkEvent(
                new BwoinkTextMessage(obj.UserId, default, Loc.GetString("bwoink-system-rate-limited"), playSound: false),
                obj.Channel);
        }

        private void OnOverrideChanged(string obj)
        {
            _overrideClientName = obj;
        }

        private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                if (_activeConversations.TryGetValue(e.Session.UserId, out var lastMessageTime))
                {
                    var timeSinceLastMessage = DateTime.Now - lastMessageTime;
                    if (timeSinceLastMessage > TimeSpan.FromMinutes(5))
                    {
                        _activeConversations.Remove(e.Session.UserId);
                        return; // Do not send disconnect message if timeout exceeded
                    }
                }

                // Check if the user has been banned
                var ban = await _dbManager.GetBanAsync(null, e.Session.UserId, null, null);
                if (ban != null)
                {
                    var banMessage = Loc.GetString("bwoink-system-player-banned", ("banReason", ban.Reason));
                    NotifyAdmins(e.Session, banMessage, PlayerStatusType.Banned);
                    _activeConversations.Remove(e.Session.UserId);
                    return;
                }
            }

            // Notify all admins if a player disconnects or reconnects
            var message = e.NewStatus switch
            {
                SessionStatus.Connected => Loc.GetString("bwoink-system-player-reconnecting"),
                SessionStatus.Disconnected => Loc.GetString("bwoink-system-player-disconnecting"),
                _ => null
            };

            if (message != null)
            {
                var statusType = e.NewStatus == SessionStatus.Connected
                    ? PlayerStatusType.Connected
                    : PlayerStatusType.Disconnected;
                NotifyAdmins(e.Session, message, statusType);
            }

            if (e.NewStatus != SessionStatus.InGame)
                return;

            RaiseNetworkEvent(new BwoinkDiscordRelayUpdated(DiscordRelayEnabled), e.Session);
        }

        private void NotifyAdmins(ICommonSession session, string message, PlayerStatusType statusType)
        {
            if (!_activeConversations.ContainsKey(session.UserId))
            {
                // If the user is not part of an active conversation, do not notify admins.
                return;
            }

            // Get the current timestamp
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var roundTime = _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss");

            // Determine the icon based on the status type
            string icon = statusType switch
            {
                PlayerStatusType.Connected => ":green_circle:",
                PlayerStatusType.Disconnected => ":red_circle:",
                PlayerStatusType.Banned => ":no_entry:",
                _ => ":question:"
            };

            // Create the message parameters for Discord
            var messageParams = new AHelpMessageParams(
                session.Name,
                message,
                true,
                roundTime,
                _gameTicker.RunLevel,
                playedSound: true,
                icon: icon
            );

            // Create the message for in-game with username
            var color = statusType switch
            {
                PlayerStatusType.Connected => Color.Green.ToHex(),
                PlayerStatusType.Disconnected => Color.Yellow.ToHex(),
                PlayerStatusType.Banned => Color.Orange.ToHex(),
                _ => Color.Gray.ToHex(),
            };
            var inGameMessage = $"[color={color}]{session.Name} {message}[/color]";

            var bwoinkMessage = new BwoinkTextMessage(
                userId: session.UserId,
                trueSender: SystemUserId,
                text: inGameMessage,
                sentAt: DateTime.Now,
                playSound: false
            );

            var admins = GetTargetAdmins();
            foreach (var admin in admins)
            {
                RaiseNetworkEvent(bwoinkMessage, admin);
            }

            // Enqueue the message for Discord relay
            if (DiscordRelayEnabled)
            {
                var queue = _messageQueues.GetOrNew(session.UserId);
                var escapedText = FormattedMessage.EscapeText(message);
                messageParams.Message = escapedText;
                var discordMessage = GenerateAHelpMessage(messageParams);
                queue.Enqueue(discordMessage);
            }
        }

        private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
        {
            // Don't make a new thread if we
            // 1. were in the lobby just now, and
            // 2. are not entering the lobby or directly into a new round.
            if (args.Old is GameRunLevel.PreRoundLobby ||
                args.New is not (GameRunLevel.PreRoundLobby or GameRunLevel.InRound))
            {
                return;
            }

            // Store the Discord thread IDs of the previous round
            _oldThreadIds = new Dictionary<NetUserId, ulong>();
            foreach (var (user, interaction) in _relayInteractions)
            {
                _oldThreadIds[user] = interaction.ThreadId;
            }

            // Messages sent in threads of the previous round are no longer relayed to the player
            _relayInteractions.Clear();
            _relayThreadOwners.Clear();
        }

        private void OnClientTypingUpdated(BwoinkClientTypingUpdated msg, EntitySessionEventArgs args)
        {
            if (_typingUpdateTimestamps.TryGetValue(args.SenderSession.UserId, out var tuple) &&
                tuple.Typing == msg.Typing &&
                tuple.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime)
            {
                return;
            }

            _typingUpdateTimestamps[args.SenderSession.UserId] = (_timing.RealTime, msg.Typing);

            // Non-admins can only ever type on their own ahelp, guard against fake messages
            var isAdmin = _adminManager.GetAdminData(args.SenderSession)?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var channel = isAdmin ? msg.Channel : args.SenderSession.UserId;
            var update = new BwoinkPlayerTypingUpdated(channel, args.SenderSession.Name, msg.Typing);

            foreach (var admin in GetTargetAdmins())
            {
                if (admin.UserId == args.SenderSession.UserId)
                    continue;

                RaiseNetworkEvent(update, admin);
            }
        }

        private void OnServerNameChanged(string obj)
        {
            _serverName = obj;
        }

        private void OnForumChannelIdChanged(string channelId)
        {
            _forumChannelId = ulong.TryParse(channelId, out var id) ? id : null;

            RaiseNetworkEvent(new BwoinkDiscordRelayUpdated(DiscordRelayEnabled));
        }

        private void OnDiscordMessageReceived(Message message)
        {
            if (message.Author.IsBot)
                return;

            // Discord messages are received on a separate thread.
            _taskManager.RunOnMainThread(() => TryRelayFromDiscord(message.ChannelId, message.Author.Username, message.Content));
        }

        /// <summary>
        ///     Relays a message sent in a Discord ahelp thread of the current round to the player it belongs to,
        ///     and to all admins.
        /// </summary>
        /// <returns>True if the channel is an ahelp thread of the current round and the message was relayed.</returns>
        public bool TryRelayFromDiscord(ulong channelId, string author, string message)
        {
            if (!_relayThreadOwners.TryGetValue(channelId, out var userId))
                return false;

            var text = message.Trim();

            // Messages with only attachments or stickers are not relayed.
            if (text == string.Empty)
                return false;

            _activeConversations[userId] = DateTime.Now;

            var escapedText = FormattedMessage.EscapeText(text);
            var authorName = FormattedMessage.EscapeText(author);

            var admins = GetTargetAdmins();
            var adminMsg = new BwoinkTextMessage(
                userId,
                SystemUserId,
                Loc.GetString("bwoink-message-discord-wrap", ("senderName", authorName), ("message", escapedText)));

            // Notify all admins
            foreach (var channel in admins)
            {
                RaiseNetworkEvent(adminMsg, channel);
            }

            // Notify player
            if (_playerManager.TryGetSessionById(userId, out var session) && !admins.Contains(session.Channel))
            {
                var playerName = _overrideClientName == string.Empty
                    ? authorName
                    : FormattedMessage.EscapeText(_overrideClientName);

                var playerMsg = new BwoinkTextMessage(
                    userId,
                    SystemUserId,
                    Loc.GetString("bwoink-message-discord-wrap", ("senderName", playerName), ("message", escapedText)));
                RaiseNetworkEvent(playerMsg, session.Channel);
            }

            return true;
        }

        private async void ProcessQueue(NetUserId userId, Queue<DiscordRelayedData> messages)
        {
            try
            {
                await RelayToDiscord(userId, messages);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while relaying ahelp messages to Discord: {e}");
            }
            finally
            {
                _processingChannels.Remove(userId);
            }
        }

        private async Task RelayToDiscord(NetUserId userId, Queue<DiscordRelayedData> messages)
        {
            if (_forumChannelId is not { } forumChannelId)
                return;

            // If there is no thread for this player yet, we create a new one
            if (!_relayInteractions.TryGetValue(userId, out var interaction))
            {
                interaction = await CreateRelayThread(userId, forumChannelId);

                if (interaction == null)
                    return;
            }

            var content = new StringBuilder();

            // Previous message was in another RunLevel, so show that in the thread
            if (interaction.LastRunLevel != _gameTicker.RunLevel)
            {
                content.AppendLine(_gameTicker.RunLevel switch
                {
                    GameRunLevel.PreRoundLobby => ":arrow_forward: _**Pre-round lobby started**_",
                    GameRunLevel.InRound => ":arrow_forward: _**Round started**_",
                    GameRunLevel.PostRound => ":stop_button: _**Post-round started**_",
                    _ => throw new ArgumentOutOfRangeException(nameof(_gameTicker.RunLevel),
                        $"{_gameTicker.RunLevel} was not matched."),
                });

                interaction.LastRunLevel = _gameTicker.RunLevel;
            }

            // If last message of the new batch is SOS then mention on-call.
            // ... as long as they haven't been mentioned already.
            var unanswered = !messages.Last().Receivers;
            var allowedMentions = AllowedMentionsProperties.None;

            if (unanswered && !interaction.OnCall && _onCallRoleId is { } roleId)
            {
                content.AppendLine($"<@&{roleId}> Unanswered SOS");
                allowedMentions = new AllowedMentionsProperties
                {
                    AllowedRoles = [roleId],
                    AllowedUsers = [],
                };
            }

            interaction.OnCall = unanswered;

            // Add available messages to the Discord message, splitting it if it gets too long
            while (messages.TryDequeue(out var message))
            {
                string text;

                // In case someone thinks they're funny
                if (message.Message.Length > MessageLengthCap)
                    text = message.Message[..(MessageLengthCap - TooLongText.Length)] + TooLongText;
                else
                    text = message.Message;

                if (content.Length + text.Length + Environment.NewLine.Length > DiscordMessageMax)
                {
                    await _discordLink.SendMessageAsync(interaction.ThreadId, content.ToString(), allowedMentions);
                    content.Clear();
                    allowedMentions = AllowedMentionsProperties.None;
                }

                content.AppendLine(text);
            }

            if (content.Length > 0)
                await _discordLink.SendMessageAsync(interaction.ThreadId, content.ToString(), allowedMentions);
        }

        private async Task<DiscordRelayInteraction?> CreateRelayThread(NetUserId userId, ulong forumChannelId)
        {
            var lookup = await _playerLocator.LookupIdAsync(userId);

            if (lookup == null)
            {
                _sawmill.Log(LogLevel.Error,
                    $"Unable to find player for NetUserId {userId} when creating Discord thread.");
                return null;
            }

            var round = GetRoundDescription();

            // Limit server name to 500 characters, in case someone tries to be a little funny
            var serverName = _serverName[..Math.Min(_serverName.Length, 500)];

            var starterMessage = new StringBuilder();
            starterMessage.AppendLine($"**Player:** {lookup.Username} (`{userId}`)");

            var characterName = _minds.GetCharacterName(userId);
            if (characterName != null)
                starterMessage.AppendLine($"**Character:** {characterName}");

            starterMessage.AppendLine($"**Server:** {serverName}");
            starterMessage.AppendLine($"**Round:** {round}");

            if (_oldThreadIds.TryGetValue(userId, out var oldThreadId))
                starterMessage.AppendLine($"**Last round's conversation:** <#{oldThreadId}>");

            var threadName = $"{lookup.Username} ({round})";
            if (threadName.Length > ThreadNameMax)
                threadName = threadName[..ThreadNameMax];

            var threadId = await _discordLink.CreateForumThreadAsync(forumChannelId, threadName, starterMessage.ToString());

            if (threadId == null)
                return null;

            var interaction = new DiscordRelayInteraction
            {
                ThreadId = threadId.Value,
                LastRunLevel = _gameTicker.RunLevel,
            };

            _relayInteractions[userId] = interaction;
            _relayThreadOwners[threadId.Value] = userId;

            return interaction;
        }

        private string GetRoundDescription()
        {
            return _gameTicker.RunLevel switch
            {
                GameRunLevel.PreRoundLobby => _gameTicker.RoundId == 0
                    ? "pre-round lobby after server restart" // first round after server restart has ID == 0
                    : $"pre-round lobby for round {_gameTicker.RoundId}",
                GameRunLevel.InRound => $"round {_gameTicker.RoundId}",
                GameRunLevel.PostRound => $"post-round {_gameTicker.RoundId}",
                _ => throw new ArgumentOutOfRangeException(nameof(_gameTicker.RunLevel),
                    $"{_gameTicker.RunLevel} was not matched."),
            };
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var userId in _messageQueues.Keys.ToArray())
            {
                if (_processingChannels.Contains(userId))
                    continue;

                var queue = _messageQueues[userId];
                _messageQueues.Remove(userId);
                if (queue.Count == 0)
                    continue;

                _processingChannels.Add(userId);

                ProcessQueue(userId, queue);
            }
        }

        private string FormatName(AdminData? senderAdmin, ICommonSession senderSession, string prefix, string name)
        {
            if (senderAdmin is not null &&
                senderAdmin.Flags ==
                AdminFlags.Adminhelp) // Mentor. Not full admin. That's why it's colored differently.
            {
                return $"[color=purple]{prefix}{name}[/color]";
            }
            else if (senderAdmin is not null && senderAdmin.HasFlag(AdminFlags.Adminhelp))
            {
                return $"[color=red]{prefix}{name}[/color]";
            }
            else
            {
                return $"{senderSession.Name}"; // Not an admin, name is not overridden.
            }
        }

        private async Task<BwoinkTextMessage> FormatFullMessageForRecipient(
            bool forAdmin,
            AdminData? senderAdmin,
            ICommonSession senderSession,
            BwoinkTextMessage input)
        {
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            string messageText;

            if (forAdmin)
            {
                messageText = await GenerateNameLinks(input.Text);
            }
            else
            {
                messageText = FormattedMessage.EscapeText(input.Text);
            }

            var adminPrefix = "";
            if (_config.GetCVar(CCVars.AhelpAdminPrefix) && senderAdmin?.Title != null)
                adminPrefix = $"[bold]\\[{FormattedMessage.EscapeText(senderAdmin.Title)}\\][/bold] ";

            var bwoinkText = FormatName(
                senderAdmin,
                senderSession,
                adminPrefix,
                forAdmin || _overrideClientName == string.Empty ? senderSession.Name : _overrideClientName);

            var statusPrefix = "";
            if (input.AdminOnly)
                statusPrefix = Loc.GetString("bwoink-message-admin-only");
            else if (!input.PlaySound)
                statusPrefix = Loc.GetString("bwoink-message-silent");

            bwoinkText = $"{statusPrefix} {bwoinkText}: {messageText}";

            // If it's not an admin / admin chooses to keep the sound and message is not an admin only message, then play it.
            var playSound = (!senderAHelpAdmin || input.PlaySound) && !input.AdminOnly;
            return new BwoinkTextMessage(
                input.UserId,
                senderSession.UserId,
                bwoinkText,
                playSound: playSound,
                adminOnly: input.AdminOnly);
        }

        protected override async void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            _activeConversations[message.UserId] = DateTime.Now;
            var senderSession = eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            var personalChannel = senderSession.UserId == message.UserId;
            var senderAdmin = _adminManager.GetAdminData(senderSession);
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var authorized = personalChannel && !message.AdminOnly || senderAHelpAdmin;
            if (!authorized)
            {
                // Unauthorized bwoink (log?)
                return;
            }

            if (_rateLimit.CountAction(eventArgs.SenderSession, RateLimitKey) != RateLimitStatus.Allowed)
                return;

            _afkManager.PlayerDidAction(senderSession);

            // If it's not an admin / admin chooses to keep the sound and message is not an admin only message, then play it.
            var playSound = (!senderAHelpAdmin || message.PlaySound) && !message.AdminOnly;

            var admins = GetTargetAdmins();
            var adminMsg = await FormatFullMessageForRecipient(forAdmin: true, senderAdmin, senderSession, message);

            // Notify all admins
            foreach (var channel in admins)
            {
                RaiseNetworkEvent(adminMsg, channel);
            }

            // Notify player
            if (_playerManager.TryGetSessionById(message.UserId, out var session) && !message.AdminOnly)
            {
                if (!admins.Contains(session.Channel))
                {
                    var playerMsg = await FormatFullMessageForRecipient(forAdmin: false, senderAdmin, senderSession, message);
                    RaiseNetworkEvent(playerMsg, session.Channel);
                }
            }

            var relaysToDiscord = DiscordRelayEnabled;
            if (relaysToDiscord)
            {
                var queue = _messageQueues.GetOrNew(message.UserId);

                var nonAfkAdmins = GetNonAfkAdmins();
                var messageParams = new AHelpMessageParams(
                    senderSession.Name,
                    message.Text,
                    !personalChannel,
                    _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss"),
                    _gameTicker.RunLevel,
                    playedSound: playSound,
                    adminOnly: message.AdminOnly,
                    noReceivers: nonAfkAdmins.Count == 0
                );
                queue.Enqueue(GenerateAHelpMessage(messageParams));
            }

            if (admins.Count != 0 || relaysToDiscord)
                return;

            // No admin online, let the player know
            var systemText = Loc.GetString("bwoink-system-starmute-message-no-other-users");
            var starMuteMsg = new BwoinkTextMessage(message.UserId, SystemUserId, systemText);
            RaiseNetworkEvent(starMuteMsg, senderSession.Channel);
        }

        private IList<INetChannel> GetNonAfkAdmins()
        {
            return _adminManager.ActiveAdmins
                .Where(p => (_adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false) &&
                            !_afkManager.IsAfk(p))
                .Select(p => p.Channel)
                .ToList();
        }

        private IList<INetChannel> GetTargetAdmins()
        {
            return _adminManager.ActiveAdmins
                .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
                .Select(p => p.Channel)
                .ToList();
        }

        private DiscordRelayedData GenerateAHelpMessage(AHelpMessageParams parameters)
        {
            var stringbuilder = new StringBuilder();

            if (parameters.Icon != null)
                stringbuilder.Append(parameters.Icon);
            else if (parameters.IsAdmin)
                stringbuilder.Append(":outbox_tray:");
            else if (parameters.NoReceivers)
                stringbuilder.Append(":sos:");
            else
                stringbuilder.Append(":inbox_tray:");

            if (parameters.RoundTime != string.Empty && parameters.RoundState == GameRunLevel.InRound)
                stringbuilder.Append($" **{parameters.RoundTime}**");
            if (!parameters.PlayedSound)
                stringbuilder.Append($" **{(parameters.AdminOnly ? Loc.GetString("bwoink-message-admin-only") : Loc.GetString("bwoink-message-silent"))}**");
            if (parameters.Icon == null)
                stringbuilder.Append($" **{parameters.Username}:** ");
            else
                stringbuilder.Append($" **{parameters.Username}** ");
            stringbuilder.Append(parameters.Message);

            return new DiscordRelayedData()
            {
                Receivers = !parameters.NoReceivers,
                Message = stringbuilder.ToString(),
            };
        }

        private record struct DiscordRelayedData
        {
            /// <summary>
            /// Was anyone online to receive it.
            /// </summary>
            public bool Receivers;

            /// <summary>
            /// What's the payload to send to discord.
            /// </summary>
            public string Message;
        }

        /// <summary>
        ///  Class specifically for holding information regarding existing Discord threads
        /// </summary>
        private sealed class DiscordRelayInteraction
        {
            public ulong ThreadId;

            /// <summary>
            /// Run level of the last interaction. If different we'll show it in the thread.
            /// </summary>
            public GameRunLevel LastRunLevel;

            /// <summary>
            /// Was on-call mentioned for the last unanswered SOS.
            /// </summary>
            public bool OnCall;
        }
    }

    public sealed class AHelpMessageParams
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public bool IsAdmin { get; set; }
        public string RoundTime { get; set; }
        public GameRunLevel RoundState { get; set; }
        public bool PlayedSound { get; set; }
        public readonly bool AdminOnly;
        public bool NoReceivers { get; set; }
        public string? Icon { get; set; }

        public AHelpMessageParams(
            string username,
            string message,
            bool isAdmin,
            string roundTime,
            GameRunLevel roundState,
            bool playedSound,
            bool adminOnly = false,
            bool noReceivers = false,
            string? icon = null)
        {
            Username = username;
            Message = message;
            IsAdmin = isAdmin;
            RoundTime = roundTime;
            RoundState = roundState;
            PlayedSound = playedSound;
            AdminOnly = adminOnly;
            NoReceivers = noReceivers;
            Icon = icon;
        }
    }

    public enum PlayerStatusType
    {
        Connected,
        Disconnected,
        Banned,
    }
}
