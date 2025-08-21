namespace Content.Server.BugReports;

/// <summary>
/// Manager for validating client bug reports, issued in-game, and relaying creation of issue in tracker to dedicated api client.
/// </summary>
public interface IBugReportManager
{
    /// <summary> Will get called when the manager is first initialized. </summary>
    public void Initialize();

    /// <summary>
    /// Will get called whenever the round is restarted.
    /// Should be used to clean up anything that needs reset after each round.
    /// </summary>
    public void Restart();

    /// <summary>
    /// Will get called whenever the round is restarted.
    /// Should be used to clean up anything that needs reset after each round.
    /// </summary>
    public void Shutdown();
}
