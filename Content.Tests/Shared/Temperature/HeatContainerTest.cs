using System.Linq;
using Content.Shared.Temperature.HeatContainer;
using NUnit.Framework;

namespace Content.Tests.Shared.Temperature;

[TestFixture, TestOf(typeof(HeatContainer))]
[Parallelizable(ParallelScope.All)]
public sealed class HeatContainerTest
{
    #region HeatContainerHelpers
    [Test]
    public void AddHeatTest()
    {
        // T = 100 K
        // C = 1000 J/K
        var c = new HeatContainer(1000f, 100f);
        var originalEnergy = c.InternalEnergy;

        // Check initial values.
        Assert.That(c.Temperature, Is.EqualTo(100f));
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));

        // Add 5000 J, the temperature should rise by 5 K.
        HeatContainerHelpers.AddHeat(ref c, 5000);
        Assert.That(c.Temperature, Is.EqualTo(105f).Within(1).Ulps);
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));
        Assert.That(c.InternalEnergy, Is.EqualTo(originalEnergy + 5000).Within(1).Ulps);

        // Subtract 15000 J, the temperature should lower by 15 K.
        HeatContainerHelpers.AddHeat(ref c, -15000);
        Assert.That(c.Temperature, Is.EqualTo(90f).Within(1).Ulps);
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));
        Assert.That(c.InternalEnergy, Is.EqualTo(originalEnergy + 5000 - 15000).Within(1).Ulps);

        // Check that we cannot go below 0 K.
        HeatContainerHelpers.AddHeat(ref c, -200000f);
        Assert.That(c.Temperature, Is.Zero);
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));
        Assert.That(c.InternalEnergy, Is.Zero);
    }

    [Test]
    public void AddHeatQueryTest()
    {
        // T = 100 K
        // C = 1000 J/K
        var c = new HeatContainer(1000f, 100f);

        // Check initial values.
        Assert.That(c.Temperature, Is.EqualTo(100f));
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));

        // Add 5000 J, the temperature should rise by 5 K from the original value.
        Assert.That(HeatContainerHelpers.AddHeatQuery(ref c, 5000), Is.EqualTo(105f).Within(1).Ulps);

        // Subtract 15000 J, the temperature should lower by 15 K from the original value.
        Assert.That(HeatContainerHelpers.AddHeatQuery(ref c, -15000), Is.EqualTo(85f).Within(1).Ulps);

        // Check that we cannot go below 0 K.
        Assert.That(HeatContainerHelpers.AddHeatQuery(ref c, -200000f), Is.Zero);

        // The original container should be unchanged.
        Assert.That(c.Temperature, Is.EqualTo(100f));
        Assert.That(c.HeatCapacity, Is.EqualTo(1000f));
    }

    [Test]
    public void SetHeatCapacityTest()
    {
        // T = 300 K
        // C = 1000 J/K
        var c = new HeatContainer(1000f, 300f);
        var originalEnergy = c.InternalEnergy;

        // We triple the heat capacity, resulting in the temeperature to become one third of the original.
        HeatContainerHelpers.SetHeatCapacity(ref c, 3000f);

        // The original container should be unchanged.
        Assert.That(c.Temperature, Is.EqualTo(100f).Within(1).Ulps);
        Assert.That(c.HeatCapacity, Is.EqualTo(3000f));

        // The total energy is conserved.
        Assert.That(c.InternalEnergy, Is.EqualTo(originalEnergy).Within(1).Ulps);
    }
    #endregion

    #region Divide
    [Test]
    public void SplitTest()
    {
        // T = 42 K
        // C = 3000 J/K
        var c1 = new HeatContainer(3000f, 42f);
        var c2 = new HeatContainer();
        var totalEnergy = c1.InternalEnergy;

        // Split equally.
        HeatContainerHelpers.SplitFrom(ref c1, ref c2, fraction: 0.5f);

        // The heat capacity should be split equally.
        // The temperature should be the same.
        // The total energy should be conserved.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(1500f).Within(1).Ulps);
        Assert.That(c2.Temperature, Is.EqualTo(42f));
        Assert.That(c2.HeatCapacity, Is.EqualTo(1500f).Within(1).Ulps);
        Assert.That(c1.InternalEnergy + c2.InternalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);

        // Reset the first container.
        c1 = new HeatContainer(3000f, 42f);

        // Split into 2/3 + 1/3.
        HeatContainerHelpers.SplitFrom(ref c1, ref c2, fraction: 1f / 3);

        // The heat capacity should be split according to the fraction.
        // The temperature should be the same.
        // The total energy should be conserved.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(2000f).Within(1).Ulps);
        Assert.That(c2.Temperature, Is.EqualTo(42f));
        Assert.That(c2.HeatCapacity, Is.EqualTo(1000f).Within(1).Ulps);
        Assert.That(c1.InternalEnergy + c2.InternalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);
    }

    [Test]
    public void SplitDiscardTest()
    {
        // T = 42 K
        // C = 3000 J/K
        var c1 = new HeatContainer(3000f, 42f);

        // Split equally.
        HeatContainerHelpers.SplitFrom(ref c1, fraction: 0.5f);

        // The heat capacity should be split equally, the temperature should be the same.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(1500f).Within(1).Ulps);

        // Reset the container.
        c1 = new HeatContainer(3000f, 42f);

        // Split into 1/3 + 2/3.
        HeatContainerHelpers.SplitFrom(ref c1, fraction: 1f / 3);

        // The heat capacity should be split according to the fraction, the temperature should be the same.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(2000f).Within(1).Ulps);
    }

    [Test]
    public void SplitArrayTest()
    {
        // T = 42 K
        // C = 1000 J/K
        const int n = 4;
        var c1 = new HeatContainer(1000f, 42f);
        var cA = new HeatContainer[n];

        // Split into n + 1 equal parts.
        HeatContainerHelpers.SplitFrom(ref c1, cA);

        for (var i = 0; i < n; i++)
        {
            // The temperature should be the same as the initial one.
            // The heat capacities should be equally split.
            Assert.That(cA[i].Temperature, Is.EqualTo(42f));
            Assert.That(cA[i].HeatCapacity, Is.EqualTo(1000f / (n + 1)).Within(1).Ulps);
        }

        // Check that the initital container is the same as the output containers.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(1000f / (n + 1)).Within(1).Ulps);
    }

    [Test]
    public void SplitAndCopyTest()
    {
        // T = 42 K
        // C = 1000 J/K
        const int n = 5;
        var c1 = new HeatContainer(1000f, 42f);
        var cA = new HeatContainer[n];

        // Divide into n equal parts.
        HeatContainerHelpers.SplitAndCopy(ref c1, cA);

        for (var i = 0; i < n; i++)
        {
            // The temperature should be the same as the initial one.
            // The heat capacities should be equally split.
            Assert.That(cA[i].Temperature, Is.EqualTo(42f));
            Assert.That(cA[i].HeatCapacity, Is.EqualTo(1000f / n).Within(1).Ulps);
        }

        // Check that the initital container is unmodified.
        Assert.That(c1.Temperature, Is.EqualTo(42f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(1000f));
    }
    #endregion

    #region Merge
    [Test]
    public void Merge2Test()
    {
        // T = 42 K
        // C = 5000 J/K
        var c1 = new HeatContainer(5000f, 42f);
        var energy1 = c1.InternalEnergy;
        // T = 100 K
        // C = 5000 J/K
        var c2 = new HeatContainer(5000f, 100f);
        var energy2 = c2.InternalEnergy;

        // Merge 2 containers of the same capacity and different temperatures.
        HeatContainerHelpers.MergeInto(ref c1, ref c2);

        // The temperature should be the average of the two initial ones.
        // The total heat capacity should be the sum of the two initial capacities.
        // The total energy should be conserved.
        Assert.That(c1.Temperature, Is.EqualTo((42f + 100f) / 2).Within(1).Ulps);
        Assert.That(c1.HeatCapacity, Is.EqualTo(10000f).Within(1).Ulps);
        Assert.That(c1.InternalEnergy, Is.EqualTo(energy1 + energy2).Within(1).Ulps);

        // The second container should remain unchanged.
        Assert.That(c2.Temperature, Is.EqualTo(100));
        Assert.That(c2.HeatCapacity, Is.EqualTo(5000f));

        // T = 100 K
        // C = 750 J/K
        // E = 75000 J
        c1 = new HeatContainer(750f, 100f);
        energy1 = c1.InternalEnergy;
        // T = 300 K
        // C = 250 J/K
        // E = 75000 J
        c2 = new HeatContainer(250f, 300f);
        energy2 = c2.InternalEnergy;

        // Merge 2 containers with different temperature and capacity.
        HeatContainerHelpers.MergeInto(ref c1, ref c2);

        // The temperature should averaged weighted by capacity.
        // (100*750+300*250)/(750+250)=150
        // The total heat capacity should be the sum of the two initial capacities.
        // The total energy should be conserved.
        Assert.That(c1.Temperature, Is.EqualTo(150f).Within(1).Ulps);
        Assert.That(c1.HeatCapacity, Is.EqualTo(750f + 250f).Within(1).Ulps);
        Assert.That(c1.InternalEnergy, Is.EqualTo(energy1 + energy2).Within(1).Ulps);

        // The second container should remain unchanged.
        Assert.That(c2.Temperature, Is.EqualTo(300));
        Assert.That(c2.HeatCapacity, Is.EqualTo(250f));
    }

    [Test]
    public void Merge1PlusArrayTest()
    {
        // T = 200 K
        // C = 50 J/K
        var c1 = new HeatContainer(50f, 200f);
        var energy1 = c1.InternalEnergy;

        // Array of 40 heat containers, each with
        // T = 100 K
        // C = 5 J/K
        const int n = 40;
        var cA1 = new HeatContainer(5f, 100);
        var cA = new HeatContainer[n];
        var energyA = cA1.InternalEnergy * n;
        for (var i = 0; i < cA.Length; i++)
        {
            cA[i] = cA1;
        }

        // Merge the array into the single heat container.
        HeatContainerHelpers.MergeInto(ref c1, cA);

        // The temperature should averaged weighted by capacity.
        // (200*50+100*5*40)/(50+5*40)=120
        // The total heat capacity should be the sum of the initial capacities.
        // The total energy should be conserved.
        Assert.That(c1.Temperature, Is.EqualTo(120f).Within(1).Ulps);
        Assert.That(c1.HeatCapacity, Is.EqualTo(50f + 5 * n).Within(1).Ulps);
        Assert.That(c1.InternalEnergy, Is.EqualTo(energy1 + energyA).Within(1).Ulps);
    }

    [Test]
    public void MergeArrayTest()
    {
        // This heat container will be overwritten.
        var c1 = new HeatContainer(50f, 200f);

        // Array of 40 heat containers, each with
        // T = 100 K
        // C = 5 J/K
        const int n = 40;
        var cA1 = new HeatContainer(5f, 100);
        var cA = new HeatContainer[n];
        var energyA = cA1.InternalEnergy * n;
        for (var i = 0; i < cA.Length; i++)
        {
            cA[i] = cA1;
        }

        // Merge the array into the single heat container.
        HeatContainerHelpers.MergeAndCopy(ref c1, cA);

        // The temperature of all merged containers was the same.
        // The total heat capacity should be the sum of the initial capacities.
        // The total energy should be the sum of the intial energies.
        Assert.That(c1.Temperature, Is.EqualTo(100f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(5 * n).Within(1).Ulps);
        Assert.That(c1.InternalEnergy, Is.EqualTo(energyA).Within(1).Ulps);
    }
    #endregion

    #region Exchange
    [Test]
    public void EquilibriumQuery2BodyTest()
    {
        // Cold c1, hot c2.
        var c1 = new HeatContainer(123f, 456f);
        var c2 = new HeatContainer(987f, 654f);

        var dQ11 = HeatContainerHelpers.EquilibriumHeatQuery(ref c1, ref c1);
        var dQ12 = HeatContainerHelpers.EquilibriumHeatQuery(ref c1, ref c2);
        var dQ21 = HeatContainerHelpers.EquilibriumHeatQuery(ref c2, ref c1);
        var dQ22 = HeatContainerHelpers.EquilibriumHeatQuery(ref c2, ref c2);
        var t11 = HeatContainerHelpers.EquilibriumTemperatureQuery(ref c1, ref c1);
        var t12 = HeatContainerHelpers.EquilibriumTemperatureQuery(ref c1, ref c2);
        var t21 = HeatContainerHelpers.EquilibriumTemperatureQuery(ref c2, ref c1);
        var t22 = HeatContainerHelpers.EquilibriumTemperatureQuery(ref c2, ref c2);

        // Containers should be in equilibrium with themselves.
        Assert.That(dQ11, Is.Zero.Within(1).Ulps);
        Assert.That(dQ22, Is.Zero.Within(1).Ulps);
        Assert.That(t11, Is.EqualTo(c1.Temperature).Within(1).Ulps);
        Assert.That(t22, Is.EqualTo(c2.Temperature).Within(1).Ulps);

        // Heat should flow from hot to cold.
        Assert.That(dQ12, Is.LessThan(0f));
        Assert.That(dQ21, Is.GreaterThan(0f));
        Assert.That(t12, Is.LessThan(c2.Temperature));
        Assert.That(t21, Is.LessThan(c2.Temperature));
        Assert.That(t12, Is.GreaterThan(c1.Temperature));
        Assert.That(t21, Is.GreaterThan(c1.Temperature));
        // The result should be symmetric.
        Assert.That(dQ21, Is.EqualTo(-dQ12).Within(1).Ulps);
        Assert.That(t12, Is.EqualTo(t21).Within(1).Ulps);

        // Check that the heat flow indeed brings them into equilibrium.
        HeatContainerHelpers.AddHeat(ref c1, -dQ12);
        HeatContainerHelpers.AddHeat(ref c2, dQ12);

        Assert.That(c1.Temperature, Is.EqualTo(c2.Temperature).Within(1).Ulps);
        Assert.That(c1.Temperature, Is.EqualTo(t12).Within(1).Ulps);
        Assert.That(c1.Temperature, Is.EqualTo(t21).Within(1).Ulps);
        Assert.That(c2.Temperature, Is.EqualTo(t12).Within(1).Ulps);
        Assert.That(c2.Temperature, Is.EqualTo(t21).Within(1).Ulps);
    }

    [Test]
    public void Equilibrium2BodyTest()
    {
        // Cold c1, hot c2.
        var c1 = new HeatContainer(123f, 456f);
        var c2 = new HeatContainer(987f, 654f);
        var totalEnergy = c1.InternalEnergy + c2.InternalEnergy;

        // Bring them into equilibrium.
        HeatContainerHelpers.Equilibrate(ref c1, ref c2);

        // Total energy should be conserved.
        Assert.That(c1.InternalEnergy + c2.InternalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);

        // The temperature should be equal, the capacities unchanged.
        Assert.That(c1.Temperature, Is.EqualTo(c2.Temperature).Within(1).Ulps);
        Assert.That(c1.HeatCapacity, Is.EqualTo(123f));
        Assert.That(c2.HeatCapacity, Is.EqualTo(987f));

        // Repeat with the out dQ overload.
        c1 = new HeatContainer(123f, 456f);
        c2 = new HeatContainer(987f, 654f);
        totalEnergy = c1.InternalEnergy + c2.InternalEnergy;
        var dQQuery = HeatContainerHelpers.EquilibriumHeatQuery(ref c1, ref c2);

        // Bring them into equilibrium.
        HeatContainerHelpers.Equilibrate(ref c1, ref c2, out var dQresult);

        // Total energy should be conserved.
        Assert.That(c1.InternalEnergy + c2.InternalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);

        // The temperature should be equal, the capacities unchanged.
        Assert.That(c1.Temperature, Is.EqualTo(c2.Temperature).Within(1).Ulps);
        Assert.That(c1.HeatCapacity, Is.EqualTo(123f));
        Assert.That(c2.HeatCapacity, Is.EqualTo(987f));

        // The output dQ should be the same as the query we did before.
        Assert.That(dQQuery, Is.EqualTo(dQresult).Within(1).Ulps);
    }

    [Test]
    public void Equilibrium3BodyTest()
    {
        // Cold c1, medium c2, hot c3.
        var c1 = new HeatContainer(300f, 123f);
        var c2 = new HeatContainer(200f, 234f);
        var c3 = new HeatContainer(100f, 456f);
        var totalEnergy = c1.InternalEnergy + c2.InternalEnergy + c3.InternalEnergy;

        // Save as array.
        var cN = new HeatContainer[3];
        cN[0] = c1;
        cN[1] = c2;
        cN[2] = c3;

        var tQuery = HeatContainerHelpers.EquilibriumTemperatureQuery(cN);
        var tQuerydQ = HeatContainerHelpers.EquilibriumTemperatureQuery(cN, out var dQQuery);

        // Both queries should result in the same temperature.
        Assert.That(tQuery, Is.EqualTo(tQuerydQ).Within(1).Ulps);

        // Heat flows from hot to cold.
        Assert.That(tQuery, Is.GreaterThan(c1.Temperature));
        Assert.That(tQuery, Is.LessThan(c3.Temperature));
        Assert.That(dQQuery[0], Is.GreaterThan(0f));
        Assert.That(dQQuery[2], Is.LessThan(0f));

        // Total energy should be conserved.
        Assert.That(dQQuery.Sum(), Is.Zero.Within(1).Ulps);

        // Check if we actually reach equilibrium with the calculated heat flow.
        HeatContainerHelpers.AddHeat(ref c1, dQQuery[0]);
        HeatContainerHelpers.AddHeat(ref c2, dQQuery[1]);
        HeatContainerHelpers.AddHeat(ref c3, dQQuery[2]);

        Assert.That(c1.Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(c2.Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(c3.Temperature, Is.EqualTo(tQuery).Within(1).Ulps);

        // Put the array into equilibrium.
        HeatContainerHelpers.Equilibrate(cN);

        // Check if we actually reached equilibrium.
        Assert.That(cN[0].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(cN[1].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(cN[2].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);

        // Total energy should be conserved.
        var newTotalEnergy = cN[0].InternalEnergy + cN[1].InternalEnergy + cN[2].InternalEnergy;
        Assert.That(newTotalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);
    }

    [Test]
    public void Equilibrium1Plus3BodyTest()
    {
        // Cold c1, medium c2 and c3, hot c4.
        var c1 = new HeatContainer(400f, 123f);
        var c2 = new HeatContainer(300f, 234f);
        var c3 = new HeatContainer(200f, 456f);
        var c4 = new HeatContainer(100f, 567f);
        var totalEnergy = c1.InternalEnergy + c2.InternalEnergy + c3.InternalEnergy + c4.InternalEnergy;

        // Save as array.
        var cN = new HeatContainer[3];
        cN[0] = c1;
        cN[1] = c2;
        cN[2] = c3;

        var tQuery = HeatContainerHelpers.EquilibriumTemperatureQuery(ref c4, cN);

        // Heat flows from hot to cold.
        Assert.That(tQuery, Is.GreaterThan(c1.Temperature));
        Assert.That(tQuery, Is.LessThan(c4.Temperature));

        // Total energy should be conserved.
        Assert.That(tQuery * (c1.HeatCapacity + c2.HeatCapacity + c3.HeatCapacity + c4.HeatCapacity), Is.EqualTo(totalEnergy).Within(1).Ulps);

        // Put everything into equilibrium.
        HeatContainerHelpers.Equilibrate(ref c4, cN);

        // Check if we actually reached equilibrium.
        Assert.That(cN[0].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(cN[1].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(cN[2].Temperature, Is.EqualTo(tQuery).Within(1).Ulps);
        Assert.That(c4.Temperature, Is.EqualTo(tQuery).Within(1).Ulps);

        // Total energy should be conserved.
        var newTotalEnergy = cN[0].InternalEnergy + cN[1].InternalEnergy + cN[2].InternalEnergy + c4.InternalEnergy;
        Assert.That(newTotalEnergy, Is.EqualTo(totalEnergy).Within(1).Ulps);
    }
    #endregion

    #region Conduct
    [Test]
    public void Conduct1Test()
    {
        // T = 100 K
        // C = 42 J/K
        var c1 = new HeatContainer(42f, 100f);
        var c2 = new HeatContainer(42f, 100f);
        var c3 = new HeatContainer(42f, 100f);
        var c4 = new HeatContainer(42f, 100f);

        // Conduct heat with a heat bath of 200K for a small time step of 0.01s and a conductance of 1.
        var dQ1 = HeatContainerHelpers.ConductHeat(ref c1, 200f, 0.01f, 100f);

        // The temperature should be between 100 and 200K.
        // The heat capacity should be unchanged.
        Assert.That(c1.Temperature, Is.GreaterThan(100f));
        Assert.That(c1.Temperature, Is.LessThan(200f));
        Assert.That(c1.HeatCapacity, Is.EqualTo(42f));

        // The conducted heat should positive, since the temperature got higher.
        Assert.That(dQ1, Is.GreaterThan(0f));

        // Check that removing the heat again brings us back where we were originally.
        var c1Copy = c1;
        HeatContainerHelpers.AddHeat(ref c1Copy, -dQ1);
        Assert.That(c1Copy.Temperature, Is.EqualTo(100f).Within(1).Ulps);

        // A greater temperature difference means a greater heat transfer.
        var dQ2 = HeatContainerHelpers.ConductHeat(ref c2, 300f, 0.01f, 100f);
        Assert.That(dQ2, Is.GreaterThan(dQ1));
        Assert.That(c2.Temperature, Is.GreaterThan(c1.Temperature));

        // A greater time step means a greater heat transfer.
        var dQ3 = HeatContainerHelpers.ConductHeat(ref c3, 200f, 0.02f, 100f);
        Assert.That(dQ3, Is.GreaterThan(dQ1));
        Assert.That(c3.Temperature, Is.GreaterThan(c1.Temperature));

        // A greater conductance means a greater heat transfer.
        var dQ4 = HeatContainerHelpers.ConductHeat(ref c4, 200f, 0.01f, 200f);
        Assert.That(dQ4, Is.GreaterThan(dQ1));
        Assert.That(c4.Temperature, Is.GreaterThan(c1.Temperature));

        // Make sure we don't overshoot with a too large time step and conductance.
        var c5 = new HeatContainer(42f, 100f);
        var dQ5 = HeatContainerHelpers.ConductHeat(ref c5, 200f, 10f, 10000f);
        Assert.That(c5.Temperature, Is.EqualTo(200f).Within(1).Ulps);

        // Check that the heat diff is still correct even when we would have overshot.
        HeatContainerHelpers.AddHeat(ref c5, -dQ5);
        Assert.That(c5.Temperature, Is.EqualTo(100f).Within(1).Ulps);

        // Check that consecutive steps become smaller, but still get us closer to equilibrium.
        var c6 = new HeatContainer(42f, 100f);
        var t6Init = c6.Temperature;
        var dQ6A = HeatContainerHelpers.ConductHeat(ref c6, 200f, 1f, 1f);
        var t6A = c6.Temperature;
        var dQ6B = HeatContainerHelpers.ConductHeat(ref c6, 200f, 1f, 1f);
        var t6B = c6.Temperature;
        Assert.That(dQ6A, Is.GreaterThan(dQ6B));
        Assert.That(t6A, Is.GreaterThan(t6Init));
        Assert.That(t6B, Is.GreaterThan(t6A));

        // Check that we converge towards the heat bath temperature.
        var c7 = new HeatContainer(42f, 100f);
        var e7Init = c7.InternalEnergy;
        var dQ7 = 0f;
        for (var i = 0; i < 10000; i++)
        {
            dQ7 += HeatContainerHelpers.ConductHeat(ref c7, 200f, 1f, 1f);
        }
        Assert.That(c7.Temperature, Is.EqualTo(200f).Within(0.1).Percent);
        Assert.That(c7.InternalEnergy - dQ7, Is.EqualTo(e7Init).Within(0.2).Percent);
    }

    [Test]
    public void Conduct2Test()
    {
        // Temperatures at 100 K and 200 K
        var cA = new HeatContainer(42f, 100f);
        var cB = new HeatContainer(123f, 200f);
        var totalEnergy = cA.InternalEnergy + cB.InternalEnergy;
        var tEquilibrium = HeatContainerHelpers.EquilibriumTemperatureQuery(ref cA, ref cB);
        var dQ = HeatContainerHelpers.ConductHeat(ref cA, ref cB, 1f, 1f);

        // Heat flow from hot B to cold A should be positive.
        Assert.That(dQ, Is.GreaterThan(0f));

        // Energy should be conserved.
        Assert.That(cA.InternalEnergy + cB.InternalEnergy, Is.EqualTo(totalEnergy));

        // Check that we got closer to equilibrium, but did not reach it.
        Assert.That(cA.Temperature, Is.GreaterThan(100f));
        Assert.That(cA.Temperature, Is.LessThan(tEquilibrium));
        Assert.That(cB.Temperature, Is.LessThan(200f));
        Assert.That(cB.Temperature, Is.GreaterThan(tEquilibrium));

        // Check that the given heat transfer amount is correct.
        HeatContainerHelpers.AddHeat(ref cA, -dQ);
        HeatContainerHelpers.AddHeat(ref cB, dQ);

        Assert.That(cA.Temperature, Is.EqualTo(100f).Within(1).Ulps);
        Assert.That(cB.Temperature, Is.EqualTo(200f).Within(1).Ulps);

        // Reset containers.
        cA = new HeatContainer(42f, 100f);
        cB = new HeatContainer(123f, 200f);

        // Check that we converge towards equilibrium.
        for (var i = 0; i < 10000; i++)
        {
            HeatContainerHelpers.ConductHeat(ref cA, ref cB, 1f, 1f);
        }
        Assert.That(cA.Temperature, Is.EqualTo(tEquilibrium).Within(0.1).Percent);
        Assert.That(cB.Temperature, Is.EqualTo(tEquilibrium).Within(0.1).Percent);
    }
    #endregion
}
