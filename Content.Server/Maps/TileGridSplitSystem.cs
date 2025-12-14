using System.Numerics;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

public sealed class TileGridSplitSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent ev)
    {
        if (!TryComp<TileHistoryComponent>(ev.Grid, out var oldHistory))
            return;

        var oldGrid = Comp<MapGridComponent>(ev.Grid);

        foreach (var gridUid in ev.NewGrids)
        {
            var newHistory = EnsureComp<TileHistoryComponent>(gridUid);
            var newGrid = Comp<MapGridComponent>(gridUid);
            var newXform = Transform(gridUid);

            foreach (var tile in _maps.GetAllTiles(gridUid, newGrid))
            {
                var localPos = newXform.LocalPosition + new Vector2((tile.GridIndices.X  + newGrid.TileSize)/2f, (tile.GridIndices.Y + newGrid.TileSize)/2f);
                var oldIndices = _maps.LocalToTile(ev.Grid, oldGrid, new EntityCoordinates(ev.Grid, localPos));

                if (oldHistory.TileHistory.TryGetValue(oldIndices, out var history))
                {
                    newHistory.TileHistory[tile.GridIndices] = new List<ProtoId<ContentTileDefinition>>(history);
                }
            }
        }
    }
}
