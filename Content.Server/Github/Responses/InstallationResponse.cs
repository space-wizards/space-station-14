using Content.Server.Github.Requests;

namespace Content.Server.Github.Responses;

/// <summary>
/// Response for Account installation request from GitHub API.
/// <seealso cref="InstallationsRequest"/>
/// </summary>
public sealed class InstallationResponse
{
    public required int Id { get; set; }

    public required GithubInstallationAccount Account { get; set; }
}

/// <summary>
/// Represents account associated with a GitHub installation.
/// </summary>
public sealed class GithubInstallationAccount
{
    public required string Login { get; set; }
}

