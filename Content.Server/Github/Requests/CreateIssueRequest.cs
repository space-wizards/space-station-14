using System.Net.Http;
using System.Text.Json.Serialization;

namespace Content.Server.Github.Requests;

/// <summary>
/// <see href="https://docs.github.com/en/rest/issues/issues?apiVersion=2022-11-28#create-an-issue"/>>
/// </summary>
public sealed class CreateIssueRequest : IGithubRequest
{
    [JsonIgnore]
    public HttpMethod RequestMethod => HttpMethod.Post;

    [JsonIgnore]
    public GithubAuthMethod AuthenticationMethod => GithubAuthMethod.Token;

    #region JSON fields

    [JsonInclude]
    public required string Title;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Assignee;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Milestone;
    [JsonInclude]
    public List<string> Labels = [];
    [JsonInclude]
    public List<string> Assignees = [];

    #endregion

    public string GetLocation(string owner, string repository)
    {
        return $"repos/{owner}/{repository}/issues";
    }
}
