using Content.Shared.Pinpointer;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float TotalSources;
    public float TotalLoads;
    public PowerMonitoringConsoleEntry[] Sources;
    public PowerMonitoringConsoleEntry[] Loads;
    public Dictionary<Vector2i, NavMapChunkPowerCables> PowerCableChunks;
    public bool Snap;
    public float Precision;

    public PowerMonitoringConsoleBoundInterfaceState
        (float totalSources,
        float totalLoads,
        PowerMonitoringConsoleEntry[] sources,
        PowerMonitoringConsoleEntry[] loads,
        Dictionary<Vector2i, NavMapChunkPowerCables> powerCableChunks,
        bool snap,
        float precision)
    {
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        Sources = sources;
        Loads = loads;
        PowerCableChunks = powerCableChunks;
        Snap = snap;
        Precision = precision;
    }
}

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public NetCoordinates Coordinates;
    public string NameLocalized;
    public string IconEntityPrototypeId;
    public double Size;
    public bool IsBattery;

    public PowerMonitoringConsoleEntry(NetEntity netEntity, NetCoordinates coordinates, string nl, string ipi, double size, bool isBattery)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        NameLocalized = nl;
        IconEntityPrototypeId = ipi;
        Size = size;
        IsBattery = isBattery;
    }
}

[Serializable, NetSerializable]
public sealed class RequestPowerMonitoringDataMessage : BoundUserInterfaceMessage
{
    public RequestPowerMonitoringDataMessage()
    {

    }
}

[Serializable, NetSerializable]
public sealed class RequestPowerCableDataEvent : BoundUserInterfaceMessage
{
    public RequestPowerCableDataEvent()
    {

    }
}

[Serializable, NetSerializable]
public enum PowerMonitoringConsoleUiKey
{
    Key
}
