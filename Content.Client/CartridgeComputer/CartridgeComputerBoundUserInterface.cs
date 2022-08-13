using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeComputer;


public abstract class CartridgeComputerBoundUserInterface : BoundUserInterface
{
    private readonly IEntityManager? _entityManager;

    protected Control? ActiveCartridgeUI;

    protected CartridgeComputerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private Control? RetrieveCartridgeUI(EntityUid cartridgeUid)
    {
        var component = _entityManager?.GetComponentOrNull<CartridgeUIComponent>(cartridgeUid);
        component?.Ui?.Setup(this);
        return component?.Ui?.GetUIFragmentRoot();
    }
}
