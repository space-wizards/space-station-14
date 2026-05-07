using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.IntegrationTests.Tests.Chemistry
{
    [TestFixture]
    [TestOf(typeof(ReactionPrototype))]
    public sealed class TryAllReactionsTest : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  id: TestSolutionContainer
  components:
  - type: Solution
    id: beaker
    solution:
      maxVol: 120";

        private static string[] _reactions = GameDataScrounger.PrototypesOfKind<ReactionPrototype>();

        [Test]
        [TestCaseSource(nameof(_reactions))]
        [TestOf(typeof(ReactionPrototype))]
        [Description("Tries an individual reaction to see if it succeeds.")]
        public async Task TryReaction(string reaction)
        {
            var pair = Pair;
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var solutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();

            var reactionPrototype = prototypeManager.Index<ReactionPrototype>(reaction);

            EntityUid beaker = default;
            Solution solution = null;
            Entity<SolutionComponent>? solutionEnt = default!;

                await server.WaitAssertion(() =>
                {
                    beaker = entityManager.SpawnEntity("TestSolutionContainer", coordinates);
                    Assert.That(solutionContainerSystem
                        .TryGetSolution(beaker, "beaker", out solutionEnt, out solution));
                    solutionContainerSystem.SetCanReact(solutionEnt!.Value, false);
                    foreach (var (id, reactant) in reactionPrototype.Reactants)
                    {
#pragma warning disable NUnit2045
                    Assert.That(solutionContainerSystem
                        .TryAddReagent(solutionEnt.Value,
                            id,
                            reactant.Amount,
                            out var quantity,
                            reactionPrototype.MinimumTemperature));
                    Assert.That(reactant.Amount, Is.EqualTo(quantity));
#pragma warning restore NUnit2045
                }

                //Get all possible reactions with the current reagents
                var possibleReactions = prototypeManager.EnumeratePrototypes<ReactionPrototype>()
                    .Where(x => x.Reactants.All(id => solution.Contents.Any(s => s.Reagent.Prototype == id.Key)))
                    .ToList();

                //Check if the reaction is the first to occur when heated
                foreach (var possibleReaction in possibleReactions.OrderBy(r => r.MinimumTemperature))
                {
                    if (possibleReaction.Priority >= reactionPrototype.Priority &&
                        possibleReaction.MinimumTemperature < reactionPrototype.MinimumTemperature &&
                        possibleReaction.MixingCategories == reactionPrototype.MixingCategories)
                    {
                        Assert.Fail(
                            $"The {possibleReaction.ID} reaction may occur before {reactionPrototype.ID} when heated.");
                    }
                }

                //Check if the reaction is the first to occur when freezing
                foreach (var possibleReaction in possibleReactions.OrderBy(r => r.MaximumTemperature))
                {
                    if (possibleReaction.Priority >= reactionPrototype.Priority &&
                        possibleReaction.MaximumTemperature > reactionPrototype.MaximumTemperature &&
                        possibleReaction.MixingCategories == reactionPrototype.MixingCategories)
                    {
                        Assert.Fail(
                            $"The {possibleReaction.ID} reaction may occur before {reactionPrototype.ID} when freezing.");
                    }
                }

                    //Now safe set the temperature and mix the reagents
                    solutionContainerSystem.SetTemperature(solutionEnt.Value, reactionPrototype.MinimumTemperature);
                    solutionContainerSystem.SetCanReact(solutionEnt.Value, true);

                if (reactionPrototype.MixingCategories != null)
                {
                    var dummyEntity = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
                    var mixerComponent = entityManager.AddComponent<ReactionMixerComponent>(dummyEntity);
                    mixerComponent.ReactionTypes = reactionPrototype.MixingCategories;
                    solutionContainerSystem.UpdateChemicals(solutionEnt.Value, true, mixerComponent);
                }
            });

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                //you just got linq'd fool
                //(i'm sorry)
                var foundProductsMap = reactionPrototype.Products
                    .Concat(reactionPrototype.Reactants
                        .Where(x => x.Value.Catalyst)
                        .ToDictionary(x => x.Key, x => x.Value.Amount)
                    )
                    .ToDictionary(x => x, _ => false);

                foreach (var (reagent, quantity) in solution.Contents)
                {
                    Assert.That(foundProductsMap.TryFirstOrNull(
                        x => x.Key.Key == reagent.Prototype && x.Key.Value == quantity,
                        out var foundProduct));
                    foundProductsMap[foundProduct!.Value.Key] = true;
                }

                Assert.That(foundProductsMap.All(x => x.Value));

                server.EntMan.DeleteEntity(beaker);
            });
        }
    }
}
