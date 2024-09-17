using Content.Shared.Atmos.Consoles;
using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAtmosMonitoringConsoleSystem))]
public sealed partial class AtmosMonitoringConsoleComponent : Component
{
    /*
     * Don't need DataFields as this can be reconstructed
     */

    /// <summary>
    /// A dictionary of the all the nav map chunks that contain anchored atmos pipes
    /// </summary>
    [ViewVariables]
    public Dictionary<Vector2i, AtmosPipeChunk> AtmosPipeChunks = new();

    /// <summary>
    /// A list of all the atmos devices that will be used to populate the nav map
    /// </summary>
    [ViewVariables]
    public Dictionary<NetEntity, AtmosDeviceNavMapData> AtmosDevices = new();
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
    /// The associated pipe network ID 
    /// </summary>
    public int NetId = -1;

    /// <summary>
    /// Pipe color (if applicable)
    /// </summary>
    public Color? Color = null;

    /// <summary>
    /// Direction of the entity (if applicable)
    /// </summary>
    public Direction? Direction = null;

    /// <summary>
    /// Populate the atmos monitoring console nav map with a single entity
    /// </summary>
    public AtmosDeviceNavMapData(NetEntity netEntity, NetCoordinates netCoordinates, AtmosMonitoringConsoleGroup group, int netId, Color? color = null, Direction? direction = null)
    {
        NetEntity = netEntity;
        NetCoordinates = netCoordinates;
        Group = group;
        NetId = netId;
        Color = color;
        Direction = direction;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// A list of all entries to populate the UI with
    /// </summary>
    public AtmosMonitoringConsoleEntry[] AtmosNetworks;

    /// <summary>
    /// Sends data from the server to the client to populate the atmos monitoring console UI
    /// </summary>
    public AtmosMonitoringConsoleBoundInterfaceState(AtmosMonitoringConsoleEntry[] atmosNetworks)
    {
        AtmosNetworks = atmosNetworks;
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
    /// The associated pipe network ID 
    /// </summary>
    public int NetId = -1;

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
    /// Mol and percentage for all detected gases 
    /// </summary>
    public Dictionary<Gas, float> GasData = new();

    /// <summary>
    /// Indicates whether the entity is powered
    /// </summary>
    public bool IsPowered = true;

    /// <summary>
    /// Used to populate the atmos monitoring console UI with data from a single air alarm
    /// </summary>
    public AtmosMonitoringConsoleEntry
        (NetEntity entity,
        NetCoordinates coordinates,
        AtmosMonitoringConsoleGroup group,
        int netId,
        string entityName,
        string address)
    {
        NetEntity = entity;
        Coordinates = coordinates;
        Group = group;
        NetId = netId;
        EntityName = entityName;
        Address = address;
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
