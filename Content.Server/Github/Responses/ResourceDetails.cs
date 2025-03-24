using System.Text.Json.Serialization;

namespace Content.Server.Github.Responses;

/// <inheritdoc cref="RateLimitResponse"/>
public sealed class ResourceDetails
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
    [JsonPropertyName("used")]
    public int Used { get; set; }
    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }
    [JsonPropertyName("reset")]
    public int Reset { get; set; }
}

/// <inheritdoc cref="RateLimitResponse"/>
public sealed class Resources
{
    [JsonPropertyName("core")]
    public ResourceDetails Core { get; set; } = new();
    [JsonPropertyName("search")]
    public ResourceDetails Search { get; set; } = new();
    [JsonPropertyName("graphql")]
    public ResourceDetails Graphql { get; set; } = new();
    [JsonPropertyName("integration_manifest")]
    public ResourceDetails Integration_manifest { get; set; } = new();
    [JsonPropertyName("source_import")]
    public ResourceDetails Source_import { get; set; } = new();
    [JsonPropertyName("code_scanning_upload")]
    public ResourceDetails Code_scanning_upload { get; set; } = new();
    [JsonPropertyName("actions_runner_registration")]
    public ResourceDetails Actions_runner_registration { get; set; } = new();
    [JsonPropertyName("scim")]
    public ResourceDetails Scim { get; set; } = new();
    [JsonPropertyName("dependency_snapshots")]
    public ResourceDetails Dependency_snapshots { get; set; } = new();
    [JsonPropertyName("code_search")]
    public ResourceDetails Code_search { get; set; } = new();
    [JsonPropertyName("code_scanning_autofix")]
    public ResourceDetails Code_scanning_autofix { get; set; } = new();
}
