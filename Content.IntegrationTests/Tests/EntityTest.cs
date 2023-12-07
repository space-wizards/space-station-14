using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Humanoid.Components;
using Content.Shared.Coordinates;
using Content.Shared.Prototypes;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class EntityTest
    {
        [Test]
        public async Task SpawnAndDeleteAllEntitiesOnDifferentMaps()
        {
            // This test dirties the pair as it simply deletes ALL entities when done. Overhead of restarting the round
            // is minimal relative to the rest of the test.
            var settings = new PoolSettings { Dirty = true };
            await using var pair = await PoolManager.GetServerClient(settings);
            var server = pair.Server;

            var entityMan = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitPost(() =>
            {
                var protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !pair.IsTestPrototype(p))
                    .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                    .Select(p => p.ID)
                    .ToList();
                foreach (var protoId in protoIds)
                {
                    var mapId = mapManager.CreateMap();
                    var grid = mapManager.CreateGrid(mapId);
                    // TODO: Fix this better in engine.
                    grid.SetTile(Vector2i.Zero, new Tile(1));
                    var coord = new EntityCoordinates(grid.Owner, 0, 0);
                    entityMan.SpawnEntity(protoId, coord);
                }
            });

            await server.WaitRunTicks(15);

            await server.WaitPost(() =>
            {
                static IEnumerable<(EntityUid, TComp)> Query<TComp>(IEntityManager entityMan)
                    where TComp : Component
                {
                    var query = entityMan.AllEntityQueryEnumerator<TComp>();
                    while (query.MoveNext(out var uid, out var meta))
                        yield return (uid, meta);
                }

                var entityMetas = Query<MetaDataComponent>(entityMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        entityMan.DeleteEntity(uid);
                }

                Assert.That(entityMan.EntityCount, Is.Zero);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task SpawnAndDeleteAllEntitiesInTheSameSpot()
        {
            // This test dirties the pair as it simply deletes ALL entities when done. Overhead of restarting the round
            // is minimal relative to the rest of the test.
            var settings = new PoolSettings { Dirty = true };
            await using var pair = await PoolManager.GetServerClient(settings);
            var server = pair.Server;
            var map = await pair.CreateTestMap();

            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitPost(() =>
            {

                var protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !pair.IsTestPrototype(p))
                    .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                    .Select(p => p.ID)
                    .ToList();
                foreach (var protoId in protoIds)
                {
                    entityMan.SpawnEntity(protoId, map.GridCoords);
                }
            });
            await server.WaitRunTicks(15);
            await server.WaitPost(() =>
            {
                static IEnumerable<(EntityUid, TComp)> Query<TComp>(IEntityManager entityMan)
                    where TComp : Component
                {
                    var query = entityMan.AllEntityQueryEnumerator<TComp>();
                    while (query.MoveNext(out var uid, out var meta))
                        yield return (uid, meta);
                };

                var entityMetas = Query<MetaDataComponent>(entityMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        entityMan.DeleteEntity(uid);
                }

                Assert.That(entityMan.EntityCount, Is.Zero);
            });

            await pair.CleanReturnAsync();
        }

        /// <summary>
        ///     Variant of <see cref="SpawnAndDeleteAllEntitiesOnDifferentMaps"/> that also launches a client and dirties
        ///     all components on every entity.
        /// </summary>
        [Test]
        public async Task SpawnAndDirtyAllEntities()
        {
            // This test dirties the pair as it simply deletes ALL entities when done. Overhead of restarting the round
            // is minimal relative to the rest of the test.
            var settings = new PoolSettings { Connected = true, Dirty = true };
            await using var pair = await PoolManager.GetServerClient(settings);
            var server = pair.Server;
            var client = pair.Client;

            var cfg = server.ResolveDependency<IConfigurationManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sEntMan = server.ResolveDependency<IEntityManager>();

            Assert.That(cfg.GetCVar(CVars.NetPVS), Is.False);

            var protoIds = prototypeMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                .Select(p => p.ID)
                .ToList();

            await server.WaitPost(() =>
            {
                foreach (var protoId in protoIds)
                {
                    var mapId = mapManager.CreateMap();
                    var grid = mapManager.CreateGrid(mapId);
                    var ent = sEntMan.SpawnEntity(protoId, new EntityCoordinates(grid.Owner, 0.5f, 0.5f));
                    foreach (var (_, component) in sEntMan.GetNetComponents(ent))
                    {
                        sEntMan.Dirty(component);
                    }
                }
            });

            await pair.RunTicksSync(15);

            // Make sure the client actually received the entities
            // 500 is completely arbitrary. Note that the client & sever entity counts aren't expected to match.
            Assert.That(client.ResolveDependency<IEntityManager>().EntityCount, Is.GreaterThan(500));

            await server.WaitPost(() =>
            {
                static IEnumerable<(EntityUid, TComp)> Query<TComp>(IEntityManager entityMan)
                    where TComp : Component
                {
                    var query = entityMan.AllEntityQueryEnumerator<TComp>();
                    while (query.MoveNext(out var uid, out var meta))
                        yield return (uid, meta);
                }

                var entityMetas = Query<MetaDataComponent>(sEntMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        sEntMan.DeleteEntity(uid);
                }

                Assert.That(sEntMan.EntityCount, Is.Zero);
            });

            await pair.CleanReturnAsync();
        }

        /// <summary>
        /// This test checks that spawning and deleting an entity doesn't somehow create other unrelated entities.
        /// </summary>
        /// <remarks>
        /// Unless an entity is intentionally designed to spawn other entities (e.g., mob spawners), they should
        /// generally not spawn unrelated / detached entities. Any entities that do get spawned should be parented to
        /// the spawned entity (e.g., in a container). If an entity needs to spawn an entity somewhere in null-space,
        /// it should delete that entity when it is no longer required. This test mainly exists to prevent "entity leak"
        /// bugs, where spawning some entity starts spawning unrelated entities in null space.
        /// </remarks>
        [Test]
        public async Task SpawnAndDeleteEntityCountTest()
        {
            var settings = new PoolSettings { Connected = true, Dirty = true };
            await using var pair = await PoolManager.GetServerClient(settings);
            var mapManager = pair.Server.ResolveDependency<IMapManager>();
            var server = pair.Server;
            var client = pair.Client;

            var excluded = new[]
            {
                "MapGrid",
                "StationEvent",
                "TimedDespawn",

                // Spawner entities
                "DragonRift",
                "RandomHumanoidSpawner",
                "RandomSpawner",
                "ConditionalSpawner",
                "GhostRoleMobSpawner",
                "NukeOperativeSpawner",
                "TimedSpawner",
            };

            Assert.That(server.CfgMan.GetCVar(CVars.NetPVS), Is.False);

            var protoIds = server.ProtoMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => !excluded.Any(p.Components.ContainsKey))
                .Select(p => p.ID)
                .ToList();

            protoIds.Sort();
            var mapId = MapId.Nullspace;

            await server.WaitPost(() =>
            {
               mapId = mapManager.CreateMap();
            });

            var coords = new MapCoordinates(Vector2.Zero, mapId);

            await pair.RunTicksSync(3);

            foreach (var protoId in protoIds)
            {
                // TODO fix ninja
                // Currently ninja fails to equip their own loadout.
                if (protoId == "MobHumanSpaceNinja")
                    continue;

                var count = server.EntMan.EntityCount;
                var clientCount = client.EntMan.EntityCount;
                EntityUid uid = default;
                await server.WaitPost(() => uid = server.EntMan.SpawnEntity(protoId, coords));
                await pair.RunTicksSync(3);

                // If the entity deleted itself, check that it didn't spawn other entities
                if (!server.EntMan.EntityExists(uid))
                {
                    if (server.EntMan.EntityCount != count)
                    {
                        Assert.Fail($"Server prototype {protoId} failed on deleting itself");
                    }

                    if (client.EntMan.EntityCount != clientCount)
                    {
                        Assert.Fail($"Client prototype {protoId} failed on deleting itself\n" +
                                    $"Expected {clientCount} and found {client.EntMan.EntityCount}.\n" +
                                    $"Server was {count}.");
                    }
                    continue;
                }

                // Check that the number of entities has increased.
                if (server.EntMan.EntityCount <= count)
                {
                    Assert.Fail($"Server prototype {protoId} failed on spawning as entity count didn't increase");
                }

                if (client.EntMan.EntityCount <= clientCount)
                {
                    Assert.Fail($"Client prototype {protoId} failed on spawning as entity count didn't increase" +
                                $"Expected at least {clientCount} and found {client.EntMan.EntityCount}. " +
                                $"Server was {count}");
                }

                await server.WaitPost(() => server.EntMan.DeleteEntity(uid));
                await pair.RunTicksSync(3);

                // Check that the number of entities has gone back to the original value.
                if (server.EntMan.EntityCount != count)
                {
                    Assert.Fail($"Server prototype {protoId} failed on deletion count didn't reset properly");
                }

                if (client.EntMan.EntityCount != clientCount)
                {
                    Assert.Fail($"Client prototype {protoId} failed on deletion count didn't reset properly:\n" +
                                $"Expected {clientCount} and found {client.EntMan.EntityCount}.\n" +
                                $"Server was {count}.");
                }
            }

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task AllComponentsOneToOneDeleteTest()
        {
            var skipComponents = new[]
            {
                "DebugExceptionOnAdd", // Debug components that explicitly throw exceptions
                "DebugExceptionExposeData",
                "DebugExceptionInitialize",
                "DebugExceptionStartup",
                "GridFill",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Broadphase",
                "StationData", // errors when removed mid-round
                "Actor", // We aren't testing actor components, those need their player session set.
                "BlobFloorPlanBuilder", // Implodes if unconfigured.
                "DebrisFeaturePlacerController", // Above.
                "LoadedChunk", // Worldgen chunk loading malding.
                "BiomeSelection", // Whaddya know, requires config.
            };

            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
            var logmill = server.ResolveDependency<ILogManager>().GetSawmill("EntityTest");

            MapGridComponent grid = default;

            await server.WaitPost(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["Plating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                mapManager.DoMapInitialize(mapId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    var testLocation = grid.ToCoordinates();

                    foreach (var type in componentFactory.AllRegisteredTypes)
                    {
                        var component = (Component) componentFactory.GetComponent(type);
                        var name = componentFactory.GetComponentName(type);

                        // If this component is ignored
                        if (skipComponents.Contains(name))
                        {
                            continue;
                        }

                        var entity = entityManager.SpawnEntity(null, testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        // The component may already exist if it is a mandatory component
                        // such as MetaData or Transform
                        if (entityManager.HasComponent(entity, type))
                        {
                            entityManager.DeleteEntity(entity);
                            continue;
                        }

                        component.Owner = entity;
                        logmill.Debug($"Adding component: {name}");

                        Assert.DoesNotThrow(() =>
                            {
                                entityManager.AddComponent(entity, component);
                            }, "Component '{0}' threw an exception.",
                            name);

                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
