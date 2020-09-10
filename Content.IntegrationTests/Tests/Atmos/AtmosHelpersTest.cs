using System.Threading.Tasks;
using Content.Server.Atmos;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(AtmosHelpersTest))]
    public class AtmosHelpersTest : ContentIntegrationTest
    {
        [Test]
        public async Task GetTileAtmosphereEntityCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var atmosphere1 = default(EntityCoordinates).GetTileAtmosphere();
                    var atmosphere2 = default(EntityCoordinates).GetTileAtmosphere(entityManager);

                    Assert.Null(atmosphere1);
                    Assert.Null(atmosphere2);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task GetTileAirEntityCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var air = default(EntityCoordinates).GetTileAir();

                    Assert.Null(air);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileAtmosphereEntityCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAtmosphere = default(EntityCoordinates).TryGetTileAtmosphere(out var atmosphere);

                    Assert.False(hasAtmosphere);
                    Assert.Null(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileTileAirEntityCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAir = default(EntityCoordinates).TryGetTileAir(out var air);

                    Assert.False(hasAir);
                    Assert.Null(air);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task GetTileAtmosphereMapIndicesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var atmosphere = default(MapIndices).GetTileAtmosphere(default);

                    Assert.Null(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task GetTileAirMapIndicesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var air = default(MapIndices).GetTileAir(default);

                    Assert.Null(air);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileAtmosphereMapIndicesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAtmosphere = default(MapIndices).TryGetTileAtmosphere(default, out var atmosphere);

                    Assert.False(hasAtmosphere);
                    Assert.Null(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileAirMapIndicesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAir = default(MapIndices).TryGetTileAir(default, out var air);

                    Assert.False(hasAir);
                    Assert.Null(air);
                });
            });

            await server.WaitIdleAsync();
        }
    }
}
