using Robust.Shared.Map;

using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Events;

/// <summary>
///     Event raised on the event horizon entity whenever an event horizon consumes an entity.
/// </summary>
public sealed class TilesConsumedByEventHorizonEvent : EntityEventArgs
{
    /// <summary>
    ///     The tiles that the event horizon is consuming.
    ///     Ripped directly from the relevant proc so the second element of each element will be Tile.Empty.
    /// </summary>
    public readonly IReadOnlyList<(Vector2i, Tile)> Tiles;

    /// <summary>
    ///     The mapgrid that the event horizon is consuming tiles of.
    /// </summary>
    public readonly IMapGrid MapGrid;

    /// <summary>
    ///     The event horizon consuming the tiles.
    /// </summary>
    public readonly EventHorizonComponent EventHorizon;

    /// <summary>
    ///     The local event horizon system.
    /// </summary>
    public readonly EventHorizonSystem EventHorizonSystem;

    public TilesConsumedByEventHorizonEvent(IReadOnlyList<(Vector2i, Tile)> tiles, IMapGrid mapGrid, EventHorizonComponent eventHorizon, EventHorizonSystem eventHorizonSystem)
    {
        Tiles = tiles;
        MapGrid = mapGrid;
        EventHorizon = eventHorizon;
        EventHorizonSystem = eventHorizonSystem;
    }
}
