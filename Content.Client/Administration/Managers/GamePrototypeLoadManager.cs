using Content.Shared.Administration;
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
        var changed = new Dictionary<Type, HashSet<string>>();
        _prototypeManager.LoadString(message.PrototypeData, true, changed);
        _prototypeManager.ResolveResults();
        _prototypeManager.ReloadPrototypes(changed);
        _localizationManager.ReloadLocalizations();
        Logger.InfoS("adminbus", "Loaded adminbus prototype data.");
    }

    public void SendGamePrototype(string prototype)
    {
        var msg = new GamePrototypeLoadMessage
        {
            PrototypeData = prototype
        };
        _netManager.ClientSendMessage(msg);
    }
}
