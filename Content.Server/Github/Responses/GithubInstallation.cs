using System.Text.Json.Serialization;

namespace Content.Server.Github.Responses;

public sealed class GithubInstallation
{
    [JsonPropertyName("access_tokens_url")]
    public string AccessToken { get; set; } = "";
}
