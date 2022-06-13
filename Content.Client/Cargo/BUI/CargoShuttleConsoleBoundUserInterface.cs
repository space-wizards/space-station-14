using Content.Client.Cargo.UI;
using Content.Shared.Cargo.BUI;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.BUI;

public sealed class CargoShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private CargoShuttleMenu? _menu;

    public CargoShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _menu = new CargoShuttleMenu(IoCManager.Resolve<IPrototypeManager>(), EntitySystem.Get<SpriteSystem>());
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CargoShuttleConsoleBoundUserInterfaceState shuttleState) return;

        _menu?.SetOrders(shuttleState.Orders);
    }
}
