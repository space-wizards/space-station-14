using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using NUnit.Framework;
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            await server.WaitPost(() =>
            {
                var atmosSystem = EntitySystem.Get<AtmosphereSystem>();

                Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases));

                Assert.That(Enum.GetValues(typeof(Gas)).Length, Is.EqualTo(Atmospherics.TotalNumberOfGases));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
