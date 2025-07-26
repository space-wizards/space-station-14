using System.Net.Http;

namespace Content.Server.Github.Requests;

public sealed class TokenRequest : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Post;

    public AuthMethod AuthenticationMethodMethod => AuthMethod.JWT;

    public string Location = "";

    public string GetLocation(string owner, string repository)
    {
        return Location;
    }
}
