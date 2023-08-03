using System.Collections.Generic;
using System.Linq;
using Content.Shared.Coordinates;
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
            var settings = new PoolSettings {NoClient = true, Dirty = true};
            await using var pairTracker = await PoolManager.GetServerClient(settings);
            var server = pairTracker.Pair.Server;

            var entityMan = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitPost(() =>
            {
                var protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                    .Select(p => p.ID)
                    .ToList();
                foreach (var protoId in protoIds)
                {
                    var mapId = mapManager.CreateMap();
                    var grid = mapManager.CreateGrid(mapId);
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
                };

                var entityMetas = Query<MetaDataComponent>(entityMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        entityMan.DeleteEntity(uid);
                }

                Assert.That(entityMan.EntityCount, Is.Zero);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task SpawnAndDeleteAllEntitiesInTheSameSpot()
        {
            // This test dirties the pair as it simply deletes ALL entities when done. Overhead of restarting the round
            // is minimal relative to the rest of the test.
            var settings = new PoolSettings {NoClient = true, Dirty = true};
            await using var pairTracker = await PoolManager.GetServerClient(settings);
            var server = pairTracker.Pair.Server;
            var map = await PoolManager.CreateTestMap(pairTracker);

            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitPost(() =>
            {

                var protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
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

            await pairTracker.CleanReturnAsync();
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
            var settings = new PoolSettings {NoClient = false, Dirty = true};
            await using var pairTracker = await PoolManager.GetServerClient(settings);
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            var cfg = server.ResolveDependency<IConfigurationManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sEntMan = server.ResolveDependency<IEntityManager>();

            Assert.That(cfg.GetCVar(CVars.NetPVS), Is.False);

            var protoIds = prototypeMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                .Select(p => p.ID)
                .ToList();

            // for whatever reason, stealth boxes are breaking this test. Surplus crates have a chance of spawning them.
            // TODO fix whatever is going wrong here.
            HashSet<string> ignored = new() { "GhostBox", "StealthBox", "CrateSyndicateSurplusBundle", "CrateSyndicateSuperSurplusBundle" };

            await server.WaitPost(() =>
            {
                foreach (var protoId in protoIds)
                {
                    if (ignored.Contains(protoId))
                        continue;

                    var mapId = mapManager.CreateMap();
                    var grid = mapManager.CreateGrid(mapId);
                    var ent = sEntMan.SpawnEntity(protoId, new EntityCoordinates(grid.Owner, 0.5f, 0.5f));
                    foreach (var (_, component) in sEntMan.GetNetComponents(ent))
                    {
                        sEntMan.Dirty(component);
                    }
                }
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 15);

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
                };

                var entityMetas = Query<MetaDataComponent>(sEntMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        sEntMan.DeleteEntity(uid);
                }

                Assert.That(sEntMan.EntityCount, Is.Zero);
            });

            await pairTracker.CleanReturnAsync();
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
                "GridFillComponent",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "StationData", // errors when removed mid-round
                "Actor", // We aren't testing actor components, those need their player session set.
                "BlobFloorPlanBuilder", // Implodes if unconfigured.
                "DebrisFeaturePlacerController", // Above.
                "LoadedChunk", // Worldgen chunk loading malding.
                "BiomeSelection", // Whaddya know, requires config.
            };

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pairTracker.Pair.Server;

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

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task AllComponentsOneEntityDeleteTest()
        {
            var skipComponents = new[]
            {
                "DebugExceptionOnAdd", // Debug components that explicitly throw exceptions
                "DebugExceptionExposeData",
                "DebugExceptionInitialize",
                "DebugExceptionStartup",
                "GridFillComponent",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "StationData", // errors when deleted mid-round
                "Actor", // We aren't testing actor components, those need their player session set.
                "BlobFloorPlanBuilder", // Implodes if unconfigured.
                "DebrisFeaturePlacerController", // Above.
                "LoadedChunk", // Worldgen chunk loading malding.
                "BiomeSelection", // Whaddya know, requires config.
            };

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pairTracker.Pair.Server;

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

                grid.SetTile(Vector2i.Zero, tile);
                mapManager.DoMapInitialize(mapId);
            });
            await server.WaitRunTicks(5);

            var distinctComponents = new List<(List<CompIdx> components, List<CompIdx> references)>
            {
                (new List<CompIdx>(), new List<CompIdx>())
            };

            // Split components into groups, ensuring that their references don't conflict
            foreach (var type in componentFactory.AllRegisteredTypes)
            {
                var registration = componentFactory.GetRegistration(type);

                for (var i = 0; i < distinctComponents.Count; i++)
                {
                    var (components, references) = distinctComponents[i];

                    if (references.Intersect(registration.References).Any())
                    {
                        // Ensure the next list if this one has conflicting references
                        if (i + 1 >= distinctComponents.Count)
                        {
                            distinctComponents.Add((new List<CompIdx>(), new List<CompIdx>()));
                        }

                        continue;
                    }

                    // Add the component and its references if no conflicting references were found
                    components.Add(registration.Idx);
                    references.AddRange(registration.References);
                }
            }

            // Sanity check
            Assert.That(distinctComponents, Is.Not.Empty);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var (components, _) in distinctComponents)
                    {
                        var testLocation = grid.ToCoordinates();
                        var entity = entityManager.SpawnEntity(null, testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        foreach (var type in components)
                        {
                            var component = (Component) componentFactory.GetComponent(type);

                            // If the entity already has this component, if it was ensured or added by another
                            if (entityManager.HasComponent(entity, component.GetType()))
                            {
                                continue;
                            }

                            var name = componentFactory.GetComponentName(component.GetType());

                            // If this component is ignored
                            if (skipComponents.Contains(name))
                                continue;

                            component.Owner = entity;
                            logmill.Debug($"Adding component: {name}");

                            // Note for the future coder: if an exception occurs where a component reference
                            // was already occupied it might be because some component is ensuring another // initialize.
                            // If so, search for cases of EnsureComponent<FailingType>, EnsureComponentWarn<FailingType>
                            // and all others variations (out parameter)
                            Assert.DoesNotThrow(() =>
                                {
                                    entityManager.AddComponent(entity, component);
                                }, "Component '{0}' threw an exception.",
                                name);
                        }
                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
