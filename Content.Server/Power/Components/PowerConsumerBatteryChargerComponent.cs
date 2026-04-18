namespace Content.Server.Power.Components;

/// <summary>
/// Used to charge a battery with <see cref="PowerConsumerComponent"/>
/// instead of <see cref="BatteryChargerComponent"/> and <see cref="PowerNetworkBatteryComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class PowerConsumerBatteryChargerComponent : Component
{
    /// <summary>
    /// Out of much power consumed by the <see cref="PowerConsumerComponent"/>
    /// will actually charge the battery
    /// </summary>
    [DataField]
    public float Efficiency = 1f;
}
