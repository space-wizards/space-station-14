using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class EntityTest : ContentIntegrationTest
    {
        [Test]
        public async Task SpawnTest()
        {
            var options = new ServerContentIntegrationOption()
            {
                CVarOverrides = {{CCVars.NPCMaxUpdates.Name, int.MaxValue.ToString()}}
            };

            var server = StartServer(options);
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            var prototypes = new List<EntityPrototype>();
            IMapGrid grid = default;
            EntityUid testEntity;

            //Build up test environment
            server.Post(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            server.Assert(() =>
            {
                var testLocation = grid.ToCoordinates();

                //Generate list of non-abstract prototypes to test
                foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (prototype.Abstract)
                    {
                        continue;
                    }

                    prototypes.Add(prototype);
                }

                Assert.Multiple(() =>
                {
                    //Iterate list of prototypes to spawn
                    foreach (var prototype in prototypes)
                    {
                        Assert.DoesNotThrow(() =>
                            {
                                Logger.LogS(LogLevel.Debug, "EntityTest", $"Testing: {prototype.ID}");
                                testEntity = entityMan.SpawnEntity(prototype.ID, testLocation);
                                server.RunTicks(2);
                                if(!entityMan.Deleted(testEntity))
                                    entityMan.DeleteEntity(testEntity);
                            }, "Entity '{0}' threw an exception.",
                            prototype.ID);
                    }
                });
            });

            await server.WaitIdleAsync();
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
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Actor", // We aren't testing actor components, those need their player session set.
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneToOneDeleteTestEntity";

            var server = StartServer(new ServerContentIntegrationOption
            {
                ExtraPrototypes = testEntity,
                FailureLogLevel = LogLevel.Error
            });
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IMapGrid grid = default;

            server.Post(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            server.Assert(() =>
            {
                Assert.Multiple(() =>
                {
                    var testLocation = grid.ToCoordinates();

                    foreach (var type in componentFactory.AllRegisteredTypes)
                    {
                        var component = (Component) componentFactory.GetComponent(type);

                        // If this component is ignored
                        if (skipComponents.Contains(component.Name))
                        {
                            continue;
                        }

                        var entity = entityManager.SpawnEntity("AllComponentsOneToOneDeleteTestEntity", testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        // The component may already exist if it is a mandatory component
                        // such as MetaData or Transform
                        if (entityManager.HasComponent(entity, type))
                        {
                            continue;
                        }

                        component.Owner = entity;

                        Logger.LogS(LogLevel.Debug, "EntityTest", $"Adding component: {component.Name}");

                        Assert.DoesNotThrow(() =>
                            {
                                entityManager.AddComponent(entity, component);
                            }, "Component '{0}' threw an exception.",
                            component.Name);

                        server.RunTicks(2);

                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await server.WaitIdleAsync();
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
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Actor", // We aren't testing actor components, those need their player session set.
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneEntityDeleteTestEntity";

            var server = StartServer(new ServerContentIntegrationOption
            {
                ExtraPrototypes = testEntity,
                FailureLogLevel = LogLevel.Error,
                Pool = false
            });
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IMapGrid grid = default;

            server.Post(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            var distinctComponents = new List<(List<Type> components, List<Type> references)>
            {
                (new List<Type>(), new List<Type>())
            };

            // Split components into groups, ensuring that their references don't conflict
            foreach (var type in componentFactory.AllRegisteredTypes)
            {
                var registration = componentFactory.GetRegistration(type);

                for (var i = 0; i < distinctComponents.Count; i++)
                {
                    var distinct = distinctComponents[i];

                    if (distinct.references.Intersect(registration.References).Any())
                    {
                        // Ensure the next list if this one has conflicting references
                        if (i + 1 >= distinctComponents.Count)
                        {
                            distinctComponents.Add((new List<Type>(), new List<Type>()));
                        }

                        continue;
                    }

                    // Add the component and its references if no conflicting references were found
                    distinct.components.Add(type);
                    distinct.references.AddRange(registration.References);
                }
            }

            // Sanity check
            Assert.That(distinctComponents, Is.Not.Empty);

            server.Assert(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var distinct in distinctComponents)
                    {
                        var testLocation = grid.ToCoordinates();
                        var entity = entityManager.SpawnEntity("AllComponentsOneEntityDeleteTestEntity", testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        foreach (var type in distinct.components)
                        {
                            var component = (Component) componentFactory.GetComponent(type);

                            // If the entity already has this component, if it was ensured or added by another
                            if (entityManager.HasComponent(entity, component.GetType()))
                            {
                                continue;
                            }

                            // If this component is ignored
                            if (skipComponents.Contains(component.Name))
                            {
                                continue;
                            }

                            component.Owner = entity;

                            Logger.LogS(LogLevel.Debug, "EntityTest", $"Adding component: {component.Name}");

                            // Note for the future coder: if an exception occurs where a component reference
                            // was already occupied it might be because some component is ensuring another // initialize.
                            // If so, search for cases of EnsureComponent<FailingType>, EnsureComponentWarn<FailingType>
                            // and all others variations (out parameter)
                            Assert.DoesNotThrow(() =>
                                {
                                    entityManager.AddComponent(entity, component);
                                }, "Component '{0}' threw an exception.",
                                component.Name);
                        }

                        server.RunTicks(2);
                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await server.WaitIdleAsync();
        }
    }
}
