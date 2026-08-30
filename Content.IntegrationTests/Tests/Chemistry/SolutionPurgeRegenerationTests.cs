using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(SolutionRegenerationSystem))]
[TestOf(typeof(SolutionPurgeSystem))]
public sealed class SolutionPurgeRegenerationTests : GameTest
{
    private static readonly EntProtoId AdvancedMop = "AdvMopItem";
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly ProtoId<ReagentPrototype> NotWater = "DexalinPlus";

    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    [Test]
    public async Task TestMop()
    {
        var testMap = await Pair.CreateTestMap();

        EntityUid mop = default!;
        Entity<SolutionComponent> solution = default!;
        await Server.WaitPost(() =>
        {
            mop = SSpawnAtPosition(AdvancedMop, testMap.GridCoords);

            var generated = SComp<SolutionRegenerationComponent>(mop).Generated;
            var purge = SComp<SolutionPurgeComponent>(mop);
            Assume.That(generated.ContainsPrototype(Water));
            Assume.That(purge.Preserve, Does.Not.Contain(NotWater));


            Assert.That(_solutionContainer.TryGetSolution(mop, "absorbed", out var mopSolution, out _));
            solution = mopSolution!.Value;
            Assert.That(_solutionContainer.AddSolution(solution, new Solution(NotWater, 50)), Is.EqualTo(FixedPoint2.New(50)));
        });

        await PoolManager.WaitUntil(Server, () => !solution.Comp.Solution.ContainsPrototype(NotWater));
        await PoolManager.WaitUntil(Server, () => solution.Comp.Solution.Volume == solution.Comp.Solution.MaxVolume);
    }
}
