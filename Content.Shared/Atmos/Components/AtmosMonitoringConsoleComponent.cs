using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
//[Access(typeof(AtmosMonitoringConsoleSystem))] - need to make shared
public sealed partial class AtmosMonitoringConsoleComponent : Component
{
    /// <summary>
    /// A dictionary of the all the nav map chunks that contain anchored atmos pipes
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, AtmosPipeChunk> AtmosPipeChunks = new();

    /// <summary>
    /// A list of all the atmos devices that will be used to populate the nav map
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<AtmosDeviceNavMapData> AtmosDevices = new();

    /// <summary>
    /// The current entity of interest (selected on the console UI)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? FocusDevice;

    /// <summary>
    /// The network associated with current entity of interest (selected on the console UI)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int? FocusNetId;
}

[Serializable, NetSerializable]
public struct AtmosPipeChunk(Vector2i origin)
{
    /// <summary>
    /// Chunk position
    /// </summary>
    [ViewVariables]
    public readonly Vector2i Origin = origin;

    /// <summary>
    /// Bitmask look up for atmos pipes, 1 for occupied and 0 for empty.
    /// Indexed by the color hexcode of the pipe
    /// </summary>
    [ViewVariables]
    public Dictionary<(int, string), ulong> AtmosPipeData = new();

    /// <summary>
    /// The last game tick that the chunk was updated
    /// </summary>
    [NonSerialized]
    public GameTick LastUpdate;
}

[Serializable, NetSerializable]
public struct AtmosDeviceNavMapData
{
    /// <summary>
    /// The entity in question
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Location of the entity
    /// </summary>
    public NetCoordinates NetCoordinates;

    /// <summary>
    /// Used to determine what map icons to use
    /// </summary>
    public AtmosMonitoringConsoleGroup Group;

    /// <summary>
    /// Pipe color (if applicable)
    /// </summary>
    public Color? Color = null;

    /// <summary>
    /// Direction of the entity (if applicable)
    /// </summary>
    public Direction? Direction = null;

    /// <summary>
    /// The network ID of the entity
    /// </summary>
    public int NetId = -1;

    /// <summary>
    /// Populate the atmos monitoring console nav map with a single entity
    /// </summary>
    public AtmosDeviceNavMapData(NetEntity netEntity, NetCoordinates netCoordinates, AtmosMonitoringConsoleGroup group, int netId, Color? color = null, Direction? direction = null)
    {
        NetEntity = netEntity;
        NetCoordinates = netCoordinates;
        Group = group;
        Color = color;
        Direction = direction;
        NetId = netId;
    }
}

[Serializable, NetSerializable]
public struct AtmosFocusDeviceData
{
    /// <summary>
    /// Focus entity
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Mol and percentage for all detected gases 
    /// </summary>
    public Dictionary<Gas, float> GasData;

    /// <summary>
    /// Network ID 
    /// </summary>
    public int NetId = -1;

    /// <summary>
    /// Populates the atmos monitoring console focus entry with atmospheric data
    /// </summary>
    public AtmosFocusDeviceData
        (NetEntity netEntity,
        Dictionary<Gas, float> gasData,
        int netId)
    {
        NetEntity = netEntity;
        GasData = gasData;
        NetId = netId;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// A list of all gas pumps
    /// </summary>
    public AtmosMonitoringConsoleEntry[] AtmosNetworks;

    /// <summary>
    /// Data for the UI focus (if applicable)
    /// </summary>
    public AtmosFocusDeviceData? FocusData;

    /// <summary>
    /// Sends data from the server to the client to populate the atmos monitoring console UI
    /// </summary>
    public AtmosMonitoringConsoleBoundInterfaceState
        (AtmosMonitoringConsoleEntry[] atmosNetworks,
        AtmosFocusDeviceData? focusData)
    {
        AtmosNetworks = atmosNetworks;
        FocusData = focusData;
    }
}

[Serializable, NetSerializable]
public struct AtmosMonitoringConsoleEntry
{
    /// <summary>
    /// The entity in question
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Location of the entity
    /// </summary>
    public NetCoordinates Coordinates;

    /// <summary>
    /// The type of entity
    /// </summary>
    public AtmosMonitoringConsoleGroup Group;

    /// <summary>
    /// Localised device name
    /// </summary>
    public string EntityName;

    /// <summary>
    /// Device network address
    /// </summary>
    public string Address;

    /// <summary>
    /// Temperature (K)
    /// </summary>
    public float TemperatureData;

    /// <summary>
    /// Pressure (kPA)
    /// </summary>
    public float PressureData;

    /// <summary>
    /// Total number of mols of gas
    /// </summary>
    public float TotalMolData;

    /// <summary>
    /// Indicates whether the entity is active
    /// </summary>
    public bool IsActive = true;

    /// <summary>
    /// Used to populate the atmos monitoring console UI with data from a single air alarm
    /// </summary>
    public AtmosMonitoringConsoleEntry
        (NetEntity entity,
        NetCoordinates coordinates,
        AtmosMonitoringConsoleGroup group,
        string entityName,
        string address)
    {
        NetEntity = entity;
        Coordinates = coordinates;
        Group = group;
        EntityName = entityName;
        Address = address;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosMonitoringConsoleFocusChangeMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;

    /// <summary>
    /// Used to inform the server that the specified focus for the atmos monitoring console has been changed by the client
    /// </summary>
    public AtmosMonitoringConsoleFocusChangeMessage(NetEntity? focusDevice)
    {
        FocusDevice = focusDevice;
    }
}

public enum AtmosPipeChunkDataFacing : byte
{
    // Values represent bit shift offsets when retrieving data in the tile array.
    North = 0,
    South = SharedNavMapSystem.ArraySize,
    East = SharedNavMapSystem.ArraySize * 2,
    West = SharedNavMapSystem.ArraySize * 3,
}

/// <summary>
/// List of all the different atmos device groups
/// </summary>
public enum AtmosMonitoringConsoleGroup
{
    GasPipeSensor,
    GasInlet,
    GasOutlet,
    GasOpening,
    GasPump,
    GasMixer,
    GasFilter,
    GasValve,
    Thermoregulator,
}

/// <summary>
/// UI key associated with the atmos monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum AtmosMonitoringConsoleUiKey
{
    Key
}
