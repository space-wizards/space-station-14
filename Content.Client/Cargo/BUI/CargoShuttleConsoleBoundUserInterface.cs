using Content.Client.Cargo.UI;
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
}
