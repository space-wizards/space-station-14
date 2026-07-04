using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Marker, for if the GitHub for reporting issues api is enabled. If it is not enabled, any actions that require GitHub API will be ignored.
    /// To fully set up the API, you also need to set <see cref="GithubIssuesAppPrivateKeyPath"/>, <see cref="GithubIssuesAppId"/>,
    /// <see cref="GithubIssuesRepositoryName"/> and <see cref="GithubIssuesRepositoryOwner"/>.
    /// </summary>
    public static readonly CVarDef<bool> GithubIssuesEnabled =
        CVarDef.Create("github_issues.github_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// GitHub app (for reporting issues) private keys location. <b>PLEASE READ THIS CAREFULLY!!</b>
    /// <list type="bullet">
    /// <item>
    ///     Its highly recommend to create a new (private) repository specifically for this app. This will help avoid
    ///     moderation issues and also allow you to ignore duplicate or useless issues. You can just transfer legitimate
    ///     issues from the private repository to the main public one.
    /// </item>
    /// <item>
    ///     Only create the auth token with the MINIMUM required access (Specifically only give it access to one
    ///     repository - and the minimum required access for your use case).
    ///     <br/><br/>If this token is only for forwarding issues then you should only need to grant read and write
    ///     permission to "Issues" and read only permissions to "Metadata".
    /// </item>
    /// </list>
    /// Also remember to use the <code>testgithubapi</code> command to test if you set everything up correctly.
    /// [Insert YouTube video link with walkthrough here]
    /// </summary>
    /// <example>
    /// (If your on linux): /home/beck/key.pem
    /// </example>
    public static readonly CVarDef<string> GithubIssuesAppPrivateKeyPath =
        CVarDef.Create("github_issues.github_app_private_key_path", "", CVar.SERVERONLY );

    /// <summary>
    /// The GitHub apps app id. Is used for reporting issues. Go to https://github.com/settings/apps/APPNAME to find the app id.
    /// </summary>
    /// <example>
    /// 1009555
    /// </example>
    public static readonly CVarDef<string> GithubIssuesAppId =
        CVarDef.Create("github_issues.github_app_id", "", CVar.SERVERONLY);

    /// <summary>
    /// Name of the targeted GitHub repository for issues.
    /// </summary>
    /// <example>
    /// If your URL was https://github.com/space-wizards/space-station-14 the repo name would be "space-station-14".
    /// </example>>
    public static readonly CVarDef<string> GithubIssuesRepositoryName =
        CVarDef.Create("github_issues.github_repository_name", string.Empty, CVar.SERVERONLY);

    /// <summary>
    /// Owner of the GitHub repository for issues.
    /// </summary>
    /// <example>
    ///  If your URL was https://github.com/space-wizards/space-station-14 the owner would be "space-wizards".
    /// </example>
    public static readonly CVarDef<string> GithubIssuesRepositoryOwner =
        CVarDef.Create("github_issues.github_repository_owner", string.Empty, CVar.SERVERONLY);

    /// <summary>
    /// The maximum number of times the github issue creating requests will retry before giving up.
    /// </summary>
    public static readonly CVarDef<int> GithubIssuesMaxRetries =
        CVarDef.Create("github_issues.github_max_retries", 3, CVar.SERVERONLY);
}
