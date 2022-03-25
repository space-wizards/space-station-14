using System.Net;
using Content.Server.Database;
using Content.Shared.Info;
using Robust.Shared.Network;

namespace Content.Server.Info;

public sealed class RulesManager : SharedRulesManager
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    private readonly Dictionary<NetUserId, DateTime> _lastReadRulesCache = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShouldShowRulesPopupMessage>();
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
        _netManager.Connected += OnConnected;
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        if (IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address))
        {
            return;
        }

        var lastRead = await _dbManager.GetLastReadRules(e.Channel.UserId);
        if (lastRead > LastValidReadTime)
        {
            return;
        }

        var message = _netManager.CreateNetMessage<ShouldShowRulesPopupMessage>();
        _netManager.ServerSendMessage(message, e.Channel);
    }

    private void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;

        if (_lastReadRulesCache.TryGetValue(message.MsgChannel.UserId, out var lastRead) &&
            lastRead > date - TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastReadRulesCache[message.MsgChannel.UserId] = date;
        _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
    }
}
