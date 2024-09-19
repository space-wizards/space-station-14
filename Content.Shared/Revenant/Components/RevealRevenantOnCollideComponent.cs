namespace Content.Shared.Revenant.Components;

[RegisterComponent]
public sealed partial class RevealRevenantOnCollideComponent : Component
{
    /// <summary>
    /// Popup text to show the revenant upon revealing. Works with
    /// localization strings as well.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string PopupText;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RevealTime = TimeSpan.FromSeconds(5);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? StunTime;
}