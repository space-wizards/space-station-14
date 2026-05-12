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
        Frezon: 20
        Oxygen: 10

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
                Assert.That(gasTankComponent!.Air.GetMoles(Gas.Frezon), Is.EqualTo(20));
                foreach (var gas in Enum.GetValues<Gas>().Where(p => p != Gas.Oxygen && p != Gas.Frezon))
                {
                    Assert.That(gasTankComponent!.Air.GetMoles(gas), Is.EqualTo(0));
                }
            });

            var legacyGasTank = prototypeManager.Index(GasTankLegacyTestDummyId);
            Assert.Multiple(() =>
            {
                Assert.That(legacyGasTank.TryGetComponent<GasTankComponent>(out var gasTankComponent, compFactory));

                Assert.That(gasTankComponent!.Air.GetMoles(3), Is.EqualTo(10));

                // Iterate through all other gases: check for 0 values
                for (var i = 0; i < Atmospherics.AdjustedNumberOfGases; i++)
                {
                    if (i == 3) // our case with a value.
                        continue;

                    Assert.That(gasTankComponent!.Air.GetMoles(i), Is.EqualTo(0));
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}
