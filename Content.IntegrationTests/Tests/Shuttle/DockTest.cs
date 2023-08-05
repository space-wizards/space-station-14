using System.Collections.Generic;
using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Shuttle;

public sealed class DockTest : ContentUnitTest
{
    private static IEnumerable<object[]> TestSource()
    {
        // I-shape for grid1, T-shape for grid2
        yield return new object[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Angle.Zero, Angle.Zero, true };
        yield return new object[] { new Vector2(0.5f, 1.5f), new Vector2(0.5f, 1.5f), Angle.Zero, Angle.Zero, false };
    }

    [Test]
    [TestCaseSource(nameof(TestSource))]
    public async Task TestDockingConfig(Vector2 dock1Pos, Vector2 dock2Pos, Angle dock1Angle, Angle dock2Angle, bool result)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Pair.Server;

        var map = await PoolManager.CreateTestMap(pair);

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var dockingSystem = entManager.System<DockingSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var mapId = map.MapId;

        await server.WaitAssertion(() =>
        {
            entManager.DeleteEntity(map.GridUid);
            var grid1 = mapManager.CreateGrid(mapId);
            var grid2 = mapManager.CreateGrid(mapId);
            var grid1Ent = grid1.Owner;
            var grid2Ent = grid2.Owner;
            var grid2Offset = new Vector2(50f, 50f);
            xformSystem.SetLocalPosition(grid2Ent, grid2Offset);

            // Tetris tests
            // Grid1 is a vertical I
            // Grid2 is a T

            var tiles1 = new List<(Vector2i Index, Tile Tile)>()
            {
                new(new Vector2i(0, 0), new Tile(1)),
                new(new Vector2i(0, 1), new Tile(1)),
                new(new Vector2i(0, 2), new Tile(1)),
            };

            grid1.SetTiles(tiles1);
            var dock1 = entManager.SpawnEntity("AirlockShuttle", new EntityCoordinates(grid1Ent, dock1Pos));
            var dock1Xform = entManager.GetComponent<TransformComponent>(dock1);
            dock1Xform.LocalRotation = dock1Angle;

            var tiles2 = new List<(Vector2i Index, Tile Tile)>()
            {
                new(new Vector2i(0, 0), new Tile(1)),
                new(new Vector2i(0, 1), new Tile(1)),
                new(new Vector2i(0, 2), new Tile(1)),
                new(new Vector2i(-1, 2), new Tile(1)),
                new(new Vector2i(1, 2), new Tile(1)),
            };

            grid2.SetTiles(tiles2);
            var dock2 = entManager.SpawnEntity("AirlockShuttle", new EntityCoordinates(grid2Ent, dock2Pos));
            var dock2Xform = entManager.GetComponent<TransformComponent>(dock2);
            dock2Xform.LocalRotation = dock2Angle;

            var config = dockingSystem.GetDockingConfig(grid1Ent, grid2Ent);

            Assert.That(result, Is.EqualTo(config != null));
        });

        await pair.CleanReturnAsync();
    }
}
