using Robust.Shared.Collections;
using Robust.Shared.Map.Components;

namespace Content.Server.Spreader;

/// <summary>
/// Raised when trying to spread to neighboring tiles.
/// If the spread is no longer able to happen you MUST cancel this event!
/// </summary>
[ByRefEvent]
public record struct SpreadNeighborsEvent
{
    public ValueList<(MapGridComponent Grid, Vector2i Tile)> NeighborFreeTiles;
    public ValueList<EntityUid> Neighbors;

    /// <summary>
    /// How many updates allowed are remaining.
    /// Subscribers can handle as they wish.
    /// </summary>
    public int Updates;
}
