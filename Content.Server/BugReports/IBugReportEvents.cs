using Robust.Shared.Network;

namespace Content.Server.BugReports;

/// <summary>
/// This event stores information related to a player submitted bug report.
/// </summary>
public sealed class ValidPlayerBugReportReceivedEvent(string title, string description, BugReportMetaData metaData, List<string> tags) : EventArgs
{
    /// <summary>
    /// Title for the bug report. This is player controlled!
    /// </summary>
    public string Title = title;

    /// <summary>
    /// Description for the bug report. This is player controlled!
    /// </summary>
    public string Description = description;

    /// <summary>
    /// Metadata for bug report, containing data collected by server.
    /// </summary>
    public BugReportMetaData MetaData = metaData;

    public List<string> Tags = tags;
}

/// <summary>
/// Metadata for a bug report. Holds relevant data for bug reports that aren't directly player controlled.
/// </summary>
public sealed class BugReportMetaData
{
    /// <summary>
    /// Bug reporter SS14 username.
    /// </summary>
    /// <example>piggylongsnout</example>
    public required string Username;

    /// <summary>
    /// The GUID of the player who reported the bug.
    /// </summary>
    public required NetUserId PlayerGUID;

    /// <summary>
    /// Name of the server from which bug report was issued.
    /// </summary>
    /// <example>DeltaV</example>>
    public required string ServerName;

    /// <summary>
    /// Date and time on which player submitted report (NOT round time).
    /// The time is UTC and based off the servers clock.
    /// </summary>
    public required DateTime SubmittedTime;

    /// <summary>
    /// Time that has elapsed in the round. Can be null if bug was not reported during a round.
    /// </summary>
    public TimeSpan? RoundTime;

    /// <summary>
    /// Round number during which bug report was issued. Can be null if bug was reported not during round.
    /// </summary>
    /// <example>1311</example>
    public int? RoundNumber;

    /// <summary>
    /// Type preset title (type of round that is being played). Can be null if bug was reported not during round.
    /// </summary>
    /// <example>Sandbox</example>
    public string? RoundType;

    /// <summary>
    /// The map being played.
    /// </summary>
    /// <example>"Dev"</example>>
    public string? Map;

    /// <summary>
    /// Number of players currently on server.
    /// </summary>
    public int NumberOfPlayers;

    /// <summary>
    /// Build version of the game.
    /// </summary>
    public required string BuildVersion;

    /// <summary>
    /// Engine version of the game.
    /// </summary>
    /// <example>253.0.0</example>
    public required string EngineVersion;
}
