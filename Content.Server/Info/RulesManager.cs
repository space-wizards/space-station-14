using System.Net;
using Content.Server.Administration.Logs;
using Content.Server.Database;
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
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

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
            ShouldShowRules = !isLocalhost && !hasCooldown,
        };
        _netManager.ServerSendMessage(showRulesMessage, e.Channel);
    }

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
        if (message.FuckRules && _player.TryGetSessionById(message.MsgChannel.UserId, out var session))
            _adminLog.Add(LogType.Connection, LogImpact.Extreme, $"Player {session} used the fuckrules command.");
    }
}
