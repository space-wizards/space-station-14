using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader;

/// <summary>
/// Cartridge ui fragments need to inherit this class. The subclass is then used in yaml to tell the cartridge loader ui to use it as the cartridges ui fragment.
/// </summary>
/// <example>
/// This is an example from the yaml definition from the notekeeper ui
/// <code>
/// - type: CartridgeUi
///     ui: !type:NotekeeperUi
/// </code>
/// </example>
[ImplicitDataDefinitionForInheritors]
public abstract class CartridgeUI
{
    public abstract Control GetUIFragmentRoot();

    public abstract void Setup(BoundUserInterface userInterface);

    public abstract void UpdateState(BoundUserInterfaceState state);

}
