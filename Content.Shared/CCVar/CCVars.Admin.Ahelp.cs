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
    public static readonly CVarDef<bool> AhelpAdminPrefix =
        CVarDef.Create("ahelp.admin_prefix", false, CVar.SERVERONLY);

    /// <summary>
    /// Maximum possible amount of candidates to show for admin help "quick info" links.
    /// </summary>
    public static readonly CVarDef<int> AhelpMaxQuickInfoCandidates =
        CVarDef.Create("ahelp.max_quick_info_candidates", 30, CVar.SERVERONLY);

    /// <summary>
    /// Minimum size of a word to start testing for name matches.
    /// </summary>
    public static readonly CVarDef<int> AhelpQuickInfoStartWordSize =
        CVarDef.Create("ahelp.quick_info_start_word_size", 4, CVar.SERVERONLY);
}
