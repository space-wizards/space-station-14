using Content.Shared.Localizations;
using NUnit.Framework;

namespace Content.Tests.Shared.Localizations
{
    [TestFixture]
    public sealed class UserInputParserTest
    {
        [Test]
        [TestCase("1234.56", 1234.56f, true)]
        [TestCase("1234,56", 1234.56f, true)]
        [TestCase(" +1234.56 ", 1234.56f, true)]
        [TestCase(" -1234.56 ", -1234.56f, true)]
        [TestCase("1234.56e7", 0f, false)]
        [TestCase("1,234.56", 0f, false)]
        [TestCase("1 234,56", 0f, false)]
        public void TryFloatTest(string input, float expectedOutput, bool expectedResult)
        {
            var result = UserInputParser.TryFloat(input, out var output);

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(output, Is.EqualTo(expectedOutput).Within(float.Epsilon));
        }

        [Test]
        [TestCase("1234.56", 1234.56d, true)]
        [TestCase("1234,56", 1234.56d, true)]
        [TestCase(" +1234.56 ", 1234.56d, true)]
        [TestCase(" -1234.56 ", -1234.56d, true)]
        [TestCase("1234.56e7", 0d, false)]
        [TestCase("1,234.56", 0d, false)]
        [TestCase("1 234,56", 0d, false)]
        public void TryDoubleTest(string input, double expectedOutput, bool expectedResult)
        {
            var result = UserInputParser.TryDouble(input, out var output);

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(output, Is.EqualTo(expectedOutput).Within(double.Epsilon));
        }
    }
}
