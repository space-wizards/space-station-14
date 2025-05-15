using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Github.Requests;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Github;

/// <summary>
/// Basic implementation of the GitHub api. This was mainly created for making issues from users bug reports - it is not
/// a full implementation! I tried to follow the spec very closely and the docs are really well done. I highly recommend
/// taking a look at them!
/// <br/>
/// <br/> Some useful information about the api:
/// <br/> <see href="https://docs.github.com/en/rest?apiVersion=2022-11-28">Api home page</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api?apiVersion=2022-11-28">Troubleshooting</see>
/// </summary>
public sealed class GithubApiManager
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IHttpClientHolder _http = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;

    #region Header constants

    private const string AcceptHeader = "Accept";
    private const string AcceptHeaderType = "application/vnd.github+json";

    private const string AuthHeader = "Authorization";
    private const string AuthHeaderBearer = "Bearer ";

    private const string VersionHeader = "X-GitHub-Api-Version";
    private const string VersionNumber = "2022-11-28";

    #endregion

    private readonly Uri BaseUri = new("https://api.github.com/");

    #region CCvar values

    private bool _enabled;
    private string _authToken = "";
    private string _repository = "";
    private string _owner = "";
    private int _maxRetries;

    #endregion

    private readonly GithubRateLimiter _rateLimiter = new();

    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.GithubEnabled, val => _enabled = val, true);
        _cfg.OnValueChanged(CCVars.GithubAuthToken, val => _authToken = val, true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryName, val => _repository = val, true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryOwner, val => _owner = val, true);
        _cfg.OnValueChanged(CCVars.GithubMaxRetries, val => _maxRetries = val, true);

        _sawmill = _log.GetSawmill("github");
    }

    #region Public functions

    /// <summary>
    /// Directly send a request to the API. This does not have any rate limits checks so be careful!
    /// <b>Only use this if you have a very good reason to!</b>
    /// </summary>
    /// <remarks>If you want the safe way to send requests, look at <see cref="TryMakeRequestSafe"/>></remarks>
    /// <param name="request">The request to make.</param>
    /// <returns>The direct HTTP response from the API. If null the request could not be made.</returns>
    public async Task<HttpResponseMessage?> TryMakeRequest(IGithubRequest request)
    {
        if (!ApiEnabled())
        {
            _sawmill.Info("Tried to make a github api request but the api was not enabled.");
            return null;
        }

        var json = JsonSerializer.Serialize(request);
        var payload = new StringContent(json, Encoding.UTF8, "application/json");

        var builder = new UriBuilder(BaseUri);
        builder.Port = -1;
        builder.Path = request.GetLocation(_owner, _repository);

        var httpRequest = new HttpRequestMessage
        {
            Method = request.RequestMethod,
            Headers =
            {
                { AcceptHeader, AcceptHeaderType },
                { AuthHeader, AuthHeaderBearer+_authToken },
                { VersionHeader, VersionNumber },
            },
            RequestUri = builder.Uri,
            Content = payload,
        };

        var response = await _http.Client.SendAsync(httpRequest);

        _sawmill.Info($"Made a github api request to: {BaseUri+request.GetLocation(_owner, _repository)}");

        return response;
    }

    /// <summary>
    /// The standard way to make requests to the GitHub api. This will ensure that the request respects the rate limit
    /// and will also retry the request if it fails. Awaiting this to finish could take a very long time depending
    /// on what exactly is going on! Only await for it if you're willing to wait a long time.
    /// </summary>
    /// <param name="request">The request you want to make.</param>
    /// <returns>The direct HTTP response from the API. If null the request could not be made.</returns>
    public async Task<HttpResponseMessage?> TryMakeRequestSafe(IGithubRequest request)
    {
        return await TryMakeRequestSafe(request, 0);
    }

    /// <inheritdoc cref="TryMakeRequestSafe"/>
    /// <param name="attempts">The number of attempts made for this request.</param>
    private async Task<HttpResponseMessage?> TryMakeRequestSafe(IGithubRequest request, uint attempts)
    {
        try
        {
            if (attempts > _maxRetries)
                return null;

            await _rateLimiter.TryAcquire();

            var response = await TryMakeRequest(request);

            // No response
            if (response == null)
            {
                _rateLimiter.ReleaseNoResponse();
                return await TryMakeRequestSafe(request, attempts+1);
            }

            // Invalid response
            if (!IsValidResponse(response, request.GetExpectedResponseCodes()))
            {
                _rateLimiter.ReleaseWithResponse(response, request.GetExpectedResponseCodes());
                return await TryMakeRequestSafe(request, attempts+1);
            }

            // Successful response
            _rateLimiter.ReleaseWithResponse(response, request.GetExpectedResponseCodes());
            return response;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Github API exception: {e.Message}");
            _rateLimiter.ReleaseNoResponse();
            return null;
        }
    }

    /// <summary>
    /// A simple helper function that just tries to parse a header value that is expected to be a long int.
    /// In general, there are just a lot of single value headers that are longs so this removes a lot of duplicate code.
    /// </summary>
    /// <param name="headers">The headers that you want to search.</param>
    /// <param name="header">The header you want to get the long value for.</param>
    /// <returns>The headers value if it exists, null otherwise.</returns>
    public static long? TryGetLongHeader(HttpResponseHeaders? headers, string header)
    {
        if (headers == null)
            return null;

        if (!headers.TryGetValues(header, out var headerValues))
            return null;

        if (!long.TryParse(headerValues.First(), out var result))
            return null;

        return result;
    }

    # endregion

    #region Helper functions

    private bool ApiEnabled()
    {
        return _enabled                                &&
               !string.IsNullOrWhiteSpace(_authToken)  &&
               !string.IsNullOrWhiteSpace(_repository) &&
               !string.IsNullOrWhiteSpace(_owner);
    }

    /// <summary>
    /// Check if response is valid. Mainly used for printing out useful error messages!
    /// <br/>
    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28"/>
    /// </summary>
    /// <param name="response">The response from the request</param>
    /// <param name="expectedStatusCodes"></param>
    /// <returns>True if the request properly went through, false otherwise.</returns>
    private bool IsValidResponse(HttpResponseMessage response, IEnumerable<HttpStatusCode> expectedStatusCodes)
    {
        foreach (var code in expectedStatusCodes)
        {
            if  (response.StatusCode == code)
                return true;
        }

        // Check if the auth token is expired.
        if (response.Headers.TryGetValues("github-authentication-token-expiration", out var authTokenExpHeader) &&
            DateTime.TryParse(authTokenExpHeader.First(), out var authTokenExp))
        {
            if (authTokenExp <= DateTime.UtcNow)
            {
                _sawmill.Error($"Github authentication token has expired! Exp date: {authTokenExp}");
                return false;
            }
        }

        // TODO: Add custom warnings depending on the status code. E.g unsupported api version is code 400.
        _sawmill.Warning($"Github api had an invalid response. Status code: {response.StatusCode}");
        return false;
    }

    #endregion
}
