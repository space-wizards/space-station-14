using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
///     Flags an entity as being a power monitoring console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPowerMonitoringConsoleSystem))]
public sealed partial class PowerMonitoringConsoleComponent : Component
{
    /// <summary>
    /// The EntityUid of the device that is the console's current focus
    /// </summary>
    public EntityUid? Focus;

    /// <summary>
    /// The group that the device that is the console's current focus belongs to
    /// </summary>
    public PowerMonitoringConsoleGroup? FocusGroup;

    /// <summary>
    /// A dictionary of the all the nav map chunks that contain anchored power cables
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, PowerCableChunk> AllChunks = new();

    /// <summary>
    /// A dictionary of the all the nav map chunks that contain anchored power cables
    /// that are directly connected to the console's current focus
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, PowerCableChunk> FocusChunks = new();

    /// <summary>
    /// A list of flags relating to currently active events of interest to the console.
    /// E.g., power sinks, power net anomalies
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public PowerMonitoringFlags Flags = PowerMonitoringFlags.None;
}

[Serializable, NetSerializable]
public struct PowerCableChunk
{
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask dictionary for power cables, 1 for occupied and 0 for empty.
    /// </summary>
    public int[] PowerCableData;

    public PowerCableChunk(Vector2i origin)
    {
        Origin = origin;
        PowerCableData = new int[3];
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

    public PowerMonitoringConsoleBoundInterfaceState
        (double totalSources,
        double totalBatteryUsage,
        double totalLoads,
        PowerMonitoringConsoleEntry[] allEntries,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads)
    {
        TotalSources = totalSources;
        TotalBatteryUsage = totalBatteryUsage;
        TotalLoads = totalLoads;
        AllEntries = allEntries;
        FocusSources = focusSources;
        FocusLoads = focusLoads;
    }
}

/// <summary>
///     Contains all the data needed to represent a single device on the power monitoring UI
/// </summary>
[Serializable, NetSerializable]
public struct PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public PowerMonitoringConsoleGroup Group;
    public double PowerValue;

    public PowerMonitoringConsoleEntry(NetEntity netEntity, PowerMonitoringConsoleGroup group, double powerValue = 0d)
    {
        NetEntity = netEntity;
        Group = group;
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
    public PowerMonitoringConsoleGroup? FocusGroup;

    public RequestPowerMonitoringUpdateMessage(NetEntity? focusDevice, PowerMonitoringConsoleGroup? focusGroup)
    {
        FocusDevice = focusDevice;
        FocusGroup = focusGroup;
    }
}

/// <summary>
///     Determines how entities are grouped and color coded on the power monitor
/// </summary>
public enum PowerMonitoringConsoleGroup : byte
{
    Generator,
    SMES,
    Substation,
    APC,
    Consumer,
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
