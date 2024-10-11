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

        _net.RegisterNetMessage<MsgUsernameBans>(ReceiveUsernameBans);
        _net.RegisterNetMessage<MsgRequestUsernameBans>();

        _client.RunLevelChanged += ClientOnRunLevelChanged;
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

    private void ReceiveUsernameBans(MsgUsernameBans msg)
    {
        _sawmill.Debug($"received {msg.UsernameBans.Count} username ban changes");

        foreach (var ban in msg.UsernameBans)
        {
            (bool add, bool extendToBan, int id, string expression, string message) = ban;


            if (!add && id == -1)
            {
                _usernameRulesCache.Clear();
                continue;
            }

            if (!add)
            {
                _usernameRulesCache.Remove(id);
                continue;
            }


            _usernameRulesCache.Add(id, (expression, message, extendToBan));
        }

        UpdatedCache?.Invoke();
    }

}
