#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Fluids;

[TestOf(typeof(PuddleComponent))]
public sealed class PuddleTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = default!;
    [SidedDependency(Side.Server)] private PuddleSystem _sPuddleSystem = default!;

    [Test]
    public async Task TilePuddleTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);

        await Server.WaitAssertion(() =>
        {
            var solution = new Solution(Water, FixedPoint2.New(20));
            var tile = TestMap.Tile;
            var gridUid = tile.GridUid;
            var (x, y) = tile.GridIndices;
            var coordinates = new EntityCoordinates(gridUid, x, y);

            Assert.That(_sPuddleSystem.TrySpillAt(coordinates, solution, out _), Is.True);
        });
    }

    [Test]
    public async Task SpaceNoPuddleTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var grid = TestMap.Grid;

        // Remove all tiles
        await Server.WaitPost(() =>
        {
            var tiles = new List<(Vector2i GridIndices, Tile Tile)>();
            var tileEnumerator = _sMapSystem.GetAllTiles(grid.Owner, grid.Comp);

            foreach (var tile in tileEnumerator)
            {
                tiles.Add((tile.GridIndices, Tile.Empty));
            }

            _sMapSystem.SetTiles(grid, tiles);
        });

        await RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            var coordinates = grid.Owner.ToCoordinates();
            var solution = new Solution(Water, FixedPoint2.New(20));

            Assert.That(_sPuddleSystem.TrySpillAt(coordinates, solution, out _), Is.False);
        });
    }
}
