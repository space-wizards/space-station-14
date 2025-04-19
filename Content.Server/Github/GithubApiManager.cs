using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Github.Requests;
using Content.Server.Github.Responses;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Github;

/// <summary>
///     Basic implementation of the GitHub api. This was mainly created for making issues from users bug reports - it is not a full implementation!
///     I tried to follow the spec very closely and the docs are really well done. I highly recommend taking a look at them!
///     <br/>
///     <br/> Some useful information about the api:
///     <br/> <see href="https://docs.github.com/en/rest?apiVersion=2022-11-28">Api home page</see>
///     <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
///     <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
///     <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api?apiVersion=2022-11-28">Troubleshooting</see>
/// </summary>
public sealed class GithubApiManager : IPostInjectInit
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

    private const string BaseUri = "https://api.github.com/";

    #region CCvar values

    private bool _enabled;
    private string _authToken = "";
    private string _repository = "";
    private string _owner = "";
    private int _maxRetries;

    #endregion

    private int _initializationAttempts;
    private bool _apiInitialized;

    private readonly ConcurrentQueue<GithubQueueEntry> _queue = new();

    private readonly GithubRateLimiter _rateLimiter = new();

    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.GithubEnabled, val => _enabled = val, true);
        _cfg.OnValueChanged(CCVars.GithubAuthToken, val => _authToken = val, true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryName, val => _repository = val, true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryOwner, val => _owner = val, true);
        _cfg.OnValueChanged(CCVars.GithubMaxRetries, val => _maxRetries = val, true);
        _cfg.OnValueChanged(CCVars.GithubRequestBuffer, _rateLimiter.UpdateRequestBuffer, true);

        // _nextApiCall = DateTime.UtcNow.AddSeconds(DefaultDelayTime);
    }

    #region Public functions

    /// <summary>
    ///     Queue a request for the api! This will try its hardest to ensure the request gets through to the API. However,
    ///     there is no way to review the response from the request or determine if it succeeded. You can also still add
    ///     items to the queue even if the api is disabled, they just won't get set until its reactivated.
    ///     <br/>
    ///     This will fully respect the API rate limits to the best of its ability. Most requests will actually be sent
    ///     ~1 second after you enqueue them.
    /// </summary>
    /// <remarks>This does not necessarily respect order.</remarks>>
    /// <param name="request">The request to enqueue.</param>
    public async void QueueRequest(IGithubRequest request)
    {
        _queue.Enqueue(new GithubQueueEntry(request));
        _sawmill.Info("Queued github request.");
    }

    /// <summary>
    ///     Directly send a request to the API. This does not have any rate limits checks so be careful!
    /// </summary>
    /// <remarks>If you want a safer way to send requests, look at <see cref="QueueRequest"/>></remarks>
    /// <param name="request">The request to make.</param>
    /// <returns>The direct HTTP response from the API with a boolean that indicates of the request was attempted at all.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<(bool, HttpResponseMessage)> TryMakeRequest(IGithubRequest request)
    {
        if (!ApiEnabled())
        {
            _sawmill.Info("Tried to make a github api request but the api was not enabled.");
            return (false, new HttpResponseMessage());
        }

        var json = JsonSerializer.Serialize(request);
        var payload = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage
        {
            Method = request.RequestMethod,
            Headers =
            {
                { AcceptHeader, AcceptHeaderType },
                { AuthHeader, AuthHeaderBearer+_authToken },
                { VersionHeader, VersionNumber },
            },
            RequestUri = new Uri(BaseUri+request.GetLocation(_owner, _repository)),
            Content = payload,
        };

        var response = await _http.Client.SendAsync(httpRequest);

        // Update rate limit
        if (TryGetLongHeader(response.Headers, "x-ratelimit-remaining", out var remainingRequests))
            _rateLimiter.UpdateRequests(remainingRequests);

        _sawmill.Info($"Made a github api request to: {BaseUri+request.GetLocation(_owner, _repository)}");

        return (true, response);
    }

    /// <summary>
    ///     A simple helper function that just tries to a header value that is a long.
    ///     In general, there are just a lot of single value headers that are longs so this removes a lot of duplicate code.
    /// </summary>
    /// <param name="headers">The headers that you want to search.</param>
    /// <param name="header">The header you want to get the long value for.</param>
    /// <param name="value">The output from the header, if unsuccessfully found or didn't parse correctly will be 0.</param>
    /// <returns>True if the header was found and was parsed correctly, false if not.</returns>
    public static bool TryGetLongHeader(HttpResponseHeaders? headers, string header, out long value)
    {
        value = 0;

        if (headers == null)
            return false;

        if (!headers.TryGetValues(header, out var headerValues))
            return false;

        return long.TryParse(headerValues.First(), out value);
    }

    # endregion

    /// <summary>
    ///     This will try to acquire the api lock every update tick. Having to get the lock ensures that we
    ///     only ever have one outgoing request at a time and that no requests violates the rate limits!
    /// </summary>
    public void Update()
    {
        if (!ApiEnabled() || _initializationAttempts >= _maxRetries)
            return;

        var apiAcquired = _rateLimiter.TryAcquire();

        if (!apiAcquired)
            return;

        if (!_apiInitialized)
        {
            TryInitializeApi();
            return;
        }

        HandleQueue();
    }

    /// <summary>
    ///     This deals with handling the queue of requests!
    /// </summary>
    private async void HandleQueue()
    {
        try
        {
            if (!_queue.TryDequeue(out var entry))
            {
                _rateLimiter.Release();
                return;
            }

            var request = await TryMakeRequest(entry.Request);

            if (!IsValidResponse(request.Item2, entry.Request.GetExpectedResponseCodes()))
            {
                entry.Failures++;

                // Don't bother putting it back if you failed too many times.
                if (entry.Failures < _maxRetries)
                    _queue.Enqueue(entry);
            }

            _rateLimiter.ReleaseWithResponse(request.Item2, entry.Request.GetExpectedResponseCodes());
        }
        catch (Exception e)
        {
            _sawmill.Error($"Github API exception: {e.Message}");
            _rateLimiter.ReleaseNoResponse();
        }
    }

    /// <summary>
    ///     This will try to initialize the api! This really just means ensuring you aren't currently rate limited.
    ///     Will instantly return and do nothing if the api is already initialized.
    /// </summary>
    private async void TryInitializeApi()
    {
        try
        {
            var rateLimitRequest = new GetRateLimit();
            var request = await TryMakeRequest(rateLimitRequest);
            var response = request.Item2;

            // This should never happen but if it somehow does we would want to return.
            if (!request.Item1)
            {
                _rateLimiter.Release();
                return;
            }

            var rateLimitRespJson = await response.Content.ReadFromJsonAsync<RateLimitResponse>();

            if (!IsValidResponse(response, rateLimitRequest.GetExpectedResponseCodes()) || rateLimitRespJson == null)
            {
                _initializationAttempts++;
                _rateLimiter.ReleaseWithResponse(response, rateLimitRequest.GetExpectedResponseCodes());
                return;
            }

            var remainingRequests = rateLimitRespJson.Resources.Core.Remaining;

            _rateLimiter.UpdateRequests(remainingRequests);
            _apiInitialized = true;

            _sawmill.Info($"Github api initialized with {remainingRequests} requests");

            // TODO: Probably a good idea to also check if your using the most up to date api version:
            // https://docs.github.com/en/rest/meta/meta?apiVersion=2022-11-28#get-all-api-versions

            _rateLimiter.ReleaseWithResponse(response, rateLimitRequest.GetExpectedResponseCodes());
        }
        catch (Exception e)
        {
            _sawmill.Error($"Github API initialization exception: {e.Message}");
            _rateLimiter.ReleaseNoResponse();
        }
    }

    #region Helper functions

    private bool ApiEnabled()
    {
        return _enabled                                &&
               !string.IsNullOrWhiteSpace(_authToken)  &&
               !string.IsNullOrWhiteSpace(_repository) &&
               !string.IsNullOrWhiteSpace(_owner);
    }

    /// <summary>
    ///     Check if response is valid. Mainly used for printing out useful error messages!
    ///     <br/>
    ///     <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28"/>
    /// </summary>
    /// <param name="response">The response from the request</param>
    /// <param name="expectedStatusCodes"></param>
    /// <returns>True if the request properly went through, false otherwise.</returns>
    private bool IsValidResponse(HttpResponseMessage response, List<HttpStatusCode> expectedStatusCodes)
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

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("GITHUB");
    }
}

/// <summary>
///     Entry for the queue. Keeps track of the amount of times this specific request has given an error.
/// </summary>
/// <param name="request">The request for this queue value.</param>
public struct GithubQueueEntry(IGithubRequest request)
{
    public IGithubRequest Request = request;
    public int Failures = 0;
}
