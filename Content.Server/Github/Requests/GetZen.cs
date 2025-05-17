using System.Net.Http;

namespace Content.Server.Github.Requests;

/// <summary>
/// <see href="https://docs.github.com/en/rest/meta/meta?apiVersion=2022-11-28#get-the-zen-of-github"/>>
/// </summary>
public sealed class GetZen : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Get;

    public string GetLocation(string owner, string repository)
    {
        return "zen";
    }
}
