using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.Fluids
{
    [TestFixture]
    [TestOf(typeof(PuddleComponent))]
    public sealed class PuddleTest
    {
        [Test]
        public async Task TilePuddleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var spillSystem = entitySystemManager.GetEntitySystem<PuddleSystem>();

            await server.WaitAssertion(() =>
            {
                var solution = new Solution("Water", FixedPoint2.New(20));
                var tile = testMap.Tile;
                var gridUid = tile.GridUid;
                var (x, y) = tile.GridIndices;
                var coordinates = new EntityCoordinates(gridUid, x, y);

                Assert.That(spillSystem.TrySpillAt(coordinates, solution, out _), Is.True);
            });
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task SpaceNoPuddleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var spillSystem = entitySystemManager.GetEntitySystem<PuddleSystem>();

            MapGridComponent grid = null;

            // Remove all tiles
            await server.WaitPost(() =>
            {
                grid = testMap.MapGrid;

                foreach (var tile in grid.GetAllTiles())
                {
                    grid.SetTile(tile.GridIndices, Tile.Empty);
                }
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitAssertion(() =>
            {
                var coordinates = grid.ToCoordinates();
                var solution = new Solution("Water", FixedPoint2.New(20));

                Assert.That(spillSystem.TrySpillAt(coordinates, solution, out _), Is.False);
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
