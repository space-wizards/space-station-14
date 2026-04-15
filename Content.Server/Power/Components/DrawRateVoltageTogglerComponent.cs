using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.Components;

/// <summary>
/// Changes the power consumption / charge rate of entities
/// when the voltage is changed via the <see cref="VoltageTogglerComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class DrawRateVoltageTogglerComponent : Component
{
    /// <summary>
    /// Different max charge rates per voltage
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Voltage, float> DrawRatePerVoltage;
}

[ByRefEvent]
public record struct DrawRateChangedEvent(float NewDrawRate);
