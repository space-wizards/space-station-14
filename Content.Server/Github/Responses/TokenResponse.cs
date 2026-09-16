using System.Text.Json.Serialization;
using Content.Server.Github.Requests;

namespace Content.Server.Github.Responses;

/// <summary>
/// Response from the GitHub API when requesting an access token.
/// <seealso cref="TokenRequest"/>
/// </summary>
public sealed class TokenResponse
{
    public required string Token { get; set; }

    [JsonPropertyName("expires_at")]
    public required DateTime ExpiresAt { get; set; }
}
