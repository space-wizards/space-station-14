using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Github;

/// <summary>
/// Basic rate limiter for the GitHub api! Will ensure there is only ever one outgoing request at a time and all
/// requests respect the rate limit the best they can.
/// <br/>
/// <br/> Links to the api for more information:
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
/// </summary>
/// <remarks> This was designed for the 2022-11-28 version of the API. </remarks>
public sealed class GithubRateLimiter : IPostInjectInit
{
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    /// The next valid time to make a request to the API.
    private DateTime _nextValidRequestTime = DateTime.UtcNow;

    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#pause-between-mutative-requests"/>
    private const long DefaultDelayTime = 1L;

    /// Extra buffer time (In seconds) after getting rate limited we don't make the request exactly when we get more credits.
    private const long ExtraBufferTime = 1L;

    /// <inheritdoc cref="ExponentialBackoff"/>
    private const double ExponentialBackoffB = 1.5;
    /// <inheritdoc cref="ExponentialBackoff"/>
    private long _exponentialBackoffC;

    private static readonly SemaphoreSlim _lock = new(1);

    #region Headers

    private const string RetryAfterHeader = "retry-after";

    private const string RemainingHeader = "x-ratelimit-remaining";
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    #endregion

    /// <summary>
    /// Try to acquire the API lock.
    /// </summary>
    /// <remarks>This doesn't have any locking or anything so it shouldn't ever be called in async functions.</remarks>>
    /// <returns>True if the API lock was acquired, false if not.</returns>
    public async Task TryAcquire()
    {
        await _lock.WaitAsync();

        if (DateTime.UtcNow <= _nextValidRequestTime)
            await Task.Delay(_nextValidRequestTime - DateTime.UtcNow);
    }

    /// <summary>
    /// Used to release the API lock when a request was never even made.
    /// </summary>
    public void Release()
    {
        _lock.Release();
    }

    /// <summary>
    /// Used to release the API lock after getting no response. This means that it either timed out or some sort
    /// of exception occured.
    /// </summary>
    public void ReleaseNoResponse()
    {
        _nextValidRequestTime = ExponentialBackoff();
        _lock.Release();
    }

    /// <summary>
    /// Used to release the API lock after getting a response. The response will be used to calculate a more accurate next valid request time.
    /// </summary>
    /// <param name="response">The actual response from the request made.</param>
    /// <param name="expectedStatusCodes">Expected status codes from the request.</param>
    public void ReleaseWithResponse(HttpResponseMessage response, IReadOnlyCollection<HttpStatusCode> expectedStatusCodes)
    {
        _nextValidRequestTime = CalculateNextRequestTime(response, expectedStatusCodes);
        _lock.Release();
    }

    /// <summary>
    /// Follows these guidelines but also has a small buffer so you should never quite hit zero:
    /// <br/>
    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#handle-rate-limit-errors-appropriately"/>
    /// </summary>
    /// <param name="response">The last response from the API.</param>
    /// <param name="expectedStatusCodes">Expected status codes - will return true if the response code is one of these.</param>
    /// <returns>The amount of time to wait until the next request.</returns>
    private DateTime CalculateNextRequestTime(HttpResponseMessage response, IReadOnlyCollection<HttpStatusCode> expectedStatusCodes)
    {
        var headers = response.Headers;
        var statusCode = response.StatusCode;

        // If the code matches one of the expected codes, just return the standard wait time.
        foreach (var code in expectedStatusCodes)
        {
            if (statusCode == code)
                return DateTime.UtcNow.AddSeconds(DefaultDelayTime);
        }

        _sawmill.Warning("Github api is potentially being rate limited.");

        // Specific checks for rate limits.
        if (statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.TooManyRequests)
        {
            // Retry after header
            if (GithubQueueHandler.TryGetLongHeader(headers, RetryAfterHeader) is { } retryAfterSeconds)
                return DateTime.UtcNow.AddSeconds(retryAfterSeconds + ExtraBufferTime);

            // Reset header (Tells us when we get more api credits)
            if (GithubQueueHandler.TryGetLongHeader(headers, RemainingHeader) is { } remainingRequests &&
                GithubQueueHandler.TryGetLongHeader(headers, RateLimitResetHeader) is { } resetTime &&
                remainingRequests == 0)
            {
                var delayTime = resetTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return DateTime.UtcNow.AddSeconds(delayTime + ExtraBufferTime);
            }
        }

        // If the status code is not the expected one or the rate limit checks are failing, just do an exponential backoff.
        return ExponentialBackoff();
    }

    /// <see href="https://en.wikipedia.org/w/index.php?title=Exponential_backoff&amp;oldid=1281333304"/>
    private DateTime ExponentialBackoff()
    {
        return DateTime.UtcNow.AddSeconds(60 * Math.Pow(ExponentialBackoffB, _exponentialBackoffC++));
    }

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("github-ratelimit");
    }
}
