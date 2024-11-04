using System.Text.Json.Serialization;

namespace Content.Server.Discord;

public struct WebhookEmbedImage
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public WebhookEmbedImage()
    {
    }
}
