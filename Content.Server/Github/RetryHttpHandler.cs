using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

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
public sealed class RetryHandler(HttpMessageHandler innerHandler, int maxRetries, ISawmill sawmill) : DelegatingHandler(innerHandler)
{
    private const int MaxWaitSeconds = 32;

    /// Extra buffer time (In seconds) after getting rate limited we don't make the request exactly when we get more credits.
    private const long ExtraBufferTime = 1L;

    #region Headers

    private const string RetryAfterHeader = "retry-after";

    private const string RemainingHeader = "x-ratelimit-remaining";
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    #endregion

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage response;
        var i = 0;
        do
        {
            response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return response;

            i++;
            if (i < maxRetries)
            {
                var waitTime = CalculateNextRequestTime(response, i);
                await Task.Delay(waitTime, cancellationToken);
            }
        } while (!response.IsSuccessStatusCode && i < maxRetries);

        return response;
    }

    /// <summary>
    /// Follows these guidelines but also has a small buffer so you should never quite hit zero:
    /// <br/>
    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#handle-rate-limit-errors-appropriately"/>
    /// </summary>
    /// <param name="response">The last response from the API.</param>
    /// <param name="attempt">Number of current call attempt.</param>
    /// <returns>The amount of time to wait until the next request.</returns>
    private TimeSpan CalculateNextRequestTime(HttpResponseMessage response, int attempt)
    {
        var headers = response.Headers;
        var statusCode = response.StatusCode;

        // Specific checks for rate limits.
        if (statusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            // Retry after header
            if (GithubClient.TryGetHeaderAsLong(headers, RetryAfterHeader, out var retryAfterSeconds))
                return TimeSpan.FromSeconds(retryAfterSeconds.Value + ExtraBufferTime);

            // Reset header (Tells us when we get more api credits)
            if (GithubClient.TryGetHeaderAsLong(headers, RemainingHeader, out var remainingRequests)
                && GithubClient.TryGetHeaderAsLong(headers, RateLimitResetHeader, out var resetTime)
                && remainingRequests == 0)
            {
                var delayTime = resetTime.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                sawmill.Warning(
                    "github returned '{status}' status, have to wait until limit reset - in '{delay}' seconds",
                    response.StatusCode,
                    delayTime
                );
                return TimeSpan.FromSeconds(delayTime + ExtraBufferTime);
            }
        }

        // If the status code is not the expected one or the rate limit checks are failing, just do an exponential backoff.
        return ExponentialBackoff(attempt);
    }

    private static TimeSpan ExponentialBackoff(int i)
    {
        return TimeSpan.FromSeconds(Math.Min(MaxWaitSeconds, Math.Pow(2, i)));
    }
}
