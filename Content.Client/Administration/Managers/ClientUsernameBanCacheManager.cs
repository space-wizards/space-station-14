using Content.Shared.Administration;
using Robust.Client;
using Robust.Shared.Network;

namespace Content.Client.Administration.Managers;

public sealed class ClientUsernameBanCacheManager : IClientUsernameBanCacheManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    private readonly Dictionary<int, (string, string, bool)> _usernameRulesCache = new();

    private ISawmill _sawmill = default!;

    public event Action? UpdatedCache;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("username_bancache");

        _net.RegisterNetMessage<MsgUsernameBan>(ReceiveUsernameBan);
        _net.RegisterNetMessage<MsgRequestUsernameBans>();

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ReceiveUsernameBan(MsgUsernameBan msg)
    {
        (bool add, bool extendToBan, int id, string expression, string message) = msg.UsernameBan;

        if (!add && id == -1)
        {
            _sawmill.Debug($"received username ban clear");
            _usernameRulesCache.Clear();
            return;
        }

        if (!add)
        {
            _sawmill.Debug($"received username ban delete {id}");
            _usernameRulesCache.Remove(id);
            return;
        }

        _sawmill.Debug($"received username ban add {id}");

        _usernameRulesCache.Add(id, (expression, message, extendToBan));

        UpdatedCache?.Invoke();
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // removal is unknown while disconnected clear all to avoid phantom username bans
            _usernameRulesCache.Clear();
        }
        else if (e.NewLevel == ClientRunLevel.InGame)
        {
            RequestUsernameBans();
        }
    }

    public void RequestUsernameBans()
    {
        _net.ClientSendMessage(new MsgRequestUsernameBans());
    }
}
