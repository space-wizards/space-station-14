using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Gravity;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(WeightlessSystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public class WeightlessStatusTests : ContentIntegrationTest
    {
        [Test]
        public async Task WeightlessStatusTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IEntity human = null;
            SharedStatusEffectsComponent statusEffects = null;

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);

                human = entityManager.SpawnEntity("HumanMob_Content", coordinates);

                Assert.True(human.TryGetComponent(out statusEffects));
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            GravityGeneratorComponent gravityGenerator = null;

            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.True(statusEffects.Statuses.ContainsKey(StatusEffect.Weightless));

                gravityGenerator = human.EnsureComponent<GravityGeneratorComponent>();
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(statusEffects.Statuses.ContainsKey(StatusEffect.Weightless));

                // Disable the gravity generator
                var args = new BreakageEventArgs {Owner = human};
                gravityGenerator.OnBreak(args);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(statusEffects.Statuses.ContainsKey(StatusEffect.Weightless));
            });
        }
    }
}
