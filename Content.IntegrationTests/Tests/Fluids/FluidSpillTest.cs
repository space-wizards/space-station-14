#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Directions;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids;

[TestFixture]
[TestOf(typeof(FluidSpreaderSystem))]
public sealed class FluidSpill : ContentIntegrationTest
{
    [Test]
    public async Task SpillEvenlyTest()
    {
        PuddleComponent? GetPuddle(IEntityManager entManager, IMapManager mapManager1, Vector2i vector2I)
        {
            var grid = GetMainGrid(mapManager1);
            foreach (var uid in grid.GetAnchoredEntities(vector2I))
            {
                if (entManager.TryGetComponent(uid, out PuddleComponent puddleComponent))
                    return puddleComponent;
            }

            return null;
        }

        var server = StartServer();

        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
        var sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var spillSystem = entitySystemManager.GetEntitySystem<SpillableSystem>();
        var sGameTiming = server.ResolveDependency<IGameTiming>();
        Vector2i pos = Vector2i.One;
        Direction[] dirs = { };

        server.Post(() =>
        {
            dirs = SharedDirectionExtensions.RandomDirections().ToArray();
            var solution = new Solution("Water", FixedPoint2.New(100));
            var grid = GetMainGrid(mapManager);
            var tileRef = GetMainTile(grid);
            pos = tileRef.GridIndices;
            var tileX = new Tile(sTileDefinitionManager["underplating"].TileId);

            // Init 3x3 tiles
            grid.SetTile(pos, tileX);
            foreach (var dir in dirs)
            {
                grid.SetTile(pos.Offset(dir), tileX);
            }

            var puddle = spillSystem.SpillAt(tileRef, solution, "PuddleSmear");

            Assert.NotNull(puddle);
        });

        var sTimeToWait = (int) Math.Ceiling(2f * sGameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        server.Assert(() =>
        {
            var puddle = GetPuddle(entityManager, mapManager, pos);
            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(20)));

            foreach (var direction in dirs)
            {
                var newPos = pos.Offset(direction);
                var sidePuddle = GetPuddle(entityManager, mapManager, newPos);
                Assert.That(sidePuddle, Is.Not.Null);
                Assert.That(sidePuddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(10)));
            }
        });

        await server.WaitIdleAsync();
    }

    [Test]
    public async Task SpillSmallOverflowTest()
    {
        PuddleComponent? GetPuddle(IEntityManager entManager, IMapManager mapManager1, Vector2i vector2I)
        {
            var grid = GetMainGrid(mapManager1);
            foreach (var uid in grid.GetAnchoredEntities(vector2I))
            {
                if (entManager.TryGetComponent(uid, out PuddleComponent puddleComponent))
                    return puddleComponent;
            }

            return null;
        }

        var server = StartServer();

        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
        var sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var spillSystem = entitySystemManager.GetEntitySystem<SpillableSystem>();
        var sGameTiming = server.ResolveDependency<IGameTiming>();
        Vector2i pos = Vector2i.One;
        Direction[] dirs = { };

        server.Post(() =>
        {
            dirs = SharedDirectionExtensions.RandomDirections().ToArray();
            var solution = new Solution("Water", FixedPoint2.New(20.01));
            var grid = GetMainGrid(mapManager);
            var tileRef = GetMainTile(grid);
            pos = tileRef.GridIndices;
            var tileX = new Tile(sTileDefinitionManager["underplating"].TileId);

            // Init 3x3 tiles
            grid.SetTile(pos, tileX);
            foreach (var dir in dirs)
            {
                grid.SetTile(pos.Offset(dir), tileX);
            }

            var puddle = spillSystem.SpillAt(tileRef, solution, "PuddleSmear");

            Assert.NotNull(puddle);
        });

        var sTimeToWait = (int) Math.Ceiling(2f * sGameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        server.Assert(() =>
        {
            var puddle = GetPuddle(entityManager, mapManager, pos);
            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddle!.CurrentVolume, Is.EqualTo(FixedPoint2.New(20)));

            // we don't know where a spill would happen
            // but there should be only one
            var emptyField = 0;
            var fullField = 0;
            foreach (var direction in dirs)
            {
                var newPos = pos.Offset(direction);
                var sidePuddle = GetPuddle(entityManager, mapManager, newPos);
                if (sidePuddle == null)
                {
                    emptyField++;
                }
                else if(sidePuddle.CurrentVolume == FixedPoint2.Epsilon)
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
