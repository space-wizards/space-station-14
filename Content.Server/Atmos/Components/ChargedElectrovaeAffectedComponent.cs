namespace Content.Server.Atmos.Components;

/// <summary>
/// Marks an entity as currently affected by charged electrovae gas.
/// Used to track and restore battery capacity and power states when the entity
/// leaves the gas or the gas dissipates.
/// </summary>
[RegisterComponent]
public sealed partial class ChargedElectrovaeAffectedComponent : Component
{
    /// <summary>
    /// Original battery max charge before capacity expansion.
    /// Null if this entity doesn't have a battery or hasn't had its capacity expanded yet.
    /// </summary>
    [DataField]
    public float? OriginalBatteryMaxCharge;

    /// <summary>
    /// Original NeedsPower value before charged electrovae bypassed power requirements.
    /// Null if this entity doesn't have a power receiver or hasn't had its power bypassed yet.
    /// </summary>
    [DataField]
    public bool? OriginalNeedsPower;
}
