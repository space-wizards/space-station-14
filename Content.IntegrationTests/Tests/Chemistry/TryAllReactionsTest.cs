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
        [Test]
        public async Task TryAllTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            foreach (var reactionPrototype in prototypeManager.EnumeratePrototypes<ReactionPrototype>())
            {
                //since i have no clue how to isolate each loop assert-wise im just gonna throw this one in for good measure
                Console.WriteLine($"Testing {reactionPrototype.ID}");

                IEntity beaker;
                Solution component = null;

                server.Assert(() =>
                {
                    mapManager.CreateNewMapEntity(MapId.Nullspace);

                    beaker = entityManager.SpawnEntity("BluespaceBeaker", MapCoordinates.Nullspace);
                    Assert.That(EntitySystem.Get<SolutionContainerSystem>()
                        .TryGetSolution(beaker.Uid, "beaker", out component));
                    foreach (var (id, reactant) in reactionPrototype.Reactants)
                    {
                        Assert.That(EntitySystem.Get<SolutionContainerSystem>()
                            .TryAddReagent(beaker.Uid, component, id, reactant.Amount, out var quantity));
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
                        .ToDictionary(x => x, x => false);
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
