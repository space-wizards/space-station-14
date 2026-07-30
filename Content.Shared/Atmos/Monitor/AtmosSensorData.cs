using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

/// <summary>
/// Contains <see cref="AtmosMonitorData"/>.
/// </summary>
public partial record struct AtmosMonitorDataPayload : INetworkPayload
{
    [DataField]
    public AtmosMonitorData Data;
}

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorData : BaseAtmosDeviceData
{
    public override void RaisePayload(EntityUid uid, DeviceAddress address, DeviceNetworkSystem deviceNetSys)
    {
        var payload = new AtmosMonitorDataPayload { Data = this };
        deviceNetSys.QueuePacket(uid, address, ref payload);
    }

    public AtmosMonitorData(float pressure, float temperature, float totalMoles, AtmosAlarmType alarmState, Dictionary<Gas, float> gases, AtmosAlarmThreshold pressureThreshold, AtmosAlarmThreshold temperatureThreshold, Dictionary<Gas, AtmosAlarmThreshold> gasThresholds)
    {
        Pressure = pressure;
        Temperature = temperature;
        TotalMoles = totalMoles;
        AlarmState = alarmState;
        Gases = gases;
        PressureThreshold = pressureThreshold;
        TemperatureThreshold = temperatureThreshold;
        GasThresholds = gasThresholds;
    }

    /// Most fields are readonly, because it's data that's meant to be transmitted.

    /// <summary>
    ///     Current pressure detected by this sensor.
    /// </summary>
    public float Pressure { get; }
    /// <summary>
    ///     Current temperature detected by this sensor.
    /// </summary>
    public float Temperature { get; }
    /// <summary>
    ///     Current amount of moles detected by this sensor.
    /// </summary>
    public float TotalMoles { get; }
    /// <summary>
    ///     Current alarm state of this sensor. Does not reflect the highest alarm state on the network.
    /// </summary>
    public AtmosAlarmType AlarmState { get; }
    /// <summary>
    ///     Current number of gases on this sensor.
    /// </summary>
    public Dictionary<Gas, float> Gases { get; }

    public AtmosAlarmThreshold PressureThreshold { get; }
    public AtmosAlarmThreshold TemperatureThreshold { get; }
    public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; }
}
