using System.Text.Json.Serialization;

namespace Content.Server.Github.Responses;

/// <summary>
/// Not all fields are filled out - only the necessary ones. If you need more just add them.
/// <see href="https://docs.github.com/en/rest/apps/apps?apiVersion=2022-11-28#create-an-installation-access-token-for-an-app"/>>
/// </summary>
public sealed class InstallationResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("account")]
    public GithubInstallationAccount Account { get; set; } = new();
}

/// <inheritdoc cref="InstallationResponse"/>
public sealed class GithubInstallationAccount
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
}

