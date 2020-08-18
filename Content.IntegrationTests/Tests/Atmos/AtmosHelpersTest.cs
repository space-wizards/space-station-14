using System.Threading.Tasks;
using Content.Server.Atmos;
using NUnit.Framework;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(AtmosHelpersTest))]
    public class AtmosHelpersTest : ContentIntegrationTest
    {
        [Test]
        public async Task GetTileAtmosphereGridCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var atmosphere = default(GridCoordinates).GetTileAtmosphere();

                    Assert.Null(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task GetTileAirGridCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var air = default(GridCoordinates).GetTileAir();

                    Assert.Null(air);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileAtmosphereGridCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAtmosphere = default(GridCoordinates).TryGetTileAtmosphere(out var atmosphere);

                    Assert.False(hasAtmosphere);
                    Assert.Null(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileTileAirGridCoordinatesNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAir = default(GridCoordinates).TryGetTileAir(out var air);

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
