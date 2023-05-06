#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids;

[TestFixture]
[TestOf(typeof(SpreaderSystem))]
public sealed class FluidSpill
{
    private static PuddleComponent? GetPuddle(IEntityManager entityManager, MapGridComponent mapGrid, Vector2i pos)
    {
        foreach (var uid in mapGrid.GetAnchoredEntities(pos))
        {
            if (entityManager.TryGetComponent(uid, out PuddleComponent? puddleComponent))
                return puddleComponent;
        }

        return null;
    }

    [Test]
    public async Task SpillCorner()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;
        var mapManager = server.ResolveDependency<IMapManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var puddleSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PuddleSystem>();
        var gameTiming = server.ResolveDependency<IGameTiming>();
        MapId mapId;
        EntityUid gridId = default;

        /*
         In this test, if o is spillage puddle and # are walls, we want to ensure all tiles are empty (`.`)
            . . .
            # . .
            o # .
        */
        await server.WaitPost(() =>
        {
            mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            gridId = grid.Owner;

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    grid.SetTile(new Vector2i(x, y), new Tile(1));
                }
            }

            entityManager.SpawnEntity("WallReinforced", grid.GridTileToLocal(new Vector2i(0, 1)));
            entityManager.SpawnEntity("WallReinforced", grid.GridTileToLocal(new Vector2i(1, 0)));
        });


        var puddleOrigin = new Vector2i(0, 0);
        await server.WaitAssertion(() =>
        {
            var grid = mapManager.GetGrid(gridId);
            var solution = new Solution("Blood", FixedPoint2.New(100));
            var tileRef = grid.GetTileRef(puddleOrigin);
            Assert.That(puddleSystem.TrySpillAt(tileRef, solution, out _), Is.True);
            Assert.That(GetPuddle(entityManager, grid, puddleOrigin), Is.Not.Null);
        });

        var sTimeToWait = (int) Math.Ceiling(2f * gameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        await server.WaitAssertion(() =>
        {
            var grid = mapManager.GetGrid(gridId);
            var puddle = GetPuddle(entityManager, grid, puddleOrigin);

            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddleSystem.CurrentVolume(puddle!.Owner, puddle), Is.EqualTo(FixedPoint2.New(100)));

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    if (x == 0 && y == 0 || x == 0 && y == 1 || x == 1 && y == 0)
                    {
                        continue;
                    }

                    var newPos = new Vector2i(x, y);
                    var sidePuddle = GetPuddle(entityManager, grid, newPos);
                    Assert.That(sidePuddle, Is.Null);
                }
            }
        });

        await pairTracker.CleanReturnAsync();
    }
}
