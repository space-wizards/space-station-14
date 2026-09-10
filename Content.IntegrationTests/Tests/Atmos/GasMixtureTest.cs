using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
[TestOf(typeof(GasMixture))]
public sealed class GasMixtureTest : AtmosTest
{
    [Test]
    [RunOnSide(Side.Server)]
    public void TestMerge()
    {
        var a = new GasMixture(10f);
        var b = new GasMixture(10f);

        a.AdjustMoles(Gas.Oxygen, 50);
        b.AdjustMoles(Gas.Nitrogen, 50);

        // a now has 50 moles of oxygen
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.TotalMoles, Is.EqualTo(50));
            Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));
        }

        // b now has 50 moles of nitrogen
        using (Assert.EnterMultipleScope())
        {
            Assert.That(b.TotalMoles, Is.EqualTo(50));
            Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));
        }

        SAtmos.Merge(b, a);

        // b now has its contents and the contents of a
        using (Assert.EnterMultipleScope())
        {
            Assert.That(b.TotalMoles, Is.EqualTo(100));
            Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(50));
            Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(50));
        }

        // a should be the same, however.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.TotalMoles, Is.EqualTo(50));
            Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(50));
        }
    }

    [Test]
    [TestCase(0.5f)]
    [TestCase(0.25f)]
    [TestCase(0.75f)]
    [TestCase(1f)]
    [TestCase(0f)]
    [TestCase(Atmospherics.BreathPercentage)]
    [RunOnSide(Side.Server)]
    public void RemoveRatio(float ratio)
    {
        var a = new GasMixture(10f);

        a.AdjustMoles(Gas.Oxygen, 100);
        a.AdjustMoles(Gas.Nitrogen, 100);

        var origTotal = a.TotalMoles;

        // we remove moles from the mixture with a ratio.
        var b = a.RemoveRatio(ratio);

        // check that the amount of moles in the original and the new mixture are correct.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(b.TotalMoles, Is.EqualTo(origTotal * ratio));
            Assert.That(a.TotalMoles, Is.EqualTo(origTotal - b.TotalMoles));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(b.GetMoles(Gas.Oxygen), Is.EqualTo(100 * ratio));
            Assert.That(b.GetMoles(Gas.Nitrogen), Is.EqualTo(100 * ratio));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.GetMoles(Gas.Oxygen), Is.EqualTo(100 - b.GetMoles(Gas.Oxygen)));
            Assert.That(a.GetMoles(Gas.Nitrogen), Is.EqualTo(100 - b.GetMoles(Gas.Nitrogen)));
        }
    }
}
