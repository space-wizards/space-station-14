namespace Content.Server.BugReports;

/// <summary>
///     This event stores information related to a player submitted bug report.
/// </summary>
public sealed class ValidPlayerBugReportReceivedEvent(string title, string description, BugReportMetaData metaData) : EventArgs
{
    /// <summary>
    ///     Title for the bug report. This is player controlled!
    /// </summary>
    public string Title = title;

    /// <summary>
    ///     Description for the bug report. This is player controlled!
    /// </summary>
    public string Description = description;

    /// <summary>
    ///     Metadata for the bug report. This is not player controlled.
    /// </summary>
    public BugReportMetaData MetaData = metaData;
}

/// <summary>
///     Metadata for a bug report. Holds relevant data for bug reports that aren't directly player controlled.
/// </summary>
public struct BugReportMetaData
{
    /// <summary>
    ///     Players SS14 username.
    /// </summary>
    public string Username;

    /// <summary>
    ///     Time that has elapsed in the round.
    /// </summary>
    public TimeSpan RoundTime;

    /// <summary>
    ///     Name of the server the player was playing on when submitting the report.
    /// </summary>
    public string ServerName;

    /// <summary>
    ///     The round the player submitted the bug report.
    /// </summary>
    public int RoundNumber;

    /// <summary>
    ///     Actual time the player submitted the report (NOT round time.)
    /// </summary>
    public DateTime SubmittedTime;

    /// <summary>
    ///     The type of round that is being played.
    /// </summary>
    public string RoundType;

    /// <summary>
    ///     The map being played.
    /// </summary>
    public string Map;

    /// <summary>
    ///     Number of players in the game.
    /// </summary>
    public int NumberOfPlayers;

    /// <summary>
    ///     Build version of the game.
    /// </summary>
    public string BuildVersion;

    /// <summary>
    ///     Engine version of the game.
    /// </summary>
    public string EngineVersion;
}
