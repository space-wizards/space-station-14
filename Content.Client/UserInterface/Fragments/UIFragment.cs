using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Fragments;

/// <summary>
/// The parent class for UI fragments. Subclasses can be used in YAML to incorporate a fragment into a main UI.
/// </summary>
/// <example>
/// This is an example from the YAML definition from the Notekeeper UI.
/// The <see cref="CartridgeUiComponent"/> is extending the PDA window here.
/// <code>
/// - type: CartridgeUi
///     ui: !type:NotekeeperUi
/// </code>
/// </example>
[ImplicitDataDefinitionForInheritors]
public abstract partial class UIFragment
{
    /// <summary>
    /// Returns the root control for this fragment.
    /// </summary>
    public abstract Control GetUIFragmentRoot();

    /// <summary>
    /// Sets up the controls for this fragment.
    /// </summary>
    /// <remarks>
    /// Controls should be tied to the lifecycle of <paramref name="userInterface"/> if possible.
    /// Prefer using <see cref="BoundUserInterfaceExt.CreateDisposableControl{T}(BoundUserInterface)"/>.
    /// </remarks>
    public abstract void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner);

    /// <summary>
    /// Passes new BUI state to the fragment.
    /// </summary>
    public abstract void UpdateState(BoundUserInterfaceState state);
}
