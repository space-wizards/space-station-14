using Content.Shared._Goobstation;
using Content.Shared._Goobstation.Standing;
using Content.Shared._Impstation.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Standing;

public sealed class LayingDownSystem : SharedLayingDownSystem
{
    [Dependency] private readonly INetConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CheckAutoGetUpEvent>(OnCheckAutoGetUp);
    }

    private void OnCheckAutoGetUp(CheckAutoGetUpEvent ev, EntitySessionEventArgs args)
    {
        var uid = GetEntity(ev.User);

        if (!TryComp(uid, out LayingDownComponent? layingDown))
            return;

        layingDown.AutoGetUp = _cfg.GetClientCVar(args.SenderSession.Channel, ImpCCVars.AutoGetUp);
        Dirty(uid, layingDown);
    }
}
