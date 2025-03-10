namespace Content.Server.BugReports;

/// <summary>
///     This manager deals with bug reports! It should listen for user reports (<see cref="Content.Shared.BugReport.BugReportMessage"/>)
///     and pass valid ones along through <see cref="ValidPlayerBugReportReceived"/>.
/// </summary>
public interface IBugReportManager
{
    /// <summary>
    ///     Will get called when the manager is first initialized.
    /// </summary>
    public void Initialize();
    /// <summary>
    ///     Will get called whenever the round is restarted. Should be used to clean up anything that should reset
    ///     after each round!
    /// </summary>
    public void Restart();

    /// <summary>
    ///     This event should be invoked whenever legitimate bug report from players is received. This manager
    ///     shouldn't save report itself, it should just invoke valid reports on this event so other
    ///     managers (E.g. a Discord relay, GitHub relay etc...) can actually deal with them properly.
    /// </summary>
    event EventHandler<ValidPlayerBugReportReceivedEvent>? ValidPlayerBugReportReceived;
}
