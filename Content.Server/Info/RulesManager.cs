using System.Net;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Info;

[InjectDependencies]
public sealed partial class RulesManager : SharedRulesManager
{
    [Dependency] private IServerDbManager _dbManager = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShouldShowRulesPopupMessage>();
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
        _netManager.Connected += OnConnected;
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        if (IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) && _cfg.GetCVar(CCVars.RulesExemptLocal))
        {
            return;
        }

        var lastRead = await _dbManager.GetLastReadRules(e.Channel.UserId);
        if (lastRead > LastValidReadTime)
        {
            return;
        }

        var message = new ShouldShowRulesPopupMessage();
        _netManager.ServerSendMessage(message, e.Channel);
    }

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
    }
}
