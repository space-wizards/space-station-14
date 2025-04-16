using Content.Shared.Atmos;
using NUnit.Framework;

namespace Content.Tests.Shared.Atmos;

[TestFixture, TestOf(typeof(GasMixture))]
[Parallelizable(ParallelScope.All)]
public sealed class GasMixtureTest
{
    [Test]
    public void TestEnumerate()
    {
        var mixture = new GasMixture();
        mixture.SetMoles(Gas.Oxygen, 20);
        mixture.SetMoles(Gas.Nitrogen, 10);
        mixture.SetMoles(Gas.Plasma, 80);

        var expectedList = new (Gas, float)[Atmospherics.TotalNumberOfGases];
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            expectedList[i].Item1 = (Gas)i;
        }

        expectedList[(int)Gas.Oxygen].Item2 = 20f;
        expectedList[(int)Gas.Nitrogen].Item2 = 10f;
        expectedList[(int)Gas.Plasma].Item2 = 80f;

        Assert.That(mixture, Is.EquivalentTo(expectedList));
    }
}
