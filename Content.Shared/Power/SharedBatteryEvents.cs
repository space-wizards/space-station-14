namespace Content.Shared.Power;

/// <summary>
/// Raised to get battery charge information for UI purposes.
/// </summary>
[ByRefEvent]
public sealed class GetBatteryInfoEvent : EntityEventArgs
{
    /// <summary>
    /// Current charge of the battery (0-1 as percentage).
    /// </summary>
    public float ChargePercent;

    /// <summary>
    /// Whether the battery exists and has valid data.
    /// </summary>
    public bool HasBattery;
}
