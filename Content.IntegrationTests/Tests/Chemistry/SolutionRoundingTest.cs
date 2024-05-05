using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(ChemicalReactionSystem))]
public sealed class SolutionRoundingTest
{
    // This test tests two things:
    // * A rounding error in reaction code while I was making chloral hydrate
    // * An assert with solution heat capacity calculations that I found a repro for while testing the above.

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: SolutionRoundingTestContainer
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 100

# This is the Chloral Hydrate recipe fyi.
- type: reagent
  id: SolutionRoundingTestReagentA
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: SolutionRoundingTestReagentB
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: SolutionRoundingTestReagentC
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: SolutionRoundingTestReagentD
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reaction
  id: SolutionRoundingTestReaction
  impact: Medium
  reactants:
    SolutionRoundingTestReagentA:
      amount: 3
    SolutionRoundingTestReagentB:
      amount: 1
    SolutionRoundingTestReagentC:
      amount: 1
  products:
    SolutionRoundingTestReagentD: 1
";

    [Test]
    public async Task Test()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        Solution solution = default;
        Entity<SolutionComponent> solutionEnt = default;

        await server.WaitPost(() =>
        {
            var system = server.System<SolutionContainerSystem>();
            var beaker = server.EntMan.SpawnEntity("SolutionRoundingTestContainer", testMap.GridCoords);

            system.TryGetSolution(beaker, "beaker", out var newSolutionEnt, out var newSolution);

            solutionEnt = newSolutionEnt!.Value;
            solution = newSolution!;

            system.TryAddSolution(solutionEnt, new Solution("SolutionRoundingTestReagentC", 50));
            system.TryAddSolution(solutionEnt, new Solution("SolutionRoundingTestReagentB", 30));

            for (var i = 0; i < 9; i++)
            {
                system.TryAddSolution(solutionEnt, new Solution("SolutionRoundingTestReagentA", 10));
            }
        });

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    solution.ContainsReagent("SolutionRoundingTestReagentA", null),
                    Is.False,
                    "Solution should not contain reagent A");

                Assert.That(
                    solution.ContainsReagent("SolutionRoundingTestReagentB", null),
                    Is.False,
                    "Solution should not contain reagent B");

                Assert.That(
                    solution![new ReagentId("SolutionRoundingTestReagentC", null)].Quantity,
                    Is.EqualTo((FixedPoint2) 20));

                Assert.That(
                    solution![new ReagentId("SolutionRoundingTestReagentD", null)].Quantity,
                    Is.EqualTo((FixedPoint2) 30));
            });
        });

        await pair.CleanReturnAsync();
    }
}
