using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    sealed class SaveLoadMapTest
    {
        [Test]
        public async Task SaveLoadMultiGridMap()
        {
            const string mapPath = @"/Maps/Test/TestMap.yml";

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            var mapManager = server.ResolveDependency<IMapManager>();
            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapLoader = sEntities.System<MapLoaderSystem>();
            var resManager = server.ResolveDependency<IResourceManager>();

            await server.WaitPost(() =>
            {
                var dir = new ResourcePath(mapPath).Directory;
                resManager.UserData.CreateDir(dir);

                var mapId = mapManager.CreateMap();

                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = mapGrid.GridEntityId;
                    sEntities.GetComponent<TransformComponent>(mapGridEnt).WorldPosition = new Vector2(10, 10);
                    mapGrid.SetTile(new Vector2i(0,0), new Tile(1, (TileRenderFlag)1, 255));
                }
                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = mapGrid.GridEntityId;
                    sEntities.GetComponent<TransformComponent>(mapGridEnt).WorldPosition = new Vector2(-8, -8);
                    mapGrid.SetTile(new Vector2i(0, 0), new Tile(2, (TileRenderFlag)1, 254));
                }

                Assert.Multiple(() => mapLoader.SaveMap(mapId, mapPath));
                Assert.Multiple(() => mapManager.DeleteMap(mapId));
            });
            await server.WaitIdleAsync();

            await server.WaitPost(() =>
            {
                Assert.Multiple(() => mapLoader.LoadMap(new MapId(10), mapPath));

            });
            await server.WaitIdleAsync();
            await server.WaitAssertion(() =>
            {
                {
                    if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(10, 10), out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(mapGrid.GridEntityId, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.That(gridXform.WorldPosition, Is.EqualTo(new Vector2(10, 10)));

                    Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(1, (TileRenderFlag)1, 255)));
                }
                {
                    if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(-8, -8), out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(mapGrid.GridEntityId, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.That(gridXform.WorldPosition, Is.EqualTo(new Vector2(-8, -8)));
                    Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(2, (TileRenderFlag)1, 254)));
                }
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
