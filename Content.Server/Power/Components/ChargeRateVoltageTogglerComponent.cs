using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.Components;

/// <summary>
/// Changes the max charge rate of a entity with <see cref="PowerNetworkBatteryComponent"/>
/// when the voltage is changed via the <see cref="VoltageTogglerComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class ChargeRateVoltageTogglerComponent : Component
{
    /// <summary>
    /// Different max charge rates per voltage
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Voltage, float> ChargeRatePerVoltage;
}
