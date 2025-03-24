using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
///     <see href="https://docs.github.com/en/rest/rate-limit/rate-limit?apiVersion=2022-11-28"/>>
/// </summary>
public sealed class GetRateLimit : IGithubRequest
{
    [JsonIgnore]
    public HttpMethod RequestMethod => HttpMethod.Get;

    public string GetLocation(string owner, string repository)
    {
        return "rate_limit";
    }

    // The spec says that 304 is also valid but there is currently no way to handle that so we will treat it as an error.
    public List<HttpStatusCode> GetExpectedResponseCodes()
    {
        return [ HttpStatusCode.OK ];
    }
}
