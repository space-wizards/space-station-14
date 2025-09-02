using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
[TestOf(typeof(Atmospherics))]
public sealed class GasArrayTest
{
    private const string GasTankTestDummyId = "GasTankTestDummy";

    private const string GasTankLegacyTestDummyId = "GasTankLegacyTestDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {GasTankTestDummyId}
  components:
  - type: GasTank
    air:
      volume: 5
      moles:
        Oxygen: 10
        Nitrogen: 20

- type: entity
  id: {GasTankLegacyTestDummyId}
  components:
  - type: GasTank
    air:
      volume: 5
      moles:
      - 0
      - 0
      - 0
      - 10
";

    [Test]
    public async Task TestGasArrayDeserialization()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var compFactory = server.ResolveDependency<IComponentFactory>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var gasTank = prototypeManager.Index(GasTankTestDummyId);
            Assert.Multiple(() =>
            {
                Assert.That(gasTank.TryGetComponent<GasTankComponent>(out var gasTankComponent, compFactory));

                Assert.That(gasTankComponent!.Air.GetMoles(Gas.Oxygen), Is.EqualTo(10));
                Assert.That(gasTankComponent!.Air.GetMoles(Gas.Nitrogen), Is.EqualTo(20));
                for (var i = 3; i < Atmospherics.AdjustedNumberOfGases; i++)
                {
                    Assert.That(gasTankComponent!.Air.GetMoles(i), Is.EqualTo(0));
                }
            });

            var legacyGasTank = prototypeManager.Index(GasTankLegacyTestDummyId);
            Assert.Multiple(() =>
            {
                Assert.That(legacyGasTank.TryGetComponent<GasTankComponent>(out var gasTankComponent, compFactory));

                Assert.That(gasTankComponent!.Air.GetMoles(Gas.Plasma), Is.EqualTo(10));

                // Iterate through all other gases: check for 0 values
                foreach (var gas in Enum.GetValues<Gas>().Where(p => p != Gas.Plasma))
                {
                    Assert.That(gasTankComponent!.Air.GetMoles(gas), Is.EqualTo(0));
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}
