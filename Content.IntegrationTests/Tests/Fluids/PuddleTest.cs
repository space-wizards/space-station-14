#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Fluids;

[TestOf(typeof(PuddleComponent))]
public sealed class PuddleTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> TestReagent = "Water";

    [SidedDependency(Side.Server)] private PuddleSystem _sPuddleSystem = null!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;

    [Test]
    [Description("Checks that spilling a solution on a grid tile succeeds.")]
    public async Task TilePuddleTest()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var solution = new Solution(TestReagent, FixedPoint2.New(20));
            Assert.That(_sPuddleSystem.TrySpillAt(TestMap!.GridCoords, solution, out _), Is.True);
        });
    }

    [Test]
    [Description("Tests that spilling a solution in space does not succeed.")]
    public async Task SpaceNoPuddleTest()
    {
        await Pair.CreateTestMap();
        var grid = TestMap!.Grid;

        // Remove all tiles
        await Server.WaitPost(() =>
        {
            var tiles = _sMapSystem.GetAllTiles(grid.Owner, grid.Comp);
            foreach (var tile in tiles)
            {
                _sMapSystem.SetTile(grid, tile.GridIndices, Tile.Empty);
            }
        });

        await RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            var solution = new Solution(TestReagent, FixedPoint2.New(20));

            Assert.That(_sPuddleSystem.TrySpillAt(TestMap.GridCoords, solution, out _), Is.False);
        });
    }
}
