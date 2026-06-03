#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Fluids;

[TestOf(typeof(SpreaderSystem))]
public sealed class FluidSpill : GameTest
{
    private static readonly EntProtoId WallReinforced = "WallReinforced";
    private static readonly ProtoId<ReagentPrototype> SpilledReagent = "Blood";

    [SidedDependency(Side.Server)] private PuddleSystem _sPuddleSystem = null!;
    [SidedDependency(Side.Server)] private IMapManager _sMapManager = null!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;

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
        EntityUid gridId = default;
        MapId mapId = default;

        /*
         In this test, if o is spillage puddle and # are walls, we want to ensure all tiles are empty (`.`)
            . . .
            # . .
            o # .
        */
        await Server.WaitPost(() =>
        {
            _sMapSystem.CreateMap(out mapId);
            var grid = _sMapManager.CreateGridEntity(mapId);
            gridId = grid.Owner;

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    _sMapSystem.SetTile(grid, new Vector2i(x, y), new Tile(1));
                }
            }

            SSpawnAtPosition(WallReinforced, _sMapSystem.GridTileToLocal(grid, grid.Comp, new Vector2i(0, 1)));
            SSpawnAtPosition(WallReinforced, _sMapSystem.GridTileToLocal(grid, grid.Comp, new Vector2i(1, 0)));
        });

        var puddleOrigin = new Vector2i(0, 0);
        await Server.WaitAssertion(() =>
        {
            var grid = SComp<MapGridComponent>(gridId);
            var solution = new Solution(SpilledReagent, FixedPoint2.New(100));
            var tileRef = _sMapSystem.GetTileRef(gridId, grid, puddleOrigin);
#pragma warning disable NUnit2045 // Interdependent tests
            Assert.That(_sPuddleSystem.TrySpillAt(tileRef, solution, out _), Is.True);
            Assert.That(GetPuddle(SEntMan, (gridId, grid), puddleOrigin), Is.Not.Null);
#pragma warning restore NUnit2045
        });

        var sTimeToWait = (int)Math.Ceiling(2f * SGameTiming.TickRate);
        await RunTicksSync(sTimeToWait);

        await Server.WaitAssertion(() =>
        {
            var grid = SComp<MapGridComponent>(gridId);
            var puddle = GetPuddleEntity(SEntMan, (gridId, grid), puddleOrigin);

#pragma warning disable NUnit2045 // Interdependent tests
            Assert.That(puddle, Is.Not.Null);
            Assert.That(_sPuddleSystem.CurrentVolume(puddle!.Value.Owner, puddle), Is.EqualTo(FixedPoint2.New(100)));
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
                    var sidePuddle = GetPuddle(SEntMan, (gridId, grid), newPos);
                    Assert.That(sidePuddle, Is.Null);
                }
            }

            // Cleanup
            _sMapSystem.DeleteMap(mapId);
        });
    }
}
