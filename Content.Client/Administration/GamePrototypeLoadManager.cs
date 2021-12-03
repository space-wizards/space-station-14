using Content.Shared.Administration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration;

public interface IGamePrototypeLoadManager
{
    public void Initialize();
    public void SendGamePrototype(string prototype);
}

public class GamePrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public void Initialize()
    {
        _netManager.RegisterNetMessage<GamePrototypeLoadMessage>(LoadGamePrototype);
    }

    private void LoadGamePrototype(GamePrototypeLoadMessage message)
    {
        _prototypeManager.LoadString(message.PrototypeData);
        Logger.InfoS("adminbus", "Loaded adminbus prototype data.");
    }

    public void SendGamePrototype(string prototype)
    {
        var msg = _netManager.CreateNetMessage<GamePrototypeLoadMessage>();
        msg.PrototypeData = prototype;
        _netManager.ClientSendMessage(msg);
    }
}
