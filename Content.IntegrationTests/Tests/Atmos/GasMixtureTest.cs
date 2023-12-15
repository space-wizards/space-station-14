using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var atmosphereSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AtmosphereSystem>();

            await server.WaitAssertion(() =>
            {
                var a = new GasMixture(10f);
                var b = new GasMixture(10f);

                a.AdjustMoles(Gas.Oxygen, 50);
                b.AdjustMoles(Gas.Nitrogen, 50);

                // a now has 50 moles of oxygen
                Assert.Multiple(() =>
                {
                    Assert.That(a.TotalMoles, Is.EqualTo(50));
                    Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));
                });

                // b now has 50 moles of nitrogen
                Assert.Multiple(() =>
                {
                    Assert.That(b.TotalMoles, Is.EqualTo(50));
                    Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));
                });

                atmosphereSystem.Merge(b, a);

                // b now has its contents and the contents of a
                Assert.Multiple(() =>
                {
                    Assert.That(b.TotalMoles, Is.EqualTo(100));
                    Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(50));
                    Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));
                });

                // a should be the same, however.
                Assert.Multiple(() =>
                {
                    Assert.That(a.TotalMoles, Is.EqualTo(50));
                    Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));
                });
            });

            await pair.CleanReturnAsync();
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var a = new GasMixture(10f);

                a.AdjustMoles(Gas.Oxygen, 100);
                a.AdjustMoles(Gas.Nitrogen, 100);

                var origTotal = a.TotalMoles;

                // we remove moles from the mixture with a ratio.
                var b = a.RemoveRatio(ratio);

                // check that the amount of moles in the original and the new mixture are correct.
                Assert.Multiple(() =>
                {
                    Assert.That(b.TotalMoles, Is.EqualTo(origTotal * ratio));
                    Assert.That(a.TotalMoles, Is.EqualTo(origTotal - b.TotalMoles));
                });

                Assert.Multiple(() =>
                {
                    Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(100 * ratio));
                    Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(100 * ratio));
                });

                Assert.Multiple(() =>
                {
                    Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(100 - b.GetMoles(Gas.Oxygen)));
                    Assert.That(a.GetMoles(Gas.Nitrogen), Is.EqualTo(100 - b.GetMoles(Gas.Nitrogen)));
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
