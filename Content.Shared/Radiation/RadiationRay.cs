using System.Numerics;
using Content.Shared.Radiation.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Systems;

/// <summary>
///     Ray emitted by radiation source towards radiation receiver.
///     Contains all information about encountered radiation blockers.
/// </summary>
public struct RadiationRay(
    MapId mapId,
    EntityUid sourceUid,
    Vector2 source,
    EntityUid destinationUid,
    Vector2 destination,
    float rads)
{
    /// <summary>
    ///     Map on which source and receiver are placed.
    /// </summary>
    public MapId MapId = mapId;
    /// <summary>
    ///     Uid of entity with <see cref="RadiationSourceComponent"/>.
    /// </summary>
    public EntityUid SourceUid = sourceUid;
    /// <summary>
    ///     World coordinates of radiation source.
    /// </summary>
    public Vector2 Source = source;
    /// <summary>
    ///     Uid of entity with radiation receiver component.
    /// </summary>
    public EntityUid DestinationUid = destinationUid;
    /// <summary>
    ///     World coordinates of radiation receiver.
    /// </summary>
    public Vector2 Destination = destination;
    /// <summary>
    ///     How many rads intensity reached radiation receiver.
    /// </summary>
    public float Rads = rads;

    /// <summary>
    ///     Has rad ray reached destination or lost all intensity after blockers?
    /// </summary>
    public bool ReachedDestination => Rads > 0;

    /// <summary>
    ///     All blockers visited by gridcast, used for debug overlays. Key is uid of grid. Values are pairs
    ///     of tile indices and floats with updated radiation value.
    /// </summary>
    /// <remarks>
    ///     Last tile may have negative value if ray has lost all intensity.
    ///     Grid traversal order isn't guaranteed.
    /// </remarks>
    public Dictionary<NetEntity, List<(Vector2i, float)>>? Blockers;

}

// Variant of RadiationRay that uses NetEntities.
[Serializable, NetSerializable]
public readonly record struct DebugRadiationRay(
    MapId MapId,
    NetEntity SourceUid,
    Vector2 Source,
    NetEntity DestinationUid,
    Vector2 Destination,
    float Rads,
    Dictionary<NetEntity, List<(Vector2i, float)>> Blockers)
{
    public bool ReachedDestination => Rads > 0;
}
