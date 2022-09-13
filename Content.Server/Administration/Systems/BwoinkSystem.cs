using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        private ISawmill _sawmill = default!;
        private readonly HttpClient _httpClient = new();
        private string _webhookUrl = string.Empty;
        private string _footerIconUrl = string.Empty;
        private string _avatarUrl = string.Empty;
        private string _serverName = string.Empty;
        private readonly Dictionary<NetUserId, (string id, string username, string messages, string? characterName)> _relayMessages = new();
        private readonly Dictionary<NetUserId, Queue<string>> _messageQueues = new();
        private readonly HashSet<NetUserId> _processingChannels = new();

        // Max embed description length is 4096, according to https://discord.com/developers/docs/resources/channel#embed-object-embed-limits
        // Keep small margin, just to be safe
        private const ushort DescriptionMax = 4000;
        private int _maxAdditionalChars;

        public override void Initialize()
        {
            base.Initialize();
            _config.OnValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged, true);
            _config.OnValueChanged(CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged, true);
            _config.OnValueChanged(CCVars.DiscordAHelpAvatar, OnAvatarChanged, true);
            _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");
            _maxAdditionalChars = GenerateAHelpMessage("", "", true).Length;

            SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
        }

        private void RoundStarting(RoundStartingEvent ev)
        {
            _relayMessages.Clear();
        }

        private void OnServerNameChanged(string obj)
        {
            _serverName = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _config.UnsubValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged);
            _config.UnsubValueChanged(CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged);
            _config.UnsubValueChanged(CVars.GameHostName, OnServerNameChanged);
        }

        private void OnWebhookChanged(string obj)
        {
            _webhookUrl = obj;
        }

        private void OnFooterIconChanged(string url)
        {
            _footerIconUrl = url;
        }

        private void OnAvatarChanged(string url)
        {
            _avatarUrl = url;
        }

        private async void ProcessQueue(NetUserId channelId, Queue<string> messages)
        {
            if (!_relayMessages.TryGetValue(channelId, out var oldMessage) || messages.Sum(x => x.Length + 2) + oldMessage.messages.Length > DescriptionMax)
            {
                var lookup = await _playerLocator.LookupIdAsync(channelId);

                if (lookup == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Unable to find player for netuserid {channelId} when sending discord webhook.");
                    _relayMessages.Remove(channelId);
                    return;
                }

                var characterName = _playerManager.GetPlayerData(channelId).ContentData()?.Mind?.CharacterName;

                oldMessage = (string.Empty, lookup.Username, string.Empty, characterName);
            }

            while (messages.TryDequeue(out var message))
            {
                oldMessage.messages += $"\n{message}";
            }

            var payload = GeneratePayload(oldMessage.messages, oldMessage.username, oldMessage.characterName);

            if (oldMessage.id == string.Empty)
            {
                var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                var content = await request.Content.ReadAsStringAsync();
                if (!request.IsSuccessStatusCode)
                {
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                var id = JsonNode.Parse(content)?["id"];
                if (id == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                oldMessage.id = id.ToString();
            }
            else
            {
                var request = await _httpClient.PatchAsync($"{_webhookUrl}/messages/{oldMessage.id}",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!request.IsSuccessStatusCode)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when patching message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }
            }

            _relayMessages[channelId] = oldMessage;

            _processingChannels.Remove(channelId);
        }

        private WebhookPayload GeneratePayload(string messages, string username, string? characterName = null)
        {
            // Add character name
            if (characterName != null)
                username += $" ({characterName})";

            // If no admins are online, set embed color to red. Otherwise green
            var color = GetTargetAdmins().Count > 0 ? 0x41F097 : 0xFF0000;

            // Limit server name to 1500 characters, in case someone tries to be a little funny
            var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];

            // If the round ID is 0, it most likely means we are in the lobby
            var round = _gameTicker.RoundId == 0 ? "lobby" : $"round {_gameTicker.RoundId}";

            return new WebhookPayload
            {
                Username = username,
                AvatarUrl = _avatarUrl,
                Embeds = new List<Embed>
                {
                    new Embed
                    {
                        Description = messages,
                        Color = color,
                        Footer = new EmbedFooter
                        {
                            Text = $"{serverName} ({round})",
                            IconUrl = _footerIconUrl,
                        },
                    },
                },
            };
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var channelId in _messageQueues.Keys.ToArray())
            {
                if (_processingChannels.Contains(channelId))
                    continue;

                var queue = _messageQueues[channelId];
                _messageQueues.Remove(channelId);
                if (queue.Count == 0)
                    continue;

                _processingChannels.Add(channelId);

                ProcessQueue(channelId, queue);
            }
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = (IPlayerSession) eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            var personalChannel = senderSession.UserId == message.ChannelId;
            var senderAdmin = _adminManager.GetAdminData(senderSession);
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var authorized = personalChannel || senderAHelpAdmin;
            if (!authorized)
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            var bwoinkText = senderAdmin switch
            {
                var x when x is not null && x.Flags == AdminFlags.Adminhelp =>
                    $"[color=purple]{senderSession.Name}[/color]: {escapedText}",
                var x when x is not null && x.HasFlag(AdminFlags.Adminhelp) =>
                    $"[color=red]{senderSession.Name}[/color]: {escapedText}",
                _ => $"{senderSession.Name}: {escapedText}",
            };

            var msg = new BwoinkTextMessage(message.ChannelId, senderSession.UserId, bwoinkText);

            LogBwoink(msg);

            var admins = GetTargetAdmins();

            // Notify all admins
            foreach (var channel in admins)
            {
                RaiseNetworkEvent(msg, channel);
            }

            // Notify player
            if (_playerManager.TryGetSessionById(message.ChannelId, out var session))
            {
                if (!admins.Contains(session.ConnectedClient))
                    RaiseNetworkEvent(msg, session.ConnectedClient);
            }

            var sendsWebhook = _webhookUrl != string.Empty;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.ChannelId))
                    _messageQueues[msg.ChannelId] = new Queue<string>();

                var str = message.Text;
                var unameLength = senderSession.Name.Length;

                if (unameLength + str.Length + _maxAdditionalChars > DescriptionMax)
                {
                    str = str[..(DescriptionMax - _maxAdditionalChars - unameLength)];
                }
                _messageQueues[msg.ChannelId].Enqueue(GenerateAHelpMessage(senderSession.Name, str, !personalChannel, admins.Count == 0));
            }

            if (admins.Count != 0)
                return;

            // No admin online, let the player know
            var systemText = sendsWebhook ?
                Loc.GetString("bwoink-system-starmute-message-no-other-users-webhook") :
                Loc.GetString("bwoink-system-starmute-message-no-other-users");
            var starMuteMsg = new BwoinkTextMessage(message.ChannelId, SystemUserId, systemText);
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

        private static string GenerateAHelpMessage(string username, string message, bool admin, bool noReceivers = false)
        {
            var stringbuilder = new StringBuilder();

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

        // https://discord.com/developers/docs/resources/channel#message-object-message-structure
        private struct WebhookPayload
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = "";

            [JsonPropertyName("avatar_url")]
            public string AvatarUrl { get; set; } = "";

            [JsonPropertyName("embeds")]
            public List<Embed>? Embeds { get; set; } = null;

            [JsonPropertyName("allowed_mentions")]
            public Dictionary<string, string[]> AllowedMentions { get; set; } =
                new()
                {
                    { "parse", Array.Empty<string>() },
                };

            public WebhookPayload() { }
        }

        // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
        private struct Embed
        {
            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [JsonPropertyName("color")]
            public int Color { get; set; } = 0;

            [JsonPropertyName("footer")]
            public EmbedFooter? Footer { get; set; } = null;

            public Embed()
            {
            }
        }

        // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
        private struct EmbedFooter
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            [JsonPropertyName("icon_url")]
            public string IconUrl { get; set; } = "";

            public EmbedFooter()
            {
            }
        }
    }
}

