using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
public struct WebhookData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("user")]
    public WebhookUser? User { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public WebhookIdentifier ToIdentifier()
    {
        return new WebhookIdentifier(Id, Token);
    }
}
