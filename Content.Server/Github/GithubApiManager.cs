using System.Threading.Channels;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Github.Requests;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.Github;

public sealed class GithubApiManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private int _periodLength;
    private bool _rateLimitAnnounceAdmins;
    private int _chatRateLimitAnnounceAdminDelay;
    private int _rateLimitCount;

    private ChannelWriter<IGithubRequest> _channelWriter = default!;

    public void Initialize(ChannelWriter<IGithubRequest> channelWriter)
    {
        _channelWriter = channelWriter;

        _configuration.OnValueChanged(CCVars.GithubIssueRateLimitPeriod, periodLength => _periodLength = (int)periodLength, true);
        _configuration.OnValueChanged(CCVars.IsGitHubIssuePerUserRateLimitAnnounceAdminsEnabled, announce => _rateLimitAnnounceAdmins = announce, true);
        _configuration.OnValueChanged(CCVars.GitHubIssuePerUserRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdminDelay = announce, true);
        _configuration.OnValueChanged(CCVars.GithubIssueRateLimitCount, limitCount => _rateLimitCount = limitCount, true);
    }

    public bool TryMakeRequest(EntityUid owner, IGithubRequest request)
    {
        if (IsRateLimited(owner, out var reason))
        {
            // todo: send client error message
            return false;
        }

        // needs separate error? would return false only if channel is Bound channel and size limit is reached
        return _channelWriter.TryWrite(request);
    }

    public bool TryMakeRequest(IGithubRequest request)
    {
        return _channelWriter.TryWrite(request);
    }

    private bool IsRateLimited(EntityUid entityUid, out string? reason)
    {
        reason = null;

        if (!_playerManager.TryGetSessionByEntity(entityUid, out var session))
            return false;

        var data = session.ContentData();

        if (data == null)
            return false;

        var rateLimit = data.GithubIssueRateLimit;
        var time = _gameTiming.RealTime;
        if (rateLimit.ActionCountExpiresAt < time)
        {
            rateLimit.ActionCountExpiresAt = time + TimeSpan.FromSeconds(_periodLength);

            // Backoff from spamming slowly
            rateLimit.ActionRateOverTime /= 2;
            rateLimit.RateLimitAnnouncedToPlayer = false;
        }

        rateLimit.ActionRateOverTime += 1;

        if (rateLimit.ActionRateOverTime <= _rateLimitCount)
            return false;

        // Breached rate limits, inform admins if configured.
        if (_rateLimitAnnounceAdmins && rateLimit.CanAnnounceToAdminsNextAt < time)
        {
            var message = Loc.GetString("gtihub-create-issue-rate-limit-admin-announcement", ("player", session.Name));
            _chatManager.SendAdminAlert(message);

            rateLimit.CanAnnounceToAdminsNextAt = time + TimeSpan.FromSeconds(_chatRateLimitAnnounceAdminDelay);
        }

        if (rateLimit.RateLimitAnnouncedToPlayer)
            return true;

        reason = Loc.GetString("github-create-issue-manager-rate-limited");
        _adminLogger.Add(LogType.ChatRateLimited, LogImpact.Medium, $"Player {session} breached chat rate limits");
        rateLimit.RateLimitAnnouncedToPlayer = true;
        return true;
    }
}
