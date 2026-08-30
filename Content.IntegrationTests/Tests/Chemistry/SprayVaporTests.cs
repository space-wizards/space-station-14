using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Decals;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(SharedSpraySystem))]
[TestOf(typeof(VaporSystem))]
public sealed class SprayVaporTests : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Blood = "Blood";
    private static readonly EntProtoId SprayBottleSpaceCleaner = "SprayBottleSpaceCleaner";
    private const string BloodPuddle = "SprayVaporTestBloodPuddle";
    private const int BloodVolume = 5;

    [TestPrototypes]
    private static readonly string Prototypes = @$"
- type: entity
  parent: Puddle
  id: {BloodPuddle}
  suffix: Blood
  components:
  - type: Solution
    id: puddle
    solution:
      maxVol: 1000
      reagents:
      - ReagentId: {Blood}
        Quantity: {BloodVolume}
";

    [SidedDependency(Side.Server)] private readonly SpraySystem _spray = default!;
    [SidedDependency(Side.Server)] private readonly SolutionContainerSystem _solutionContainer = default!;
    [SidedDependency(Side.Server)] private readonly SharedTransformSystem _transform = default!;

    [Test]
    public async Task TestSprayingSpaceCleaner()
    {
        var testMap = await Pair.CreateTestMap();

        Entity<SolutionComponent> puddle = default!;

        await Server.WaitAssertion(() =>
        {
            var sprayCleaner = SSpawnAtPosition(SprayBottleSpaceCleaner, testMap.GridCoords);
            Assume.That(sprayCleaner, Has.Comp<SprayComponent>(Server));
            _transform.SetLocalPositionNoLerp(sprayCleaner, SComp<TransformComponent>(sprayCleaner).LocalPosition + new Vector2(1, 1));

            var puddleUid = SSpawnAtPosition(BloodPuddle, testMap.GridCoords);
            Assume.That(puddleUid, Has.Comp<PuddleComponent>(Server));
            Assume.That(_solutionContainer.TryGetSolution(puddleUid, "puddle", out var puddleSolution, out _));
            puddle = puddleSolution!.Value;
            Assume.That(puddle.Comp.Solution.ContainsPrototype(Blood));

            _spray.Spray((sprayCleaner, SComp<SprayComponent>(sprayCleaner)), _transform.GetMapCoordinates(puddleUid));
            var vaporEnum = SEntMan.EntityQueryEnumerator<VaporComponent>();
            Assume.That(vaporEnum.MoveNext(out _));
        });

        await PoolManager.WaitUntil(Server, () => !SEntMan.EntityQueryEnumerator<VaporComponent>().MoveNext(out _));

        await Server.WaitAssertion(() =>
        {
            Assert.That(!puddle.Comp.Solution.ContainsPrototype(Blood));
        });
    }
}
