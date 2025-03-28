using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly DiscordWebhook _webhook = default!;

    [GeneratedRegex(@"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$")]
    private static partial Regex DiscordRegex();

    private readonly HttpClient _httpClient = new();

    private bool _lockProcessing;
    private AdminLogDiscordRelayInteraction? _relayMessage = null;

    private string _footerIconUrl = string.Empty;
    private string _avatarUrl = string.Empty;
    private string _serverName = string.Empty;
    private string _webhookUrl = string.Empty;

    private readonly Queue<string> _messageQueue = new();
    private WebhookData? _webhookData;
    private int _maxAdditionalChars;

    public void InitializeWebhook()
    {
        _configuration.OnValueChanged(CCVars.DiscordAdminLogWebhook, OnWebhookChanged, true);
        _configuration.OnValueChanged(CCVars.DiscordAHelpFooterIcon, OnFooterIconChanged, true);
        _configuration.OnValueChanged(CCVars.DiscordAHelpAvatar, OnAvatarChanged, true);
        _configuration.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);

        var defaultParams = new AdminLogMessageParams(
            string.Empty,
            TimeSpan.Zero.ToString("hh\\:mm\\:ss"),
            _runLevel,
            null
        );
        _maxAdditionalChars = GenerateAdminLogMessage(defaultParams).Length;
    }

    public void UpdateWebhook()
    {
        if (_lockProcessing)
            return;

        var queue = _messageQueue;
        if (queue.Count == 0)
            return;

        _lockProcessing = true;

        ProcessQueue(queue);
    }

    #region OnChanged

    private async void OnWebhookChanged(string url)
    {
        _webhookUrl = url;

        if (url == string.Empty)
            return;

        if (!_webhook.TryGetWebhookIdToken(url, out var webhookId, out var webhookToken))
            return;

        // Fire and forget
        _webhookData = await _webhook.GetWebhookData(webhookId, webhookToken);
    }

    private void OnFooterIconChanged(string url)
    {
        _footerIconUrl = url;
    }

    private void OnAvatarChanged(string url)
    {
        _avatarUrl = url;
    }

    private void OnServerNameChanged(string obj)
    {
        _serverName = obj;
    }

    public void WebhookOnGameRunLevelChanged()
    {
        _relayMessage = null;
    }

    #endregion

    private void SendAdminLogWebhook(string message, LogImpact? logImpact = null)
    {
        // Enqueue the message for Discord relay
        if (_webhookUrl != string.Empty)
        {
            // Get the current timestamp
            var roundTime = System.TimeSpan.Zero.ToString("hh\\:mm\\:ss");
            if (_entSys.TryGetEntitySystem<GameTicker>(out var gameTicker))
                roundTime = gameTicker.RoundDuration().ToString("hh\\:mm\\:ss");

            var str = message;
            if (str.Length + _maxAdditionalChars > DiscordWebhook.DescriptionMax)
            {
                str = str[..(DiscordWebhook.DescriptionMax - _maxAdditionalChars)];
            }

            // Create the message parameters for Discord
            var messageParams = new AdminLogMessageParams(
                str,
                roundTime,
                _runLevel,
                logImpact
            );

            var discordMessage = GenerateAdminLogMessage(messageParams);
            _messageQueue.Enqueue(discordMessage);
        }
    }

    private async void ProcessQueue(Queue<string> messages)
    {
        // Whether an embed already exists for this player
        var exists = _relayMessage != null;

        // Whether the message will become too long after adding these new messages
        var tooLong = exists && messages.Sum(msg => Math.Min(msg.Length, DiscordWebhook.MessageLengthCap) + "\n".Length)
            + _relayMessage?.Description.Length > DiscordWebhook.DescriptionMax;

        // If there is no existing embed, or it is getting too long, we create a new embed
        if (!exists || tooLong)
        {
            var linkToPrevious = string.Empty;

            // If we have all the data required, we can link to the embed of the previous round or embed that was too long
            if (_webhookData is { GuildId: { } guildId, ChannelId: { } channelId })
            {
                if (tooLong && _relayMessage?.Id != null)
                {
                    linkToPrevious =
                        $"**[Go to previous embed of this round](https://discord.com/channels/{guildId}/{channelId}/{_relayMessage.Id})**\n";
                }
            }

            _relayMessage = new AdminLogDiscordRelayInteraction()
            {
                Id = null,
                Description = linkToPrevious,
                LastRunLevel = _runLevel,
            };
        }

        // Previous message was in another RunLevel, so show that in the embed
        if (_relayMessage!.LastRunLevel != _runLevel)
        {
            _relayMessage.Description += _runLevel switch
            {
                GameRunLevel.PreRoundLobby => "\n\n:arrow_forward: _**Pre-round lobby started**_\n",
                GameRunLevel.InRound => "\n\n:arrow_forward: _**Round started**_\n",
                GameRunLevel.PostRound => "\n\n:stop_button: _**Post-round started**_\n",
                _ => throw new ArgumentOutOfRangeException(nameof(_runLevel),
                    $"{_runLevel} was not matched."),
            };

            _relayMessage.LastRunLevel = _runLevel;
        }

        // Add available messages to the embed description
        while (messages.TryDequeue(out var message))
        {
            _relayMessage.Description += $"\n{_webhook.TrimMessage(message)}";
        }

        var payload = GeneratePayload(_relayMessage.Description);

        // Attempt to post the payload; if failed, wipe the relay message since it could not be posted/found.
        var success = await _webhook.PostPayload(_relayMessage, _webhookUrl, payload);
        if (!success)
            _relayMessage = null;

        _lockProcessing = false;
    }

    private string GenerateAdminLogMessage(AdminLogMessageParams parameters)
    {
        var stringbuilder = new StringBuilder();

        if (parameters.Impact == LogImpact.Extreme)
            stringbuilder.Append(":red_square: ");
        else if (parameters.Impact == LogImpact.High)
            stringbuilder.Append(":orange_square: ");
        else if (parameters.Impact == LogImpact.Medium)
            stringbuilder.Append(":blue_square: ");
        else if (parameters.Impact == LogImpact.Low)
            stringbuilder.Append(":green_square: ");

        if (parameters.RoundTime != string.Empty && parameters.RoundState == GameRunLevel.InRound)
            stringbuilder.Append($" **{parameters.RoundTime}** ");

        stringbuilder.Append(parameters.Message);

        return stringbuilder.ToString();
    }

    private WebhookPayload GeneratePayload(string messages)
    {
        // Limit server name to 1500 characters, in case someone tries to be a little funny
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];

        int roundId = 0;
        if (_entSys.TryGetEntitySystem<GameTicker>(out var gameTicker))
            roundId = gameTicker.RoundId;

        var round = _runLevel switch
        {
            GameRunLevel.PreRoundLobby => roundId == 0
                ? "pre-round lobby after server restart" // first round after server restart has ID == 0
                : $"pre-round lobby for round {roundId + 1}",
            GameRunLevel.InRound => $"round {roundId}",
            GameRunLevel.PostRound => $"post-round {roundId}",
            _ => throw new ArgumentOutOfRangeException(nameof(_runLevel),
                $"{_runLevel} was not matched."),
        };

        return new WebhookPayload
        {
            AvatarUrl = string.IsNullOrWhiteSpace(_avatarUrl) ? null : _avatarUrl,
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Description = messages,
                    Color = 0xadadad,
                    Footer = new WebhookEmbedFooter
                    {
                        Text = $"{serverName} ({round})",
                        IconUrl = string.IsNullOrWhiteSpace(_footerIconUrl) ? null : _footerIconUrl
                    },
                },
            },
        };
    }
}

public sealed class AdminLogMessageParams
{
    public string Message { get; set; }
    public string RoundTime { get; set; }
    public GameRunLevel RoundState { get; set; }

    public LogImpact? Impact { get; set; }

    public AdminLogMessageParams(
        string message,
        string roundTime,
        GameRunLevel roundState,
        LogImpact? impact)
    {
        Message = message;
        RoundTime = roundTime;
        RoundState = roundState;
        Impact = impact;
    }
}

internal sealed class AdminLogDiscordRelayInteraction : DiscordRelayInteraction
{
    /// <summary>
    /// Run level of the last interaction. If different we'll link to the last Id.
    /// </summary>
    public GameRunLevel LastRunLevel;
}
