using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Maps;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.GameTicking;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Server.Corvax.RoundNotifications;

/// <summary>
/// Listen game events and send notifications to Discord
/// </summary>
public sealed class RoundNotificationsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();

    private string _webhookUrl = string.Empty;
    private string _roleId = string.Empty;
    private string _serverName = string.Empty;
    private bool _roundStartOnly;

    private const int EmbedColor = 0xff6600;
    private const int EmbedColorRestart = 0x00eb1f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        _config.OnValueChanged(CCCVars.DiscordRoundWebhook, value => _webhookUrl = value, true);
        _config.OnValueChanged(CCCVars.DiscordRoundRoleId, value => _roleId = value, true);
        _config.OnValueChanged(CCCVars.DiscordRoundStartOnly, value => _roundStartOnly = value, true);
        _config.OnValueChanged(CVars.GameHostName, value => _serverName = value, true);

        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("notifications");
    }

    private void OnRoundRestart(RoundRestartCleanupEvent e)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            return;

        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var payload = new WebhookPayload()
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("discord-round-embed-title", ("server", serverName)),
                    Description = Loc.GetString("discord-round-new"),
                    Color = EmbedColorRestart
                }
            }
        };

        if (!string.IsNullOrEmpty(_roleId))
        {
            payload.Content = $"<@&{_roleId}>";
            payload.AllowedMentions = new Dictionary<string, string[]>
            {
                { "roles", new[] { _roleId } }
            };
        }

        SendDiscordMessage(payload);
    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            return;

        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var map = _gameMapManager.GetSelectedMap();
        var mapName = map?.MapName ?? Loc.GetString("discord-round-unknown-map");
        var text = Loc.GetString("discord-round-start",
            ("id", e.RoundId),
            ("map", mapName));

        var payload = new WebhookPayload()
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("discord-round-embed-title", ("server", serverName)),
                    Description = text,
                    Color = EmbedColor
                }
            }
        };

        SendDiscordMessage(payload);
    }

    private void OnRoundEnded(RoundEndedEvent e)
    {
        if (string.IsNullOrEmpty(_webhookUrl) || _roundStartOnly)
            return;

        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var text = Loc.GetString("discord-round-end",
            ("id", e.RoundId),
            ("hours", e.RoundDuration.Hours),
            ("minutes", e.RoundDuration.Minutes),
            ("seconds", e.RoundDuration.Seconds));

        var payload = new WebhookPayload()
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("discord-round-embed-title", ("server", serverName)),
                    Description = text,
                    Color = EmbedColor
                }
            }
        };

        SendDiscordMessage(payload);
    }

    private async void SendDiscordMessage(WebhookPayload payload)
    {
        var request = await _httpClient.PostAsync(_webhookUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error,
                $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
        }
    }

    private struct WebhookPayload
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("embeds")]
        public List<Embed>? Embeds { get; set; } = null;

        [JsonPropertyName("allowed_mentions")]
        public Dictionary<string, string[]> AllowedMentions { get; set; } =
            new()
            {
                { "parse", Array.Empty<string>() }
            };

        public WebhookPayload()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
    private struct Embed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        public Embed()
        {
        }
    }
}
