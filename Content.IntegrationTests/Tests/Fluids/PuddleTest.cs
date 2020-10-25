using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Chemistry;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Fluids
{
    [TestFixture]
    [TestOf(typeof(PuddleComponent))]
    public class PuddleTest : ContentIntegrationTest
    {
        [Test]
        public async Task TilePuddleTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            EntityCoordinates coordinates = default;

            // Build up test environment
            server.Post(() =>
            {
                // Create a one tile grid to spill onto
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                var solution = new Solution("water", ReagentUnit.New(20));
                var puddle = solution.SpillAt(coordinates, "PuddleSmear");
                Assert.NotNull(puddle);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task SpaceNoPuddleTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();
            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            IMapGrid grid = null;

            // Build up test environment
            server.Post(() =>
            {
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                var gridId = new GridId(1);
                var coordinates = grid.ToCoordinates();
                var solution = new Solution("water", ReagentUnit.New(20));
                var puddle = solution.SpillAt(coordinates, "PuddleSmear");
                Assert.Null(puddle);
            });

            await server.WaitIdleAsync();
        }
    }
}
