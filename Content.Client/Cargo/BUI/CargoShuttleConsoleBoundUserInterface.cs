using Content.Client.Cargo.UI;
using Content.Shared.Cargo.BUI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class CargoShuttleConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CargoShuttleMenu? _menu;

    public CargoShuttleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        var collection = IoCManager.Instance;

        if (collection == null)
            return;

        _menu = new CargoShuttleMenu(collection.Resolve<IPrototypeManager>(), collection.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>());
        _menu.OnClose += Close;

        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _menu?.Dispose();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CargoShuttleConsoleBoundUserInterfaceState cargoState) return;
        _menu?.SetAccountName(cargoState.AccountName);
        _menu?.SetShuttleName(cargoState.ShuttleName);
        _menu?.SetOrders(cargoState.Orders);
    }
}
