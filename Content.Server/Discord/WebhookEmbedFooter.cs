using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
public struct WebhookEmbedFooter
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    public WebhookEmbedFooter()
    {
    }
}
