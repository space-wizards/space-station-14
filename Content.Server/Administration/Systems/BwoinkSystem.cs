using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
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
    public sealed class BwoinkSystem : SharedBwoinkSystem
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

        private ISawmill _sawmill = default!;
        private readonly Dictionary<NetUserId, (TimeSpan Timestamp, bool Typing)> _typingUpdateTimestamps = new();
        private string _overrideClientName = string.Empty;

        // Ahelp relay
        private readonly Dictionary<NetUserId, Queue<string>> _messageQueues = new();
        private string _footerIconUrl = string.Empty;
        private string _serverName = string.Empty;
        private readonly HashSet<NetUserId> _processingChannels = new();
        private readonly Dictionary<NetUserId, (RestThreadChannel channel, GameRunLevel lastRunLevel)> _relayMessages = new();
        private ulong _channelId = 0;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");

            _config.OnValueChanged(CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged, true);
            _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
            _config.OnValueChanged(CCVars.AdminAhelpOverrideClientName, OnOverrideChanged, true);
            _config.OnValueChanged(CCVars.AdminAhelpRelayChannelId, OnChannelIdChanged, true);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            if (_discord.Client is not null)
                _discord.Client.MessageReceived += OnDiscordMessageReceived;

            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
            SubscribeNetworkEvent<BwoinkClientTypingUpdated>(OnClientTypingUpdated);
        }

        private void OnChannelIdChanged(string obj)
        {
            if (!ulong.TryParse(obj, out var id))
            {
                _sawmill.Warning("Invalid channel ID.");
                return;
            }

            _channelId = id;
        }

        private Task OnDiscordMessageReceived(SocketMessage arg)
        {
            if (arg.Channel is not SocketThreadChannel threadChannel)
                return Task.CompletedTask;

            if (threadChannel.ParentChannel.Id != _channelId)
                return Task.CompletedTask;

            if (arg.Author.IsBot) // Not ignoring bots would probably cause a loop.
                return Task.CompletedTask;

            foreach (var messages in _relayMessages)
            {
                if (messages.Value.channel.Id != threadChannel.Id)
                    continue;

                if (!_playerManager.TryGetSessionById(messages.Key, out var session))
                {
                    _sawmill.Warning($"Failed to find session for {messages.Key.UserId}.");
                    continue;
                }

                // It was originally blue, but that blue tone was too dark, then lucky said I should make it yellow. So now it's yellow.
                var content = "[color=yellow] (d) " + arg.Author.Username + "[/color]: " + arg.Content;

                var msg = new BwoinkTextMessage(messages.Key, messages.Key, content);
                RaiseNetworkEvent(msg, session.ConnectedClient);

                LogBwoink(msg);

                foreach (var admin in GetTargetAdmins())
                {
                    RaiseNetworkEvent(msg, admin);
                }
            }

            return Task.CompletedTask;
        }

        private void OnOverrideChanged(string obj)
        {
            _overrideClientName = obj;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
                return;

            RaiseNetworkEvent(new BwoinkDiscordRelayUpdated(_discord.Client is not null), e.Session);
        }

        private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
        {
            // Don't make a new embed if we
            // 1. were in the lobby just now, and
            // 2. are not entering the lobby or directly into a new round.
            if (args.Old is GameRunLevel.PreRoundLobby ||
                args.New is not (GameRunLevel.PreRoundLobby or GameRunLevel.InRound))
            {
                return;
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
            _config.UnsubValueChanged(CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged);
            _config.UnsubValueChanged(CVars.GameHostName, OnServerNameChanged);
            _config.UnsubValueChanged(CCVars.AdminAhelpOverrideClientName, OnOverrideChanged);
            _config.UnsubValueChanged(CCVars.AdminAhelpRelayChannelId, OnChannelIdChanged);
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            if (_discord.Client is not null)
                _discord.Client.MessageReceived -= OnDiscordMessageReceived;

            _relayMessages.Clear(); // Just in case.
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
                    _sawmill.Warning($"Failed to lookup Discord ID for {charName} ({userId}).");
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
            if (_discord.Client is null)
                return null;
            var clr = GetTargetAdmins().Count > 0 ? Color.Green : Color.Red;
            if (!_playerManager.TryGetSessionById(targetPlayer, out var playerUid))
            {
                _sawmill.Error($"Requested new relay, but player session for {targetPlayer.UserId} was not found.");
                return null;
            }

            var job = "No entity attached";
            if (playerUid.AttachedEntity.HasValue)
            {
                if (_idCardSystem.TryFindIdCard(playerUid.AttachedEntity.Value, out var idCard))
                {
                    job = idCard.Comp.JobTitle ?? "Unknown";
                }
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
                        Value = string.Join(", ", _gameTicker.GetActiveGameRules()
                            .Select(addedGameRule => _entityManager.MetaQuery.GetComponent(addedGameRule))
                            .Select(meta => meta.EntityPrototype?.ID ?? meta.EntityPrototype?.Name ?? "Unknown")
                            .ToList())
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
                        Value = _adminSystem.PlayerList
                            .Where(p => p.Key == targetPlayer)
                            .Any(p => p.Value.Antag)
                            ? "Yes"
                            : "No"
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
                    }
                },
                Color = clr,

            };

            return await _discord.GetGuild()
                    .GetForumChannel(_channelId)
                    .CreatePostAsync(title,
                        ThreadArchiveDuration.OneHour,
                        null,
                        null,
                        embed.Build()
                    );
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
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            string bwoinkText;

            if (senderAdmin is not null && senderAdmin.Flags == AdminFlags.Adminhelp) // Mentor. Not full admin. That's why it's colored differently.
            {
                bwoinkText = $"[color=purple]{senderSession.Name}[/color]: {escapedText}";
            }
            else if (senderAdmin is not null && senderAdmin.HasFlag(AdminFlags.Adminhelp))
            {
                bwoinkText = $"[color=red]{senderSession.Name}[/color]: {escapedText}";
            }
            else
            {
                bwoinkText = $"{senderSession.Name}: {escapedText}";
            }

            var msg = new BwoinkTextMessage(message.UserId, senderSession.UserId, bwoinkText);

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
                if (!admins.Contains(session.ConnectedClient))
                {
                    // If _overrideClientName is set, we generate a new message with the override name. The admins name will still be the original name for the webhooks.
                    if (_overrideClientName != string.Empty)
                    {
                        string overrideMsgText;
                        // Doing the same thing as above, but with the override name. Theres probably a better way to do this.
                        if (senderAdmin is not null && senderAdmin.Flags == AdminFlags.Adminhelp) // Mentor. Not full admin. That's why it's colored differently.
                        {
                            overrideMsgText = $"[color=purple]{_overrideClientName}[/color]: {escapedText}";
                        }
                        else if (senderAdmin is not null && senderAdmin.HasFlag(AdminFlags.Adminhelp))
                        {
                            overrideMsgText = $"[color=red]{_overrideClientName}[/color]: {escapedText}";
                        }
                        else
                        {
                            overrideMsgText = $"{senderSession.Name}: {escapedText}"; // Not an admin, name is not overridden.
                        }

                        RaiseNetworkEvent(new BwoinkTextMessage(message.UserId, senderSession.UserId, overrideMsgText), session.ConnectedClient);
                    }
                    else
                        RaiseNetworkEvent(msg, session.ConnectedClient);
                }
            }

            var sendsWebhook = _discord.Client is not null && _channelId != 0;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.UserId))
                    _messageQueues[msg.UserId] = new Queue<string>();

                _messageQueues[msg.UserId].Enqueue(GenerateAHelpMessage(senderSession.Name, message.Text, !personalChannel, admins.Count == 0));
            }

            if (admins.Count != 0 || sendsWebhook)
                return;

            // No admin online, let the player know
            var systemText = Loc.GetString("bwoink-system-starmute-message-no-other-users");
            var starMuteMsg = new BwoinkTextMessage(message.UserId, SystemUserId, systemText);
            RaiseNetworkEvent(starMuteMsg, senderSession.ConnectedClient);
        }

        // Returns all online admins with AHelp access
        private IList<INetChannel> GetTargetAdmins()
        {
            return _adminManager.ActiveAdmins
               .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
               .Select(p => p.ConnectedClient)
               .ToList();
        }

        private string GenerateAHelpMessage(string username, string message, bool admin, bool noReceivers = false)
        {
            var stringbuilder = new StringBuilder();

            stringbuilder.Append($@"`{_gameTicker.RoundDuration():hh\:mm\:ss}` - ");

            if (admin)
                stringbuilder.Append(":outbox_tray:");
            else if (noReceivers)
                stringbuilder.Append(":sos:");
            else
                stringbuilder.Append(":inbox_tray:");

            stringbuilder.Append($" **{username}:** ");
            stringbuilder.Append(message);
            return stringbuilder.ToString();
        }
    }
}

