using System.Net.Http;

namespace Content.Server.Github.Requests;

/// <summary>
/// <see href="https://docs.github.com/en/rest/apps/installations?apiVersion=2022-11-28#list-app-installations-accessible-to-the-user-access-token"/>>
/// </summary>
public sealed class InstallationsRequest : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Get;

    public GithubAuthMethod AuthenticationMethod => GithubAuthMethod.JWT;

    public string GetLocation(string owner, string repository)
    {
        return "app/installations";
    }
}
