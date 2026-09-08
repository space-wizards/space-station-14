using Content.Server.Power.Components;
using Content.Shared.Power.Components;

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

