using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Allow users to submit bug reports. Will enable a button on the hotbar. See <see cref="GithubEnabled" /> for
    /// setting up the GitHub API!
    /// </summary>
    public static readonly CVarDef<bool> EnablePlayerBugReports =
        CVarDef.Create("bug_reports.enable_player_bug_reports", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Minimum playtime that players need to have played to submit bug reports.
    /// </summary>
    public static readonly CVarDef<int> MinimumPlaytimeInMinutesToEnableBugReports =
        CVarDef.Create("bug_reports.minimum_playtime_in_minutes_to_enable_bug_reports", 120, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum number of bug reports a user can submit per round.
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportsPerRound =
        CVarDef.Create("bug_reports.maximum_bug_reports_per_round", 5, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Minimum time between bug reports.
    /// </summary>
    public static readonly CVarDef<int> MinimumSecondsBetweenBugReports =
        CVarDef.Create("bug_reports.minimum_seconds_between_bug_reports", 120, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum length of a bug report title.
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportTitleLength =
        CVarDef.Create("bug_reports.maximum_bug_report_title_length", 35, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Minimum length of a bug report title.
    /// </summary>
    public static readonly CVarDef<int> MinimumBugReportTitleLength =
        CVarDef.Create("bug_reports.minimum_bug_report_title_length", 10, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum length of a bug report description.
    /// </summary>
    public static readonly CVarDef<int> MaximumBugReportDescriptionLength =
        CVarDef.Create("bug_reports.maximum_bug_report_description_length", 750, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Minimum length of a bug report description.
    /// </summary>
    public static readonly CVarDef<int> MinimumBugReportDescriptionLength =
        CVarDef.Create("bug_reports.minimum_bug_report_description_length", 10, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// List of tags that are added to the report. Separate each value with ",".
    /// </summary>
    /// <example>
    /// IG report, Bug
    /// </example>
    public static readonly CVarDef<string> BugReportTags =
        CVarDef.Create("bug_reports.tags", "IG bug report", CVar.SERVER | CVar.REPLICATED);
}
