using System.Net;
using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Info;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Info;

public sealed class RulesManager
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    public void Initialize()
    {
        _netManager.Connected += OnConnected;
        _netManager.RegisterNetMessage<SendRulesInformationMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        var isLocalhost = IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) &&
                          _cfg.GetCVar(CCVars.RulesExemptLocal);

        var lastRead = await _dbManager.GetLastReadRules(e.Channel.UserId);
        var hasCooldown = lastRead > LastValidReadTime;

        var showRulesMessage = new SendRulesInformationMessage
        {
            PopupTime = _cfg.GetCVar(CCVars.RulesWaitTime),
            CoreRules = _cfg.GetCVar(CCVars.RulesFile),
            ShouldShowRules = !isLocalhost && !hasCooldown
        };
        _netManager.ServerSendMessage(showRulesMessage, e.Channel);
    }

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);

        if (_playerManager.TryGetSessionById(message.MsgChannel.UserId, out var session))
        {
            var playTime = _playTimeTracking.GetOverallPlaytime(session);
            if (message.FuckedRules && playTime < TimeSpan.FromHours(1))
            {
                var skippedMessage = Loc.GetString("admin-alert-new-player-skipping-rules", ("username", session.Name));
                _chatManager.SendAdminAlert(skippedMessage);

                _adminLogger.Add(LogType.Action,
                    LogImpact.Medium,
                    $"{skippedMessage}");
            }
        }
    }
}
