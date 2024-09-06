#nullable enable
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
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
    private static PuddleComponent? GetPuddle(IEntityManager entityManager, Entity<MapGridComponent> mapGrid, Vector2i pos)
    {
        return GetPuddleEntity(entityManager, mapGrid, pos)?.Comp;
    }

    private static Entity<PuddleComponent>? GetPuddleEntity(IEntityManager entityManager, Entity<MapGridComponent> mapGrid, Vector2i pos)
    {
        var mapSys = entityManager.System<SharedMapSystem>();
        foreach (var uid in mapSys.GetAnchoredEntities(mapGrid, mapGrid.Comp, pos))
        {
            if (entityManager.TryGetComponent(uid, out PuddleComponent? puddleComponent))
                return (uid, puddleComponent);
        }

        return null;
    }

    [Test]
    public async Task SpillCorner()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var mapManager = server.ResolveDependency<IMapManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var puddleSystem = server.System<PuddleSystem>();
        var mapSystem = server.System<SharedMapSystem>();
        var gameTiming = server.ResolveDependency<IGameTiming>();
        EntityUid gridId = default;

        /*
         In this test, if o is spillage puddle and # are walls, we want to ensure all tiles are empty (`.`)
            . . .
            # . .
            o # .
        */
        await server.WaitPost(() =>
        {
            mapSystem.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            gridId = grid.Owner;

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    mapSystem.SetTile(grid, new Vector2i(x, y), new Tile(1));
                }
            }

            entityManager.SpawnEntity("WallReinforced", mapSystem.GridTileToLocal(grid, grid.Comp, new Vector2i(0, 1)));
            entityManager.SpawnEntity("WallReinforced", mapSystem.GridTileToLocal(grid, grid.Comp, new Vector2i(1, 0)));
        });


        var puddleOrigin = new Vector2i(0, 0);
        await server.WaitAssertion(() =>
        {
            var grid = entityManager.GetComponent<MapGridComponent>(gridId);
            var solution = new Solution("Blood", FixedPoint2.New(100));
            var tileRef = mapSystem.GetTileRef(gridId, grid, puddleOrigin);
#pragma warning disable NUnit2045 // Interdependent tests
            Assert.That(puddleSystem.TrySpillAt(tileRef, solution, out _), Is.True);
            Assert.That(GetPuddle(entityManager, (gridId, grid), puddleOrigin), Is.Not.Null);
#pragma warning restore NUnit2045
        });

        var sTimeToWait = (int) Math.Ceiling(2f * gameTiming.TickRate);
        await server.WaitRunTicks(sTimeToWait);

        await server.WaitAssertion(() =>
        {
            var grid = entityManager.GetComponent<MapGridComponent>(gridId);
            var puddle = GetPuddleEntity(entityManager, (gridId, grid), puddleOrigin);

#pragma warning disable NUnit2045 // Interdependent tests
            Assert.That(puddle, Is.Not.Null);
            Assert.That(puddleSystem.CurrentVolume(puddle!.Value.Owner, puddle), Is.EqualTo(FixedPoint2.New(100)));
#pragma warning restore NUnit2045

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    if (x == 0 && y == 0 || x == 0 && y == 1 || x == 1 && y == 0)
                    {
                        continue;
                    }

                    var newPos = new Vector2i(x, y);
                    var sidePuddle = GetPuddle(entityManager, (gridId, grid), newPos);
                    Assert.That(sidePuddle, Is.Null);
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
