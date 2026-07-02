using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.SensorMonitoring;

[Serializable, NetSerializable]
public sealed partial class BatterySensorDataPayload : HandledNetworkPayload
{
    [DataField]
    public BatterySensorData Data;
}

/// <summary>
/// A request for <see cref="BatterySensorDataPayload"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BatterySensorSyncPayload : HandledNetworkPayload;

/// <summary>
/// Device network data sent by a <see cref="BatterySensorComponent"/>.
/// </summary>
/// <param name="Charge">The current energy charge of the battery, in joules (J).</param>
/// <param name="MaxCharge">The maximum energy capacity of the battery, in joules (J).</param>
/// <param name="Receiving">The current amount of power being received by the battery, in watts (W).</param>
/// <param name="MaxReceiving">The maximum amount of power that can be received by the battery, in watts (W).</param>
/// <param name="Supplying">The current amount of power being supplied by the battery, in watts (W).</param>
/// <param name="MaxSupplying">The maximum amount of power that can be received by the battery, in watts (W).</param>
[DataRecord]
[Serializable, NetSerializable]
public partial record struct BatterySensorData(
    float Charge,
    float MaxCharge,
    float Receiving,
    float MaxReceiving,
    float Supplying,
    float MaxSupplying
);
