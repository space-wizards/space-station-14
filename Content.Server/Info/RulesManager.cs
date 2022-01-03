using Content.Shared.Info;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Info;

public class RulesManager : SharedRulesManager
{
    [Dependency] private readonly INetManager _netManager = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ShowRulesPopupMessage>();
    }
}
