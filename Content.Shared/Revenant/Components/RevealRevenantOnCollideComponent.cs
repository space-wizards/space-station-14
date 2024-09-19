using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevealRevenantOnCollideComponent : Component
{
    /// <summary>
    /// Popup text to show the revenant upon revealing. Works with
    /// localization strings as well.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string PopupText = "revenant-revealed-default";

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan RevealTime = TimeSpan.FromSeconds(5);

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan? StunTime;
}