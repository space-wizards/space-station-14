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

    private readonly List<string> LoadedPrototypes = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<GamePrototypeLoadMessage>(ClientLoadsPrototype);
        _netManager.Connected += NetManagerOnConnected;
    }

    public void SendGamePrototype(string prototype)
    {

    }

    public event Action? GamePrototypeLoaded;

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
        LoadedPrototypes.Add(prototypeData);
        var msg = new GamePrototypeLoadMessage();
        msg.PrototypeData = prototypeData;
        _netManager.ServerSendToAll(msg); // everyone load it up!
        _prototypeManager.LoadString(prototypeData, true); // server needs it too.
        _prototypeManager.ResolveResults();
        _localizationManager.ReloadLocalizations();
        GamePrototypeLoaded?.Invoke();
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // Just dump all the prototypes on connect, before them missing could be an issue.
        foreach (var prototype in LoadedPrototypes)
        {
            var msg = new GamePrototypeLoadMessage();
            msg.PrototypeData = prototype;
            e.Channel.SendMessage(msg);
        }
    }
}
