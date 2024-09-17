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

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RevealTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? StunTime;
}