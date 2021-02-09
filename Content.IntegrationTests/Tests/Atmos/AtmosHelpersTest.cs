using System.Threading.Tasks;
using Content.Server.Atmos;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(AtmosHelpersTest))]
    public class AtmosHelpersTest : ContentIntegrationTest
    {
        [Test]
        public async Task GetTileAtmosphereEntityCoordinatesNotNullTest()
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

                    Assert.NotNull(atmosphere1);
                    Assert.NotNull(atmosphere2);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task GetTileAirEntityCoordinatesNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var air = default(EntityCoordinates).GetTileAir();

                    Assert.NotNull(air);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileAtmosphereEntityCoordinatesNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAtmosphere = default(EntityCoordinates).TryGetTileAtmosphere(out var atmosphere);

                    Assert.True(hasAtmosphere);
                    Assert.NotNull(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TryGetTileTileAirEntityCoordinatesNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAir = default(EntityCoordinates).TryGetTileAir(out var air);

                    Assert.True(hasAir);
                    Assert.NotNull(air);
                });
            });

            await server.WaitIdleAsync();
        }

        // ReSharper disable once InconsistentNaming
        [Test]
        public async Task GetTileAtmosphereVector2iNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var atmosphere = default(Vector2i).GetTileAtmosphere(default);

                    Assert.NotNull(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        // ReSharper disable once InconsistentNaming
        [Test]
        public async Task GetTileAirVector2iNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var air = default(Vector2i).GetTileAir(default);

                    Assert.NotNull(air);
                });
            });

            await server.WaitIdleAsync();
        }

        // ReSharper disable once InconsistentNaming
        [Test]
        public async Task TryGetTileAtmosphereVector2iNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAtmosphere = default(Vector2i).TryGetTileAtmosphere(default, out var atmosphere);

                    Assert.True(hasAtmosphere);
                    Assert.NotNull(atmosphere);
                });
            });

            await server.WaitIdleAsync();
        }

        // ReSharper disable once InconsistentNaming
        [Test]
        public async Task TryGetTileAirVector2iNotNullTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    var hasAir = default(Vector2i).TryGetTileAir(default, out var air);

                    Assert.True(hasAir);
                    Assert.NotNull(air);
                });
            });

            await server.WaitIdleAsync();
        }
    }
}
