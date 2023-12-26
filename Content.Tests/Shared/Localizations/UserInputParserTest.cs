using System.Globalization;
using System.Linq;
using Content.Shared.Localizations;
using Moq;
using NUnit.Framework;
using Robust.Client.Timing;
using Robust.Shared.Localization;

namespace Content.Tests.Shared.Localizations
{
    [TestFixture]
    public sealed class UserInputParserTest
    {
        [Test]
        [TestCase(null, new string[] { }, "1234.56", 1234.56f, true)]
        [TestCase(null, new string[] { }, "1234,56", 1234.56f, true)]
        [TestCase("en-US", new string[] { }, "1234.56", 1234.56f, true)]
        [TestCase("en-US", new string[] { }, "1234,56", 1234.56f, true)]
        [TestCase("en-SE", new string[] { }, "1234.56", 1234.56f, true)]
        [TestCase("en-SE", new string[] { }, "1234,56", 1234.56f, true)]
        [TestCase("en-SE", new[] { "en-US" }, "1234.56", 1234.56f, true)]
        [TestCase("en-SE", new[] { "en-US" }, "1234,56", 1234.56f, true)]
        public void TryFloatTest(string defaultCulture, string[] fallbackCultures, string input, float expectedOutput,
            bool expectedResult)
        {
            var locMock = new Mock<ILocalizationManager>();
            locMock.Setup(m => m.DefaultCulture)
                .Returns(defaultCulture == null ? null : new CultureInfo(defaultCulture));
            locMock.Setup(m => m.FallbackCultures)
                .Returns(fallbackCultures.Select(culture => new CultureInfo(culture)).ToArray());

            var result = UserInputParser.TryFloat(input, locMock.Object, out var output);

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(output, Is.EqualTo(expectedOutput).Within(float.Epsilon));
        }

        [Test]
        [TestCase(null, new string[] { }, "1234.56", 1234.56d, true)]
        [TestCase(null, new string[] { }, "1234,56", 1234.56d, true)]
        [TestCase("en-US", new string[] { }, "1234.56", 1234.56d, true)]
        [TestCase("en-US", new string[] { }, "1234,56", 1234.56d, true)]
        [TestCase("en-SE", new string[] { }, "1234.56", 1234.56d, true)]
        [TestCase("en-SE", new string[] { }, "1234,56", 1234.56d, true)]
        [TestCase("en-SE", new[] { "en-US" }, "1234.56", 1234.56d, true)]
        [TestCase("en-SE", new[] { "en-US" }, "1234,56", 1234.56d, true)]
        public void TryDoubleTest(string defaultCulture, string[] fallbackCultures, string input, double expectedOutput,
            bool expectedResult)
        {
            var locMock = new Mock<ILocalizationManager>();
            locMock.Setup(m => m.DefaultCulture)
                .Returns(defaultCulture == null ? null : new CultureInfo(defaultCulture));
            locMock.Setup(m => m.FallbackCultures)
                .Returns(fallbackCultures.Select(culture => new CultureInfo(culture)).ToArray());

            var result = UserInputParser.TryDouble(input, locMock.Object, out var output);

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(output, Is.EqualTo(expectedOutput).Within(double.Epsilon));
        }
    }
}
