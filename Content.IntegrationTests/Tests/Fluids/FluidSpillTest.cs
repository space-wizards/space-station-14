#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids;

[TestFixture]
[TestOf(typeof(FluidSpreaderSystem))]
public sealed class FluidSpill : ContentIntegrationTest
{
    private const string SpillMapsYml = "Maps/Test/floor3x3.yml";

    private static PuddleComponent? GetPuddle(IEntityManager entityManager, IMapGrid mapGrid, Vector2i pos)
    {
        foreach (var uid in mapGrid.GetAnchoredEntities(pos))
        {
            if (entityManager.TryGetComponent(uid, out PuddleComponent puddleComponent))
                return puddleComponent;
        }

        return null;
    }

    private readonly Direction[] _dirs =
    {
        Direction.East,
        Direction.SouthEast,
        Direction.South,
        Direction.SouthWest,
        Direction.West,
        Direction.NorthWest,
        Direction.North,
        Direction.NorthEast,
    };


    private readonly Vector2i _origin = new(-1, -1);

    [Test]
    public async Task SpillEvenlyTest()
    {
        // --- Setup
        var server = StartServer();
        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var mapLoader = server.ResolveDependency<IMapLoader>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var spillSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SpillableSystem>();
        var gameTiming = server.ResolveDependency<IGameTiming>();
        MapId mapId;
        IMapGrid? grid = null;

        await server.WaitPost(() =>
        {
            mapId = mapManager.CreateMap();
            grid = mapLoader.LoadBlueprint(mapId, SpillMapsYml)!;
        });

        if (grid == null)
        {
            Assert.Fail($"Test blueprint {SpillMapsYml} not found.");
            return;
        }

        await server.WaitAssertion(() =>
        {
            var solution = new Solution("Water", FixedPoint2.New(100));
            var tileRef = grid.GetTileRef(_origin);
            var puddle = spillSystem.SpillAt(tileRef, solution, "PuddleSmear");
            Assert.That(puddle, Is.Not.Null);
            Assert.That(GetPuddle(entityManager, grid, _origin), Is.Not.Null);
        });

        var sTimeToWait = (int) Math.Ceiling(2f * gameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        server.Assert(() =>
        {
            var puddle = GetPuddle(entityManager, grid, _origin);

            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(20)));

            foreach (var direction in _dirs)
            {
                var newPos = _origin.Offset(direction);
                var sidePuddle = GetPuddle(entityManager, grid, newPos);
                Assert.That(sidePuddle, Is.Not.Null);
                Assert.That(sidePuddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(10)));
            }
        });

        await server.WaitIdleAsync();
    }


    [Test]
    public async Task SpillSmallOverflowTest()
    {
        // --- Setup
        var server = StartServer();
        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var mapLoader = server.ResolveDependency<IMapLoader>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var spillSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SpillableSystem>();
        var gameTiming = server.ResolveDependency<IGameTiming>();
        MapId mapId;
        IMapGrid? grid = null;

        await server.WaitPost(() =>
        {
            mapId = mapManager.CreateMap();
            grid = mapLoader.LoadBlueprint(mapId, SpillMapsYml)!;
        });

        if (grid == null)
        {
            Assert.Fail($"Test blueprint {SpillMapsYml} not found.");
            return;
        }

        await server.WaitAssertion(() =>
        {
            var solution = new Solution("Water", FixedPoint2.New(20.01));

            var tileRef = grid.GetTileRef(_origin);
            var puddle = spillSystem.SpillAt(tileRef, solution, "PuddleSmear");

            Assert.That(puddle, Is.Not.Null);
        });

        if (grid == null)
        {
            Assert.Fail($"Test blueprint {SpillMapsYml} not found.");
            return;
        }

        var sTimeToWait = (int) Math.Ceiling(2f * gameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        server.Assert(() =>
        {
            var puddle = GetPuddle(entityManager, grid, _origin);
            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(20)));

            // we don't know where a spill would happen
            // but there should be only one
            var emptyField = 0;
            var fullField = 0;
            foreach (var direction in _dirs)
            {
                var newPos = _origin.Offset(direction);
                var sidePuddle = GetPuddle(entityManager, grid, newPos);
                if (sidePuddle == null)
                {
                    emptyField++;
                }
                else if (sidePuddle.CurrentVolume == FixedPoint2.Epsilon)
                {
                    fullField++;
                }
            }

            Assert.That(emptyField, Is.EqualTo(7));
            Assert.That(fullField, Is.EqualTo(1));
        });

        await server.WaitIdleAsync();
    }
}
