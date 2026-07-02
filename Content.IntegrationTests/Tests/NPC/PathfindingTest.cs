using Content.IntegrationTests.Fixtures;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.NPC;

[TestFixture]
public sealed class PathfindingTest : GameTest
{
    [Test]
    public async Task DetectsWalls()
    {
        var pair = Pair;
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var mapId = new MapId();
        Entity<MapGridComponent> grid = default;

        await server.WaitPost(() =>
        {
            mapSystem.CreateMap(out mapId);
            grid = mapSystem.CreateGridEntity(mapId);

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

        var pathfinding = entityManager.System<PathfindingSystem>();

        var chunk = new GridPathfindingChunk
        {
            Origin = new Vector2i(0, 0)
        };

        pathfinding.BuildBreadcrumbs(chunk, grid);
        Assert.That(chunk.BufferPolygons[0][0].Data.IsFreeSpace); // Empty Space
        Assert.That(!chunk.BufferPolygons[1][0].Data.IsFreeSpace); // A wall
    }
}
