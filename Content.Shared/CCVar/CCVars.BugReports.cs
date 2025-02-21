using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Allow users to submit bug reports. You should have some kind of system to listen for the reports.
    /// </summary>
    public static readonly CVarDef<bool> EnablePlayerBugReports =
        CVarDef.Create("bug_reports.enable_player_bug_reports", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Minimum playtime that players need to have to sumbit bug reports.
    /// </summary>
    /// <remarks>In hours!</remarks>
    public static readonly CVarDef<int> MinimumPlaytimeBugReports =
        CVarDef.Create("bug_reports.minimum_playtime_bug_reports", 1, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum number of bug reports a user can submit per round
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportsPerRound =
        CVarDef.Create("bug_reports.maximum_bug_reports_per_round", 3, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum length of a bug report title.
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportTitleLength =
        CVarDef.Create("bug_reports.maximum_bug_report_title_length", 35, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Minimum length of a bug report title.
    /// </summary>
    public static readonly CVarDef<int> MinimumBugReportTitleLength =
        CVarDef.Create("bug_reports.minimum_bug_report_title_length", 10, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum length of a bug report description.
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportDescriptionLength =
        CVarDef.Create("bug_reports.maximum_bug_report_description_length", 750, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Minimum length of a bug report description.
    /// </summary>
    public static readonly CVarDef<int> MinimumBugReportDescriptionLength =
        CVarDef.Create("bug_reports.minimum_bug_report_description_length", 10, CVar.SERVER | CVar.REPLICATED);
}
