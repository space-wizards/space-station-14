using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Shared component that contains battery information for display purposes.
/// Automatically added to items with batteries and synced from server to client.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BatteryItemStatusComponent : Component
{
    /// <summary>
    /// Whether to show On/Off toggle state for this battery-powered item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowToggleState = true;

    /// <summary>
    /// Current charge percentage (0-100).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ChargePercent = 0;
}
