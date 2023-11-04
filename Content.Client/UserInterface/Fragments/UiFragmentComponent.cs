namespace Content.Client.UserInterface.Fragments;

/// <summary>
/// The component used for defining a ui fragment to attach to an entity
/// </summary>
/// <remarks>
/// This is used primarily for PDA cartridges.
/// </remarks>
/// <seealso cref="UIFragment"/>
[RegisterComponent]
public sealed partial class UIFragmentComponent : Component
{
    [DataField("ui", true)]
    public UIFragment? Ui;
}
