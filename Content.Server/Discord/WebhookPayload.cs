using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/channel#message-object-message-structure
public struct WebhookPayload
{
    /// <summary>
    ///     The message to send in the webhook. Maximum of 2000 characters.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; } = "";

    [JsonPropertyName("embeds")]
    public List<WebhookEmbed>? Embeds { get; set; } = null;

    [JsonPropertyName("allowed_mentions")]
    public WebhookMentions AllowedMentions { get; set; } = new();

    public WebhookPayload()
    {
    }
}
