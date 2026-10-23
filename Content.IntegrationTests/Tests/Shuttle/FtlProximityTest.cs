using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Maps;
using Content.Server.Shuttles.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Shuttle;



public sealed class FtlProximityTest : GameTest
{

    private readonly record struct RectSize(int Width, int Height);

    private readonly record struct FTLPositioning(RectSize Station, RectSize Shuttle);
    private readonly record struct FTLContext(TestMapData Station, TestMapData Shuttle, EntityUid Target);

    private async Task AssertNoOverlap(FTLContext scenario, FTLPositioning positionContext, int seed)
    {
        var server = Pair.Server;
        var mapSys = server.System<SharedMapSystem>();
        var shuttleSys = server.System<ShuttleSystem>();
        var xformSys = server.System<SharedTransformSystem>();
        var random = server.ResolveDependency<IRobustRandom>();

        await server.WaitAssertion(() =>
        {
            random.SetSeed(seed);
            mapSys.SetTiles(scenario.Station.Grid, [.. TileShapes.Rectangle(positionContext.Station.Width, positionContext.Station.Height, 1)]);
            mapSys.SetTiles(scenario.Shuttle.Grid, [.. TileShapes.Rectangle(positionContext.Shuttle.Width, positionContext.Shuttle.Height, 1)]);

            var arrived = shuttleSys.TryFTLProximity(scenario.Shuttle.Grid, scenario.Target);
            Assert.That(arrived, Is.True);

            var stationBox = xformSys.GetWorldMatrix(scenario.Station.Grid.Owner)
                .TransformBox(scenario.Station.Grid.Comp.LocalAABB);
            var shuttleOrigin = xformSys.GetWorldPosition(scenario.Shuttle.Grid.Owner);
            var shuttleBox = new Box2Rotated(
                scenario.Shuttle.Grid.Comp.LocalAABB.Translated(shuttleOrigin),
                xformSys.GetWorldRotation(scenario.Shuttle.Grid.Owner),
                shuttleOrigin);

            var angle = xformSys.GetWorldRotation(scenario.Shuttle.Grid.Owner);
            var failureInfo = $"sizes={positionContext} station={stationBox} " +
                              $"shuttle={shuttleBox}" +
                              $"angle={angle}";
            var grids = new List<Entity<MapGridComponent>>();
            mapSys.FindGridsIntersecting(scenario.Station.MapUid, shuttleBox, ref grids, includeMap: false);
            var anyOverlap = grids.Any(g => g.Owner == scenario.Station.Grid.Owner);
            Assert.That(anyOverlap, Is.False, failureInfo);
        });
    }

    [Test]
    public async Task ShuttleDoesNotOverlapTargetGrid()
    {
        var station = await Pair.CreateTestMap();
        var shuttle = await Pair.CreateTestMap();
        var context = new FTLContext(station, shuttle, station.Grid.Owner);
        var positioning = new FTLPositioning(new RectSize(12, 8), new RectSize(3, 2));
        await AssertNoOverlap(context, positioning, 0);
    }


    [Test]
    public async Task ShuttleDoesNotOverlapStationWhenTargetIsMap()
    {
        var station = await Pair.CreateTestMap();
        var shuttle = await Pair.CreateTestMap();
        var context = new FTLContext(station, shuttle, station.MapUid);
        var positioning = new FTLPositioning(new RectSize(12, 8), new RectSize(3, 2));
        await AssertNoOverlap(context, positioning, 0);
    }

    // Seed 19 lands the shuttle with a corner about 15 tiles inside the station.
    [Test]
    public async Task ShuttleDoesNotOverlapTargetGridSeed19()
    {
        var station = await Pair.CreateTestMap();
        var shuttle = await Pair.CreateTestMap();
        var context = new FTLContext(station, shuttle, station.Grid.Owner);
        var positioning = new FTLPositioning(new RectSize(74, 153), new RectSize(66, 40));
        await AssertNoOverlap(context, positioning, 19);
    }

    private const int MinSize = 1;
    private const int MaxStationSize = 224;
    private const int MaxShuttleSize = 100;

    // Each seed is one test run. The seed shows in the test name, so a failing run can be replayed.
    private static IEnumerable<int> Seeds()
    {
        return Enumerable.Range(0, 1);
    }

    [TestCaseSource(nameof(Seeds))]
    public async Task ShuttleDoesNotOverlapTargetGridRandomSizes(int seed)
    {
        var rng = new Random(seed);
        var station = await Pair.CreateTestMap();
        var shuttle = await Pair.CreateTestMap();
        var context = new FTLContext(station, shuttle, station.Grid.Owner);
        // Shuttle first, then a station strictly larger than it in both dimensions.
        var shuttleSize = new RectSize(rng.Next(MinSize, MaxShuttleSize + 1), rng.Next(MinSize, MaxShuttleSize + 1));
        var stationSize = new RectSize(
            rng.Next(shuttleSize.Width + 1, MaxStationSize + 1),
            rng.Next(shuttleSize.Height + 1, MaxStationSize + 1));
        var positioning = new FTLPositioning(stationSize, shuttleSize);
        await AssertNoOverlap(context, positioning, seed);
    }

}
