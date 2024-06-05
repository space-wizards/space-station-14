using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Color = Discord.Color;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed partial class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly SharedMindSystem _minds = default!;
        [Dependency] private readonly DiscordLink _discord = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly AdminSystem _adminSystem = default!;
        [Dependency] private readonly IGameMapManager _mapManager = default!;
        [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
        [Dependency] private readonly IAfkManager _afkManager = default!;

        [GeneratedRegex(@"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$")]
        private static partial Regex DiscordRegex();

        private ISawmill _sawmill = default!;
        private readonly Dictionary<NetUserId, (TimeSpan Timestamp, bool Typing)> _typingUpdateTimestamps = new();
        private string _overrideClientName = string.Empty;

        // Ahelp relay
        private readonly Dictionary<NetUserId, Queue<string>> _messageQueues = new();
        private string _footerIconUrl = string.Empty;
        private string _serverName = string.Empty;
        private readonly HashSet<NetUserId> _processingChannels = new();
        private readonly Dictionary<NetUserId, (RestThreadChannel channel, GameRunLevel lastRunLevel)> _relayMessages = new();
        /// <summary>
        ///     A dictionary of old relays that were removed from the relay messages.
        ///     This is used to give a reference to the old relay channel to the admins. If for example, the round restarts, as the relay messages are cleared, the old relays are moved here.
        /// </summary>
        private readonly Dictionary<NetUserId, (RestThreadChannel channel, GameRunLevel lastRunLevel)> _oldRelayMessages = new();
        private ulong _channelId = 0;
        private bool _showThatTheMessageWasFromDiscord = true;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");

            Subs.CVar(_config, CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged, true);
            Subs.CVar(_config, CVars.GameHostName, OnServerNameChanged, true);
            Subs.CVar(_config, CCVars.AdminAhelpOverrideClientName, OnOverrideChanged, true);
            Subs.CVar(_config, CCVars.AdminAhelpRelayChannelId, OnChannelIdChanged, true);
            Subs.CVar(_config, CCVars.AdminAhelpRelayShowDiscord, OnShowDiscordChanged, true);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _discord.OnMessageReceived += OnDiscordMessageReceived;
            _discord.OnCommandReceived += OnReceiveNewRelay;

            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
            SubscribeNetworkEvent<BwoinkClientTypingUpdated>(OnClientTypingUpdated);
        }

        private void OnShowDiscordChanged(bool show)
        {
            _showThatTheMessageWasFromDiscord = show;
        }
        private async void OnReceiveNewRelay(CommandReceivedEventArgs commandArgs)
        {
            if (commandArgs.Command != "ahelp")
                return;

            // Check if args are valid
            if (commandArgs.Arguments.Length != 1)
            {
                // Don't respond to user, multiple servers may be running on the same bot.
                return;
            }

            // Try to find user
            var username = commandArgs.Arguments[0];
            if (!_playerManager.TryGetSessionByUsername(username, out var session))
            {
                // Don't respond to user, multiple servers may be running on the same bot.
                return;
            }

            // Check if user is already in ahelp
            if (_relayMessages.TryGetValue(session.UserId, out var relay))
            {
                await commandArgs.Message.Channel.SendMessageAsync("**Warning**: There is already an ahelp thread for this player.");
                await commandArgs.Message.Channel.SendMessageAsync($"<#{relay.channel.Id}>");
                return;
            }

            var charName = _minds.GetCharacterName(session.UserId);
            var lookup = await _playerLocator.LookupIdAsync(session.UserId);

            if (lookup is null)
            {
                _sawmill.Error($"Failed to lookup Discord ID for {charName} ({session.UserId}).");
                await commandArgs.Message.Channel.SendMessageAsync("**Warning**: Failed to lookup Discord ID for player.");
                return;
            }

            // Create new ahelp thread
            var channel = await RequestNewRelay($"{lookup.Username} ({charName}) - {_gameTicker.RoundId}", session.UserId);
            if (channel is null)
            {
                await commandArgs.Message.Channel.SendMessageAsync("**Warning**: Failed to create ahelp thread.");
                return;
            }

            // Add to relay messages
            relay = (channel, _gameTicker.RunLevel);
            _relayMessages[session.UserId] = relay;

            // Notify admins
            await channel.SendMessageAsync($"**Info:** Thread created. Further messages will be relayed to the player. <@{commandArgs.Message.Author.Id}>");
            await commandArgs.Message.Channel.SendMessageAsync($"<#{relay.channel.Id}>");
        }

        private void OnChannelIdChanged(string obj)
        {
            if (string.IsNullOrEmpty(obj))
            {
                // No warning, this is a valid case. The relay is just disabled.
                return;
            }

            if (!ulong.TryParse(obj, out var id))
            {
                _sawmill.Error("Invalid channel ID.");
                return;
            }

            _channelId = id;
        }

        private void OnDiscordMessageReceived(SocketMessage arg)
        {
            if (arg.Channel is not SocketThreadChannel threadChannel)
                return;

            if (threadChannel.ParentChannel.Id != _channelId)
                return;

            if (arg.Author.IsBot) // Not ignoring bots would probably cause a loop.
                return;

            foreach (var messages in _relayMessages)
            {
                if (messages.Value.channel.Id != threadChannel.Id)
                    continue;

                if (!_playerManager.TryGetSessionById(messages.Key, out var session))
                {
                    _sawmill.Verbose($"Failed to find session for {messages.Key.UserId}.");
                    // Respond with error message to inform admins that the player is not online.
                    arg.Channel.SendMessageAsync("**Warning**: Failed to find session for player. They may not be online.");
                    continue;
                }

                if (arg.Content.Trim().StartsWith("-"))
                    continue; // That is a comment for breadmemes to discuss within the thread

                // Command handling.
                // I do this here instead of OnCommandReceived because I need to know the session and I want to avoid code duplication.
                if (arg.Content.StartsWith(_discord.BotPrefix))
                {
                    var contentWithoutPrefix = arg.Content.Remove(0, _discord.BotPrefix.Length);
                    switch (contentWithoutPrefix)
                    {
                        case "status":
                            var embed = GenerateEmbed(session);
                            arg.Channel.SendMessageAsync(embed: embed.Build());
                            break;
                        default:
                            arg.Channel.SendMessageAsync("**Warning**: Unknown command.");
                            break;
                    }
                    continue;
                }

                // It was originally blue, but that blue tone was too dark, then lucky said I should make it yellow. So now it's yellow.
                var content = "[color=yellow] (d) " + arg.Author.Username + "[/color]: " + arg.Content;
                if (!_showThatTheMessageWasFromDiscord)
                    content = "[color=red]" + arg.Author.Username + "[/color]: " + arg.Content;

                var msg = new BwoinkTextMessage(messages.Key, messages.Key, content);
                RaiseNetworkEvent(msg, session.ConnectedClient);

                LogBwoink(msg);

                foreach (var admin in GetTargetAdmins())
                {
                    RaiseNetworkEvent(msg, admin);
                }
            }
        }

        private void OnOverrideChanged(string obj)
        {
            _overrideClientName = obj;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (_relayMessages.TryGetValue(e.Session.UserId, out var relay))
            {
                switch (e.NewStatus)
                {
                    case SessionStatus.Connected:
                        relay.channel.SendMessageAsync($"**Warning**: {e.Session.Name} has reconnected. Further messages will be relayed to them.");
                        // Also send it in the in-game chat. This is to make sure admins in-game are aware of the reconnection.
                        foreach (var admin in GetTargetAdmins())
                        {
                            RaiseNetworkEvent(new BwoinkTextMessage(e.Session.UserId, e.Session.UserId, "Player has reconnected."), admin);
                        }
                        break;
                    case SessionStatus.Disconnected:
                        relay.channel.SendMessageAsync($"**Warning**: {e.Session.Name} has disconnected. Any messages sent to them will not be received.");
                        foreach (var admin in GetTargetAdmins())
                        {
                            RaiseNetworkEvent(new BwoinkTextMessage(e.Session.UserId, e.Session.UserId, "Player has disconnected."), admin);
                        }
                        break;
                }
            }

            if (e.NewStatus != SessionStatus.InGame)
                return;

            RaiseNetworkEvent(new BwoinkDiscordRelayUpdated(_discord.IsConnected), e.Session);
        }

        private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
        {
            foreach (var relayMessage in _relayMessages)
            {
                var text = args.New switch
                {
                    GameRunLevel.PreRoundLobby => "\n\n:arrow_forward: _**Pre-round lobby started**_\n",
                    GameRunLevel.InRound => "\n\n:arrow_forward: _**Round started**_\n",
                    GameRunLevel.PostRound => "\n\n:stop_button: _**Post-round started**_\n",
                    _ => throw new ArgumentOutOfRangeException(nameof(_gameTicker.RunLevel),
                        $"{_gameTicker.RunLevel} was not matched.")
                };

                relayMessage.Value.channel.SendMessageAsync(text);
            }

            // Don't make a new embed if we
            // 1. were in the lobby just now, and
            // 2. are not entering the lobby or directly into a new round.
            if (args.Old is GameRunLevel.PreRoundLobby ||
                args.New is not (GameRunLevel.PreRoundLobby or GameRunLevel.InRound))
            {
                return;
            }

            _oldRelayMessages.Clear();
            foreach (var item in _relayMessages)
            {
                _oldRelayMessages.Add(item.Key, item.Value);
            }

            // Send info message to the relay channels, that the relay is now OFF.
            foreach (var relayMessage in _relayMessages)
            {
                relayMessage.Value.channel.SendMessageAsync("**Warning**: Relay closed. Server is restarting or round ended. Any messages sent to will not be received.");
            }

            _relayMessages.Clear();
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

        public override void Shutdown()
        {
            base.Shutdown();

            _sawmill.Verbose("Shutting down AHELP system. Sending shutdown messages to all relay channels.");
            foreach (var message in _relayMessages)
            {
                message.Value.channel.SendMessageAsync("**Warning**: Server is shutting down. Any messages sent to will not be received.", false, null,null, AllowedMentions.None);
            }

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _discord.OnMessageReceived -= OnDiscordMessageReceived;
            _discord.OnCommandReceived -= OnReceiveNewRelay;

            _relayMessages.Clear(); // Just in case.
            _oldRelayMessages.Clear();
        }



        private void OnFooterIconChanged(string url)
        {
            _footerIconUrl = url;
        }

        private async void ProcessQueue(NetUserId userId, Queue<string> messages)
        {
            // If there is no relay, we need a new one.
            if (!_relayMessages.TryGetValue(userId, out var relay))
            {
                var charName = _minds.GetCharacterName(userId);
                var lookup = await _playerLocator.LookupIdAsync(userId);

                if (lookup is null)
                {
                    _sawmill.Error($"Failed to lookup Discord ID for {charName} ({userId}).");
                    return;
                }

                var newRelay = await RequestNewRelay($"{lookup.Username} ({charName}) - {_gameTicker.RoundId}", userId);
                if (newRelay is null)
                {
                    _processingChannels.Remove(userId);
                    return;
                }

                relay = (newRelay, _gameTicker.RunLevel);
                _relayMessages[userId] = relay;
            }

            while (messages.TryDequeue(out var msg))
            {
                if (msg.Length > 2000) // Discord message limit
                {
                    var split = Regex.Split(msg, @"(?<=\G.{2000})", RegexOptions.Singleline);
                    foreach (var s in split)
                    {
                        // This looks fucking awful. Thanks discord.net
                        await relay.channel.SendMessageAsync(s, false, null, null, new AllowedMentions(AllowedMentionTypes.None));
                    }
                }
                else
                {
                    await relay.channel.SendMessageAsync(msg, false, null, null, new AllowedMentions(AllowedMentionTypes.None));
                }
            }

            _processingChannels.Remove(userId);
        }

        private async Task<RestThreadChannel?> RequestNewRelay(string title, NetUserId targetPlayer)
        {
            if (!_discord.IsConnected)
                return null;
            if (!_playerManager.TryGetSessionById(targetPlayer, out var playerUid))
            {
                _sawmill.Error($"Requested new relay, but player session for {targetPlayer.UserId} was not found.");
                return null;
            }

            var embed = GenerateEmbed(playerUid);

            var guild = _discord.GetGuild();
            if (guild is null)
            {
                _sawmill.Error("Requested new relay, but guild was not found.");
                return null;
            }

            var relay = await guild
                    .GetForumChannel(_channelId)
                    .CreatePostAsync(title,
                        ThreadArchiveDuration.OneHour,
                        null,
                        null,
                        embed.Build()
                    );

            // If the relay is not null, give a list of available commands for the admins to use.
            if (relay is not null)
            {
                await relay.SendMessageAsync($"Use `{_discord.BotPrefix}status` to regenerate the status embed as seen above. `-` in the beginning of a message will be cause the message to not be relayed to the player.");

                // Check if there are old relays for this player, if so, inform the admins about it.
                if (_oldRelayMessages.TryGetValue(targetPlayer, out var oldRelay))
                {
                    await relay.SendMessageAsync($"**Info**: Jump to previous thread: <#{oldRelay.channel.Id}>");
                    _oldRelayMessages.Remove(targetPlayer);
                }
            }

            return relay;
        }

        private EmbedBuilder GenerateEmbed(ICommonSession session)
        {
            var clr = GetNonAfkAdmins().Count > 0 ? Color.Green : Color.Red;
            var job = "No entity attached";
            var entityName = "No entity attached";
            if (session.AttachedEntity.HasValue)
            {
                if (_idCardSystem.TryFindIdCard(session.AttachedEntity.Value, out var idCard))
                {
                    job = idCard.Comp.JobTitle ?? "Unknown";
                }

                var name = Prototype(session.AttachedEntity.Value);
                if (name != null)
                {
                    entityName = name.ID;
                }
            }

            var gameRules = string.Join(", ", _gameTicker.GetActiveGameRules()
                .Select(addedGameRule => _entityManager.MetaQuery.GetComponent(addedGameRule))
                .Select(meta => meta.EntityPrototype?.ID ?? meta.EntityPrototype?.Name ?? "Unknown")
                .ToList());

            if (gameRules == string.Empty)
                gameRules = "None";

            var playerInfo = _adminSystem.GetCachedPlayerInfo(session.UserId);
            var antagStatus = "Unknown";
            if (playerInfo != null)
            {
                antagStatus = playerInfo.Antag ? "Yes" : "No";
            }

            var embed = new EmbedBuilder()
            {
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = _footerIconUrl
                },
                Fields = new List<EmbedFieldBuilder>()
                {
                    new()
                    {
                        Name = "Current Preset",
                        Value = _gameTicker.Preset?.ID ?? "Unknown"
                    },
                    new()
                    {
                        Name = "Active Gamerules",
                        Value = gameRules
                    },
                    new()
                    {
                        Name = "Server Name",
                        Value = _serverName
                    },
                    new()
                    {
                        Name = "Round ID",
                        Value = _gameTicker.RoundId
                    },
                    new()
                    {
                        Name = "Antag Status",
                        Value = antagStatus
                    },
                    new()
                    {
                        Name = "Map",
                        Value = _mapManager.GetSelectedMap()?.MapName ?? "Unknown"
                    },
                    new()
                    {
                        Name = "Job on ID card",
                        Value = job
                    },
                    new()
                    {
                        Name = "Current Entity Name",
                        Value = entityName
                    },
                    new()
                    {
                        Name = "Run Level",
                        Value = _gameTicker.RunLevel.ToString()
                    }
                },
                Color = clr,
            };

            return embed;
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

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            var personalChannel = senderSession.UserId == message.UserId;
            var senderAdmin = _adminManager.GetAdminData(senderSession);
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var authorized = personalChannel || senderAHelpAdmin;
            if (!authorized)
            {
                _sawmill.Warning($"Unauthorized bwoink from {senderSession.Name} ({senderSession.UserId}) to {message.UserId}");
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            string bwoinkText;

            if (senderAdmin is not null && senderAdmin.Flags == AdminFlags.Adminhelp) // Mentor. Not full admin. That's why it's colored differently.
            {
                bwoinkText = $"[color=purple]{senderSession.Name}[/color]";
            }
            else if (senderAdmin is not null && senderAdmin.HasFlag(AdminFlags.Adminhelp))
            {
                bwoinkText = $"[color=red]{senderSession.Name}[/color]";
            }
            else
            {
                bwoinkText = $"{senderSession.Name}";
            }

            bwoinkText = $"{(message.PlaySound ? "" : "(S) ")}{bwoinkText}: {escapedText}";

            // If it's not an admin / admin chooses to keep the sound then play it.
            var playSound = !senderAHelpAdmin || message.PlaySound;
            var msg = new BwoinkTextMessage(message.UserId, senderSession.UserId, bwoinkText, playSound: playSound);

            LogBwoink(msg);

            var admins = GetTargetAdmins();

            // Notify all admins
            foreach (var channel in admins)
            {
                RaiseNetworkEvent(msg, channel);
            }

            // Notify player
            if (_playerManager.TryGetSessionById(message.UserId, out var session))
            {
                if (!admins.Contains(session.Channel))
                {
                    // If _overrideClientName is set, we generate a new message with the override name. The admins name will still be the original name for the webhooks.
                    if (_overrideClientName != string.Empty)
                    {
                        string overrideMsgText;
                        // Doing the same thing as above, but with the override name. Theres probably a better way to do this.
                        if (senderAdmin is not null && senderAdmin.Flags == AdminFlags.Adminhelp) // Mentor. Not full admin. That's why it's colored differently.
                        {
                            overrideMsgText = $"[color=purple]{_overrideClientName}[/color]";
                        }
                        else if (senderAdmin is not null && senderAdmin.HasFlag(AdminFlags.Adminhelp))
                        {
                            overrideMsgText = $"[color=red]{_overrideClientName}[/color]";
                        }
                        else
                        {
                            overrideMsgText = $"{senderSession.Name}"; // Not an admin, name is not overridden.
                        }

                        overrideMsgText = $"{(message.PlaySound ? "" : "(S) ")}{overrideMsgText}: {escapedText}";

                        RaiseNetworkEvent(new BwoinkTextMessage(message.UserId, senderSession.UserId, overrideMsgText, playSound: playSound), session.Channel);
                    }
                    else
                        RaiseNetworkEvent(msg, session.Channel);
                }
            }

            var sendsWebhook = _discord.IsConnected && _channelId != 0;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.UserId))
                    _messageQueues[msg.UserId] = new Queue<string>();

                var nonAfkAdmins = GetNonAfkAdmins();
                _messageQueues[msg.UserId].Enqueue(GenerateAHelpMessage(senderSession.Name, EscapeMarkdown(message.Text), !personalChannel, nonAfkAdmins.Count == 0));
            }

            if (admins.Count != 0 || sendsWebhook)
                return;

            // No admin online, let the player know
            var systemText = Loc.GetString("bwoink-system-starmute-message-no-other-users");
            var starMuteMsg = new BwoinkTextMessage(message.UserId, SystemUserId, systemText);
            RaiseNetworkEvent(starMuteMsg, senderSession.Channel);
        }

        private IList<INetChannel> GetNonAfkAdmins()
        {
            return _adminManager.ActiveAdmins
                .Where(p => (_adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false) && !_afkManager.IsAfk(p))
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

        private string GenerateAHelpMessage(string username, string message, bool admin, bool noReceivers = false, bool playedSound = false)
        {
            var stringbuilder = new StringBuilder();

            stringbuilder.Append($@"`{_gameTicker.RoundDuration():hh\:mm\:ss}` - ");
            if (admin)
                stringbuilder.Append(":outbox_tray:");
            else if (noReceivers)
                stringbuilder.Append(":sos:");
            else
                stringbuilder.Append(":inbox_tray:");

            if (!playedSound)
                stringbuilder.Append(" **(S)**");

            stringbuilder.Append($" **{username}:** ");
            stringbuilder.Append(message);
            return stringbuilder.ToString();
        }

        private static string EscapeMarkdown(string text)
        {
            return Regex.Replace(text, @"([*_`])|(```)", "\\$1");
        }
    }
}

