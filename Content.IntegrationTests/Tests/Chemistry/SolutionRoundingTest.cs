#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(ChemicalReactionSystem))]
public sealed class SolutionRoundingTest : GameTest
{
    // This test tests two things:
    // * A rounding error in reaction code while I was making chloral hydrate
    // * An assert with solution heat capacity calculations that I found a repro for while testing the above.

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {SolutionRoundingTestContainer}
  components:
  - type: Solution
    id: beaker
    solution:
      maxVol: 100

# This is the Chloral Hydrate recipe fyi.
- type: reagent
  id: {SolutionRoundingTestReagentA}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {SolutionRoundingTestReagentB}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {SolutionRoundingTestReagentC}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {SolutionRoundingTestReagentD}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reaction
  id: SolutionRoundingTestReaction
  impact: Medium
  reactants:
    {SolutionRoundingTestReagentA}:
      amount: 3
    {SolutionRoundingTestReagentB}:
      amount: 1
    {SolutionRoundingTestReagentC}:
      amount: 1
  products:
    {SolutionRoundingTestReagentD}: 1
";
    private const string SolutionRoundingTestContainer = "SolutionRoundingTestContainer";
    private const string SolutionRoundingTestReagentA = "SolutionRoundingTestReagentA";
    private const string SolutionRoundingTestReagentB = "SolutionRoundingTestReagentB";
    private const string SolutionRoundingTestReagentC = "SolutionRoundingTestReagentC";
    private const string SolutionRoundingTestReagentD = "SolutionRoundingTestReagentD";

    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _sSolutionContainer = null!;

    [Test]
    public async Task Test()
    {
        await Pair.CreateTestMap();

        Solution solution = default!;
        Entity<SolutionComponent> solutionEnt = default;

        await Server.WaitPost(() =>
        {
            var beaker = SSpawnAtPosition(SolutionRoundingTestContainer, TestMap!.GridCoords);

            _sSolutionContainer.TryGetSolution(beaker, "beaker", out var newSolutionEnt, out var newSolution);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(newSolutionEnt, Is.Not.Null);
                Assert.That(newSolution, Is.Not.Null);
            }

            solutionEnt = newSolutionEnt.Value;
            solution = newSolution;

            _sSolutionContainer.TryAddSolution(solutionEnt, new Solution(SolutionRoundingTestReagentC, 50));
            _sSolutionContainer.TryAddSolution(solutionEnt, new Solution(SolutionRoundingTestReagentB, 30));

            for (var i = 0; i < 9; i++)
            {
                _sSolutionContainer.TryAddSolution(solutionEnt, new Solution(SolutionRoundingTestReagentA, 10));
            }
        });

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    solution.ContainsReagent(SolutionRoundingTestReagentA, null),
                    Is.False,
                    "Solution should not contain reagent A");

                Assert.That(
                    solution.ContainsReagent(SolutionRoundingTestReagentB, null),
                    Is.False,
                    "Solution should not contain reagent B");

                Assert.That(
                    solution[new ReagentId(SolutionRoundingTestReagentC, null)].Quantity,
                    Is.EqualTo((FixedPoint2)20));

                Assert.That(
                    solution[new ReagentId(SolutionRoundingTestReagentD, null)].Quantity,
                    Is.EqualTo((FixedPoint2)30));
            }
        });
    }
}
