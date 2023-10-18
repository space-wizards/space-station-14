using Content.Server.EUI;
using Content.Shared.CryoSleep;
using Content.Shared.Eui;

namespace Content.Server.CryoSleep;

public sealed class CryoSleepEui : BaseEui
{
    private readonly CryoSleepSystem _cryoSystem;
    private readonly EntityUid _mind;

    public CryoSleepEui(EntityUid mind, CryoSleepSystem cryoSys)
    {
        _mind = mind;
        _cryoSystem = cryoSys;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptCryoChoiceMessage choice ||
            choice.Button == AcceptCryoUiButton.Deny)
        {
            Close();
            return;
        }

        if (_mind is { Valid: true } body)
        {
            _cryoSystem.CryoStoreBody(body);
        }

        Close();
    }
}
