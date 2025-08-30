using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly;

/// <summary>
/// Exposes anomaly core-powered item status information via item status control.
/// Synced to clients to display core status and charges.
/// </summary>
/// <seealso cref="AnomalyItemStatusSystem"/>
/// <seealso cref="AnomalyStatusControl"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnomalyItemStatusComponent : Component
{
    /// <summary>
    /// Whether the item has an anomaly core inserted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasCore;

    /// <summary>
    /// Whether the inserted core is decayed (limited charges).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsDecayed;

    /// <summary>
    /// Number of charges remaining (if core is decayed).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges;
}
