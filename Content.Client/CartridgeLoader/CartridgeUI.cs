using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader;

[ImplicitDataDefinitionForInheritors]
public abstract class CartridgeUI
{
    public abstract Control GetUIFragmentRoot();

    public abstract void Setup(BoundUserInterface userInterface);

    public abstract void UpdateState(BoundUserInterfaceState state);

}
