using System.Linq;
using System.Threading.Tasks;
using Content.Server.AI.Pathfinding;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Pathfinding
{
    [TestFixture]
    [TestOf(typeof(PathfindingChunk))]
    public class PathfindingChunkTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServer();

            server.Assert(() =>
            {
                var pathfindingSystem = EntitySystem.Get<PathfindingSystem>();
                var mapMan = IoCManager.Resolve<IMapManager>();

                // Setup
                var grid = GetMainGrid(mapMan);
                var chunkTile = grid.GetTileRef(new Vector2i(0, 0));
                var chunk = pathfindingSystem.GetChunk(chunkTile);
                Assert.That(chunk.Nodes.Length == PathfindingChunk.ChunkSize * PathfindingChunk.ChunkSize);

                // Neighbors
                var chunkNeighbors = chunk.GetNeighbors().ToList();
                Assert.That(chunkNeighbors.Count == 0);
                var neighborChunkTile = grid.GetTileRef(new Vector2i(PathfindingChunk.ChunkSize, PathfindingChunk.ChunkSize));
                var neighborChunk = pathfindingSystem.GetChunk(neighborChunkTile);
                chunkNeighbors = chunk.GetNeighbors().ToList();
                Assert.That(chunkNeighbors.Count == 1);

                // Directions
                Assert.That(PathfindingHelpers.RelativeDirection(neighborChunk, chunk) == Direction.NorthEast);
                Assert.That(PathfindingHelpers.RelativeDirection(chunk.Nodes[0, 1], chunk.Nodes[0, 0]) == Direction.North);

                // Nodes
                var node = chunk.Nodes[1, 1];
                var nodeNeighbors = node.GetNeighbors().ToList();
                Assert.That(nodeNeighbors.Count == 8);

                // Bottom-left corner with no chunk neighbor
                node = chunk.Nodes[0, 0];
                nodeNeighbors = node.GetNeighbors().ToList();
                Assert.That(nodeNeighbors.Count == 3);

                // Given we have 1 NE neighbor then NE corner should have 4 neighbors due to the 1 extra from the neighbor chunk
                node = chunk.Nodes[PathfindingChunk.ChunkSize - 1, PathfindingChunk.ChunkSize - 1];
                nodeNeighbors = node.GetNeighbors().ToList();
                Assert.That(nodeNeighbors.Count == 4);
            });
            await server.WaitIdleAsync();
        }
    }
}
