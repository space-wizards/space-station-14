using Robust.Shared.Map;
using Robust.Shared.Map.Components;

using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Events;

/// <summary>
/// A by-ref event raised on an <paramref name="EventHorizon"/> whenever it consumes a set of <paramref name="Tiles"/> from a <paramref name="Grid"/>.
/// </summary>
/// <param name="EventHorizon">The event horizon that is consuming part of the <paramref name="Grid"/>.</param>
/// <param name="Grid">The grid that owns the <paramref name="Tiles"/> that the <paramref name="EventHorizon"/> is consuming.</param>
/// <param name="Tiles">The tiles of the <paramref name="Grid"/> that the <paramref name="EventHorizon"/> is consuming.</param>
[ByRefEvent]
public readonly record struct TilesConsumedByEventHorizonEvent(Entity<EventHorizonComponent> EventHorizon, Entity<MapGridComponent> Grid, IReadOnlyList<(Vector2i, Tile)> Tiles);
