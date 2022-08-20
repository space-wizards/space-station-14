using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class NotekeeperUi : CartridgeUI
{
    public override Control GetUIFragmentRoot()
    {
        return new NotekeeperUiFragment();
    }

    public override void Setup(BoundUserInterface userInterface)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {

    }
}
