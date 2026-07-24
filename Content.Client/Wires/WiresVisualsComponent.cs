namespace Content.Client.Wires;

/// <summary>
/// Denotes an entity with an openable maintenance panel.
/// The sprite layer with a map of "enum.WiresVisualLayers.MaintenancePanel" will be hidden and shown when the maintenance panel is open.
/// </summary>
/// <seealso cref="WiresVisuals.MaintenancePanelState"/>
[RegisterComponent]
public sealed partial class WiresVisualsComponent : Component
{
    /// <summary>
    /// If true, the maintenance panel should be visible normally.
    /// </summary>
    [DataField]
    public bool Inverted;
}
