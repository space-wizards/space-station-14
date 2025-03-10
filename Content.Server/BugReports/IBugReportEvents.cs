namespace Content.Server.BugReports;

public sealed class ValidPlayerBugReportReceivedEvent(string title, string description, BugReportMetaData metaData) : EventArgs
{
    public string Title = title;
    public string Description = description;
    public BugReportMetaData MetaData = metaData;
}

public struct BugReportMetaData
{
    public string Username;
    public TimeSpan RoundTime;
    public string ServerName;
    public int RoundNumber;
    public DateTime SubmittedTime;
    public string RoundType;
    public string Map;
    public int NumberOfPlayers;
    public string BuildVersion;
    public string EngineVersion;
}
