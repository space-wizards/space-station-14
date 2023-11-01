using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
///     Flags an entity as being a power monitoring console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerMonitoringConsoleComponent : Component
{
    public EntityUid? Focus;
    public List<EntityUid>? LastReachableNodes = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, PowerCableChunk> AllChunks = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, PowerCableChunk> FocusChunks = new();
}

[Serializable, NetSerializable]
public sealed class PowerCableChunk
{
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask for power cables, 1 for occupied and 0 for empty.
    /// </summary>
    public Dictionary<CableType, int> PowerCableData;

    public PowerCableChunk(Vector2i origin)
    {
        Origin = origin;
        PowerCableData = new Dictionary<CableType, int>();
    }
}

/// <summary>
///     Data from by the server to the client for the power monitoring console UI
/// </summary>
[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public double TotalSources;
    public double TotalBatteryUsage;
    public double TotalLoads;
    public PowerMonitoringConsoleEntry[] AllEntries;
    public PowerMonitoringConsoleEntry[] FocusSources;
    public PowerMonitoringConsoleEntry[] FocusLoads;
    public PowerMonitoringFlags Flags;

    public PowerMonitoringConsoleBoundInterfaceState
        (double totalSources,
        double totalBatteryUsage,
        double totalLoads,
        PowerMonitoringConsoleEntry[] allEntries,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads,
        PowerMonitoringFlags flags)
    {
        TotalSources = totalSources;
        TotalBatteryUsage = totalBatteryUsage;
        TotalLoads = totalLoads;
        AllEntries = allEntries;
        FocusSources = focusSources;
        FocusLoads = focusLoads;
        Flags = flags;
    }
}

/// <summary>
///     Contains all the data needed to represent a single device on the power monitoring UI
/// </summary>
[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public NetCoordinates? Coordinates;
    public PowerMonitoringConsoleGroup Group;
    public string NameLocalized;
    public double PowerValue;

    public PowerMonitoringConsoleEntry
        (NetEntity netEntity,
        NetCoordinates? coordinates,
        PowerMonitoringConsoleGroup group,
        string name,
        double powerValue)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        Group = group;
        NameLocalized = name;
        PowerValue = powerValue;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestPowerMonitoringUpdateMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;

    public RequestPowerMonitoringUpdateMessage(NetEntity? focusDevice)
    {
        FocusDevice = focusDevice;
    }
}

/// <summary>
///     Determines how entities are grouped and color coded on the power monitor
/// </summary>
public enum PowerMonitoringConsoleGroup : byte
{
    Consumer,
    APC,
    Substation,
    SMES,
    Generator
}

[Flags]
public enum PowerMonitoringFlags : byte
{
    None = 0,
    RoguePowerConsumer = 1,
    PowerNetAbnormalities = 2,
}

/// <summary>
///     UI key associated with the power monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum PowerMonitoringConsoleUiKey
{
    Key
}
