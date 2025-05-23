using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(Atmospherics))]
public sealed class ConstantsTest
{
    [Test]
    public async Task TotalGasesTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.EntMan;
        var protoManager = server.ProtoMan;

        await server.WaitPost(() =>
        {
            var atmosSystem = entityManager.System<AtmosphereSystem>();

            Assert.Multiple(() =>
            {
                // adding new gases needs a few changes in the code, so make sure this is done everywhere
                var gasProtos = protoManager.EnumeratePrototypes<GasPrototype>().ToList();

                // number of gas prototypes
                Assert.That(gasProtos, Has.Count.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"Number of GasPrototypes is not equal to TotalNumberOfGases.");
                // number of gas prototypes used in the atmos system
                Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"AtmosSystem.Gases is not equal to TotalNumberOfGases.");
                // enum mapping gases to their Id
                Assert.That(Enum.GetValues<Gas>(), Has.Length.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"Gas enum size is not equal to TotalNumberOfGases.");
                // localized abbreviations for UI purposes
                Assert.That(Atmospherics.GasAbbreviations, Has.Count.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"GasAbbreviations size is not equal to TotalNumberOfGases.");

                // the ID for each gas has to be a number from 0 to TotalNumberOfGases-1
                foreach (var gas in gasProtos)
                {
                    var validInteger = int.TryParse(gas.ID, out var number);
                    Assert.That(validInteger, Is.True, $"GasPrototype {gas.ID} has an invalid ID. It has to be an integer between 0 and TotalNumberOfGases - 1.");
                    Assert.That(number, Is.InRange(0, Atmospherics.TotalNumberOfGases - 1), $"GasPrototype {gas.ID} has an invalid ID. It has to be an integer between 0 and TotalNumberOfGases - 1.");
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}

