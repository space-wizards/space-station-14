using System.Linq;
using Content.Client.Administration.UI;
using Content.Shared.Administration;
using Robust.Client;
using Robust.Shared.Network;

namespace Content.Client.Administration.Managers;

public sealed class ClientUsernameBanCacheManager : IClientUsernameBanCacheManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    private readonly SortedDictionary<int, UsernameCacheLine> _usernameRulesCache = new();

    private ISawmill _sawmill = default!;

    public event Action<List<UsernameCacheLine>>? UpdatedCache;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("username_bancache");

        _net.RegisterNetMessage<MsgUsernameBan>(ReceiveUsernameBan);
        _net.RegisterNetMessage<MsgRequestUsernameBans>();

        _net.RegisterNetMessage<MsgFullUsernameBan>(ReceiveFullUsernameBan);
        _net.RegisterNetMessage<MsgRequestFullUsernameBan>();

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ReceiveUsernameBan(MsgUsernameBan msg)
    {
        int id = msg.UsernameBan.Id;
        bool add = msg.UsernameBan.Add;
        bool extendToBan = msg.UsernameBan.ExtendToBan;
        bool regex = msg.UsernameBan.Regex;
        string expression = msg.UsernameBan.Expression;

        if (!add && id == -1)
        {
            _sawmill.Debug($"received username ban clear");
            _usernameRulesCache.Clear();
            UpdatedCache?.Invoke(BanList.ToList());
            return;
        }

        if (!add)
        {
            _sawmill.Debug($"received username ban delete {id}");
            _usernameRulesCache.Remove(id);
            UpdatedCache?.Invoke(BanList.ToList());
            return;
        }

        _sawmill.Verbose($"received username ban add {id}");
        _usernameRulesCache.Add(id, new UsernameCacheLine(expression, id, extendToBan, regex));
        UpdatedCache?.Invoke(BanList.ToList());
    }

    private void ReceiveFullUsernameBan(MsgFullUsernameBan msg)
    {
        _sawmill.Debug($"spawning window for {msg.FullUsernameBan.Id}");
        var window = new UsernameBanInfoWindow(msg.FullUsernameBan);
        window.OpenCentered();
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

    public void RequestFullUsernameBan(int id)
    {
        _net.ClientSendMessage(new MsgRequestFullUsernameBan() { BanId = id });
    }

    public IReadOnlyList<UsernameCacheLine> BanList => _usernameRulesCache.Values.ToList();
}
