using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
/// Interface for all github api requests.
/// </summary>
/// <remarks>
/// WARNING: You must add this JsonDerivedType for all requests that have json otherwise they will not parse properly!
/// </remarks>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(CreateIssueRequest))]
[JsonDerivedType(typeof(InstallationsRequest))]
[JsonDerivedType(typeof(TokenRequest))]
public interface IGithubRequest
{
    /// <summary>
    /// The kind of request method for the request.
    /// </summary>
    [JsonIgnore]
    public HttpMethod RequestMethod { get; }

    /// <summary>
    /// There are different types of authentication methods depending on which endpoint you are working with.
    /// E.g. the app api endpoint mostly uses JWTs, while stuff like issue creation uses Tokens
    /// </summary>
    [JsonIgnore]
    public GithubAuthMethod AuthenticationMethod { get; }

    /// <summary>
    /// Location of the api endpoint for this request.
    /// </summary>
    /// <param name="owner">Owner of the repository.</param>
    /// <param name="repository">The repository to make the request.</param>
    /// <returns>The api location for this request.</returns>
    public string GetLocation(string owner, string repository);
}

public enum GithubAuthMethod
{
    JWT,
    Token,
}
