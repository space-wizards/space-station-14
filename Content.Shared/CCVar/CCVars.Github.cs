using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Marker, for if the GitHub api is enabled. If it is not enabled, any actions that require GitHub API will be ignored.
    /// </summary>
    public static readonly CVarDef<bool> GithubEnabled =
        CVarDef.Create("github.github_enabled", true, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Auth token for the GitHub api. <b>PLEASE READ THIS CAREFULLY!!</b>
    /// <list type="bullet">
    /// <item>
    ///     <b>DO NOT</b> use your personal GitHub account - there is no reason and is quite dangerous.
    ///     GitHub allows accounts for bot only usage: <see href="https://docs.github.com/en/get-started/learning-about-github/types-of-github-accounts#user-accounts">Github user accounts</see>.
    /// </item>
    /// <item>
    ///     Its highly recommend to create a new (private) repository specifically for this api. This will help avoid
    ///     moderation issues and also allow you to ignore duplicate or useless issues. You can just transfer legitimate
    ///     issues from the private repository to the main public one.
    /// </item>
    /// <item>
    ///     Only create the auth token with the MINIMUM required access (Specifically only give it access to one
    ///     repository - see above - and the minimum required access for your use case).
    ///     See <see href="https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#fine-grained-personal-access-tokens">Fine-grained PATs</see>
    ///     and to create a token go here: <see href="https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token">Create PAT</see>.
    ///     <br/><br/>If this token is only for fowarding issues then you should only need to grant read and write
    ///     permission to "Issues" and read only permissions to "Metadata".
    /// </item>
    /// </list>
    ///  If you follow the above steps its probably pretty safe to give a non expiring token so you don't need to worry
    ///  about refreshing it every month.
    /// </summary>
    /// <example>
    ///  A PAT should look something like this:
    ///  <br/> github_pat_11XYZ123A0b98xJKLmNoPQ_7rT5UV6wOp9yBC3DfGh42zMnvQ1WXYZaBsJK789LmNOPQRSTU
    /// </example>
    public static readonly CVarDef<string> GithubAuthToken =
        CVarDef.Create("github.github_auth_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

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
    /// </example>>
    public static readonly CVarDef<string> GithubRepositoryOwner =
        CVarDef.Create("github.github_repository_owner", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// The maximum number of times the api will retry requests before giving up.
    /// </summary>
    public static readonly CVarDef<int> GithubMaxRetries =
        CVarDef.Create("github.github_max_retries", 3, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Limit for amount github issues could be created by single user until
    /// admins are notified of 'excessive' user activity.
    /// </summary>
    public static readonly CVarDef<int> GitHubIssuePerUserRateLimitAnnounceAdmins =
        CVarDef.Create("github.issue_limit_notify_admins", 3, CVar.SERVERONLY);

    /// <summary>
    /// Marker, if notifying admins on github issue created limit was reached.
    /// </summary>
    public static readonly CVarDef<bool> IsGitHubIssuePerUserRateLimitAnnounceAdminsEnabled =
        CVarDef.Create("github.is_issue_limit_notify_admins_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Window during which rate limit of github issue creation per-user is calculated.
    /// Rate limit values are accounted in periods of this size (seconds).
    /// After the period has passed, the count resets.
    /// </summary>
    /// <seealso cref="GithubIssueRateLimitCount"/>
    public static readonly CVarDef<float> GithubIssueRateLimitPeriod =
        CVarDef.Create("github.rate_limit_period", 3600f, CVar.SERVERONLY);

    /// <summary>
    /// How many github issues are allowed in a single rate limit period.
    /// </summary>
    /// <remarks>
    /// The total rate limit throughput per second is effectively
    /// <see cref="GithubIssueRateLimitCount"/> divided by <see cref="ChatRateLimitCount"/>.
    /// </remarks>
    /// <seealso cref="GithubIssueRateLimitPeriod"/>
    public static readonly CVarDef<int> GithubIssueRateLimitCount =
        CVarDef.Create("github.rate_limit_count", 10, CVar.SERVERONLY);
}
