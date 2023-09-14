#nullable enable
using System.Linq;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    /// <summary>
    /// Creates a map, a grid, and a tile, and gives back references to them.
    /// </summary>
    public async Task<TestMapData> CreateTestMap()
    {
        await Server.WaitIdleAsync();
        var tileDefinitionManager = Server.ResolveDependency<ITileDefinitionManager>();

        var mapData = new TestMapData();
        await Server.WaitPost(() =>
        {
            mapData.MapId = Server.MapMan.CreateMap();
            mapData.MapUid = Server.MapMan.GetMapEntityId(mapData.MapId);
            mapData.MapGrid = Server.MapMan.CreateGrid(mapData.MapId);
            mapData.GridUid = mapData.MapGrid.Owner; // Fixing this requires an engine PR.
            mapData.GridCoords = new EntityCoordinates(mapData.GridUid, 0, 0);
            var plating = tileDefinitionManager["Plating"];
            var platingTile = new Tile(plating.TileId);
            mapData.MapGrid.SetTile(mapData.GridCoords, platingTile);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapData.MapGrid.GetAllTiles().First();
        });
        
        if (Settings.Connected)
            await RunTicksSync(10);

        TestMap = mapData;
        return mapData;
    }
}