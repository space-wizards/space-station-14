using System;
using Content.Shared.Administration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.Managers;

public sealed class GamePrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<GamePrototypeLoadMessage>(LoadGamePrototype);
    }

    private void LoadGamePrototype(GamePrototypeLoadMessage message)
    {
        _prototypeManager.LoadString(message.PrototypeData, true);
        _prototypeManager.ResolveResults();
        _localizationManager.ReloadLocalizations();
        GamePrototypeLoaded?.Invoke();
        Logger.InfoS("adminbus", "Loaded adminbus prototype data.");
    }

    public void SendGamePrototype(string prototype)
    {
        var msg = _netManager.CreateNetMessage<GamePrototypeLoadMessage>();
        msg.PrototypeData = prototype;
        _netManager.ClientSendMessage(msg);
    }

    public event Action? GamePrototypeLoaded;
}
