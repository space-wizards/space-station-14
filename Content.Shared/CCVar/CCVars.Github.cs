using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Is the API enabled? Requests can still be queued up if its false but nothing will actually be sent
    ///     to the api unless it's true.
    /// </summary>
    public static readonly CVarDef<bool> GithubEnabled =
        CVarDef.Create("github.github_enabled", true, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Auth token for the GitHub api. PLEASE READ THIS CAREFULLY!!
    ///     <list type="bullet">
    ///     <item>
    ///         DO NOT use your personal GitHub account - there is no reason and is quite dangerous.
    ///         GitHub allows accounts for bot only usage: <see href="https://docs.github.com/en/get-started/learning-about-github/types-of-github-accounts#user-accounts">Github user accounts</see>.
    ///     </item>
    ///     <item>
    ///         Its highly recommend to create a new (private) repository specifically for this api. This will help avoid moderation issues and also allow you to ignore duplicate or useless issues.
    ///         You can just transfer legitimate issues from the private repository to the main public one.
    ///     </item>
    ///     <item>
    ///         Only create the auth token with the MINIMUM required access (Specifically only give it access to one repository - see above - and the minimum required access for your use case).
    ///         See <see href="https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#fine-grained-personal-access-tokens">Fine-grained PATs</see>
    ///         and to create a token go here: <see href="https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token">Create PAT</see>.
    ///         <br/><br/>If this token is only for fowarding issues then you should only need to grant read and write permission to "Issues" and read only permissions to "Metadata".
    ///     </item>
    ///     </list>
    ///     If you follow the above steps its probably pretty safe to give a non expiring token so you don't need to worry about refreshing it every month.
    /// </summary>
    /// <example>
    ///     A PAT should look something like this:
    ///     <br/> github_pat_11XYZ123A0b98xJKLmNoPQ_7rT5UV6wOp9yBC3DfGh42zMnvQ1WXYZaBsJK789LmNOPQRSTU
    /// </example>
    public static readonly CVarDef<string> GithubAuthToken =
        CVarDef.Create("github.github_auth_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Name of the targeted GitHub repository.
    /// </summary>
    /// <example>
    ///     If your URL was https://github.com/space-wizards/space-station-14 the repo name would be "space-station-14".
    /// </example>>
    public static readonly CVarDef<string> GithubRepositoryName =
        CVarDef.Create("github.github_repository_name", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Owner of the GitHub repository.
    /// </summary>
    /// <example>
    ///     If your URL was https://github.com/space-wizards/space-station-14 the owner would be "space-wizards".
    /// </example>>
    public static readonly CVarDef<string> GithubRepositoryOwner =
        CVarDef.Create("github.github_repository_owner", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     The maximum number of times the api will retry requests before giving up. Is used both for the queue and initialization.
    /// </summary>
    public static readonly CVarDef<int> GithubMaxRetries =
        CVarDef.Create("github.github_max_retries", 3, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     When we reach this number of requests the api will stop making new requests until the api credits are refreshed.
    ///     This is just a small buffer to ensure that you never get rate limited.
    ///     <br/>
    ///     <br/> You get 5000 requests per hour so stopping a little early is fine for this use case -
    ///     <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28#primary-rate-limit-for-authenticated-users">Rate limit for authenticated users</see>.
    /// </summary>
    public static readonly CVarDef<long> GithubRequestBuffer =
        CVarDef.Create("github.github_request_buffer", 15L, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
