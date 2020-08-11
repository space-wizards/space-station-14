using System;
using System.Linq;
using Content.Shared.Atmos;
using NUnit.Framework;

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

            server.Post(() =>
            {
                Assert.That(Atmospherics.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases));

                Assert.That(Enum.GetValues(typeof(Gas)).Length, Is.EqualTo(Atmospherics.TotalNumberOfGases));
            });
        }
    }
}
