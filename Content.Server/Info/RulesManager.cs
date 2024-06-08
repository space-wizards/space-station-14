using System.Net;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Info;

public sealed class RulesManager
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    public void Initialize()
    {
        _netManager.Connected += OnConnected;
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        if (IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) && _cfg.GetCVar(CCVars.RulesExemptLocal))
            return;

        var lastRead = await _dbManager.GetLastReadRules(e.Channel.UserId);
        if (lastRead > LastValidReadTime)
            return;

        var message = new ShowRulesPopupMessage();
        message.PopupTime = _cfg.GetCVar(CCVars.RulesWaitTime);
        _netManager.ServerSendMessage(message, e.Channel);
    }

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
    }
}
