#nullable enable
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry.Reagent;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.Fluids;

[TestOf(typeof(AbsorbentComponent))]
public sealed class AbsorbentTest : GameTest
{
    private const string UserDummyId = "UserDummy";
    private const string AbsorbentDummyId = "AbsorbentDummy";
    private const string RefillableDummyId = "RefillableDummy";
    private const string SmallRefillableDummyId = "SmallRefillableDummy";

    private static readonly ProtoId<ReagentPrototype> EvaporablePrototypeId = "Water";
    private static readonly ProtoId<ReagentPrototype> NonEvaporablePrototypeId = "Cola";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {UserDummyId}
  id: {UserDummyId}

- type: entity
  name: {AbsorbentDummyId}
  id: {AbsorbentDummyId}
  components:
  - type: Absorbent
    useAbsorberSolution: true
  - type: Solution
    id: absorbed
    solution:
      maxVol: 100

- type: entity
  name: {RefillableDummyId}
  id: {RefillableDummyId}
  components:
  - type: Solution
    id: refillable
    solution:
      maxVol: 200
  - type: RefillableSolution
    solution: refillable

- type: entity
  name: {SmallRefillableDummyId}
  id: {SmallRefillableDummyId}
  components:
  - type: Solution
    id: refillable
    solution:
      maxVol: 20
  - type: RefillableSolution
    solution: refillable
";
    public sealed record TestSolutionReagents(FixedPoint2 VolumeOfEvaporable, FixedPoint2 VolumeOfNonEvaporable);

    public record TestSolutionCase(
        string Case, // Only for clarity purposes
        TestSolutionReagents InitialAbsorbentSolution,
        TestSolutionReagents InitialRefillableSolution,
        TestSolutionReagents ExpectedAbsorbentSolution,
        TestSolutionReagents ExpectedRefillableSolution);

    [SidedDependency(Side.Server)] private AbsorbentSystem _sAbsorbentSystem = null!;
    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _sSolutionContainerSystem = null!;

    [TestCaseSource(nameof(TestCasesToRun))]
    public async Task AbsorbentOnRefillableTest(TestSolutionCase testCase)
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var user = SSpawnAtPosition(UserDummyId, coordinates);
            var absorbent = SSpawnAtPosition(AbsorbentDummyId, coordinates);
            var refillable = SSpawnAtPosition(RefillableDummyId, coordinates);

            var component = SComp<AbsorbentComponent>(absorbent);
            _sSolutionContainerSystem.TryGetSolution(absorbent, component.SolutionName, out var absorbentSoln, out var absorbentSolution);
            _sSolutionContainerSystem.TryGetRefillableSolution(refillable, out var refillableSoln, out var refillableSolution);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(absorbentSoln, Is.Not.Null);
                Assert.That(absorbentSolution, Is.Not.Null);
                Assert.That(refillableSoln, Is.Not.Null);
                Assert.That(refillableSolution, Is.Not.Null);
            }

            // Arrange
            if (testCase.InitialAbsorbentSolution.VolumeOfEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(absorbentSoln.Value, new Solution(EvaporablePrototypeId, testCase.InitialAbsorbentSolution.VolumeOfEvaporable));
            if (testCase.InitialAbsorbentSolution.VolumeOfNonEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(absorbentSoln.Value, new Solution(NonEvaporablePrototypeId, testCase.InitialAbsorbentSolution.VolumeOfNonEvaporable));

            if (testCase.InitialRefillableSolution.VolumeOfEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(refillableSoln.Value, new Solution(EvaporablePrototypeId, testCase.InitialRefillableSolution.VolumeOfEvaporable));
            if (testCase.InitialRefillableSolution.VolumeOfNonEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(refillableSoln.Value, new Solution(NonEvaporablePrototypeId, testCase.InitialRefillableSolution.VolumeOfNonEvaporable));

            // Act
            _sAbsorbentSystem.Mop((absorbent, component), user, refillable);

            // Assert
            var absorbentComposition = absorbentSolution.GetReagentPrototypes(SProtoMan).ToDictionary(r => r.Key.ID, r => r.Value);
            var refillableComposition = refillableSolution.GetReagentPrototypes(SProtoMan).ToDictionary(r => r.Key.ID, r => r.Value);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(VolumeOfPrototypeInComposition(absorbentComposition, EvaporablePrototypeId), Is.EqualTo(testCase.ExpectedAbsorbentSolution.VolumeOfEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(absorbentComposition, NonEvaporablePrototypeId), Is.EqualTo(testCase.ExpectedAbsorbentSolution.VolumeOfNonEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(refillableComposition, EvaporablePrototypeId), Is.EqualTo(testCase.ExpectedRefillableSolution.VolumeOfEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(refillableComposition, NonEvaporablePrototypeId), Is.EqualTo(testCase.ExpectedRefillableSolution.VolumeOfNonEvaporable));
            }
        });
    }

    [TestCaseSource(nameof(TestCasesToRunOnSmallRefillable))]
    public async Task AbsorbentOnSmallRefillableTest(TestSolutionCase testCase)
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var user = SSpawnAtPosition(UserDummyId, coordinates);
            var absorbent = SSpawnAtPosition(AbsorbentDummyId, coordinates);
            var refillable = SSpawnAtPosition(SmallRefillableDummyId, coordinates);

            var component = SComp<AbsorbentComponent>(absorbent);
            _sSolutionContainerSystem.TryGetSolution(absorbent, component.SolutionName, out var absorbentSoln, out var absorbentSolution);
            _sSolutionContainerSystem.TryGetRefillableSolution(refillable, out var refillableSoln, out var refillableSolution);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(absorbentSoln, Is.Not.Null);
                Assert.That(absorbentSolution, Is.Not.Null);
                Assert.That(refillableSoln, Is.Not.Null);
                Assert.That(refillableSolution, Is.Not.Null);
            }

            // Arrange
            _sSolutionContainerSystem.AddSolution(absorbentSoln.Value, new Solution(EvaporablePrototypeId, testCase.InitialAbsorbentSolution.VolumeOfEvaporable));
            if (testCase.InitialAbsorbentSolution.VolumeOfNonEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(absorbentSoln.Value, new Solution(NonEvaporablePrototypeId, testCase.InitialAbsorbentSolution.VolumeOfNonEvaporable));

            if (testCase.InitialRefillableSolution.VolumeOfEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(refillableSoln.Value, new Solution(EvaporablePrototypeId, testCase.InitialRefillableSolution.VolumeOfEvaporable));
            if (testCase.InitialRefillableSolution.VolumeOfNonEvaporable > FixedPoint2.Zero)
                _sSolutionContainerSystem.AddSolution(refillableSoln.Value, new Solution(NonEvaporablePrototypeId, testCase.InitialRefillableSolution.VolumeOfNonEvaporable));

            // Act
            _sAbsorbentSystem.Mop((absorbent, component), user, refillable);

            // Assert
            var absorbentComposition = absorbentSolution.GetReagentPrototypes(SProtoMan).ToDictionary(r => r.Key.ID, r => r.Value);
            var refillableComposition = refillableSolution.GetReagentPrototypes(SProtoMan).ToDictionary(r => r.Key.ID, r => r.Value);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(VolumeOfPrototypeInComposition(absorbentComposition, EvaporablePrototypeId), Is.EqualTo(testCase.ExpectedAbsorbentSolution.VolumeOfEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(absorbentComposition, NonEvaporablePrototypeId), Is.EqualTo(testCase.ExpectedAbsorbentSolution.VolumeOfNonEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(refillableComposition, EvaporablePrototypeId), Is.EqualTo(testCase.ExpectedRefillableSolution.VolumeOfEvaporable));
                Assert.That(VolumeOfPrototypeInComposition(refillableComposition, NonEvaporablePrototypeId), Is.EqualTo(testCase.ExpectedRefillableSolution.VolumeOfNonEvaporable));
            }
        });
    }

    private static FixedPoint2 VolumeOfPrototypeInComposition(Dictionary<string, FixedPoint2> composition, string prototypeId)
    {
        return composition.TryGetValue(prototypeId, out var value) ? value : FixedPoint2.Zero;
    }

    public static readonly TestSolutionCase[] TestCasesToRun =
    [
        // Both empty case
        new(
            "Both empty - no transfer",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero)
        ),
        // Just water cases
        new(
            "Transfer water to empty refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero)
        ),
        new(
            "Transfer water to empty absorbent",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero)
        ),
        new(
            "Both partially filled with water while everything fits in absorbent",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(40), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(90), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero)
        ),
        new(
            "Both partially filled with water while not everything fits in absorbent",
            new TestSolutionReagents(FixedPoint2.New(70), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(20), FixedPoint2.Zero)
        ),
        // Just contaminants cases
        new(
            "Transfer contaminants to empty refillable",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50))
        ),
        new(
            "Do not transfer contaminants back to empty absorbent",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50))
        ),
        new(
            "Add contaminants to preexisting while everything fits in refillable",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(130)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(180))
        ),
        new(
            "Add contaminants to preexisting while not everything fits in refillable",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(90)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(130)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(20)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(200))
        ),
        // Mixed: water and contaminants cases
        new(
            "Transfer just contaminants into empty refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50))
        ),
        new(
            "Transfer just contaminants into non-empty refillable while everything fits",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(60)),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(110))
        ),
        new(
            "Transfer just contaminants into non-empty refillable while not everything fits",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(170)),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(20)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(200))
        ),
        new(
            "Transfer just contaminants and absorb water from water refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.New(70), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(20), FixedPoint2.New(50))
        ),
        new(
            "Transfer just contaminants and absorb water from a full water refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.New(200), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(150), FixedPoint2.New(50))
        ),
        new(
            "Transfer just contaminants and absorb water from a full mixed refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.New(100)),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(150))
        ),
        new(
            "Transfer just contaminants and absorb water from a low-water mixed refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.New(10), FixedPoint2.New(100)),
            new TestSolutionReagents(FixedPoint2.New(60), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(150))
        ),
        new(
            "Contaminants for water exchange",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(100)),
            new TestSolutionReagents(FixedPoint2.New(200), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(100), FixedPoint2.New(100))
        )
    ];

    public static readonly TestSolutionCase[] TestCasesToRunOnSmallRefillable =
    [
        // Only testing cases where small refillable AvailableVolume makes a difference
        new(
            "Transfer water to empty refillable",
            new TestSolutionReagents(FixedPoint2.New(50), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(30), FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.New(20), FixedPoint2.Zero)
        ),
        new(
            "Transfer contaminants to empty refillable",
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(50)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.Zero),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(30)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(20))
        ),
        new(
            "Mixed transfer in limited space",
            new TestSolutionReagents(FixedPoint2.New(20), FixedPoint2.New(25)),
            new TestSolutionReagents(FixedPoint2.New(10), FixedPoint2.New(5)),
            new TestSolutionReagents(FixedPoint2.New(30), FixedPoint2.New(10)),
            new TestSolutionReagents(FixedPoint2.Zero, FixedPoint2.New(20))
        )
    ];
}
