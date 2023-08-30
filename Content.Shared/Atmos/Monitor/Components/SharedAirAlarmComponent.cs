using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor.Components;

[Serializable, NetSerializable]
public enum SharedAirAlarmInterfaceKey
{
    Key
}

[Serializable, NetSerializable]
public enum AirAlarmMode
{
    None,
    Filtering,
    WideFiltering,
    Fill,
    Panic,
}

[Serializable, NetSerializable]
public enum AirAlarmWireStatus
{
    Power,
    Access,
    Panic,
    DeviceSync
}

public interface IAtmosDeviceData
{
    public bool Enabled { get; set; }
    public bool Dirty { get; set; }
    public bool IgnoreAlarms { get; set; }
}

[Serializable, NetSerializable]
public sealed class AirAlarmUIState : BoundUserInterfaceState
{
    public AirAlarmUIState(string address, int deviceCount, float pressureAverage, float temperatureAverage, Dictionary<string, IAtmosDeviceData> deviceData, AirAlarmMode mode, AirAlarmTab tab, AtmosAlarmType alarmType, bool autoMode)
    {
        Address = address;
        DeviceCount = deviceCount;
        PressureAverage = pressureAverage;
        TemperatureAverage = temperatureAverage;
        DeviceData = deviceData;
        Mode = mode;
        Tab = tab;
        AlarmType = alarmType;
        AutoMode = autoMode;
    }

    public string Address { get; }
    public int DeviceCount { get; }
    public float PressureAverage { get; }
    public float TemperatureAverage { get; }
    /// <summary>
    ///     Every single device data that can be seen from this
    ///     air alarm. This includes vents, scrubbers, and sensors.
    ///     The device data you get, however, depends on the current
    ///     selected tab.
    /// </summary>
    public Dictionary<string, IAtmosDeviceData> DeviceData { get; }
    public AirAlarmMode Mode { get; }
    public AirAlarmTab Tab { get; }
    public AtmosAlarmType AlarmType { get; }
    public bool AutoMode { get; }
}

[Serializable, NetSerializable]
public sealed class AirAlarmTabSetMessage : BoundUserInterfaceMessage
{
    public AirAlarmTabSetMessage(AirAlarmTab tab)
    {
        Tab = tab;
    }

    public AirAlarmTab Tab { get; }
}

[Serializable, NetSerializable]
public sealed class AirAlarmResyncAllDevicesMessage : BoundUserInterfaceMessage
{}

[Serializable, NetSerializable]
public sealed class AirAlarmUpdateAlarmModeMessage : BoundUserInterfaceMessage
{
    public AirAlarmMode Mode { get; }

    public AirAlarmUpdateAlarmModeMessage(AirAlarmMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class AirAlarmUpdateAutoModeMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public AirAlarmUpdateAutoModeMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class AirAlarmUpdateDeviceDataMessage : BoundUserInterfaceMessage
{
    public string Address { get; }
    public IAtmosDeviceData Data { get; }

    public AirAlarmUpdateDeviceDataMessage(string addr, IAtmosDeviceData data)
    {
        Address = addr;
        Data = data;
    }
}

[Serializable, NetSerializable]
public sealed class AirAlarmCopyDeviceDataMessage : BoundUserInterfaceMessage
{
    public IAtmosDeviceData Data { get; }

    public AirAlarmCopyDeviceDataMessage(IAtmosDeviceData data)
    {
        Data = data;
    }
}

[Serializable, NetSerializable]
public sealed class AirAlarmUpdateAlarmThresholdMessage : BoundUserInterfaceMessage
{
    public string Address { get; }
    public AtmosAlarmThreshold Threshold { get; }
    public AtmosMonitorThresholdType Type { get; }
    public Gas? Gas { get; }

    public AirAlarmUpdateAlarmThresholdMessage(string address, AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
    {
        Address = address;
        Threshold = threshold;
        Type = type;
        Gas = gas;
    }
}

public enum AirAlarmTab
{
    Vent,
    Scrubber,
    Sensors,
    Settings
}
