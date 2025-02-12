using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Fragments;

/// <summary>
/// Specific ui fragments need to inherit this class. The subclass is then used in yaml to tell a main ui to use it as a ui fragment.
/// </summary>
/// <example>
/// This is an example from the yaml definition from the notekeeper ui
/// <code>
/// - type: CartridgeUi
///     ui: !type:NotekeeperUi
/// </code>
/// </example>
[ImplicitDataDefinitionForInheritors]
public abstract partial class UIFragment
{
    public abstract Control GetUIFragmentRoot();

    public abstract void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner);

    public abstract void UpdateState(BoundUserInterfaceState state);

}
