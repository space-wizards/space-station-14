using System;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids
{
    [TestFixture]
    [TestOf(typeof(EvaporationComponent))]
    public class EvaporationTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: puddleLower
  id: PuddleLower
  description: A puddle of liquid. LowerBound
  components:
  - type: Puddle
  - type: Evaporation
    evaporateTime: 1
    lowerLimit: 2

- type: entity
  name: puddleUpper
  id: PuddleUpper
  description: A puddle of liquid. UpperBound
  components:
  - type: Puddle
  - type: Evaporation
    evaporateTime: 1
    upperLimit: 2
";

        [Test]
        public async Task EvaporatePuddleLowerLimit()
        {
            var options = new ServerIntegrationOptions { ExtraPrototypes = Prototypes };
            var server = StartServerDummyTicker(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var tileManager = server.ResolveDependency<ITileDefinitionManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var gameTiming = server.ResolveDependency<IGameTiming>();
            var puddleSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PuddleSystem>();

            PuddleComponent puddle = null;
            EvaporationComponent evaporationComponent;
            float evaporateTime = 4;
            float lowerBound = 0;

            MapId mapId = default;
            GridId gridId = default;
            IMapGrid mapGrid;
            IEntity gridEntity = null;
            EntityCoordinates coordinates = default;

            // Spawn a paused map with one tile to spawn puddles on
            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                pauseManager.SetMapPaused(mapId, true);
                mapGrid = mapManager.CreateGrid(mapId);
                gridEntity = entityManager.GetEntity(mapGrid.GridEntityId);
                gridEntity.Paused = true; 
                gridId = mapGrid.Index;

                var tileDefinition = tileManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                coordinates = mapGrid.ToCoordinates();

                mapGrid.SetTile(coordinates, tile);
            });
            
            // Check that the map and grid are paused
            await server.WaitAssertion(() =>
            {
                Assert.True(pauseManager.IsGridPaused(gridId));
                Assert.True(pauseManager.IsMapPaused(mapId));
                Assert.True(gridEntity.Paused);
            });

            await server.WaitAssertion(() =>
            {
                var solution = new Solution("water", ReagentUnit.New(evaporateTime));
                puddle = puddleSystem.SpillAt(solution, coordinates, "PuddleLower");
                evaporationComponent = puddle?.Owner.GetComponent<EvaporationComponent>();
                
                // Check that the puddle was created
                Assert.NotNull(puddle);
                Assert.NotNull(evaporationComponent);

                lowerBound = evaporationComponent.LowerLimit.Float();

                // Check that the puddle is going to evaporate
                Assert.Positive(evaporationComponent.EvaporateTime);

                // Should be properly initialized
                Assert.That(evaporationComponent.Accumulator, Is.EqualTo(0f));
            });
            
            // Unpause the map
            await server.WaitPost(() =>
            {
                pauseManager.SetMapPaused(mapId, false);
            });

            var sTimeToWait = (int)Math.Ceiling(evaporateTime * gameTiming.TickRate);
            await server.WaitRunTicks(sTimeToWait);

            // Wait enough time for it to theoretically evaporate
            await server.WaitAssertion(() =>
            {
                // By now, the puddle shouldn't have 
                Assert.False(puddle.Owner.Paused);

                // Check that the puddle still exists
                Assert.False(puddle.Owner.Deleted);
                
                // Check that the puddle still exists and is on lowerBound
                Assert.That(puddle.CurrentVolume.Float(), Is.EqualTo(lowerBound));
            });
        }

        [Test]
        public async Task EvaporatePuddleUpperLimit()
        {
                        var options = new ServerIntegrationOptions { ExtraPrototypes = Prototypes };
            var server = StartServerDummyTicker(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var tileManager = server.ResolveDependency<ITileDefinitionManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var gameTiming = server.ResolveDependency<IGameTiming>();
            var puddleSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PuddleSystem>();

            PuddleComponent puddle = null;
            EvaporationComponent evaporationComponent;
            float evaporateTime = 4;
            float upperBound = 0;

            MapId mapId = default;
            GridId gridId = default;
            IMapGrid mapGrid;
            IEntity gridEntity = null;
            EntityCoordinates coordinates = default;

            // Spawn a paused map with one tile to spawn puddles on
            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                pauseManager.SetMapPaused(mapId, true);
                mapGrid = mapManager.CreateGrid(mapId);
                gridEntity = entityManager.GetEntity(mapGrid.GridEntityId);
                gridEntity.Paused = true; 
                gridId = mapGrid.Index;

                var tileDefinition = tileManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                coordinates = mapGrid.ToCoordinates();

                mapGrid.SetTile(coordinates, tile);
            });
            
            // Check that the map and grid are paused
            await server.WaitAssertion(() =>
            {
                Assert.True(pauseManager.IsGridPaused(gridId));
                Assert.True(pauseManager.IsMapPaused(mapId));
                Assert.True(gridEntity.Paused);
            });

            await server.WaitAssertion(() =>
            {
                var solution = new Solution("water", ReagentUnit.New(evaporateTime));
                puddle = puddleSystem.SpillAt(solution, coordinates, "PuddleUpper");
                evaporationComponent = puddle?.Owner.GetComponent<EvaporationComponent>();
                
                // Check that the puddle was created
                Assert.NotNull(puddle);
                Assert.NotNull(evaporationComponent);

                // Check that the puddle is going to evaporate
                Assert.Positive(evaporationComponent.EvaporateTime);

                upperBound = evaporationComponent.UpperLimit.Float();

                // Should be properly initialized
                Assert.That(evaporationComponent.Accumulator, Is.EqualTo(0f));
            });
            
            // Unpause the map
            await server.WaitPost(() =>
            {
                pauseManager.SetMapPaused(mapId, false);
            });
            
            var sTimeToWait = (int)Math.Ceiling(3f * gameTiming.TickRate);

            // Wait enough time for it to theoretically evaporate
            await server.WaitRunTicks(sTimeToWait);

            // It should hit lower limit
            await server.WaitAssertion(() =>
            {
                // By now, the puddle shouldn't have 
                Assert.False(puddle.Owner.Paused);

                // Check that the puddle still exists
                Assert.False(puddle.Owner.Deleted);
                
                // Check that the puddle still exists
                Assert.That(puddle.CurrentVolume.Float(), Is.GreaterThanOrEqualTo(upperBound));
            });
        }
    }
}