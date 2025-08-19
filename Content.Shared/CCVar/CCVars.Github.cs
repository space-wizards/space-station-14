using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Marker, for if the GitHub api is enabled. If it is not enabled, any actions that require GitHub API will be ignored.
    /// To fully set up the API, you also need to set <see cref="GithubAppPrivateKeyPath"/>, <see cref="GithubAppId"/>,
    /// <see cref="GithubRepositoryName"/> and <see cref="GithubRepositoryOwner"/>.
    /// </summary>
    public static readonly CVarDef<bool> GithubEnabled =
        CVarDef.Create("github.github_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// GitHub app private keys location. <b>PLEASE READ THIS CAREFULLY!!</b>
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
    public static readonly CVarDef<string> GithubAppPrivateKeyPath =
        CVarDef.Create("github.github_app_private_key_path", "", CVar.SERVERONLY  | CVar.CONFIDENTIAL);

    /// <summary>
    /// The GitHub apps app id. Go to https://github.com/settings/apps/APPNAME to find the app id.
    /// </summary>
    /// <example>
    /// 1009555
    /// </example>
    public static readonly CVarDef<string> GithubAppId =
        CVarDef.Create("github.github_app_id", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Name of the targeted GitHub repository.
    /// </summary>
    /// <example>
    /// If your URL was https://github.com/space-wizards/space-station-14 the repo name would be "space-station-14".
    /// </example>>
    public static readonly CVarDef<string> GithubRepositoryName =
        CVarDef.Create("github.github_repository_name", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Owner of the GitHub repository.
    /// </summary>
    /// <example>
    ///  If your URL was https://github.com/space-wizards/space-station-14 the owner would be "space-wizards".
    /// </example>
    public static readonly CVarDef<string> GithubRepositoryOwner =
        CVarDef.Create("github.github_repository_owner", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// The maximum number of times the api will retry requests before giving up.
    /// </summary>
    public static readonly CVarDef<int> GithubMaxRetries =
        CVarDef.Create("github.github_max_retries", 3, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
