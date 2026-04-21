using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.Components;

/// <summary>
/// Changes the efficiency of <see cref="PowerConsumerComponent"/>
/// when the voltage is changed via the <see cref="VoltageTogglerComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class PowerConsumerEfficiencyVoltageTogglerComponent : Component
{
    /// <summary>
    /// Different efficiencies per voltage
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Voltage, float> EfficiencyPerVoltage;
}
