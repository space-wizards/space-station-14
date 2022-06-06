using Robust.Client.GameObjects;

namespace Content.Client.Cargo;

public abstract class CargoBoundUserInterface : BoundUserInterface
{
    protected CargoBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
}
