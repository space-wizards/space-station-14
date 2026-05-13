#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;


// We are adding two non-reactive solutions in these tests
// To ensure volume(A) + volume(B) = volume(A+B)
// reactions can change this assumption
[TestOf(typeof(SharedSolutionContainerSystem))]
public sealed class SolutionSystemTests : GameTest
{
    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {SolutionTarget}
  components:
  - type: Solution
    id: beaker
    solution:
      maxVol: 50
  - type: Spillable

- type: reagent
  id: {TestReagentA}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {TestReagentB}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {TestReagentC}
  specificHeat: 2.0
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reagent
  id: {TestReagentD}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing

- type: reaction
  id: {TestReagentA}
  reactants:
    {TestReagentC}:
      amount: 1
    {TestReagentD}:
      amount: 1
  products:
    {TestReagentA}: 20
";

    private const string SolutionTarget = "SolutionTarget";
    private const string TestReagentA = "TestReagentA";
    private const string TestReagentB = "TestReagentB";
    private const string TestReagentC = "TestReagentC";
    private const string TestReagentD = "TestReagentD";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly ProtoId<ReagentPrototype> Oil = "Oil";

    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _solutionContainer = default!;

    [Test]
    public async Task TryAddTwoNonReactiveReagent()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid beaker;

        await Server.WaitAssertion(() =>
        {
            var oilQuantity = FixedPoint2.New(15);
            var waterQuantity = FixedPoint2.New(10);

            var oilAdded = new Solution(Oil, oilQuantity);
            var originalWater = new Solution(Water, waterQuantity);

            beaker = SSpawnAtPosition(SolutionTarget, coordinates);

            Assert.That(_solutionContainer
                .TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution, Is.Not.Null);
                Assert.That(solutionEnt, Is.Not.Null);
            }

            solution.AddSolution(originalWater, SProtoMan);
            Assert.That(_solutionContainer
                .TryAddSolution(solutionEnt.Value, oilAdded));

            var water = solution.GetTotalPrototypeQuantity(Water);
            var oil = solution.GetTotalPrototypeQuantity(Oil);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(water, Is.EqualTo(waterQuantity));
                Assert.That(oil, Is.EqualTo(oilQuantity));
            }
        });
    }

    // This test mimics current behavior
    // i.e. if adding too much `TryAddSolution` adding will fail
    [Test]
    public async Task TryAddTooMuchNonReactiveReagent()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid beaker;

        await Server.WaitAssertion(() =>
        {
            var oilQuantity = FixedPoint2.New(1500);
            var waterQuantity = FixedPoint2.New(10);

            var oilAdded = new Solution(Oil, oilQuantity);
            var originalWater = new Solution(Water, waterQuantity);

            beaker = SSpawnAtPosition(SolutionTarget, coordinates);
            Assert.That(_solutionContainer
                .TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution, Is.Not.Null);
                Assert.That(solutionEnt, Is.Not.Null);
            }

            solution.AddSolution(originalWater, SProtoMan);
            Assert.That(_solutionContainer.TryAddSolution(solutionEnt.Value, oilAdded), Is.False);

            var water = solution.GetTotalPrototypeQuantity(Water);
            var oil = solution.GetTotalPrototypeQuantity(Oil);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(water, Is.EqualTo(waterQuantity));
                Assert.That(oil, Is.EqualTo(FixedPoint2.Zero));
            }
        });
    }

    [Test]
    public async Task TryOverflowReaction()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var reagentC = new Solution(TestReagentC, 5);
            var reagentD = new Solution(TestReagentD, 5);

            var beaker = SSpawnAtPosition(SolutionTarget, TestMap!.GridCoords);

            Assert.That(_solutionContainer.TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution, Is.Not.Null);
                Assert.That(solutionEnt, Is.Not.Null);
            }

            _solutionContainer.AddSolution(solutionEnt.Value, reagentC);
            _solutionContainer.AddSolution(solutionEnt.Value, reagentD);

            Assert.That(solution.Volume, Is.EqualTo(FixedPoint2.New(50)));
            var query = SEntMan.EntityQueryEnumerator<PuddleComponent>();
            Assert.That(query.MoveNext(out _), "A puddle should have been spawned from the solution");
        });
    }

    // Unlike TryAddSolution this adds and two solution without then splits leaving only threshold in original
    [Test]
    public async Task TryMixAndOverflowTooMuchReagent()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid beaker;

        await Server.WaitAssertion(() =>
        {
            const int ratio = 9;
            const int threshold = 20;
            var waterQuantity = FixedPoint2.New(10);
            var oilQuantity = FixedPoint2.New(ratio * waterQuantity.Int());

            var oilAdded = new Solution(Oil, oilQuantity);
            var originalWater = new Solution(Water, waterQuantity);

            beaker = SSpawnAtPosition(SolutionTarget, coordinates);
            Assert.That(_solutionContainer
                .TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution, Is.Not.Null);
                Assert.That(solutionEnt, Is.Not.Null);
            }

            solution.AddSolution(originalWater, SProtoMan);
            Assert.That(_solutionContainer
                .TryMixAndOverflow(solutionEnt.Value, oilAdded, threshold, out var overflowingSolution));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution.Volume, Is.EqualTo(FixedPoint2.New(threshold)));

                var waterMix = solution.GetTotalPrototypeQuantity(Water);
                var oilMix = solution.GetTotalPrototypeQuantity(Oil);
                Assert.That(waterMix, Is.EqualTo(FixedPoint2.New(threshold / (ratio + 1))));
                Assert.That(oilMix, Is.EqualTo(FixedPoint2.New(threshold / (ratio + 1) * ratio)));

                Assert.That(overflowingSolution, Is.Not.Null);
                Assert.That(overflowingSolution!.Volume, Is.EqualTo(FixedPoint2.New(80)));

                var waterOverflow = overflowingSolution.GetTotalPrototypeQuantity(Water);
                var oilOverFlow = overflowingSolution.GetTotalPrototypeQuantity(Oil);
                Assert.That(waterOverflow, Is.EqualTo(waterQuantity - waterMix));
                Assert.That(oilOverFlow, Is.EqualTo(oilQuantity - oilMix));
            }
        });
    }

    // TryMixAndOverflow will fail if Threshold larger than MaxVolume
    [Test]
    public async Task TryMixAndOverflowTooBigOverflow()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid beaker;

        await Server.WaitAssertion(() =>
        {
            const int ratio = 9;
            const int threshold = 60;
            var waterQuantity = FixedPoint2.New(10);
            var oilQuantity = FixedPoint2.New(ratio * waterQuantity.Int());

            var oilAdded = new Solution(Oil, oilQuantity);
            var originalWater = new Solution(Water, waterQuantity);

            beaker = SSpawnAtPosition(SolutionTarget, coordinates);
            Assert.That(_solutionContainer
                .TryGetSolution(beaker, "beaker", out var solutionEnt, out var solution));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution, Is.Not.Null);
                Assert.That(solutionEnt, Is.Not.Null);
            }

            solution.AddSolution(originalWater, SProtoMan);
            Assert.That(_solutionContainer
                .TryMixAndOverflow(solutionEnt.Value, oilAdded, threshold, out _),
                Is.False);
        });
    }

    [Test]
    public async Task TestTemperatureCalculations()
    {
        const float temp = 100.0f;

        // Adding reagent with adjusts temperature
        await Server.WaitAssertion(() =>
        {
            var solution = new Solution(TestReagentA, FixedPoint2.New(100)) { Temperature = temp };
            Assert.That(solution.Temperature, Is.EqualTo(temp * 1));

            solution.AddSolution(new Solution(TestReagentA, FixedPoint2.New(100)) { Temperature = temp * 3 }, SProtoMan);
            Assert.That(solution.Temperature, Is.EqualTo(temp * 2));

            solution.AddSolution(new Solution(TestReagentB, FixedPoint2.New(100)) { Temperature = temp * 5 }, SProtoMan);
            Assert.That(solution.Temperature, Is.EqualTo(temp * 3));
        });

        // adding solutions combines thermal energy
        await Server.WaitAssertion(() =>
        {
            var solutionOne = new Solution(TestReagentA, FixedPoint2.New(100)) { Temperature = temp };

            var solutionTwo = new Solution(TestReagentB, FixedPoint2.New(100)) { Temperature = temp };
            solutionTwo.AddReagent(TestReagentC, FixedPoint2.New(100));

            var thermalEnergyOne = solutionOne.GetHeatCapacity(SProtoMan) * solutionOne.Temperature;
            var thermalEnergyTwo = solutionTwo.GetHeatCapacity(SProtoMan) * solutionTwo.Temperature;
            solutionOne.AddSolution(solutionTwo, SProtoMan);
            Assert.That(solutionOne.GetHeatCapacity(SProtoMan) * solutionOne.Temperature, Is.EqualTo(thermalEnergyOne + thermalEnergyTwo));
        });
    }
}
