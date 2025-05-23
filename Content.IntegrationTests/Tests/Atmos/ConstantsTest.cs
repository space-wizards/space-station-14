using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
[TestOf(typeof(Atmospherics))]
public sealed class ConstantsTest
{
    [Test]
    public async Task TotalGasesTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitPost(() =>
        {
            var atmosSystem = entityManager.System<AtmosphereSystem>();

            Assert.Multiple(() =>
            {
                // adding new gases needs a few changes in the code, so make sure this is done everywhere
                var gasProtos = protoManager.EnumeratePrototypes<GasPrototype>().ToList();

                // number of gas prototypes
                Assert.That(gasProtos, Has.Count.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"Number of GasPrototypes ({gasProtos.Count}) is not equal to TotalNumberOfGases ({Atmospherics.TotalNumberOfGases}).");
                // number of gas prototypes used in the atmos system
                Assert.That(atmosSystem.Gases.Count(), Is.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"AtmosSystem.Gases ({atmosSystem.Gases.Count()}) is not equal to TotalNumberOfGases ({Atmospherics.TotalNumberOfGases}).");
                // enum mapping gases to their Id
                Assert.That(Enum.GetValues<Gas>(), Has.Length.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"Gas enum size ({Enum.GetValues<Gas>().Length}) is not equal to TotalNumberOfGases ({Atmospherics.TotalNumberOfGases}).");
                // localized abbreviations for UI purposes
                Assert.That(Atmospherics.GasAbbreviations, Has.Count.EqualTo(Atmospherics.TotalNumberOfGases),
                     $"GasAbbreviations size ({Atmospherics.GasAbbreviations.Count}) is not equal to TotalNumberOfGases ({Atmospherics.TotalNumberOfGases}).");

                // the ID for each gas has to be a number from 0 to TotalNumberOfGases-1
                foreach (var gas in gasProtos)
                {
                    var validId = int.TryParse(gas.ID, out var number) && number >= 0 && number < Atmospherics.TotalNumberOfGases;
                    Assert.That(validId, Is.True, $"GasPrototype {gas.ID} has an invalid Id. It has to be an integer between 0 and {Atmospherics.TotalNumberOfGases - 1}.");
                }
            });
        });
        await pair.CleanReturnAsync();
    }
}

