using Robust.Shared.Map;
using Robust.Shared.Map.Components;

using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
public sealed class TilesConsumedByEventHorizonEvent : EntityEventArgs
{
    /// <summary>
    /// The tiles that the event horizon is consuming.
    /// Ripped directly from the relevant proc so the second element of each element will be what the tiles are going to be after the grid is updated; usually <see cref="Tile.Empty"/>.
    /// </summary>
    public readonly IReadOnlyList<(Vector2i, Tile)> Tiles;

    /// <summary>
    /// The mapgrid that the event horizon is consuming tiles of.
    /// </summary>
    public readonly MapGridComponent MapGrid;

    /// <summary>
    /// The event horizon consuming the tiles.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    public TilesConsumedByEventHorizonEvent(IReadOnlyList<(Vector2i, Tile)> tiles, MapGridComponent mapGrid, EventHorizonComponent eventHorizon)
    {
        Tiles = tiles;
        MapGrid = mapGrid;
        EventHorizon = eventHorizon;
    }
}
