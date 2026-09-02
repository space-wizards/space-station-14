using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
[TestOf(typeof(SharedAtmosphereSystem))]
public sealed class GasArrayTest : GameTest
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

    [SidedDependency(Side.Server)] private readonly IComponentFactory _compFactory = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void TestGasArrayDeserialization()
    {
        var gasTank = SProtoMan.Index(GasTankTestDummyId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(gasTank.TryGetComponent<GasTankComponent>(out var gasTankComponent, _compFactory));

            Assert.That(gasTankComponent!.Air.GetMoles(Gas.Oxygen), Is.EqualTo(10));
            Assert.That(gasTankComponent!.Air.GetMoles(Gas.Frezon), Is.EqualTo(20));
            foreach (var gas in Enum.GetValues<Gas>().Where(p => p != Gas.Oxygen && p != Gas.Frezon))
            {
                Assert.That(gasTankComponent!.Air.GetMoles(gas), Is.Zero);
            }
        }

        var legacyGasTank = SProtoMan.Index(GasTankLegacyTestDummyId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(legacyGasTank.TryGetComponent<GasTankComponent>(out var gasTankComponent, _compFactory));

            Assert.That(gasTankComponent!.Air.GetMoles(3), Is.EqualTo(10));

            // Iterate through all other gases: check for 0 values
            for (var i = 0; i < Atmospherics.AdjustedNumberOfGases; i++)
            {
                if (i == 3) // our case with a value.
                    continue;

                Assert.That(gasTankComponent!.Air.GetMoles(i), Is.Zero);
            }
        }
    }
}
