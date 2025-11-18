using Robust.Shared.Serialization;
using ConsoleUIState = Content.Shared.SensorMonitoring.SensorMonitoringConsoleBoundInterfaceState;

namespace Content.Shared.SensorMonitoring;

[Serializable, NetSerializable]
public sealed class SensorMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public TimeSpan RetentionTime;
    public SensorData[] Sensors = Array.Empty<SensorData>();

    [Serializable, NetSerializable]
    public sealed class SensorData
    {
        public int NetId;
        public string Name = "";
        public string Address = "";
        public SensorDeviceType DeviceType;

        public SensorStream[] Streams = Array.Empty<SensorStream>();
    }

    [Serializable, NetSerializable]
    public sealed class SensorStream
    {
        public int NetId;
        public string Name = "";
        public SensorUnit Unit;
        public SensorSample[] Samples = Array.Empty<SensorSample>();
    }
}

[Serializable, NetSerializable]
public sealed class SensorMonitoringIncrementalUpdate : BoundUserInterfaceMessage
{
    public TimeSpan RelTime;
    public SensorData[] Sensors = Array.Empty<SensorData>();
    public int[] RemovedSensors = Array.Empty<int>();

    [Serializable, NetSerializable]
    public sealed class SensorData
    {
        public int NetId;
        public SensorStream[] Streams = Array.Empty<SensorStream>();
    }

    [Serializable, NetSerializable]
    public sealed class SensorStream
    {
        public int NetId;
        public SensorUnit Unit;
        // Note: these samples have their time values relative to RelTime.
        // This improves effectiveness of integer compression in NetSerializer.
        public SensorSample[] Samples = Array.Empty<SensorSample>();
    }
}

[Serializable, NetSerializable]
public enum SensorMonitoringConsoleUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum SensorUnit : byte
{
    Undetermined = 0,

    /// <summary>
    /// A pressure value in kilopascals (kPa).
    /// </summary>
    PressureKpa,

    /// <summary>
    /// A temperature value in Kelvin (K).
    /// </summary>
    TemperatureK,

    /// <summary>
    /// An amount of matter in moles.
    /// </summary>
    Moles,

    /// <summary>
    /// A value in the range 0-1.
    /// </summary>
    /* L + */ Ratio,

    /// <summary>
    /// Power in Watts (W).
    /// </summary>
    PowerW,

    /// <summary>
    /// Energy in Joules (J).
    /// </summary>
    EnergyJ
}

[Serializable, NetSerializable]
public enum SensorDeviceType
{
    Unknown = 0,
    Teg,
    AtmosSensor,
    ThermoMachine,
    VolumePump,
    Battery,
}

[Serializable, NetSerializable]
public record struct SensorSample(TimeSpan Time, float Value);
