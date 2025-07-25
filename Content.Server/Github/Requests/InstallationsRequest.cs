using System.Net.Http;

namespace Content.Server.Github.Requests;

public sealed class InstallationsRequest : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Get;

    public AuthMethod AuthenticationMethodMethod => AuthMethod.JWT;

    public string GetLocation(string owner, string repository)
    {
        return "app/installations";
    }
}
