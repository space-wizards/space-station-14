using System.Net;
using System.Net.Http;

namespace Content.Server.Github;

/// <summary>
///     Basic rate limiter for the GitHub api! Will ensure there is only ever one outgoing request at a time and all requests respect the rate limit the best they can.
///     <br/>
///     <br/> Links to the api for more information:
///     <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
///     <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
/// </summary>
/// <remarks> This was designed for the 2022-11-28 version of the API. </remarks>
public sealed class GithubRateLimiter
{
    // This assumes all requests use the same "Core" resource. If more methods are added, this will need to be updated to be more robust.
    private long _remainingRequests;

    private long _requestBuffer;

    /// If true, there is currently an outgoing requests.
    private bool _ongoingRequest;

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

    /// <summary>
    ///     Try to acquire the API lock.
    /// </summary>
    /// <remarks>This doesn't have any locking or anything so it shouldn't ever be called in async functions.</remarks>>
    /// <returns>True if the API lock was acquired, false if not.</returns>
    public bool TryAcquire()
    {
        if (_ongoingRequest || DateTime.UtcNow <= _nextValidRequestTime)
            return false;

        _ongoingRequest = true;
        return true;
    }

    /// <summary>
    ///     Used to release the API lock when a request was never even made.
    /// </summary>
    public void Release()
    {
        _ongoingRequest = false;
    }

    /// <summary>
    ///     Used to release the API lock after getting no response. This means that it either timed out or some sort
    ///     of exception occured.
    /// </summary>
    public void ReleaseNoResponse()
    {
        _ongoingRequest = false;
        _nextValidRequestTime = ExponentialBackoff();
    }

    /// <summary>
    ///     Used to release the API lock after getting a response. The response will be used to calculate a more accurate next valid request time.
    /// </summary>
    /// <param name="response">The actual response from the request made.</param>
    /// <param name="expectedStatusCodes">Expected status codes from the request.</param>
    public void ReleaseWithResponse(HttpResponseMessage response, List<HttpStatusCode> expectedStatusCodes)
    {
        _ongoingRequest = false;
        _nextValidRequestTime = CalculateNextRequestTime(response, expectedStatusCodes);
    }

    /// <summary>
    ///     Follows these guidelines but also has a small buffer so you should never quite hit zero:
    ///     <br/>
    ///     <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#handle-rate-limit-errors-appropriately"/>
    /// </summary>
    /// <param name="response">The last response from the API.</param>
    /// <param name="expectedStatusCodes">Expected status codes - will return true if the response code is one of these.</param>
    /// <returns>The amount of time to wait until the next request.</returns>
    private DateTime CalculateNextRequestTime(HttpResponseMessage response, List<HttpStatusCode> expectedStatusCodes)
    {
        var headers = response.Headers;
        var statusCode = response.StatusCode;

        if (_remainingRequests > _requestBuffer)
        {
            // If the code matches one of the expected codes, just return the standard wait time.
            foreach (var code in expectedStatusCodes)
            {
                if (statusCode == code)
                    return DateTime.UtcNow.AddSeconds(DefaultDelayTime);
            }
        }

        // Specific checks for rate limits.
        if (_remainingRequests <= _requestBuffer || statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.TooManyRequests)
        {
            // Retry after header
            if (GithubApiManager.TryGetLongHeader(headers, "retry-after", out var retryAfterSeconds))
            {
                return DateTime.UtcNow.AddSeconds(retryAfterSeconds + ExtraBufferTime);
            }

            // Reset header (Tells us when we get more api credits)
            if (GithubApiManager.TryGetLongHeader(headers, "x-ratelimit-remaining", out var remainingRequests) &&
                GithubApiManager.TryGetLongHeader(headers, "x-ratelimit-reset", out var resetTime))
            {
                // If it's not zero, something is wrong so just do an exponential backoff.
                if (remainingRequests == 0)
                {
                    var delayTime = resetTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return DateTime.UtcNow.AddSeconds(delayTime + ExtraBufferTime);
                }
            }

            // _sawmill.Warning("Github api is potentially being rate limited.");
        }

        // If the status code is not the expected one or the rate limit checks are failing, just do an exponential backoff.
        return ExponentialBackoff();
    }

    /// <see href="https://en.wikipedia.org/w/index.php?title=Exponential_backoff&amp;oldid=1281333304"/>
    private DateTime ExponentialBackoff()
    {
        return DateTime.UtcNow.AddSeconds(60 * Math.Pow(ExponentialBackoffB, _exponentialBackoffC++));
    }

    /// <summary>
    ///     Manually update the remaining requests. This could be called from functions that aren't rate limited
    ///     but have useful rate limiting information.
    /// </summary>
    /// <param name="remaining">Remaining api requests.</param>
    public void UpdateRequests(long remaining)
    {
        _remainingRequests = remaining;
    }

    /// <summary>
    ///     Update the buffer, should really only be called when the ccvar is updated.
    /// </summary>
    /// <param name="buffer">The new buffer value.</param>
    public void UpdateRequestBuffer(long buffer)
    {
        _requestBuffer = buffer;
    }
}
