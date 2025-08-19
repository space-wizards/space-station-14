using Content.Shared.Atmos.Consoles;
using Content.Shared.Pinpointer;
using Content.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Entities capable of opening the atmos monitoring console UI
/// require this component to function correctly
/// </summary>
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

    /// <summary>
    /// Color of the floor tiles on the nav map screen
    /// </summary>
    [DataField, ViewVariables]
    public Color NavMapTileColor;

    /// <summary>
    /// Color of the wall lines on the nav map screen
    /// </summary>
    [DataField, ViewVariables]
    public Color NavMapWallColor;

    /// <summary>
    /// The next time this component is dirtied, it will force the full state
    /// to be sent to the client, instead of just the delta state
    /// </summary>
    [ViewVariables]
    public bool ForceFullUpdate = false;
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
    /// Indexed by the net ID, layer and color hexcode of the pipe
    /// </summary>
    [ViewVariables]
    public Dictionary<AtmosMonitoringConsoleSubnet, ulong> AtmosPipeData = new();

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
    /// The associated pipe network ID
    /// </summary>
    public int NetId = -1;

    /// <summary>
    /// Prototype ID for the nav map blip
    /// </summary>
    public ProtoId<NavMapBlipPrototype> NavMapBlip;

    /// <summary>
    /// Direction of the entity
    /// </summary>
    public Direction Direction;

    /// <summary>
    /// Color of the attached pipe
    /// </summary>
    public Color PipeColor;

    /// <summary>
    /// The pipe layer the entity is on
    /// </summary>
    public AtmosPipeLayer PipeLayer;

    /// <summary>
    /// Populate the atmos monitoring console nav map with a single entity
    /// </summary>
    public AtmosDeviceNavMapData(NetEntity netEntity,
        NetCoordinates netCoordinates,
        int netId,
        ProtoId<NavMapBlipPrototype> navMapBlip,
        Direction direction,
        Color pipeColor,
        AtmosPipeLayer pipeLayer)
    {
        NetEntity = netEntity;
        NetCoordinates = netCoordinates;
        NetId = netId;
        NavMapBlip = navMapBlip;
        Direction = direction;
        PipeColor = pipeColor;
        PipeLayer = pipeLayer;
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
    /// The color to be associated with the pipe network
    /// </summary>
    public Color Color;

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
        int netId,
        string entityName,
        string address)
    {
        NetEntity = entity;
        Coordinates = coordinates;
        NetId = netId;
        EntityName = entityName;
        Address = address;
    }
}

/// <summary>
/// Used to group atmos pipe chunks into subnets based on their properties and
/// improve the efficiency of rendering these chunks on the atmos monitoring console.
/// </summary>
/// <param name="NetId">The associated network ID.</param>
/// <param name="PipeLayer">The associated pipe layer.</param>
/// <param name="HexCode">The color of the pipe.</param>
[Serializable, NetSerializable]
public record AtmosMonitoringConsoleSubnet(int NetId, AtmosPipeLayer PipeLayer, string HexCode);

public enum AtmosPipeChunkDataFacing : byte
{
    // Values represent bit shift offsets when retrieving data in the tile array.
    North = 0,
    South = SharedNavMapSystem.ArraySize,
    East = SharedNavMapSystem.ArraySize * 2,
    West = SharedNavMapSystem.ArraySize * 3,
}

/// <summary>
/// UI key associated with the atmos monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum AtmosMonitoringConsoleUiKey
{
    Key
}
