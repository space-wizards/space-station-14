using System;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using NUnit.Framework;

namespace Content.Tests.Server.Atmos;

[TestFixture, TestOf(typeof(AtmosphereSystem))]
[Parallelizable(ParallelScope.All)]
public sealed class AddMolsToMixtureTest
{
    /// <summary>
    /// Assert that an exception is thrown if the length of the array passed in
    /// does not match the number of gases.
    /// </summary>
    [Test]
    [TestCase(-1)]
    [TestCase(1)]
    public void AddMolsToMixture_Throws_OnLengthMismatch(int num)
    {
        var mixture = new GasMixture();
        var wrongLength = new float[Atmospherics.AdjustedNumberOfGases + num];

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AtmosphereSystem.AddMolsToMixture(mixture, wrongLength));

        Assert.That(ex!.ParamName, Is.EqualTo("Length"));
    }

    /// <summary>
    /// Assert that the added mols are correctly added.
    /// </summary>
    [Test]
    public void AddMolsToMixture_Adds_CheckElementwise()
    {
        var mixture = new GasMixture();
        mixture.SetMoles(Gas.Oxygen, 1f);
        mixture.SetMoles(Gas.Nitrogen, 2f);

        var add = new float[Atmospherics.AdjustedNumberOfGases];
        add[(int)Gas.Oxygen] = 3f;
        add[(int)Gas.Nitrogen] = 4f;

        AtmosphereSystem.AddMolsToMixture(mixture, add);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mixture.GetMoles(Gas.Oxygen), Is.EqualTo(4f));
            Assert.That(mixture.GetMoles(Gas.Nitrogen), Is.EqualTo(6f));
        }
    }

    /// <summary>
    /// Assert that the added mols are correctly clamped at zero.
    /// </summary>
    [Test]
    public void AddMolsToMixture_EnsureClamp()
    {
        var mixture = new GasMixture();
        mixture.SetMoles(Gas.Oxygen, 1f);
        mixture.SetMoles(Gas.Nitrogen, 2f);

        var add = new float[Atmospherics.AdjustedNumberOfGases];
        add[(int)Gas.Oxygen] = -2f; // would go to -1 without clamping
        add[(int)Gas.Nitrogen] = -1f; // should become 1

        AtmosphereSystem.AddMolsToMixture(mixture, add);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mixture.GetMoles(Gas.Oxygen), Is.Zero);
            Assert.That(mixture.GetMoles(Gas.Nitrogen), Is.EqualTo(1f));
        }
    }
}
