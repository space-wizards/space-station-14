using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Logger = Robust.Shared.Log.Logger;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(Entity))]
    public class EntityTest : ContentIntegrationTest
    {
        [Test]
        public async Task SpawnTest()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var mapMan = server.ResolveDependency<IMapManager>();
            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var pauseMan = server.ResolveDependency<IPauseManager>();
            var prototypes = new List<EntityPrototype>();
            IMapGrid grid = default;
            IEntity testEntity;

            //Build up test environment
            server.Post(() =>
            {
                var mapId = mapMan.CreateMap();
                pauseMan.AddUninitializedMap(mapId);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/stationstation.yml");
            });

            server.Assert(() =>
            {
                var testLocation = new GridCoordinates(new Vector2(0, 0), grid);

                //Generate list of non-abstract prototypes to test
                foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (prototype.Abstract)
                    {
                        continue;
                    }
                    prototypes.Add(prototype);
                }

                //Iterate list of prototypes to spawn
                foreach (var prototype in prototypes)
                {
                    Assert.DoesNotThrow(() =>
                        {
                            Logger.LogS(LogLevel.Debug, "EntityTest", $"Testing: {prototype.ID}");
                            testEntity = entityMan.SpawnEntity(prototype.ID, testLocation);
                            server.RunTicks(2);
                            Assert.That(testEntity.Initialized);
                            entityMan.DeleteEntity(testEntity.Uid);
                        }, "Entity '{0}' threw an exception.",
                        prototype.ID);
                }
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task NotAbstractIconTest()
        {
            var client = StartClient();
            await client.WaitIdleAsync();
            var prototypeMan = client.ResolveDependency<IPrototypeManager>();

            client.Assert(() =>
            {
                foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (prototype.Abstract)
                    {
                        continue;
                    }

                    Assert.That(prototype.Components.ContainsKey("Icon"), $"Entity {prototype.ID} does not have an Icon component, but is not abstract");
                }
            });

            await client.WaitIdleAsync();
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
                "MapGrid"
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneToOneDeleteTestEntity";

            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            IMapGrid grid = default;

            server.Post(() =>
            {
                // Load test entity
                using var reader = new StringReader(testEntity);
                prototypeManager.LoadFromStream(reader);

                // Load test map
                var mapId = mapManager.CreateMap();
                pauseManager.AddUninitializedMap(mapId);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/stationstation.yml");
                pauseManager.DoMapInitialize(mapId);
            });

            server.Assert(() =>
            {
                var testLocation = new GridCoordinates(new Vector2(0, 0), grid);

                foreach (var type in componentFactory.AllRegisteredTypes)
                {
                    var component = (Component) componentFactory.GetComponent(type);

                    // If this component is ignored
                    if (skipComponents.Contains(component.Name))
                    {
                        continue;
                    }

                    var entity = entityManager.SpawnEntity("AllComponentsOneToOneDeleteTestEntity", testLocation);

                    Assert.That(entity.Initialized);

                    // The component may already exist if it is a mandatory component
                    // such as MetaData or Transform
                    if (entity.HasComponent(type))
                    {
                        continue;
                    }

                    component.Owner = entity;

                    Logger.LogS(LogLevel.Debug, "EntityTest", $"Adding component: {component.Name}");

                    Assert.DoesNotThrow(() =>
                        {
                            entityManager.ComponentManager.AddComponent(entity, component);
                        }, "Component '{0}' threw an exception.",
                        component.Name);

                    server.RunTicks(10);

                    entityManager.DeleteEntity(entity.Uid);
                }
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
                "MapGrid"
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneEntityDeleteTestEntity";

            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            IMapGrid grid = default;

            server.Post(() =>
            {
                // Load test entity
                using var reader = new StringReader(testEntity);
                prototypeManager.LoadFromStream(reader);

                // Load test map
                var mapId = mapManager.CreateMap();
                pauseManager.AddUninitializedMap(mapId);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/stationstation.yml");
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
                foreach (var distinct in distinctComponents)
                {
                    var testLocation = new GridCoordinates(new Vector2(0, 0), grid);
                    var entity = entityManager.SpawnEntity("AllComponentsOneEntityDeleteTestEntity", testLocation);

                    Assert.That(entity.Initialized);

                    foreach (var type in distinct.components)
                    {
                        var component = (Component) componentFactory.GetComponent(type);

                        // If the entity already has this component, if it was ensured or added by another
                        if (entity.HasComponent(component.GetType()))
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

                        Assert.DoesNotThrow(() =>
                            {
                                entityManager.ComponentManager.AddComponent(entity, component);
                            }, "Component '{0}' threw an exception.",
                            component.Name);
                    }

                    server.RunTicks(48); // Run one full second on the server

                    entityManager.DeleteEntity(entity.Uid);
                }
            });

            await server.WaitIdleAsync();
        }
    }
}
