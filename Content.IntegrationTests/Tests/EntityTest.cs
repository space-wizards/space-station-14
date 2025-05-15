using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Robust.Shared;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class EntityTest
    {
        private static readonly ProtoId<EntityCategoryPrototype> SpawnerCategory = "Spawner";

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
            var mapSystem = entityMan.System<SharedMapSystem>();

            await server.WaitPost(() =>
            {
                var protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !pair.IsTestPrototype(p))
                    .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                    .Where(p => !p.Components.ContainsKey("RoomFill")) // This comp can delete all entities, and spawn others
                    .Select(p => p.ID)
                    .ToList();

                foreach (var protoId in protoIds)
                {
                    mapSystem.CreateMap(out var mapId);
                    var grid = mapManager.CreateGridEntity(mapId);
                    // TODO: Fix this better in engine.
                    mapSystem.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
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
                    {
                        yield return (uid, meta);
                    }
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
                    .Where(p => !p.Components.ContainsKey("RoomFill")) // This comp can delete all entities, and spawn others
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
                    {
                        yield return (uid, meta);
                    }
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
            var mapSys = server.System<SharedMapSystem>();

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
                    mapSys.CreateMap(out var mapId);
                    var grid = mapManager.CreateGridEntity(mapId);
                    var ent = sEntMan.SpawnEntity(protoId, new EntityCoordinates(grid.Owner, 0.5f, 0.5f));
                    foreach (var (_, component) in sEntMan.GetNetComponents(ent))
                    {
                        sEntMan.Dirty(ent, component);
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
                    {
                        yield return (uid, meta);
                    }
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
        /// bugs, where spawning some entity starts spawning unrelated entities in null space that stick around after
        /// the original entity is gone.
        ///
        /// Note that this isn't really a strict requirement, and there are probably quite a few edge cases. Its a pretty
        /// crude test to try catch issues like this, and possibly should just be disabled.
        /// </remarks>
        [Test]
        public async Task SpawnAndDeleteEntityCountTest()
        {
            var settings = new PoolSettings { Connected = true, Dirty = true };
            await using var pair = await PoolManager.GetServerClient(settings);
            var mapSys = pair.Server.System<SharedMapSystem>();
            var server = pair.Server;
            var client = pair.Client;

            var excluded = new[]
            {
                "MapGrid",
                "StationEvent",
                "TimedDespawn",

                // makes an announcement on mapInit.
                "AnnounceOnSpawn",
            };

            Assert.That(server.CfgMan.GetCVar(CVars.NetPVS), Is.False);

            var protoIds = server.ProtoMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => !excluded.Any(p.Components.ContainsKey))
                .Where(p => p.Categories.All(x => x.ID != SpawnerCategory))
                .Select(p => p.ID)
                .ToList();

            protoIds.Sort();
            var mapId = MapId.Nullspace;

            await server.WaitPost(() =>
            {
                mapSys.CreateMap(out mapId);
            });

            var coords = new MapCoordinates(Vector2.Zero, mapId);

            await pair.RunTicksSync(3);

            // We consider only non-audio entities, as some entities will just play sounds when they spawn.
            int Count(IEntityManager ent) =>  ent.EntityCount - ent.Count<AudioComponent>();
            IEnumerable<EntityUid> Entities(IEntityManager entMan) => entMan.GetEntities().Where(entMan.HasComponent<AudioComponent>);

            await Assert.MultipleAsync(async () =>
            {
                foreach (var protoId in protoIds)
                {
                    var count = Count(server.EntMan);
                    var clientCount = Count(client.EntMan);
                    var serverEntities = new HashSet<EntityUid>(Entities(server.EntMan));
                    var clientEntities = new HashSet<EntityUid>(Entities(client.EntMan));
                    EntityUid uid = default;
                    await server.WaitPost(() => uid = server.EntMan.SpawnEntity(protoId, coords));
                    await pair.RunTicksSync(3);

                    // If the entity deleted itself, check that it didn't spawn other entities
                    if (!server.EntMan.EntityExists(uid))
                    {
                        Assert.That(Count(server.EntMan), Is.EqualTo(count), $"Server prototype {protoId} failed on deleting itself\n" +
                            BuildDiffString(serverEntities, Entities(server.EntMan), server.EntMan));
                        Assert.That(Count(client.EntMan), Is.EqualTo(clientCount), $"Client prototype {protoId} failed on deleting itself\n" +
                            $"Expected {clientCount} and found {client.EntMan.EntityCount}.\n" +
                            $"Server count was {count}.\n" +
                            BuildDiffString(clientEntities, Entities(client.EntMan), client.EntMan));
                        continue;
                    }

                    // Check that the number of entities has increased.
                    Assert.That(Count(server.EntMan), Is.GreaterThan(count), $"Server prototype {protoId} failed on spawning as entity count didn't increase\n" +
                        BuildDiffString(serverEntities, Entities(server.EntMan), server.EntMan));
                    Assert.That(Count(client.EntMan), Is.GreaterThan(clientCount), $"Client prototype {protoId} failed on spawning as entity count didn't increase\n" +
                        $"Expected at least {clientCount} and found {client.EntMan.EntityCount}. " +
                        $"Server count was {count}.\n" +
                        BuildDiffString(clientEntities, Entities(client.EntMan), client.EntMan));

                    await server.WaitPost(() => server.EntMan.DeleteEntity(uid));
                    await pair.RunTicksSync(3);

                    // Check that the number of entities has gone back to the original value.
                    Assert.That(Count(server.EntMan), Is.EqualTo(count), $"Server prototype {protoId} failed on deletion: count didn't reset properly\n" +
                        BuildDiffString(serverEntities, Entities(server.EntMan), server.EntMan));
                    Assert.That(client.EntMan.EntityCount, Is.EqualTo(clientCount), $"Client prototype {protoId} failed on deletion: count didn't reset properly:\n" +
                        $"Expected {clientCount} and found {client.EntMan.EntityCount}.\n" +
                        $"Server count was {count}.\n" +
                        BuildDiffString(clientEntities, Entities(client.EntMan), client.EntMan));
                }
            });

            await pair.CleanReturnAsync();
        }

        private static string BuildDiffString(IEnumerable<EntityUid> oldEnts, IEnumerable<EntityUid> newEnts, IEntityManager entMan)
        {
            var sb = new StringBuilder();
            var addedEnts = newEnts.Except(oldEnts);
            var removedEnts = oldEnts.Except(newEnts);
            if (addedEnts.Any())
                sb.AppendLine("Listing new entities:");
            foreach (var addedEnt in addedEnts)
            {
                sb.AppendLine(entMan.ToPrettyString(addedEnt));
            }
            if (removedEnts.Any())
                sb.AppendLine("Listing removed entities:");
            foreach (var removedEnt in removedEnts)
            {
                sb.AppendLine("\t" + entMan.ToPrettyString(removedEnt));
            }
            return sb.ToString();
        }

        private static bool HasRequiredDataField(Component component)
        {
            foreach (var field in component.GetType().GetFields())
            {
                foreach (var attribute in field.GetCustomAttributes(true))
                {
                    if (attribute is not DataFieldAttribute dataField)
                        continue;

                    if (dataField.Required)
                        return true;
                }
            }
            foreach (var property in component.GetType().GetProperties())
            {
                foreach (var attribute in property.GetCustomAttributes(true))
                {
                    if (attribute is not DataFieldAttribute dataField)
                        continue;

                    if (dataField.Required)
                        return true;
                }
            }
            return false;
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
                "RoomFill",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Broadphase",
                "StationData", // errors when removed mid-round
                "StationJobs",
                "Actor", // We aren't testing actor components, those need their player session set.
                "BlobFloorPlanBuilder", // Implodes if unconfigured.
                "DebrisFeaturePlacerController", // Above.
                "LoadedChunk", // Worldgen chunk loading malding.
                "BiomeSelection", // Whaddya know, requires config.
                "ActivatableUI", // Requires enum key
            };

            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var logmill = server.ResolveDependency<ILogManager>().GetSawmill("EntityTest");

            await pair.CreateTestMap();
            await server.WaitRunTicks(5);
            var testLocation = pair.TestMap.GridCoords;

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {

                    foreach (var type in componentFactory.AllRegisteredTypes)
                    {
                        var component = (Component)componentFactory.GetComponent(type);
                        var name = componentFactory.GetComponentName(type);

                        if (HasRequiredDataField(component))
                            continue;

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
