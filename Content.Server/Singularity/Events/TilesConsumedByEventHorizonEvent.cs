using Robust.Shared.Map;
using Robust.Shared.Map.Components;

using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
[ByRefEvent]
public readonly record struct TilesConsumedByEventHorizonEvent
(IReadOnlyList<(Vector2i, Tile)> tiles, EntityUid mapGridUid, MapGridComponent mapGrid, EntityUid eventHorizonUid, EventHorizonComponent eventHorizon)
{
    /// <summary>
    /// The tiles that the event horizon is consuming.
    /// Ripped directly from the relevant proc so the second element of each element will be what the tiles are going to be after the grid is updated; usually <see cref="Tile.Empty"/>.
    /// </summary>
    public readonly IReadOnlyList<(Vector2i, Tile)> Tiles = tiles;

    /// <summary>
    /// The uid of the map grid the event horizon is consuming part of.
    /// </summary>
    public readonly EntityUid MapGridUid = mapGridUid;

    /// <summary>
    /// The mapgrid that the event horizon is consuming tiles of.
    /// </summary>
    public readonly MapGridComponent MapGrid = mapGrid;

    /// <summary>
    /// The uid of the event horizon consuming the entity.
    /// </summary>
    public readonly EntityUid EventHorizonUid = eventHorizonUid;

    /// <summary>
    /// The event horizon consuming the tiles.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon = eventHorizon;
}
