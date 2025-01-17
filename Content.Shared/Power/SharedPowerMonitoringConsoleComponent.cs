using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
///     Flags an entity as being a power monitoring console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPowerMonitoringConsoleSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class PowerMonitoringConsoleComponent : Component
{
    /// <summary>
    /// The EntityUid of the device that is the console's current focus
    /// </summary>
    /// <remarks>
    /// Not-networked - set by the console UI
    /// </remarks>
    [ViewVariables]
    public EntityUid? Focus;

    /// <summary>
    /// The group that the device that is the console's current focus belongs to
    /// </summary>
    /// /// <remarks>
    /// Not-networked - set by the console UI
    /// </remarks>
    [ViewVariables]
    public PowerMonitoringConsoleGroup FocusGroup = PowerMonitoringConsoleGroup.Generator;

    /// <summary>
    /// A list of flags relating to currently active events of interest to the console.
    /// E.g., power sinks, power net anomalies
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public PowerMonitoringFlags Flags = PowerMonitoringFlags.None;

    /// <summary>
    /// A dictionary containing all the meta data for tracked power monitoring devices
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, PowerMonitoringDeviceMetaData> PowerMonitoringDeviceMetaData = new();
}

[Serializable, NetSerializable]
public struct PowerMonitoringDeviceMetaData
{
    public string EntityName;
    public NetCoordinates Coordinates;
    public PowerMonitoringConsoleGroup Group;
    public string SpritePath;
    public string SpriteState;
    public NetEntity? CollectionMaster;

    public PowerMonitoringDeviceMetaData(string name, NetCoordinates coordinates, PowerMonitoringConsoleGroup group, string spritePath, string spriteState)
    {
        EntityName = name;
        Coordinates = coordinates;
        Group = group;
        SpritePath = spritePath;
        SpriteState = spriteState;
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
///     Contains all the data needed to update a single device on the power monitoring UI
/// </summary>
[Serializable, NetSerializable]
public struct PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public PowerMonitoringConsoleGroup Group;
    public double PowerValue;
    public float? BatteryLevel;

    [NonSerialized] public PowerMonitoringDeviceMetaData? MetaData = null;

    public PowerMonitoringConsoleEntry(NetEntity netEntity, PowerMonitoringConsoleGroup group, double powerValue = 0d, float? batteryLevel = null)
    {
        NetEntity = netEntity;
        Group = group;
        PowerValue = powerValue;
        BatteryLevel = batteryLevel;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;
    public PowerMonitoringConsoleGroup FocusGroup;

    public PowerMonitoringConsoleMessage(NetEntity? focusDevice, PowerMonitoringConsoleGroup focusGroup)
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
