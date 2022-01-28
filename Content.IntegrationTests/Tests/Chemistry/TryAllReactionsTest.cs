using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry
{
    [TestFixture]
    [TestOf(typeof(ReactionPrototype))]
    public class TryAllReactionsTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  id: TestSolutionContainer
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50";

        [Test]
        public async Task TryAllTest()
        {
            var options = new ServerContentIntegrationOption{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var coordinates = GetMainEntityCoordinates(mapManager);

            foreach (var reactionPrototype in prototypeManager.EnumeratePrototypes<ReactionPrototype>())
            {
                //since i have no clue how to isolate each loop assert-wise im just gonna throw this one in for good measure
                Console.WriteLine($"Testing {reactionPrototype.ID}");

                EntityUid beaker;
                Solution component = null;

                server.Assert(() =>
                {
                    beaker = entityManager.SpawnEntity("TestSolutionContainer", coordinates);
                    Assert.That(EntitySystem.Get<SolutionContainerSystem>()
                        .TryGetSolution(beaker, "beaker", out component));
                    foreach (var (id, reactant) in reactionPrototype.Reactants)
                    {
                        Assert.That(EntitySystem.Get<SolutionContainerSystem>()
                            .TryAddReagent(beaker, component, id, reactant.Amount, out var quantity));
                        Assert.That(reactant.Amount, Is.EqualTo(quantity));
                    }
                });

                await server.WaitIdleAsync();

                server.Assert(() =>
                {
                    //you just got linq'd fool
                    //(i'm sorry)
                    var foundProductsMap = reactionPrototype.Products
                        .Concat(reactionPrototype.Reactants.Where(x => x.Value.Catalyst).ToDictionary(x => x.Key, x => x.Value.Amount))
                        .ToDictionary(x => x, _ => false);
                    foreach (var reagent in component.Contents)
                    {
                        Assert.That(foundProductsMap.TryFirstOrNull(x => x.Key.Key == reagent.ReagentId && x.Key.Value == reagent.Quantity, out var foundProduct));
                        foundProductsMap[foundProduct.Value.Key] = true;
                    }

                    Assert.That(foundProductsMap.All(x => x.Value));
                });
            }

        }
    }

}
