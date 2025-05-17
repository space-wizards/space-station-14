using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Github.Requests;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

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
public sealed class GithubClient
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private HttpClient _httpClient = default!;

    private ISawmill _sawmill = default!;

    #region Header constants

    private const string AcceptHeader = "Accept";
    private const string AcceptHeaderType = "application/vnd.github+json";

    private const string AuthHeader = "Authorization";
    private const string AuthHeaderBearer = "Bearer ";

    private const string VersionHeader = "X-GitHub-Api-Version";
    private const string VersionNumber = "2022-11-28";

    #endregion

    private readonly Uri _baseUri = new("https://api.github.com/");

    #region CCvar values

    private string _authToken = "";
    private string _repository = "";
    private string _owner = "";
    private int _maxRetries;

    #endregion

    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.GithubAuthToken, val => SetValueAndInitHttpClient(ref _authToken, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryName, val => SetValueAndInitHttpClient(ref _repository, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryOwner, val => SetValueAndInitHttpClient(ref _owner, val), true);
        _cfg.OnValueChanged(CCVars.GithubMaxRetries, val => SetValueAndInitHttpClient(ref _maxRetries, val), true);

        _sawmill = _log.GetSawmill("github");
    }

    private void SetValueAndInitHttpClient<T>(ref T toSet, T value)
    {
        Interlocked.Exchange(ref toSet, value);

        if(!HaveFullApiData())
            return;


        var httpMessageHandler = new RetryHandler(new HttpClientHandler(), _maxRetries, _sawmill);
        var newClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = _baseUri,
            DefaultRequestHeaders =
            {
                { AcceptHeader, AcceptHeaderType },
                { AuthHeader, AuthHeaderBearer + _authToken },
                { VersionHeader, VersionNumber },
            },
            Timeout = TimeSpan.FromSeconds(15)
        };
        Interlocked.Exchange(ref _httpClient, newClient);
    }

    #region Public functions

    /// <summary>
    /// The standard way to make requests to the GitHub api. This will ensure that the request respects the rate limit
    /// and will also retry the request if it fails. Awaiting this to finish could take a very long time depending
    /// on what exactly is going on! Only await for it if you're willing to wait a long time.
    /// </summary>
    /// <param name="request">The request you want to make.</param>
    /// <param name="ct">Token for operation cancellation.</param>
    /// <returns>The direct HTTP response from the API. If null the request could not be made.</returns>
    public async Task TryMakeRequestSafe(IGithubRequest request, CancellationToken ct)
    {
        if (!HaveFullApiData())
        {
            _sawmill.Info("Tried to make a github api request but the api was not enabled.");
            return;
        }

        var httpRequestMessage = BuildRequest(request);

        var response = await _httpClient.SendAsync(httpRequestMessage, ct);

        _sawmill.Info("Made a github api request to: '{uri}', status is {status}", httpRequestMessage.RequestUri, response.StatusCode);
    }

    /// <summary>
    /// A simple helper function that just tries to parse a header value that is expected to be a long int.
    /// In general, there are just a lot of single value headers that are longs so this removes a lot of duplicate code.
    /// </summary>
    /// <param name="headers">The headers that you want to search.</param>
    /// <param name="header">The header you want to get the long value for.</param>
    /// <param name="value">Value of header, if found, null otherwise.</param>
    /// <returns>The headers value if it exists, null otherwise.</returns>
    public static bool TryGetHeaderAsLong(HttpResponseHeaders? headers, string header, [NotNullWhen(true)] out long? value)
    {
        value = null;
        if (headers == null)
            return false;

        if (!headers.TryGetValues(header, out var headerValues))
            return false;

        if (!long.TryParse(headerValues.First(), out var result))
            return false;

        value = result;
        return true;
    }

    # endregion

    #region Helper functions

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

    private HttpRequestMessage BuildRequest(IGithubRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var payload = new StringContent(json, Encoding.UTF8, "application/json");

        var builder = new UriBuilder(_baseUri)
        {
            Port = -1,
            Path = request.GetLocation(_owner, _repository),
        };

        var httpRequest = new HttpRequestMessage
        {
            Method = request.RequestMethod,
            RequestUri = builder.Uri,
            Content = payload,
        };
        return httpRequest;
    }


    private bool HaveFullApiData()
    {
        return !string.IsNullOrWhiteSpace(_authToken) &&
               !string.IsNullOrWhiteSpace(_repository) &&
               !string.IsNullOrWhiteSpace(_owner);
    }

    #endregion
}
