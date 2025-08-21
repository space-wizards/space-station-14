using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Github;
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

/// <inheritdoc cref="IBugReportManager"/>
public sealed class BugReportManager : IBugReportManager, IPostInjectInit
{
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly IGameMapManager _map = default!;
    [Dependency] private readonly GithubApiManager _githubApiManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// List of player NetIds and the number of bug reports they have submitted this round.
    /// UserId -> (bug reports this round, last submitted bug report)
    /// </summary>
    private readonly Dictionary<NetUserId, (int ReportsCount, DateTime ReportedDateTime)> _bugReportsPerPlayerThisRound = new();

    private BugReportLimits _limits = default!;

    private List<string> _tags = [];

    private ConfigurationMultiSubscriptionBuilder _configSub = default!;

    public void Initialize()
    {
        _net.RegisterNetMessage<BugReportMessage>(ReceivedPlayerBugReport);

        _limits = new BugReportLimits();

        _configSub = _cfg.SubscribeMultiple()
            .OnValueChanged(CCVars.MaximumBugReportTitleLength, x => _limits.TitleMaxLength = x, true)
            .OnValueChanged(CCVars.MinimumBugReportTitleLength, x => _limits.TitleMinLength = x, true)
            .OnValueChanged(CCVars.MaximumBugReportDescriptionLength, x => _limits.DescriptionMaxLength = x, true)
            .OnValueChanged(CCVars.MinimumBugReportDescriptionLength, x => _limits.DescriptionMinLength = x, true)
            .OnValueChanged(CCVars.MinimumPlaytimeInMinutesToEnableBugReports, x => _limits.MinimumPlaytimeToEnableBugReports = TimeSpan.FromMinutes(x), true)
            .OnValueChanged(CCVars.MaximumBugReportsPerRound, x => _limits.MaximumBugReportsForPlayerPerRound = x, true)
            .OnValueChanged(CCVars.MinimumSecondsBetweenBugReports, x => _limits.MinimumTimeBetweenBugReports = TimeSpan.FromSeconds(x), true)
            .OnValueChanged(CCVars.BugReportTags, x => _tags = x.Split(",").ToList(), true);
    }

    public void Restart()
    {
        // When the round restarts, clear the dictionary.
        _bugReportsPerPlayerThisRound.Clear();
    }

    public void Shutdown()
    {
        _configSub.Dispose();
    }

    private void ReceivedPlayerBugReport(BugReportMessage message)
    {
        if (!_cfg.GetCVar(CCVars.EnablePlayerBugReports))
            return;

        var netId = message.MsgChannel.UserId;
        var userName = message.MsgChannel.UserName;
        var report = message.ReportInformation;
        if (!IsBugReportValid(report, (NetId: netId, UserName: userName)) || !CanPlayerSendReport(netId, userName))
            return;

        var playerBugReportingStats = _bugReportsPerPlayerThisRound.GetValueOrDefault(netId);
        _bugReportsPerPlayerThisRound[netId] = (playerBugReportingStats.ReportsCount + 1, DateTime.UtcNow);

        var title = report.BugReportTitle;
        var description = report.BugReportDescription;

        _admin.Add(LogType.BugReport, LogImpact.High, $"{message.MsgChannel.UserName}, {netId}: submitted a bug report. Title: {title}, Description: {description}");

        var bugReport = CreateBugReport(message);

        _githubApiManager.TryCreateIssue(bugReport);
    }

    /// <summary>
    /// Checks that the given report is valid (E.g. not too long etc...).
    /// Logs problems if report is invalid.
    /// </summary>
    /// <returns>True if the report is valid, false there is an issue with the report.</returns>
    private bool IsBugReportValid(PlayerBugReportInformation report, (NetUserId NetId, string UserName) userData)
    {
        var descriptionLen = report.BugReportDescription.Length;
        var titleLen = report.BugReportTitle.Length;

        // These should only happen if there is a hacked client or a glitch!
        if (titleLen < _limits.TitleMinLength || titleLen > _limits.TitleMaxLength)
        {
            _sawmill.Warning(
                $"{userData.UserName}, {userData.NetId}: has tried to submit a bug report "
                + $"with a title of {titleLen} characters, min/max: {_limits.TitleMinLength}/{_limits.TitleMaxLength}."
            );
            return false;
        }

        if (descriptionLen < _limits.DescriptionMinLength || descriptionLen > _limits.DescriptionMaxLength)
        {
            _sawmill.Warning(
                $"{userData.UserName}, {userData.NetId}: has tried to submit a bug report "
                + $"with a description of {descriptionLen} characters, min/max: {_limits.DescriptionMinLength}/{_limits.DescriptionMaxLength}."
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks that the player sending the report is allowed to (E.g. not spamming etc...).
    /// Logs problems if report is invalid.
    /// </summary>
    /// <returns>True if the player can submit a report, false if they can't.</returns>
    private bool CanPlayerSendReport(NetUserId netId, string userName)
    {
        var session = _player.GetSessionById(netId);
        var playtime = _playTime.GetOverallPlaytime(session);
        if (_limits.MinimumPlaytimeToEnableBugReports > playtime)
            return false;

        var playerBugReportingStats = _bugReportsPerPlayerThisRound.GetValueOrDefault(netId);
        var maximumBugReportsForPlayerPerRound = _limits.MaximumBugReportsForPlayerPerRound;
        if (playerBugReportingStats.ReportsCount >= maximumBugReportsForPlayerPerRound)
        {
            _admin.Add(LogType.BugReport,
                LogImpact.High,
                $"{userName}, {netId}: has tried to submit more than {maximumBugReportsForPlayerPerRound} bug reports this round.");
            return false;
        }

        var timeSinceLastReport = DateTime.UtcNow - playerBugReportingStats.ReportedDateTime;
        var timeBetweenBugReports = _limits.MinimumTimeBetweenBugReports;
        if (timeSinceLastReport <= timeBetweenBugReports)
        {
            _admin.Add(LogType.BugReport,
                LogImpact.High,
                $"{userName}, {netId}: has tried to submit a bug report. "
                + $"Last bug report was {timeSinceLastReport:g} ago. The limit is {timeBetweenBugReports:g} minutes."
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Create a bug report out of the given message. Add will extra metadata that could be useful, along with
    /// the original text report from the user.
    /// </summary>
    /// <param name="message">The message from user.</param>
    /// <returns>A <see cref="ValidPlayerBugReportReceivedEvent"/> based of the user report.</returns>
    private ValidPlayerBugReportReceivedEvent CreateBugReport(BugReportMessage message)
    {
        // todo: dont request entity system out of sim, check if you are in-sim before doing so. Bug report should work out of sim too.
        var ticker = _entity.System<GameTicker>();
        var metadata = new BugReportMetaData
        {
            Username = message.MsgChannel.UserName,
            PlayerGUID = message.MsgChannel.UserData.UserId,
            ServerName = _cfg.GetCVar(CCVars.AdminLogsServerName),
            NumberOfPlayers = _player.PlayerCount,
            SubmittedTime = DateTime.UtcNow,
            BuildVersion = _cfg.GetCVar(CVars.BuildVersion),
            EngineVersion = _cfg.GetCVar(CVars.BuildEngineVersion),
        };

        // Only add these if your in round.
        if (ticker.Preset != null)
        {
            metadata.RoundTime = _timing.CurTime.Subtract(ticker.RoundStartTimeSpan);
            metadata.RoundNumber = ticker.RoundId;
            metadata.RoundType = Loc.GetString(ticker.CurrentPreset?.ModeTitle ?? "bug-report-report-unknown");
            metadata.Map = _map.GetSelectedMap()?.MapName ?? Loc.GetString("bug-report-report-unknown");
        }

        return new ValidPlayerBugReportReceivedEvent(
            message.ReportInformation.BugReportTitle.Trim(),
            message.ReportInformation.BugReportDescription.Trim(),
            metadata,
            _tags
        );
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("BugReport");
    }

    private sealed class BugReportLimits
    {
        public int TitleMaxLength;
        public int TitleMinLength;
        public int DescriptionMaxLength;
        public int DescriptionMinLength;

        public TimeSpan MinimumPlaytimeToEnableBugReports;
        public int MaximumBugReportsForPlayerPerRound;
        public TimeSpan MinimumTimeBetweenBugReports;
    }
}
