using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed class GamePrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    private readonly List<string> _loadedPrototypes = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<GamePrototypeLoadMessage>(ClientLoadsPrototype);
        _netManager.Connected += NetManagerOnConnected;
    }

    public void SendGamePrototype(string prototype)
    {

    }

    private void ClientLoadsPrototype(GamePrototypeLoadMessage message)
    {
        var player = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (_adminManager.IsAdmin(player) && _adminManager.HasAdminFlag(player, AdminFlags.Query))
        {
            LoadPrototypeData(message.PrototypeData);
            Logger.InfoS("adminbus", $"Loaded adminbus prototype data from {player.Name}.");
        }
        else
        {
            message.MsgChannel.Disconnect("Sent prototype message without permission!");
        }
    }

    private void LoadPrototypeData(string prototypeData)
    {
        _loadedPrototypes.Add(prototypeData);
        var msg = new GamePrototypeLoadMessage
        {
            PrototypeData = prototypeData
        };
        _netManager.ServerSendToAll(msg); // everyone load it up!
        var changed = new Dictionary<Type, HashSet<string>>();
        _prototypeManager.LoadString(prototypeData, true, changed); // server needs it too.
        _prototypeManager.ResolveResults();
        _prototypeManager.ReloadPrototypes(changed);
        _localizationManager.ReloadLocalizations();
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // Just dump all the prototypes on connect, before them missing could be an issue.
        foreach (var prototype in _loadedPrototypes)
        {
            var msg = new GamePrototypeLoadMessage
            {
                PrototypeData = prototype
            };
            e.Channel.SendMessage(msg);
        }
    }
}
