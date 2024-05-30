using Content.Client.Cargo.UI;
using Content.Shared.Cargo.BUI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class CargoShuttleConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private CargoShuttleMenu? _menu;

    public CargoShuttleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<CargoShuttleMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CargoShuttleConsoleBoundUserInterfaceState cargoState) return;
        _menu?.SetAccountName(cargoState.AccountName);
        _menu?.SetShuttleName(cargoState.ShuttleName);
        _menu?.SetOrders(EntMan.System<SpriteSystem>(), _protoManager, cargoState.Orders);
    }
}
