using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
public struct WebhookEmbed
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("color")]
    public int Color { get; set; } = 0;

    [JsonPropertyName("footer")]
    public WebhookEmbedFooter? Footer { get; set; } = null;

    public WebhookEmbed()
    {
    }
}
