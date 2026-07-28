using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(Atmospherics))]
public sealed class ConstantsTest : AtmosTest
{
    [SidedDependency(Side.Server)] private readonly IPrototypeManager _protoManager = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void TotalGasesTest()
    {
        using (Assert.EnterMultipleScope())
        {
            // adding new gases needs a few changes in the code, so make sure this is done everywhere
            var gasProtos = _protoManager.EnumeratePrototypes<GasPrototype>().ToList();

            // number of gas prototypes
            Assert.That(gasProtos,
                Has.Count.EqualTo(Atmospherics.TotalNumberOfGases),
                $"Number of GasPrototypes is not equal to TotalNumberOfGases.");
            // number of gas prototypes used in the atmos system
            Assert.That(SAtmos.Gases.Count(),
                Is.EqualTo(Atmospherics.TotalNumberOfGases),
                $"AtmosSystem.Gases is not equal to TotalNumberOfGases.");
            // enum mapping gases to their Id
            Assert.That(Enum.GetValues<Gas>(),
                Has.Length.EqualTo(Atmospherics.TotalNumberOfGases),
                $"Gas enum size is not equal to TotalNumberOfGases.");

            // the ID for each gas has to correspond to a value in the Gas enum (converted to a string)
            foreach (var gas in gasProtos)
            {
                Assert.That(Enum.TryParse<Gas>(gas.ID, out _),
                    $"GasPrototype {gas.ID} has an invalid ID. " +
                    $"It must correspond to a value in the {nameof(Gas)} enum.");
            }
        }
    }
}
