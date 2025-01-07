using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Ahelp rate limit values are accounted in periods of this size (seconds).
    ///     After the period has passed, the count resets.
    /// </summary>
    /// <seealso cref="AhelpRateLimitCount"/>
    public static readonly CVarDef<float> AhelpRateLimitPeriod =
        CVarDef.Create("ahelp.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     How many ahelp messages are allowed in a single rate limit period.
    /// </summary>
    /// <seealso cref="AhelpRateLimitPeriod"/>
    public static readonly CVarDef<int> AhelpRateLimitCount =
        CVarDef.Create("ahelp.rate_limit_count", 10, CVar.SERVERONLY);

    /// <summary>
    ///     Should the administrator's position be displayed in ahelp.
    ///     If it is is false, only the admin's ckey will be displayed in the ahelp.
    /// </summary>
    /// <seealso cref="AdminUseCustomNamesAdminRank"/>
    /// <seealso cref="AhelpAdminPrefixWebhook"/>
    public static readonly CVarDef<bool> AhelpAdminPrefix =
        CVarDef.Create("ahelp.admin_prefix", false, CVar.SERVERONLY);

    /// <summary>
    ///     Should the administrator's position be displayed in the webhook.
    ///     If it is is false, only the admin's ckey will be displayed in webhook.
    /// </summary>
    /// <seealso cref="AdminUseCustomNamesAdminRank"/>
    /// <seealso cref="AhelpAdminPrefix"/>
    public static readonly CVarDef<bool> AhelpAdminPrefixWebhook =
        CVarDef.Create("ahelp.admin_prefix_webhook", false, CVar.SERVERONLY);
}
