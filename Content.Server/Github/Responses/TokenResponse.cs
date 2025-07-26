using System.Text.Json.Serialization;

namespace Content.Server.Github.Responses;

public sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("expires_at")]
    public DateTime Exp { get; set; } = default!;
}
