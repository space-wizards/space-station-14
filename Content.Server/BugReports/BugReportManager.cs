using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.BugReport;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.BugReports;

/// <summary>
///     Simple manager to handle players bug reports. Will forward valid reports through <see cref="ValidPlayerBugReportReceived"/>
/// </summary>
public sealed class BugReportManager : IBugReportManager
{
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly IGameMapManager _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private GameTicker _ticker = default!;
    private bool _tickerInitialized;

    public event EventHandler<ValidPlayerBugReportReceivedEvent>? ValidPlayerBugReportReceived;

    /// <summary>
    ///     List of player NetIds and the number of bug reports they have submitted this round.
    /// </summary>
    private readonly Dictionary<NetUserId, int> _bugReportsPerPlayerThisRound = new();

    public void Initialize()
    {
        _net.RegisterNetMessage<BugReportMessage>(ReceivedPlayerBugReport);
    }

    private void ReceivedPlayerBugReport(BugReportMessage message)
    {
        // This is the only way I know to get entity systems in a manager, and it's needed for the round number and stuff.
        // I'm using this approach to "avoid" nullability of _ticker.
        // The code gets ugly if I have to constantly check for it being null.
        if (!_tickerInitialized)
        {
            _tickerInitialized = true;
            _ticker = _entity.System<GameTicker>();
        }

        var netId = message.MsgChannel.UserId;

        if (!CheckIfValid(message, netId))
            return;

        _bugReportsPerPlayerThisRound[netId] = _bugReportsPerPlayerThisRound.GetValueOrDefault(netId) + 1;

        var title = message.ReportInformation.BugReportTitle;
        var description = message.ReportInformation.BugReportDescription;

        _admin.Add(LogType.BugReport, LogImpact.High, $"{message.MsgChannel.UserName}, {netId}: submitted a bug report. Title: {title}, Description: {description}");

        var bugReport = CreateBugReport(message);

        ValidPlayerBugReportReceived?.Invoke(this, bugReport);
    }

    // When the round restarts, clear the dictionary.
    public void Restart()
    {
        _bugReportsPerPlayerThisRound.Clear();
    }

    /// <summary>
    ///     Checks that the given report is valid. Includes various checks on the report and the player.
    /// </summary>
    /// <returns>True if the report is valid, false there is an issue with the report or the user sending it.</returns>
    private bool CheckIfValid(BugReportMessage message, NetUserId netId)
    {
        var session = _player.GetSessionById(netId);
        var playtime = _playTime.GetOverallPlaytime(session);

        if (!_cfg.GetCVar(CCVars.EnablePlayerBugReports))
            return false;

        if (TimeSpan.FromHours(_cfg.GetCVar(CCVars.MinimumPlaytimeBugReports)) > playtime)
            return false;

        if (_bugReportsPerPlayerThisRound.GetValueOrDefault(netId) >= _cfg.GetCVar(CCVars.MaximumBugReportsPerRound))
        {
            _admin.Add(LogType.BugReport, LogImpact.High, $"{message.MsgChannel.UserName}, {netId}: has tried to submit more than {_cfg.GetCVar(CCVars.MaximumBugReportsPerRound)} bug reports this round.");
            return false;
        }

        var titleMaxLen = _cfg.GetCVar(CCVars.MaximumBugReportTitleLength);
        var titleMinLen = _cfg.GetCVar(CCVars.MinimumBugReportTitleLength);
        var descriptionMaxLen = _cfg.GetCVar(CCVars.MaximumBugReportDescriptionLength);
        var descriptionMinLen = _cfg.GetCVar(CCVars.MinimumBugReportDescriptionLength);

        var descriptionLen = message.ReportInformation.BugReportDescription.Length;
        var titleLen = message.ReportInformation.BugReportTitle.Length;

        // These should only happen if there is a hacked client or a glitch!
        if (titleLen < titleMinLen || titleLen > titleMaxLen)
        {
            _admin.Add(LogType.BugReport, LogImpact.High, $"{message.MsgChannel.UserName}, {netId}: has tried to submit a bug report with a title of {titleLen} characters, min/max: {titleMinLen}/{titleMaxLen}.");
            return false;
        }

        if (descriptionLen < descriptionMinLen || descriptionLen > descriptionMaxLen)
        {
            _admin.Add(LogType.BugReport, LogImpact.High, $"{message.MsgChannel.UserName}, {netId}: has tried to submit a bug report with a description of {descriptionLen} characters, min/max: {descriptionMinLen}/{descriptionMaxLen}.");
            return false;
        }

        return true;
    }
    /// <summary>
    ///     Create a bug report out of the given message. This function will extra metadata that could be useful, along with
    ///     the original text report from the user.
    /// </summary>
    /// <param name="message">The message the user sent.</param>
    /// <returns>A <see cref="ValidPlayerBugReportReceivedEvent"/> based of the user report.</returns>
    private ValidPlayerBugReportReceivedEvent CreateBugReport(BugReportMessage message)
    {
        var metaData = new BugReportMetaData
        {
            Username = message.MsgChannel.UserName,
            RoundTime = _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan),
            // Yes, apparently the AdminLogsServerName is the one we want
            ServerName = _cfg.GetCVar(CCVars.AdminLogsServerName),
            RoundNumber = _ticker.RoundId,
            SubmittedTime = DateTime.UtcNow,
            RoundType = Loc.GetString(_ticker.CurrentPreset?.ModeTitle ?? "bug-report-report-unknown"),
            Map = _map.GetSelectedMap()?.MapName ?? Loc.GetString("bug-report-report-unknown"),
            NumberOfPlayers = _player.PlayerCount,
            BuildVersion = _cfg.GetCVar(CVars.BuildVersion),
            EngineVersion = _cfg.GetCVar(CVars.BuildEngineVersion),
        };

        return new ValidPlayerBugReportReceivedEvent(message.ReportInformation.BugReportTitle, message.ReportInformation.BugReportDescription, metaData);
    }
}
