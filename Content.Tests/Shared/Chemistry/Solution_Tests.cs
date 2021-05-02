using Content.Shared.Chemistry;
using NUnit.Framework;

namespace Content.Tests.Shared.Chemistry
{
    [TestFixture, Parallelizable, TestOf(typeof(Solution))]
    public class Solution_Tests
    {
        [Test]
        public void AddReagentAndGetSolution()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity.Int(), Is.EqualTo(1000));
        }

        [Test]
        public void ConstructorAddReagent()
        {
            var solution = new Solution("water", ReagentUnit.New(1000));
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity.Int(), Is.EqualTo(1000));
        }

        [Test]
        public void NonExistingReagentReturnsZero()
        {
            var solution = new Solution();
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity.Int(), Is.EqualTo(0));
        }

        [Test]
        public void AddLessThanZeroReagentReturnsZero()
        {
            var solution = new Solution("water", ReagentUnit.New(-1000));
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity.Int(), Is.EqualTo(0));
        }

        [Test]
        public void AddingReagentsSumsProperly()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("water", ReagentUnit.New(2000));
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity.Int(), Is.EqualTo(3000));
        }

        [Test]
        public void ReagentQuantitiesStayUnique()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(1000));
            Assert.That(solution.GetReagentQuantity("fire").Int(), Is.EqualTo(2000));
        }

        [Test]
        public void TotalVolumeIsCorrect()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(3000));
        }

        [Test]
        public void CloningSolutionIsCorrect()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            var newSolution = solution.Clone();

            Assert.That(newSolution.GetReagentQuantity("water").Int(), Is.EqualTo(1000));
            Assert.That(newSolution.GetReagentQuantity("fire").Int(), Is.EqualTo(2000));
            Assert.That(newSolution.TotalVolume.Int(), Is.EqualTo(3000));
        }

        [Test]
        public void RemoveSolutionRecalculatesProperly()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            solution.RemoveReagent("water", ReagentUnit.New(500));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(500));
            Assert.That(solution.GetReagentQuantity("fire").Int(), Is.EqualTo(2000));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(2500));
        }

        [Test]
        public void RemoveLessThanOneQuantityDoesNothing()
        {
            var solution = new Solution("water", ReagentUnit.New(100));

            solution.RemoveReagent("water", ReagentUnit.New(-100));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(100));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(100));
        }

        [Test]
        public void RemoveMoreThanTotalRemovesAllReagent()
        {
            var solution = new Solution("water", ReagentUnit.New(100));

            solution.RemoveReagent("water", ReagentUnit.New(1000));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(0));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(0));
        }

        [Test]
        public void RemoveNonExistReagentDoesNothing()
        {
            var solution = new Solution("water", ReagentUnit.New(100));

            solution.RemoveReagent("fire", ReagentUnit.New(1000));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(100));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(100));
        }

        [Test]
        public void RemoveSolution()
        {
            var solution = new Solution("water", ReagentUnit.New(700));

            solution.RemoveSolution(ReagentUnit.New(500));

            //Check that edited solution is correct
            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(200));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(200));
        }

        [Test]
        public void RemoveSolutionMoreThanTotalRemovesAll()
        {
            var solution = new Solution("water", ReagentUnit.New(800));

            solution.RemoveSolution(ReagentUnit.New(1000));

            //Check that edited solution is correct
            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(0));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(0));
        }

        [Test]
        public void RemoveSolutionRatioPreserved()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            solution.RemoveSolution(ReagentUnit.New(1500));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(500));
            Assert.That(solution.GetReagentQuantity("fire").Int(), Is.EqualTo(1000));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(1500));
        }

        [Test]
        public void RemoveSolutionLessThanOneDoesNothing()
        {
            var solution = new Solution("water", ReagentUnit.New(800));

            solution.RemoveSolution(ReagentUnit.New(-200));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(800));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(800));
        }

        [Test]
        public void SplitSolution()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1000));
            solution.AddReagent("fire", ReagentUnit.New(2000));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(750));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(750));
            Assert.That(solution.GetReagentQuantity("fire").Int(), Is.EqualTo(1500));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(2250));

            Assert.That(splitSolution.GetReagentQuantity("water").Int(), Is.EqualTo(250));
            Assert.That(splitSolution.GetReagentQuantity("fire").Int(), Is.EqualTo(500));
            Assert.That(splitSolution.TotalVolume.Int(), Is.EqualTo(750));
        }

        [Test]
        public void SplitSolutionFractional()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1));
            solution.AddReagent("fire", ReagentUnit.New(2));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(1));

            Assert.That(solution.GetReagentQuantity("water").Float(), Is.EqualTo(0.67f));
            Assert.That(solution.GetReagentQuantity("fire").Float(), Is.EqualTo(1.33f));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(2));

            Assert.That(splitSolution.GetReagentQuantity("water").Float(), Is.EqualTo(0.33f));
            Assert.That(splitSolution.GetReagentQuantity("fire").Float(), Is.EqualTo(0.67f));
            Assert.That(splitSolution.TotalVolume.Int(), Is.EqualTo(1));
        }

        [Test]
        public void SplitSolutionFractionalOpposite()
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(1));
            solution.AddReagent("fire", ReagentUnit.New(2));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(2));

            Assert.That(solution.GetReagentQuantity("water").Float(), Is.EqualTo(0.33f));
            Assert.That(solution.GetReagentQuantity("fire").Float(), Is.EqualTo(0.67f));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(1));

            Assert.That(splitSolution.GetReagentQuantity("water").Float(), Is.EqualTo(0.67f));
            Assert.That(splitSolution.GetReagentQuantity("fire").Float(), Is.EqualTo(1.33f));
            Assert.That(splitSolution.TotalVolume.Int(), Is.EqualTo(2));
        }

        [Test]
        [TestCase(0.03f, 0.01f, 0.02f)]
        [TestCase(0.03f, 0.02f, 0.01f)]
        public void SplitSolutionTinyFractionalBigSmall(float initial, float reduce, float remainder)
        {
            var solution = new Solution();
            solution.AddReagent("water", ReagentUnit.New(initial));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(reduce));

            Assert.That(solution.GetReagentQuantity("water").Float(), Is.EqualTo(remainder));
            Assert.That(solution.TotalVolume.Float(), Is.EqualTo(remainder));

            Assert.That(splitSolution.GetReagentQuantity("water").Float(), Is.EqualTo(reduce));
            Assert.That(splitSolution.TotalVolume.Float(), Is.EqualTo(reduce));
        }

        [Test]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void SplitRounding(int amount)
        {
            var solutionOne = new Solution();
            solutionOne.AddReagent("foo", ReagentUnit.New(amount));
            solutionOne.AddReagent("bar", ReagentUnit.New(amount));
            solutionOne.AddReagent("baz", ReagentUnit.New(amount));

            var splitAmount = ReagentUnit.New(5);
            var split = solutionOne.SplitSolution(splitAmount);

            Assert.That(split.TotalVolume, Is.EqualTo(splitAmount));
        }

        [Test]
        public void SplitSolutionMoreThanTotalRemovesAll()
        {
            var solution = new Solution("water", ReagentUnit.New(800));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(1000));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(0));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(0));

            Assert.That(splitSolution.GetReagentQuantity("water").Int(), Is.EqualTo(800));
            Assert.That(splitSolution.TotalVolume.Int(), Is.EqualTo(800));
        }

        [Test]
        public void SplitSolutionLessThanOneDoesNothing()
        {
            var solution = new Solution("water", ReagentUnit.New(800));

            var splitSolution = solution.SplitSolution(ReagentUnit.New(-200));

            Assert.That(solution.GetReagentQuantity("water").Int(), Is.EqualTo(800));
            Assert.That(solution.TotalVolume.Int(), Is.EqualTo(800));

            Assert.That(splitSolution.GetReagentQuantity("water").Int(), Is.EqualTo(0));
            Assert.That(splitSolution.TotalVolume.Int(), Is.EqualTo(0));
        }

        [Test]
        public void SplitSolutionZero()
        {
            var solution = new Solution();
            solution.AddReagent("Impedrezene", ReagentUnit.New(0.01 + 0.19));
            solution.AddReagent("Thermite", ReagentUnit.New(0.01 + 0.39));
            solution.AddReagent("Li", ReagentUnit.New(0.01 + 0.17));
            solution.AddReagent("F", ReagentUnit.New(0.01 + 0.17));
            solution.AddReagent("Na", ReagentUnit.New(0 + 0.13));
            solution.AddReagent("Hg", ReagentUnit.New(0.15 + 4.15));
            solution.AddReagent("Cu", ReagentUnit.New(0 + 0.13));
            solution.AddReagent("U", ReagentUnit.New(0.76 + 20.77));
            solution.AddReagent("Fe", ReagentUnit.New(0.01 + 0.36));
            solution.AddReagent("SpaceDrugs", ReagentUnit.New(0.02 + 0.41));
            solution.AddReagent("Al", ReagentUnit.New(0));
            solution.AddReagent("Glucose", ReagentUnit.New(0));
            solution.AddReagent("O", ReagentUnit.New(0));

            solution.SplitSolution(ReagentUnit.New(0.98));
        }

        [Test]
        public void AddSolution()
        {
            var solutionOne = new Solution();
            solutionOne.AddReagent("water", ReagentUnit.New(1000));
            solutionOne.AddReagent("fire", ReagentUnit.New(2000));

            var solutionTwo = new Solution();
            solutionTwo.AddReagent("water", ReagentUnit.New(500));
            solutionTwo.AddReagent("earth", ReagentUnit.New(1000));

            solutionOne.AddSolution(solutionTwo);

            Assert.That(solutionOne.GetReagentQuantity("water").Int(), Is.EqualTo(1500));
            Assert.That(solutionOne.GetReagentQuantity("fire").Int(), Is.EqualTo(2000));
            Assert.That(solutionOne.GetReagentQuantity("earth").Int(), Is.EqualTo(1000));
            Assert.That(solutionOne.TotalVolume.Int(), Is.EqualTo(4500));
        }
    }
}
