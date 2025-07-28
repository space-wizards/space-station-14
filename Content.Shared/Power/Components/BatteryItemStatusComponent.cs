using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Exposes a battery's charge information via item status control.
/// Synced to clients to display charge percent and optional On/Off state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BatteryItemStatusComponent : Component
{
    /// <summary>
    /// Whether to show On/Off toggle state for this battery-powered item.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowToggleState = false;

    /// <summary>
    /// Current charge percentage (0-100).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ChargePercent;
}
