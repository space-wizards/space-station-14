namespace Content.Server.Power.Components;

/// <summary>
/// Used to charge a battery with <see cref="PowerConsumerComponent"/>
/// instead of <see cref="BatteryChargerComponent"/> and <see cref="PowerNetworkBatteryComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class PowerConsumerBatteryChargerComponent : Component
{
    /// <summary>
    /// Out of much power consumed can be used to power devices and charge batteries
    /// </summary>
    [DataField]
    public float Efficiency = 1f;
}
