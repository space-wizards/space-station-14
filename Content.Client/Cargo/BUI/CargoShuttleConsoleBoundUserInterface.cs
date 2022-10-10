using Content.Client.Cargo.UI;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Cargo.BUI;

public sealed class CargoShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private CargoShuttleMenu? _menu;

    public CargoShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        var collection = IoCManager.Instance;

        if (collection == null)
            return;

        _menu = new CargoShuttleMenu(collection.Resolve<IGameTiming>(), collection.Resolve<IPrototypeManager>(), collection.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>());
        _menu.ShuttleCallRequested += OnShuttleCall;
        _menu.ShuttleRecallRequested += OnShuttleRecall;
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

    private void OnShuttleRecall()
    {
        SendMessage(new CargoRecallShuttleMessage());
    }

    private void OnShuttleCall()
    {
        SendMessage(new CargoCallShuttleMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CargoShuttleConsoleBoundUserInterfaceState cargoState) return;
        _menu?.SetAccountName(cargoState.AccountName);
        _menu?.SetShuttleName(cargoState.ShuttleName);
        _menu?.SetShuttleETA(cargoState.ShuttleETA);
        _menu?.SetOrders(cargoState.Orders);
        _menu?.SetCanRecall(cargoState.CanRecall);
    }
}
