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

    public CargoShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _menu = new CargoShuttleMenu(IoCManager.Resolve<IGameTiming>(), IoCManager.Resolve<IPrototypeManager>(), EntitySystem.Get<SpriteSystem>());

        _menu.ShuttleCallRequested += OnShuttleCall;
        _menu.ShuttleRecallRequested += OnShuttleRecall;

        _menu.OpenCentered();
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
        if (state is not CargoShuttleConsoleBoundUserInterfaceState shuttleState) return;

        _menu?.SetAccountName(shuttleState.AccountName);
        _menu?.SetShuttleName(shuttleState.ShuttleName);
        _menu?.SetShuttleETA(shuttleState.ShuttleETA);
        _menu?.SetOrders(shuttleState.Orders);
        _menu?.SetCanRecall(shuttleState.CanRecall);
    }
}
