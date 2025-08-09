using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
/// <see href="https://docs.github.com/en/rest/apps/apps?apiVersion=2022-11-28#create-an-installation-access-token-for-an-app"/>>
/// </summary>
public sealed class TokenRequest : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Post;

    public GithubAuthMethod AuthenticationMethod => GithubAuthMethod.JWT;

    [JsonPropertyName("id")]
    public required int InstallationId;

    public string GetLocation(string owner, string repository)
    {
        return $"/app/installations/{InstallationId}/access_tokens";
    }
}
