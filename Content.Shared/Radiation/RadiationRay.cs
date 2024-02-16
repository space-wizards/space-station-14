using System.Numerics;
using Content.Shared.Radiation.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Systems;

/// <summary>
///     Ray emitted by radiation source towards radiation receiver.
///     Contains all information about encountered radiation blockers.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadiationRay
{
    /// <summary>
    ///     Map on which source and receiver are placed.
    /// </summary>
    public MapId MapId;
    /// <summary>
    ///     Uid of entity with <see cref="RadiationSourceComponent"/>.
    /// </summary>
    public NetEntity SourceUid;
    /// <summary>
    ///     World coordinates of radiation source.
    /// </summary>
    public Vector2 Source;
    /// <summary>
    ///     Uid of entity with radiation receiver component.
    /// </summary>
    public NetEntity DestinationUid;
    /// <summary>
    ///     World coordinates of radiation receiver.
    /// </summary>
    public Vector2 Destination;
    /// <summary>
    ///     How many rads intensity reached radiation receiver.
    /// </summary>
    public float Rads;

    /// <summary>
    ///     Has rad ray reached destination or lost all intensity after blockers?
    /// </summary>
    public bool ReachedDestination => Rads > 0;

    /// <summary>
    ///     All blockers visited by gridcast. Key is uid of grid. Values are pairs
    ///     of tile indices and floats with updated radiation value.
    /// </summary>
    /// <remarks>
    ///     Last tile may have negative value if ray has lost all intensity.
    ///     Grid traversal order isn't guaranteed.
    /// </remarks>
    public Dictionary<NetEntity, List<(Vector2i, float)>> Blockers = new();

    public RadiationRay(MapId mapId, NetEntity sourceUid, Vector2 source,
        NetEntity destinationUid, Vector2 destination, float rads)
    {
        MapId = mapId;
        SourceUid = sourceUid;
        Source = source;
        DestinationUid = destinationUid;
        Destination = destination;
        Rads = rads;
    }
}
