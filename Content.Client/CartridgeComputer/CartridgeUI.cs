using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeComputer;

public abstract class CartridgeUI
{

    public abstract Control GetUIFragmentRoot();

    public abstract void Setup(BoundUserInterface userInterface);

}
