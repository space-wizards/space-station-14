using Content.Shared.Access.Systems;
using Content.Shared._DeltaV.Shipyard;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._DeltaV.Shipyard.UI;

public sealed class ShipyardConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly AccessReaderSystem _access;
    private readonly EntityWhitelistSystem _whitelist;

    [ViewVariables]
    private ShipyardConsoleMenu? _menu;

    public ShipyardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _access = EntMan.System<AccessReaderSystem>();
        _whitelist = EntMan.System<EntityWhitelistSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = new ShipyardConsoleMenu(Owner, _proto, EntMan, _player, _access, _whitelist);
        _menu.OpenCentered();
        _menu.OnClose += Close;
        _menu.OnPurchased += Purchase;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ShipyardConsoleState cast)
            return;

        _menu?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _menu?.Dispose();
    }

    private void Purchase(string id)
    {
        SendMessage(new ShipyardConsolePurchaseMessage(id));
    }
}
