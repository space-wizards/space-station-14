using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(GasMixture))]
    public sealed class GasMixtureTest
    {
        [Test]
        public async Task TestMerge()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var atmosphereSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AtmosphereSystem>();

            await server.WaitAssertion(() =>
            {
                var a = new GasMixture(10f);
                var b = new GasMixture(10f);

                a.AdjustMoles(Gas.Oxygen, 50);
                b.AdjustMoles(Gas.Nitrogen, 50);

                // a now has 50 moles of oxygen
                Assert.That(a.TotalMoles, Is.EqualTo(50));
                Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));

                // b now has 50 moles of nitrogen
                Assert.That(b.TotalMoles, Is.EqualTo(50));
                Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));

                atmosphereSystem.Merge(b, a);

                // b now has its contents and the contents of a
                Assert.That(b.TotalMoles, Is.EqualTo(100));
                Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(50));
                Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));

                // a should be the same, however.
                Assert.That(a.TotalMoles, Is.EqualTo(50));
                Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        [TestCase(0.5f)]
        [TestCase(0.25f)]
        [TestCase(0.75f)]
        [TestCase(1f)]
        [TestCase(0f)]
        [TestCase(Atmospherics.BreathPercentage)]
        public async Task RemoveRatio(float ratio)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            await server.WaitAssertion(() =>
            {
                var a = new GasMixture(10f);

                a.AdjustMoles(Gas.Oxygen, 100);
                a.AdjustMoles(Gas.Nitrogen, 100);

                var origTotal = a.TotalMoles;

                // we remove moles from the mixture with a ratio.
                var b = a.RemoveRatio(ratio);

                // check that the amount of moles in the original and the new mixture are correct.
                Assert.That(b.TotalMoles, Is.EqualTo(origTotal * ratio));
                Assert.That(a.TotalMoles, Is.EqualTo(origTotal - b.TotalMoles));

                Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(100 * ratio));
                Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(100 * ratio));

                Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(100 - b.GetMoles(Gas.Oxygen)));
                Assert.That(a.GetMoles(Gas.Nitrogen), Is.EqualTo(100 - b.GetMoles(Gas.Nitrogen)));
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
