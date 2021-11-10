using System.Threading.Tasks;
using NUnit.Framework;
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
    class SaveLoadMapTest : ContentIntegrationTest
    {
        [Test]
        public async Task SaveLoadMultiGridMap()
        {
            const string mapPath = @"/Maps/Test/TestMap.yml";

            var server = StartServer(new ServerContentIntegrationOption
            {
                FailureLogLevel = LogLevel.Error
            });
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var resManager = server.ResolveDependency<IResourceManager>();

            server.Post(() =>
            {
                var dir = new ResourcePath(mapPath).Directory;
                resManager.UserData.CreateDir(dir);

                var nextMapId = mapManager.NextMapId();
                var mapId = mapManager.CreateMap(nextMapId);

                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = entityManager.GetEntity(mapGrid.GridEntityId);
                    mapGridEnt.Transform.WorldPosition = new Vector2(10, 10);
                    mapGrid.SetTile(new Vector2i(0,0), new Tile(1, 512));
                }
                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = entityManager.GetEntity(mapGrid.GridEntityId);
                    mapGridEnt.Transform.WorldPosition = new Vector2(-8, -8);
                    mapGrid.SetTile(new Vector2i(0, 0), new Tile(2, 511));
                }

                mapLoader.SaveMap(mapId, mapPath);

                mapManager.DeleteMap(nextMapId);
            });
            await server.WaitIdleAsync();

            server.Post(() =>
            {
                mapLoader.LoadMap(new MapId(10), mapPath);
            });
            await server.WaitIdleAsync();

            {
                if(!mapManager.TryFindGridAt(new MapId(10), new Vector2(10,10), out var mapGrid))
                    Assert.Fail();

                Assert.That(mapGrid.WorldPosition, Is.EqualTo(new Vector2(10, 10)));
                Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(1, 512)));
            }
            {
                if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(-8, -8), out var mapGrid))
                    Assert.Fail();

                Assert.That(mapGrid.WorldPosition, Is.EqualTo(new Vector2(-8, -8)));
                Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(2, 511)));
            }

        }
    }
}
