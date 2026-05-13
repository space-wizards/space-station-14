#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestOf(typeof(ReactionPrototype))]
public sealed class TryAllReactionsTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TestSolutionContainer}
  components:
  - type: Solution
    id: beaker
    solution:
      maxVol: 120";

    private const string TestSolutionContainer = "TestSolutionContainer";

    private static readonly string[] Reactions = GameDataScrounger.PrototypesOfKind<ReactionPrototype>();

    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _sSolutionContainer = null!;

    [Test]
    [TestCaseSource(nameof(Reactions))]
    [TestOf(typeof(ReactionPrototype))]
    [Description("Tries an individual reaction to see if it succeeds.")]
    public async Task TryReaction(string reaction)
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        var reactionPrototype = SProtoMan.Index<ReactionPrototype>(reaction);

        EntityUid beaker = default;
        Solution? solution = null;
        Entity<SolutionComponent>? solutionEnt = default!;

        await Server.WaitAssertion(() =>
        {
            beaker = SSpawnAtPosition(TestSolutionContainer, coordinates);
            Assert.That(_sSolutionContainer
                .TryGetSolution(beaker, "beaker", out solutionEnt, out solution));
            Assert.That(solution, Is.Not.Null);
            _sSolutionContainer.SetCanReact(solutionEnt!.Value, false);
            foreach (var (id, reactant) in reactionPrototype.Reactants)
            {
#pragma warning disable NUnit2045
                Assert.That(_sSolutionContainer
                    .TryAddReagent(solutionEnt.Value,
                        id,
                        reactant.Amount,
                        out var quantity,
                        reactionPrototype.MinimumTemperature));
                Assert.That(reactant.Amount, Is.EqualTo(quantity));
#pragma warning restore NUnit2045
            }

            //Get all possible reactions with the current reagents
            var possibleReactions = SProtoMan.EnumeratePrototypes<ReactionPrototype>()
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
            _sSolutionContainer.SetTemperature(solutionEnt.Value, reactionPrototype.MinimumTemperature);
            _sSolutionContainer.SetCanReact(solutionEnt.Value, true);

            if (reactionPrototype.MixingCategories != null)
            {
                var dummyEntity = SSpawn(null);
                var mixerComponent = SEntMan.AddComponent<ReactionMixerComponent>(dummyEntity);
                mixerComponent.ReactionTypes = reactionPrototype.MixingCategories;
                _sSolutionContainer.UpdateChemicals(solutionEnt.Value, true, mixerComponent);
            }
        });

        await Server.WaitIdleAsync();

        await Server.WaitAssertion(() =>
        {
            //you just got linq'd fool
            //(i'm sorry)
            var foundProductsMap = reactionPrototype.Products
                .Concat(reactionPrototype.Reactants
                    .Where(x => x.Value.Catalyst)
                    .ToDictionary(x => x.Key, x => x.Value.Amount)
                )
                .ToDictionary(x => x, _ => false);

            foreach (var (reagent, quantity) in solution!.Contents)
            {
                Assert.That(foundProductsMap.TryFirstOrNull(
                    x => x.Key.Key == reagent.Prototype && x.Key.Value == quantity,
                    out var foundProduct));
                foundProductsMap[foundProduct!.Value.Key] = true;
            }

            Assert.That(foundProductsMap.Values, Is.All.True);
        });
    }
}
