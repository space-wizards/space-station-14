using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared.Chemistry;

[TestFixture, Parallelizable, TestOf(typeof(Solution))]
public sealed class SolutionTests : ContentUnitTest
{
    private const string Water = "water";
    private const string Fire = "fire";
    private const string Earth = "earth";

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IPrototypeManager>().Initialize();
    }

    [Test]
    public void AddReagentAndGetSolution()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        var quantity = solution.GetTotalPrototypeQuantity(Water);

        Assert.That(quantity.Int(), Is.EqualTo(1000));
    }

    [Test]
    public void ScaleSolution()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(20));
        solution.AddReagent(Fire, FixedPoint2.New(30));

        // Test integer scaling
        {
            var tmp = solution.Clone();
            tmp.ScaleSolution(0);
            Assert.That(tmp.Contents.Count, Is.EqualTo(0));
            Assert.That(tmp.Volume, Is.EqualTo(FixedPoint2.Zero));

            tmp = solution.Clone();
            tmp.ScaleSolution(2);
            Assert.That(tmp.Contents.Count, Is.EqualTo(2));
            Assert.That(tmp.Volume, Is.EqualTo(FixedPoint2.New(100)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Water), Is.EqualTo(FixedPoint2.New(40)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Fire), Is.EqualTo(FixedPoint2.New(60)));
        }

        // Test float scaling
        {
            var tmp = solution.Clone();
            tmp.ScaleSolution(0f);
            Assert.That(tmp.Contents.Count, Is.EqualTo(0));
            Assert.That(tmp.Volume, Is.EqualTo(FixedPoint2.Zero));

            tmp = solution.Clone();
            tmp.ScaleSolution(2f);
            Assert.That(tmp.Contents.Count, Is.EqualTo(2));
            Assert.That(tmp.Volume, Is.EqualTo(FixedPoint2.New(100)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Water), Is.EqualTo(FixedPoint2.New(40)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Fire), Is.EqualTo(FixedPoint2.New(60)));

            tmp = solution.Clone();
            tmp.ScaleSolution(0.3f);
            Assert.That(tmp.Contents.Count, Is.EqualTo(2));
            Assert.That(tmp.Volume, Is.EqualTo(FixedPoint2.New(15)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Water), Is.EqualTo(FixedPoint2.New(6)));
            Assert.That(tmp.GetTotalPrototypeQuantity(Fire), Is.EqualTo(FixedPoint2.New(9)));
        }
    }

    [Test]
    public void ConstructorAddReagent()
    {
        var solution = new Solution(Water, FixedPoint2.New(1000));
        var quantity = solution.GetTotalPrototypeQuantity(Water);

        Assert.That(quantity.Int(), Is.EqualTo(1000));
    }

    [Test]
    public void NonExistingReagentReturnsZero()
    {
        var solution = new Solution();
        var quantity = solution.GetTotalPrototypeQuantity(Water);

        Assert.That(quantity.Int(), Is.EqualTo(0));
    }

#if !DEBUG
    [Test]
    public void AddLessThanZeroReagentReturnsZero()
    {
        var solution = new Solution(Water, FixedPoint2.New(-1000));
        var quantity = solution.GetTotalPrototypeQuantity(Water);

        Assert.That(quantity.Int(), Is.EqualTo(0));
    }
#endif

    [Test]
    public void AddingReagentsSumsProperly()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Water, FixedPoint2.New(2000));
        var quantity = solution.GetTotalPrototypeQuantity(Water);

        Assert.That(quantity.Int(), Is.EqualTo(3000));
    }

    [Test]
    public void ReagentQuantitiesStayUnique()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(1000));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(2000));
    }

    [Test]
    public void TotalVolumeIsCorrect()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        Assert.That(solution.Volume.Int(), Is.EqualTo(3000));
    }

    [Test]
    public void CloningSolutionIsCorrect()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        var newSolution = solution.Clone();

        Assert.That(newSolution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(1000));
        Assert.That(newSolution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(2000));
        Assert.That(newSolution.Volume.Int(), Is.EqualTo(3000));
    }

    [Test]
    public void RemoveSolutionRecalculatesProperly()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        solution.RemoveReagent(Water, FixedPoint2.New(500));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(500));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(2000));
        Assert.That(solution.Volume.Int(), Is.EqualTo(2500));
    }

    [Test]
    public void RemoveLessThanOneQuantityDoesNothing()
    {
        var solution = new Solution(Water, FixedPoint2.New(100));

        solution.RemoveReagent(Water, FixedPoint2.New(-100));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(100));
        Assert.That(solution.Volume.Int(), Is.EqualTo(100));
    }

    [Test]
    public void RemoveMoreThanTotalRemovesAllReagent()
    {
        var solution = new Solution(Water, FixedPoint2.New(100));

        solution.RemoveReagent(Water, FixedPoint2.New(1000));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(0));
        Assert.That(solution.Volume.Int(), Is.EqualTo(0));
    }

    [Test]
    public void RemoveNonExistReagentDoesNothing()
    {
        var solution = new Solution(Water, FixedPoint2.New(100));

        solution.RemoveReagent(Fire, FixedPoint2.New(1000));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(100));
        Assert.That(solution.Volume.Int(), Is.EqualTo(100));
    }

    [Test]
    public void RemoveSolution()
    {
        var solution = new Solution(Water, FixedPoint2.New(700));

        solution.RemoveSolution(FixedPoint2.New(500));

        //Check that edited solution is correct
        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(200));
        Assert.That(solution.Volume.Int(), Is.EqualTo(200));
    }

    [Test]
    public void RemoveSolutionMoreThanTotalRemovesAll()
    {
        var solution = new Solution(Water, FixedPoint2.New(800));

        solution.RemoveSolution(FixedPoint2.New(1000));

        //Check that edited solution is correct
        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(0));
        Assert.That(solution.Volume.Int(), Is.EqualTo(0));
    }

    [Test]
    public void RemoveSolutionRatioPreserved()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        solution.RemoveSolution(FixedPoint2.New(1500));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(500));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(1000));
        Assert.That(solution.Volume.Int(), Is.EqualTo(1500));
    }

    [Test]
    public void RemoveSolutionLessThanOneDoesNothing()
    {
        var solution = new Solution(Water, FixedPoint2.New(800));

        solution.RemoveSolution(FixedPoint2.New(-200));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(800));
        Assert.That(solution.Volume.Int(), Is.EqualTo(800));
    }

    [Test]
    public void SplitSolution()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1000));
        solution.AddReagent(Fire, FixedPoint2.New(2000));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(750));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(750));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(1500));
        Assert.That(solution.Volume.Int(), Is.EqualTo(2250));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(250));
        Assert.That(splitSolution.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(500));
        Assert.That(splitSolution.Volume.Int(), Is.EqualTo(750));
    }

    [Test]
    public void SplitSolutionFractional()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1));
        solution.AddReagent(Fire, FixedPoint2.New(2));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(1));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(0.66f));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Float(), Is.EqualTo(1.34f));
        Assert.That(solution.Volume.Int(), Is.EqualTo(2));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(0.34f));
        Assert.That(splitSolution.GetTotalPrototypeQuantity(Fire).Float(), Is.EqualTo(0.66f));
        Assert.That(splitSolution.Volume.Int(), Is.EqualTo(1));
    }

    [Test]
    public void SplitSolutionFractionalOpposite()
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(1));
        solution.AddReagent(Fire, FixedPoint2.New(2));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(2));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(0.33f));
        Assert.That(solution.GetTotalPrototypeQuantity(Fire).Float(), Is.EqualTo(0.67f));
        Assert.That(solution.Volume.Int(), Is.EqualTo(1));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(0.67f));
        Assert.That(splitSolution.GetTotalPrototypeQuantity(Fire).Float(), Is.EqualTo(1.33f));
        Assert.That(splitSolution.Volume.Int(), Is.EqualTo(2));
    }

    [Test]
    [TestCase(0.03f, 0.01f, 0.02f)]
    [TestCase(0.03f, 0.02f, 0.01f)]
    public void SplitSolutionTinyFractionalBigSmall(float initial, float reduce, float remainder)
    {
        var solution = new Solution();
        solution.AddReagent(Water, FixedPoint2.New(initial));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(reduce));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(remainder));
        Assert.That(solution.Volume.Float(), Is.EqualTo(remainder));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Float(), Is.EqualTo(reduce));
        Assert.That(splitSolution.Volume.Float(), Is.EqualTo(reduce));
    }

    [Test]
    [TestCase(2)]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1000)]
    public void SplitRounding(int amount)
    {
        var foo = "foo";
        var bar = "bar";
        var baz = "baz";

        var solutionOne = new Solution();
        solutionOne.AddReagent(foo, FixedPoint2.New(amount));
        solutionOne.AddReagent(bar, FixedPoint2.New(amount));
        solutionOne.AddReagent(baz, FixedPoint2.New(amount));

        var splitAmount = FixedPoint2.New(5);
        var split = solutionOne.SplitSolution(splitAmount);

        Assert.That(split.Volume, Is.EqualTo(splitAmount));
    }

    [Test]
    public void SplitSolutionMoreThanTotalRemovesAll()
    {
        var solution = new Solution(Water, FixedPoint2.New(800));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(1000));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(0));
        Assert.That(solution.Volume.Int(), Is.EqualTo(0));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(800));
        Assert.That(splitSolution.Volume.Int(), Is.EqualTo(800));
    }

    [Test]
    public void SplitSolutionLessThanOneDoesNothing()
    {
        var solution = new Solution(Water, FixedPoint2.New(800));

        var splitSolution = solution.SplitSolution(FixedPoint2.New(-200));

        Assert.That(solution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(800));
        Assert.That(solution.Volume.Int(), Is.EqualTo(800));

        Assert.That(splitSolution.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(0));
        Assert.That(splitSolution.Volume.Int(), Is.EqualTo(0));
    }

    [Test]
    public void SplitSolutionZero()
    {
        var impedrezene = "Impedrezene";
        var thermite = "Thermite";
        var lithium = "Li";
        var flourine = "F";
        var sodium = "Na";
        var mercury = "Hg";
        var copper = "Cu";
        var uranium = "U";
        var iron = "Fe";
        var spaceDrugs = "SpaceDrugs";
        var aluminum = "Al";
        var glucose = "Glucose";
        var oxygen = "O";

        var solution = new Solution();
        solution.AddReagent(impedrezene, FixedPoint2.New(0.01 + 0.19));
        solution.AddReagent(thermite, FixedPoint2.New(0.01 + 0.39));
        solution.AddReagent(lithium, FixedPoint2.New(0.01 + 0.17));
        solution.AddReagent(flourine, FixedPoint2.New(0.01 + 0.17));
        solution.AddReagent(sodium, FixedPoint2.New(0 + 0.13));
        solution.AddReagent(mercury, FixedPoint2.New(0.15 + 4.15));
        solution.AddReagent(copper, FixedPoint2.New(0 + 0.13));
        solution.AddReagent(uranium, FixedPoint2.New(0.76 + 20.77));
        solution.AddReagent(iron, FixedPoint2.New(0.01 + 0.36));
        solution.AddReagent(spaceDrugs, FixedPoint2.New(0.02 + 0.41));
        solution.AddReagent(aluminum, FixedPoint2.New(0));
        solution.AddReagent(glucose, FixedPoint2.New(0));
        solution.AddReagent(oxygen, FixedPoint2.New(0));

        solution.SplitSolution(FixedPoint2.New(0.98));
    }

    [Test]
    public void AddSolution()
    {
        var solutionOne = new Solution();
        solutionOne.AddReagent(Water, FixedPoint2.New(1000));
        solutionOne.AddReagent(Fire, FixedPoint2.New(2000));

        var solutionTwo = new Solution();
        solutionTwo.AddReagent(Water, FixedPoint2.New(500));
        solutionTwo.AddReagent(Earth, FixedPoint2.New(1000));

        solutionOne.AddSolution(solutionTwo, null);

        Assert.That(solutionOne.GetTotalPrototypeQuantity(Water).Int(), Is.EqualTo(1500));
        Assert.That(solutionOne.GetTotalPrototypeQuantity(Fire).Int(), Is.EqualTo(2000));
        Assert.That(solutionOne.GetTotalPrototypeQuantity(Earth).Int(), Is.EqualTo(1000));
        Assert.That(solutionOne.Volume.Int(), Is.EqualTo(4500));
    }

    // Tests concerning thermal energy and temperature.

    #region Thermal Energy and Temperature

    [Test]
    public void EmptySolutionHasNoHeatCapacity()
    {
        var solution = new Solution();
        Assert.That(solution.GetHeatCapacity(null), Is.EqualTo(0.0f));
    }

    [Test]
    public void AddReagentWithNullTemperatureDoesNotEffectTemperature()
    {
        const float initialTemp = 100.0f;

        var solution = new Solution(Water, FixedPoint2.New(100)) { Temperature = initialTemp };

        solution.AddReagent(Water, FixedPoint2.New(100));
        Assert.That(solution.Temperature, Is.EqualTo(initialTemp));

        solution.AddReagent(Earth, FixedPoint2.New(100));
        Assert.That(solution.Temperature, Is.EqualTo(initialTemp));
    }

    [Test]
    public void AddSolutionWithEqualTemperatureDoesNotChangeTemperature()
    {
        const float initialTemp = 100.0f;

        var solutionOne = new Solution();
        solutionOne.AddReagent(Water, FixedPoint2.New(100));
        solutionOne.Temperature = initialTemp;

        var solutionTwo = new Solution();
        solutionTwo.AddReagent(Water, FixedPoint2.New(100));
        solutionTwo.AddReagent(Earth, FixedPoint2.New(100));
        solutionTwo.Temperature = initialTemp;

        solutionOne.AddSolution(solutionTwo, null);
        Assert.That(solutionOne.Temperature, Is.EqualTo(initialTemp));
    }

    [Test]
    public void RemoveReagentDoesNotEffectTemperature()
    {
        const float initialTemp = 100.0f;

        var solution = new Solution(Water, FixedPoint2.New(100)) { Temperature = initialTemp };
        solution.RemoveReagent(Water, FixedPoint2.New(50));
        Assert.That(solution.Temperature, Is.EqualTo(initialTemp));
    }

    [Test]
    public void RemoveSolutionDoesNotEffectTemperature()
    {
        const float initialTemp = 100.0f;

        var solution = new Solution(Water, FixedPoint2.New(100)) { Temperature = initialTemp };
        solution.RemoveSolution(FixedPoint2.New(50));
        Assert.That(solution.Temperature, Is.EqualTo(initialTemp));
    }

    [Test]
    public void SplitSolutionDoesNotEffectTemperature()
    {
        const float initialTemp = 100.0f;

        var solution = new Solution(Water, FixedPoint2.New(100)) { Temperature = initialTemp };
        solution.SplitSolution(FixedPoint2.New(50));
        Assert.That(solution.Temperature, Is.EqualTo(initialTemp));
    }

    #endregion Thermal Energy and Temperature
}
