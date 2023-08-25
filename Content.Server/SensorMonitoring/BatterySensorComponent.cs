using Content.Server.Power.Components;

namespace Content.Server.SensorMonitoring;

/// <summary>
/// Enables a battery entity (such as an SMES) to be monitored via the sensor monitoring console.
/// </summary>
/// <remarks>
/// The entity should also have a <see cref="BatteryComponent"/> and <see cref="PowerNetworkBatteryComponent"/>.
/// </remarks>
[RegisterComponent]
public sealed partial class BatterySensorComponent : Component
{
}

/// <summary>
/// Device network data sent by a <see cref="BatterySensorComponent"/>.
/// </summary>
/// <param name="Charge">The current energy charge of the battery, in joules (J).</param>
/// <param name="MaxCharge">The maximum energy capacity of the battery, in joules (J).</param>
/// <param name="Receiving">The current amount of power being received by the battery, in watts (W).</param>
/// <param name="MaxReceiving">The maximum amount of power that can be received by the battery, in watts (W).</param>
/// <param name="Supplying">The current amount of power being supplied by the battery, in watts (W).</param>
/// <param name="MaxSupplying">The maximum amount of power that can be received by the battery, in watts (W).</param>
public sealed record BatterySensorData(
    float Charge,
    float MaxCharge,
    float Receiving,
    float MaxReceiving,
    float Supplying,
    float MaxSupplying
);
