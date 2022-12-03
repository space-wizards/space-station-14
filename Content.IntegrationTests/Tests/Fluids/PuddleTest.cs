using System;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids
{
    [TestFixture]
    [TestOf(typeof(PuddleComponent))]
    public sealed class PuddleTest
    {
        [Test]
        public async Task TilePuddleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var spillSystem = entitySystemManager.GetEntitySystem<SpillableSystem>();

            await server.WaitAssertion(() =>
            {
                var solution = new Solution("Water", FixedPoint2.New(20));
                var tile = testMap.Tile;
                var gridUid = tile.GridUid;
                var (x, y) = tile.GridIndices;
                var coordinates = new EntityCoordinates(gridUid, x, y);
                var puddle = spillSystem.SpillAt(solution, coordinates, "PuddleSmear");

                Assert.NotNull(puddle);
            });
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task SpaceNoPuddleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var spillSystem = entitySystemManager.GetEntitySystem<SpillableSystem>();

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
                var puddle = spillSystem.SpillAt(solution, coordinates, "PuddleSmear");
                Assert.Null(puddle);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task PuddlePauseTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var metaSystem = entityManager.EntitySysManager.GetEntitySystem<MetaDataSystem>();

            MapId sMapId = default;
            MapGridComponent sGrid;
            EntityUid sGridId = default;
            EntityCoordinates sCoordinates = default;

            // Spawn a paused map with one tile to spawn puddles on
            await server.WaitPost(() =>
            {
                sMapId = sMapManager.CreateMap();
                sMapManager.SetMapPaused(sMapId, true);
                sGrid = sMapManager.CreateGrid(sMapId);
                sGridId = sGrid.GridEntityId;
                metaSystem.SetEntityPaused(sGridId, true); // See https://github.com/space-wizards/RobustToolbox/issues/1444

                var tileDefinition = sTileDefinitionManager["UnderPlating"];
                var tile = new Tile(tileDefinition.TileId);
                sCoordinates = sGrid.ToCoordinates();

                sGrid.SetTile(sCoordinates, tile);
            });

            // Check that the map and grid are paused
            await server.WaitAssertion(() =>
            {
                Assert.True(metaSystem.EntityPaused(sGridId));
                Assert.True(sMapManager.IsMapPaused(sMapId));
            });

            float evaporateTime = default;
            PuddleComponent puddle = null;
            MetaDataComponent meta = null;
            EvaporationComponent evaporation;

            var amount = 2;

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var spillSystem = entitySystemManager.GetEntitySystem<SpillableSystem>();

            // Spawn a puddle
            await server.WaitAssertion(() =>
            {
                var solution = new Solution("Water", FixedPoint2.New(amount));
                puddle = spillSystem.SpillAt(solution, sCoordinates, "PuddleSmear");
                meta = entityManager.GetComponent<MetaDataComponent>(puddle.Owner);

                // Check that the puddle was created
                Assert.NotNull(puddle);

                evaporation = entityManager.GetComponent<EvaporationComponent>(puddle.Owner);
                metaSystem.SetEntityPaused(puddle.Owner, true, meta); // See https://github.com/space-wizards/RobustToolbox/issues/1445

                Assert.True(metaSystem.EntityPaused(puddle.Owner, meta));

                // Check that the puddle is going to evaporate
                Assert.Positive(evaporation.EvaporateTime);

                // Should have a timer component added to it for evaporation
                Assert.That(evaporation.Accumulator, Is.EqualTo(0f));

                evaporateTime = evaporation.EvaporateTime;
            });

            // Wait enough time for it to evaporate if it was unpaused
            var sTimeToWait = 5 + (int)Math.Ceiling(amount * evaporateTime * sGameTiming.TickRate);
            await PoolManager.RunTicksSync(pairTracker.Pair, sTimeToWait);

            // No evaporation due to being paused
            await server.WaitAssertion(() =>
            {
                Assert.True(meta.EntityPaused);

                // Check that the puddle still exists
                Assert.False(meta.EntityDeleted);
            });

            // Unpause the map
            await server.WaitPost(() => { sMapManager.SetMapPaused(sMapId, false); });

            // Check that the map, grid and puddle are unpaused
            await server.WaitAssertion(() =>
            {
                Assert.False(sMapManager.IsMapPaused(sMapId));
                Assert.False(metaSystem.EntityPaused(sGridId));
                Assert.False(meta.EntityPaused);

                // Check that the puddle still exists
                Assert.False(meta.EntityDeleted);
            });

            // Wait enough time for it to evaporate
            await PoolManager.RunTicksSync(pairTracker.Pair, sTimeToWait);

            // Puddle evaporation should have ticked
            await server.WaitAssertion(() =>
            {
                // Check that puddle has been deleted
                Assert.True(puddle.Deleted);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
