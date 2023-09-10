#nullable enable
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting;

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
        TestMap = mapData;
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

        if (!Settings.Connected)
            return mapData;

        await RunTicksSync(10);
        mapData.CMapUid = ToClientUid(mapData.MapUid);
        mapData.CGridUid = ToClientUid(mapData.GridUid);
        mapData.CGridCoords = new EntityCoordinates(mapData.CGridUid, 0, 0);

        return mapData;
    }

    /// <summary>
    /// Convert a client-side uid into a server-side uid
    /// </summary>
    public EntityUid ToServerUid(EntityUid uid) => ConvertUid(uid, Client, Server);

    /// <summary>
    /// Convert a server-side uid into a client-side uid
    /// </summary>
    public EntityUid ToClientUid(EntityUid uid) => ConvertUid(uid, Server, Client);

    private static EntityUid ConvertUid(
        EntityUid uid,
        RobustIntegrationTest.IntegrationInstance source,
        RobustIntegrationTest.IntegrationInstance destination)
    {
        if (!uid.IsValid())
            return EntityUid.Invalid;

        if (!source.EntMan.TryGetComponent<MetaDataComponent>(uid, out var meta))
        {
            Assert.Fail($"Failed to resolve MetaData while converting the EntityUid for entity {uid}");
            return EntityUid.Invalid;
        }

        if (!destination.EntMan.TryGetEntity(meta.NetEntity, out var otherUid))
        {
            Assert.Fail($"Failed to resolve net ID while converting the EntityUid entity {source.EntMan.ToPrettyString(uid)}");
            return EntityUid.Invalid;
        }

        return otherUid.Value;
    }
}
