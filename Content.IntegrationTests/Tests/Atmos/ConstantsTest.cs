using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(Atmospherics))]
    public class ConstantsTest : ContentIntegrationTest
    {
        [Test]
        public async Task TotalGasesTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var atmosSystem = server.ResolveDependency<AtmosphereSystem>();

            server.Post(() =>
            {
                Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases));

                Assert.That(Enum.GetValues(typeof(Gas)).Length, Is.EqualTo(Atmospherics.TotalNumberOfGases));
            });
        }
    }
}
