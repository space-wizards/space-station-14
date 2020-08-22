using System;
using System.Collections.Generic;
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
            IEntity testEntity = null;

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
                    try
                    {
                        Logger.LogS(LogLevel.Debug, "EntityTest", "Testing: " + prototype.ID);
                        testEntity = entityMan.SpawnEntity(prototype.ID, testLocation);
                        server.RunTicks(2);
                        Assert.That(testEntity.Initialized);
                        entityMan.DeleteEntity(testEntity.Uid);
                    }

                    //Fail any exceptions thrown on spawn
                    catch (Exception e)
                    {
                        Logger.LogS(LogLevel.Error, "EntityTest", "Entity '" + prototype.ID + "' threw: " + e.Message);
                        //Assert.Fail();
                        throw;
                    }
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
    }
}
