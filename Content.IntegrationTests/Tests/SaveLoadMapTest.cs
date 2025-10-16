using System.Numerics;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class SaveLoadMapTest
    {
        [Test]
        public async Task SaveLoadMultiGridMap()
        {
            var mapPath = new ResPath("/Maps/Test/TestMap.yml");

            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var mapManager = server.ResolveDependency<IMapManager>();
            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapLoader = sEntities.System<MapLoaderSystem>();
            var mapSystem = sEntities.System<SharedMapSystem>();
            var xformSystem = sEntities.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
            var resManager = server.ResolveDependency<IResourceManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitAssertion(() =>
            {
                var dir = mapPath.Directory;
                resManager.UserData.CreateDir(dir);

                mapSystem.CreateMap(out var mapId);

                {
                    var mapGrid = mapManager.CreateGridEntity(mapId);
                    xformSystem.SetWorldPosition(mapGrid, new Vector2(10, 10));
                    mapSystem.SetTile(mapGrid, new Vector2i(0, 0), new Tile(typeId: 1, flags: 1, variant: 255));
                }
                {
                    var mapGrid = mapManager.CreateGridEntity(mapId);
                    xformSystem.SetWorldPosition(mapGrid, new Vector2(-8, -8));
                    mapSystem.SetTile(mapGrid, new Vector2i(0, 0), new Tile(typeId: 2, flags: 1, variant: 254));
                }

                Assert.That(mapLoader.TrySaveMap(mapId, mapPath));
                mapSystem.DeleteMap(mapId);
            });

            await server.WaitIdleAsync();

            MapId newMap = default;
            await server.WaitAssertion(() =>
            {
                Assert.That(mapLoader.TryLoadMap(mapPath, out var map, out _));
                newMap = map!.Value.Comp.MapId;
            });

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                {
                    if (!mapManager.TryFindGridAt(newMap, new Vector2(10, 10), out var gridUid, out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(gridUid, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.Multiple(() =>
                    {
                        Assert.That(xformSystem.GetWorldPosition(gridXform), Is.EqualTo(new Vector2(10, 10)));
                        Assert.That(mapSystem.GetTileRef(gridUid, mapGrid, new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(typeId: 1, flags: 1, variant: 255)));
                    });
                }
                {
                    if (!mapManager.TryFindGridAt(newMap, new Vector2(-8, -8), out var gridUid, out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(gridUid, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.Multiple(() =>
                    {
                        Assert.That(xformSystem.GetWorldPosition(gridXform), Is.EqualTo(new Vector2(-8, -8)));
                        Assert.That(mapSystem.GetTileRef(gridUid, mapGrid, new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(typeId: 2, flags: 1, variant: 254)));
                    });
                }
            });

            await pair.CleanReturnAsync();
        }
    }
}
