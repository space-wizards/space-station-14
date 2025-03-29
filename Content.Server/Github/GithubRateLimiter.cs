using System.Net;
using System.Net.Http;

namespace Content.Server.Github;

public sealed class GithubRateLimiter
{
    // This assumes all requests use the same "Core" resource. If more methods are added, this will need to be updated to be more robust.
    private long _remainingRequests;

    private long _requestBuffer = 15;

    private bool _ongoingRequest;
    private DateTime _nextValidRequestTime = DateTime.UtcNow;

    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#pause-between-mutative-requests"/>
    private const long DefaultDelayTime = 1L;

    /// Extra buffer time (In seconds) after getting rate limited we don't make the request exactly when we get more credits.
    private const long ExtraBufferTime = 1L;

    /// <see href="https://en.wikipedia.org/w/index.php?title=Exponential_backoff&amp;oldid=1281333304"/>
    private const double ExponentialBackoffB = 1.5;
    /// <inheritdoc cref="ExponentialBackoffB"/>
    private long _exponentialBackoffC;

    public bool Acquire()
    {
        if (_ongoingRequest || DateTime.UtcNow <= _nextValidRequestTime)
            return false;

        _ongoingRequest = true;
        return true;
    }

    public void Release()
    {
        _ongoingRequest = false;
    }

    public void ReleaseNoResponse()
    {
        _ongoingRequest = false;
        ExponentialBackoff();
    }

    public void ReleaseWithResponse(HttpResponseMessage response, List<HttpStatusCode> expectedStatusCodes)
    {
        _ongoingRequest = false;
        _nextValidRequestTime = CalculateNextRequestTime(response, expectedStatusCodes);
    }

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

    private DateTime ExponentialBackoff()
    {
        return DateTime.UtcNow.AddSeconds(60 * Math.Pow(ExponentialBackoffB, _exponentialBackoffC++));
    }

    public void UpdateRequests(long remaining)
    {
        _remainingRequests = remaining;
    }

}
