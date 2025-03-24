using System.Text.Json.Serialization;

namespace Content.Server.Github.Responses;

/// <summary>
///     <see href="https://docs.github.com/en/rest/rate-limit/rate-limit?apiVersion=2022-11-28"/>>
/// </summary>
public sealed class RateLimitResponse
{
    [JsonPropertyName("resources")]
    public Resources Resources { get; set; } = new();

    /// <see href="https://docs.github.com/en/rest/rate-limit/rate-limit?apiVersion=2022-11-28#get-rate-limit-status-for-the-authenticated-user"/>
    [JsonPropertyName("rate"), Obsolete("If you're writing new API client code or updating existing code, you should use the core object instead of the rate object.")]
    public ResourceDetails Rate { get; set; } = new();
}

