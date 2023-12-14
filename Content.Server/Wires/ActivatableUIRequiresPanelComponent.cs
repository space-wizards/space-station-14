namespace Content.Server.Wires;

/// <summary>
/// This is used for activatable UIs that require the entity to have a panel in a certain state.
/// </summary>
[RegisterComponent]
public sealed partial class ActivatableUIRequiresPanelComponent : Component
{
    /// <summary>
    /// TRUE: the panel must be open to access the UI.
    /// FALSE: the panel must be closed to access the UI.
    /// </summary>
    [DataField("requireOpen"), ViewVariables(VVAccess.ReadWrite)]
    public bool RequireOpen = true;
}
