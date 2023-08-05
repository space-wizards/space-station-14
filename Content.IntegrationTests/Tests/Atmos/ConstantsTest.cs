using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(Atmospherics))]
    public sealed class ConstantsTest
    {
        [Test]
        public async Task TotalGasesTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitPost(() =>
            {
                var atmosSystem = entityManager.System<AtmosphereSystem>();

                Assert.Multiple(() =>
                {
                    Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases));
                    Assert.That(Enum.GetValues(typeof(Gas)), Has.Length.EqualTo(Atmospherics.TotalNumberOfGases));
                });
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
