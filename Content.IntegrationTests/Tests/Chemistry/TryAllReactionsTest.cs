using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.IntegrationTests.Tests.Chemistry;

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

    private static readonly string[] Reactions = GameDataScrounger.PrototypesOfKind<ReactionPrototype>();

    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    [Test]
    [TestOf(typeof(ReactionPrototype))]
    [Description("Tries an individual reaction to see if it succeeds.")]
    public async Task TryReaction()
    {
        var testMap = await Pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        // they call me a bird the way i be nesting
        try
        {
            foreach (var reaction in Reactions)
            {
                var reactionPrototype = SProtoMan.Index<ReactionPrototype>(reaction);

                EntityUid beaker = default;
                Solution solution = null;

                try
                {
                    await Pair.Server.WaitAssertion(() =>
                    {
                        beaker = SEntMan.SpawnEntity("TestSolutionContainer", coordinates);
                        Assert.That(_solutionContainerSystem
                            .TryGetSolution(beaker, "beaker", out var solutionEnt, out solution));
                        _solutionContainerSystem.SetCanReact(solutionEnt!.Value, false);
                        foreach (var (id, reactant) in reactionPrototype.Reactants)
                        {
                            Assert.That(_solutionContainerSystem
                                .TryAddReagent(solutionEnt.Value,
                                    id,
                                    reactant.Amount,
                                    out var quantity,
                                    reactionPrototype.MinimumTemperature));
                            Assert.That(reactant.Amount, Is.EqualTo(quantity));
                        }

                        //Get all possible reactions with the current reagents
                        var possibleReactions = SProtoMan.EnumeratePrototypes<ReactionPrototype>()
                            .Where(x => x.Reactants.All(id =>
                                solution.Contents.Any(s => s.Reagent.Prototype == id.Key)))
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
                        _solutionContainerSystem.SetTemperature(solutionEnt.Value,
                            reactionPrototype.MinimumTemperature);
                        _solutionContainerSystem.SetCanReact(solutionEnt.Value, true);

                        if (reactionPrototype.MixingCategories != null)
                        {
                            var dummyEntity = SEntMan.SpawnEntity(null, MapCoordinates.Nullspace);
                            var mixerComponent = SEntMan.AddComponent<ReactionMixerComponent>(dummyEntity);
                            mixerComponent.ReactionTypes = reactionPrototype.MixingCategories;
                            _solutionContainerSystem.UpdateChemicals(solutionEnt.Value, true, mixerComponent);
                        }
                    });

                    await Pair.Server.WaitIdleAsync();

                    await Pair.Server.WaitAssertion(() =>
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
                    });
                }
                finally
                {
                    await Server.WaitPost(() => SEntMan.DeleteEntity(beaker));
                }
            }
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.DeleteEntity(testMap.MapUid));
        }
    }
}
