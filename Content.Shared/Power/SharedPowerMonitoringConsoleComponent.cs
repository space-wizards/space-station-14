using Content.Shared.Pinpointer;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float TotalSources;
    public float TotalLoads;
    public PowerMonitoringConsoleEntry[] AllSources;
    public PowerMonitoringConsoleEntry[] AllLoads;
    public PowerMonitoringConsoleEntry[] SubSources;
    public PowerMonitoringConsoleEntry[] SubLoads;
    public Dictionary<Vector2i, NavMapChunkPowerCables> PowerCableChunks;
    public Dictionary<Vector2i, NavMapChunkPowerCables>? FocusChunks;

    public PowerMonitoringConsoleBoundInterfaceState
        (float totalSources,
        float totalLoads,
        PowerMonitoringConsoleEntry[] allSources,
        PowerMonitoringConsoleEntry[] allLoads,
        PowerMonitoringConsoleEntry[] subSources,
        PowerMonitoringConsoleEntry[] subLoads,
        Dictionary<Vector2i, NavMapChunkPowerCables> powerCableChunks,
        Dictionary<Vector2i, NavMapChunkPowerCables>? focusChunks)
    {
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        AllSources = allSources;
        AllLoads = allLoads;
        SubSources = subSources;
        SubLoads = subLoads;
        PowerCableChunks = powerCableChunks;
        FocusChunks = focusChunks;
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
    public NetEntity? NetEntity;
    public RequestPowerMonitoringDataMessage(NetEntity? netEntity)
    {
        NetEntity = netEntity;
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
