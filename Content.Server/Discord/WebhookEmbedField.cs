using System.Text.Json.Serialization;

namespace Content.Server.Discord;

// https://discord.com/developers/docs/resources/channel#embed-object-embed-field-structure
public struct WebhookEmbedField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("inline")]
    public bool Inline { get; set; } = true;

    public WebhookEmbedField()
    {
    }
}
