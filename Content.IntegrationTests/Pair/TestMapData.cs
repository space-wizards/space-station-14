using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Pair;

/// <summary>
/// Simple data class that stored information about a map being used by a test.
/// </summary>
public sealed class TestMapData
{
    public EntityUid MapUid { get; set; }
    public EntityUid GridUid { get; set; }
    public MapId MapId { get; set; }
    public MapGridComponent MapGrid { get; set; } = default!;
    public EntityCoordinates GridCoords { get; set; }
    public MapCoordinates MapCoords { get; set; }
    public TileRef Tile { get; set; }

    // Client-side uids
    public EntityUid CMapUid { get; set; }
    public EntityUid CGridUid { get; set; }
    public EntityCoordinates CGridCoords { get; set; }
}