using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
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
    
    private string _discordWebhook = String.Empty;
    private string _discordRoleId = String.Empty;
    
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);

        _config.OnValueChanged(CCVars.DiscordRoundWebhook, value => _discordWebhook = value, true);
        _config.OnValueChanged(CCVars.DiscordRoundRoleId, value => _discordRoleId = value, true);

        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("notifications");
    }

    private void OnRoundRestart(RoundRestartCleanupEvent e)
    {
        if (String.IsNullOrEmpty(_discordWebhook))
            return;

        var payload = new WebhookPayload()
        {
            Content = Loc.GetString("discord-round-new"),
        };

        if (!String.IsNullOrEmpty(_discordRoleId))
        {
            payload = new WebhookPayload()
            {
                Content = $"<@&{_discordRoleId}> {Loc.GetString("discord-round-new")}",
                AllowedMentions = new Dictionary<string, string[]>
                {
                    { "roles", new []{ _discordRoleId } }
                },
            };
        }

        SendDiscordMessage(payload);
    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        if (String.IsNullOrEmpty(_discordWebhook))
            return;

        var map = _gameMapManager.GetSelectedMap();
        var mapName = map?.MapName ?? Loc.GetString("discord-round-unknown-map");
        var text = Loc.GetString("discord-round-start",
            ("id", e.RoundId),
            ("map", mapName));
        var payload = new WebhookPayload() { Content = text };

        SendDiscordMessage(payload);
    }
    
    private void OnRoundEnd(RoundEndMessageEvent e)
    {
        if (String.IsNullOrEmpty(_discordWebhook))
            return;

        var text = Loc.GetString("discord-round-end",
            ("id", e.RoundId),
            ("hours", e.RoundDuration.Hours),
            ("minutes", e.RoundDuration.Minutes),
            ("seconds", e.RoundDuration.Seconds));
        var payload = new WebhookPayload() { Content = text };

        SendDiscordMessage(payload);
    }

    private async void SendDiscordMessage(WebhookPayload payload)
    {
        var request = await _httpClient.PostAsync(_discordWebhook,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
            return;
        }
    }

    private struct WebhookPayload
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

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
}