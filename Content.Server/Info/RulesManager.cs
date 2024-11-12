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
        _netManager.RegisterNetMessage<SendRulesInformationMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
         var isLocalhost = IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) &&
                               _cfg.GetCVar(CCVars.RulesExemptLocal);

        var lastRead = await _dbManager.GetLastReadRules(e.Channel.UserId);
        var bans = await _dbManager.GetServerBansAsync(e.Channel.RemoteEndPoint.Address, e.Channel.UserId, e.Channel.UserData.HWId);
        var hasCooldown = lastRead > LastValidReadTime;

        // When someone is banned, the next time they connect to the server, they will be forced to have the rules popup again.
        // I'm sure they will read it closely this time :)
        var banOverride = bans.Count > 0 && bans[^1].ExpirationTime > lastRead;

        var showRulesMessage = new SendRulesInformationMessage
        {
            PopupTime = _cfg.GetCVar(CCVars.RulesWaitTime),
            CoreRules = _cfg.GetCVar(CCVars.RulesFile),
            ShouldShowRules = banOverride || !isLocalhost && !hasCooldown,
        };
        _netManager.ServerSendMessage(showRulesMessage, e.Channel);
    }

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
    }
}
