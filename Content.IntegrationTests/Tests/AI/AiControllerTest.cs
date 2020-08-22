using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.VendingMachines;
using Robust.Server.AI;
using Robust.Shared.Log;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(AiControllerTest))]
    public class AiControllerTest : ContentIntegrationTest
    {
        [Test]
        public async Task AiProcessorNamesValidTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                var reflectionManager = IoCManager.Resolve<IReflectionManager>();
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateMap(new MapId(1));
                var grid = mapManager.CreateGrid(new MapId(1));

                var processorNames = new List<string>();

                // Verify they all have the required attribute
                foreach (var processor in reflectionManager.GetAllChildren(typeof(AiLogicProcessor)))
                {
                    var attrib = (AiLogicProcessorAttribute) Attribute.GetCustomAttribute(processor, typeof(AiLogicProcessorAttribute));
                    Assert.That(attrib != null, $"No AiLogicProcessorAttribute found on {processor.Name}");
                    processorNames.Add(attrib.SerializeName);
                }
                
                foreach (var entity in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    var comps = entity.Components;

                    if (!comps.ContainsKey("AiController")) continue;

                    var aiEntity = entityManager.SpawnEntity(entity.ID, new GridCoordinates(new Vector2(0, 0), grid.Index));
                    var aiController = aiEntity.GetComponent<AiControllerComponent>();
                    Assert.That(processorNames.Contains(aiController.LogicName), $"Could not find valid processor named {aiController.LogicName} on entity {entity.ID}");
                }
            });

            await server.WaitIdleAsync();
        }

    }
}
