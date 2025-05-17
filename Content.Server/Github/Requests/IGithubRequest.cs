using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
/// Interface for all github api requests.
/// </summary>
/// <remarks>
/// WARNING: You must add this JsonDerivedType for all requests that have json otherwise they will not parse properly!
/// </remarks>
[JsonDerivedType(typeof(GetRateLimit))]
[JsonDerivedType(typeof(CreateIssue))]
[JsonDerivedType(typeof(GetZen))]
public interface IGithubRequest
{
    /// <summary>
    /// What kind of request should we make for this?
    /// </summary>
    public HttpMethod RequestMethod { get; }

    /// <summary>
    /// Location of the api endpoint for this request.
    /// </summary>
    /// <param name="owner">Owner of the repository.</param>
    /// <param name="repository">The repository to make the request.</param>
    /// <returns>The api location for this request.</returns>
    public string GetLocation(string owner, string repository);
}
