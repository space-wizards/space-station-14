using Content.Client.UserInterface.Fragments;

namespace Content.Client.CartridgeLoader;

/// <summary>
/// The component used for defining a ui fragment to attach to an entity
/// </summary>
/// <remarks>
/// This is used primarily for PDA cartridges.
/// </remarks>
/// <seealso cref="UIFragment"/>
/// <seealso cref="UIFragmentSerializer"/>
[RegisterComponent]
public sealed class UIFragmentComponent : Component
{
    [DataField("ui", true, customTypeSerializer: typeof(UIFragmentSerializer))]
    public UIFragment? Ui;
}
