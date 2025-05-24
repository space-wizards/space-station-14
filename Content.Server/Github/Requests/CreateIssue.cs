using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
/// <see href="https://docs.github.com/en/rest/issues/issues?apiVersion=2022-11-28#create-an-issue"/>>
/// </summary>
public sealed class CreateIssue : IGithubRequest
{
    public HttpMethod RequestMethod => HttpMethod.Post;

    [JsonPropertyName("title"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required string Title;
    [JsonPropertyName("body"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body;
    [JsonPropertyName("assignee"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Assignee;
    [JsonPropertyName("milestone"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Milestone;
    [JsonPropertyName("labels"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> Labels = [];
    [JsonPropertyName("assignees"), JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> Assignees = [];

    public string GetLocation(string owner, string repository)
    {
        return $"repos/{owner}/{repository}/issues";
    }
}
