using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
public struct WebhookEmbed
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("color")]
    public int Color { get; set; } = 0;

    [JsonPropertyName("footer")]
    public WebhookEmbedFooter? Footer { get; set; } = null;


    [JsonPropertyName("fields")]
    public List<WebhookEmbedField> Fields { get; set; } = default!;

    public WebhookEmbed()
    {
    }
}
