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
            solution.AddReagent("water", 1000);
            var quantity = solution.GetReagentQuantity("water");
            
            Assert.That(quantity, Is.EqualTo(1000));
        }

        [Test]
        public void ConstructorAddReagent()
        {
            var solution = new Solution("water", 1000);
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity, Is.EqualTo(1000));
        }

        [Test]
        public void NonExistingReagentReturnsZero()
        {
            var solution = new Solution();
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity, Is.EqualTo(0));
        }

        [Test]
        public void AddLessThanZeroReagentReturnsZero()
        {
            var solution = new Solution("water", -1000);
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity, Is.EqualTo(0));
        }

        [Test]
        public void AddingReagentsSumsProperly()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("water", 2000);
            var quantity = solution.GetReagentQuantity("water");

            Assert.That(quantity, Is.EqualTo(3000));
        }

        [Test]
        public void ReagentQuantitiesStayUnique()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(1000));
            Assert.That(solution.GetReagentQuantity("fire"), Is.EqualTo(2000));
        }

        [Test]
        public void TotalVolumeIsCorrect()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            Assert.That(solution.TotalVolume, Is.EqualTo(3000));
        }

        [Test]
        public void CloningSolutionIsCorrect()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            var newSolution = solution.Clone();

            Assert.That(newSolution.GetReagentQuantity("water"), Is.EqualTo(1000));
            Assert.That(newSolution.GetReagentQuantity("fire"), Is.EqualTo(2000));
            Assert.That(newSolution.TotalVolume, Is.EqualTo(3000));
        }

        [Test]
        public void RemoveSolutionRecalculatesProperly()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            solution.RemoveReagent("water", 500);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(500));
            Assert.That(solution.GetReagentQuantity("fire"), Is.EqualTo(2000));
            Assert.That(solution.TotalVolume, Is.EqualTo(2500));
        }

        [Test]
        public void RemoveLessThanOneQuantityDoesNothing()
        {
            var solution = new Solution("water", 100);

            solution.RemoveReagent("water", -100);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(100));
            Assert.That(solution.TotalVolume, Is.EqualTo(100));
        }

        [Test]
        public void RemoveMoreThanTotalRemovesAllReagent()
        {
            var solution = new Solution("water", 100);

            solution.RemoveReagent("water", 1000);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(0));
            Assert.That(solution.TotalVolume, Is.EqualTo(0));
        }

        [Test]
        public void RemoveNonExistReagentDoesNothing()
        {
            var solution = new Solution("water", 100);

            solution.RemoveReagent("fire", 1000);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(100));
            Assert.That(solution.TotalVolume, Is.EqualTo(100));
        }

        [Test]
        public void RemoveSolution()
        {
            var solution = new Solution("water", 700);

            solution.RemoveSolution(500,  out var removedSolution);

            //Check that edited solution is correct
            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(200));
            Assert.That(solution.TotalVolume, Is.EqualTo(200));
            //Check that removed solution is correct
            Assert.That(removedSolution.GetReagentQuantity("water"), Is.EqualTo(500));
            Assert.That(removedSolution.TotalVolume, Is.EqualTo(500));
        }

        [Test]
        public void RemoveSolutionMoreThanTotalRemovesAll()
        {
            var solution = new Solution("water", 800);

            solution.RemoveSolution(1000, out var removedSolution);

            //Check that edited solution is correct
            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(0));
            Assert.That(solution.TotalVolume, Is.EqualTo(0));
            //Check that removed solution is correct
            Assert.That(removedSolution.GetReagentQuantity("water"), Is.EqualTo(800));
            Assert.That(removedSolution.TotalVolume, Is.EqualTo(800));
        }

        [Test]
        public void RemoveSolutionRatioPreserved()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            solution.RemoveSolution(1500, out var removedSolution);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(500));
            Assert.That(solution.GetReagentQuantity("fire"), Is.EqualTo(1000));
            Assert.That(solution.TotalVolume, Is.EqualTo(1500));

            Assert.That(removedSolution.GetReagentQuantity("water"), Is.EqualTo(500));
            Assert.That(removedSolution.GetReagentQuantity("fire"), Is.EqualTo(1000));
            Assert.That(removedSolution.TotalVolume, Is.EqualTo(1500));
        }

        [Test]
        public void RemoveSolutionLessThanOneDoesNothing()
        {
            var solution = new Solution("water", 800);

            solution.RemoveSolution(-200, out var removedSolution);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(800));
            Assert.That(solution.TotalVolume, Is.EqualTo(800));

            Assert.That(removedSolution.GetReagentQuantity("water"), Is.EqualTo(0));
            Assert.That(removedSolution.TotalVolume, Is.EqualTo(0));
        }

        [Test]
        public void SplitSolution()
        {
            var solution = new Solution();
            solution.AddReagent("water", 1000);
            solution.AddReagent("fire", 2000);

            var splitSolution = solution.SplitSolution(750);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(750));
            Assert.That(solution.GetReagentQuantity("fire"), Is.EqualTo(1500));
            Assert.That(solution.TotalVolume, Is.EqualTo(2250));

            Assert.That(splitSolution.GetReagentQuantity("water"), Is.EqualTo(250));
            Assert.That(splitSolution.GetReagentQuantity("fire"), Is.EqualTo(500));
            Assert.That(splitSolution.TotalVolume, Is.EqualTo(750));
        }

        [Test]
        public void SplitSolutionMoreThanTotalRemovesAll()
        {
            var solution = new Solution("water", 800);

            var splitSolution = solution.SplitSolution(1000);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(0));
            Assert.That(solution.TotalVolume, Is.EqualTo(0));

            Assert.That(splitSolution.GetReagentQuantity("water"), Is.EqualTo(800));
            Assert.That(splitSolution.TotalVolume, Is.EqualTo(800));
        }

        [Test]
        public void SplitSolutionLessThanOneDoesNothing()
        {
            var solution = new Solution("water", 800);

            var splitSolution = solution.SplitSolution(-200);

            Assert.That(solution.GetReagentQuantity("water"), Is.EqualTo(800));
            Assert.That(solution.TotalVolume, Is.EqualTo(800));

            Assert.That(splitSolution.GetReagentQuantity("water"), Is.EqualTo(0));
            Assert.That(splitSolution.TotalVolume, Is.EqualTo(0));
        }

        [Test]
        public void AddSolution()
        {
            var solutionOne = new Solution();
            solutionOne.AddReagent("water", 1000);
            solutionOne.AddReagent("fire", 2000);

            var solutionTwo = new Solution();
            solutionTwo.AddReagent("water", 500);
            solutionTwo.AddReagent("earth", 1000);

            solutionOne.AddSolution(solutionTwo);

            Assert.That(solutionOne.GetReagentQuantity("water"), Is.EqualTo(1500));
            Assert.That(solutionOne.GetReagentQuantity("fire"), Is.EqualTo(2000));
            Assert.That(solutionOne.GetReagentQuantity("earth"), Is.EqualTo(1000));
            Assert.That(solutionOne.TotalVolume, Is.EqualTo(4500));
        }
    }
}
