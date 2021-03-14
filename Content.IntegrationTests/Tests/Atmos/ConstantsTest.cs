using System;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(Atmospherics))]
    public class ConstantsTest : ContentIntegrationTest
    {
        [Test]
        public void TotalGasesTest()
        {
            var server = StartServerDummyTicker();

            server.WaitIdleAsync();

            server.Post(() =>
            {
                var atmosSystem = EntitySystem.Get<AtmosphereSystem>();

                Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases));

                Assert.That(Enum.GetValues(typeof(Gas)).Length, Is.EqualTo(Atmospherics.TotalNumberOfGases));
            });
        }
    }
}
