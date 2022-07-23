using System;
using System.Collections.Generic;
using Content.Shared.Wires;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Shared
{
    // Making sure nobody forgets to set values for these wire colors/letters.
    // Also a thinly veiled excuse to bloat the test count.

    [TestFixture]
    public sealed class WireHackingTest : RobustUnitTest
    {
        public static IEnumerable<WireColor> ColorValues = (WireColor[]) Enum.GetValues(typeof(WireColor));
        public static IEnumerable<WireLetter> LetterValues = (WireLetter[]) Enum.GetValues(typeof(WireLetter));

        [Test]
        public void TestColorNameExists([ValueSource(nameof(ColorValues))] WireColor color)
        {
            Assert.DoesNotThrow(() => color.Name());
        }

        [Test]
        public void TestColorValueExists([ValueSource(nameof(ColorValues))] WireColor color)
        {
            Assert.DoesNotThrow(() => color.ColorValue());
        }

        [Test]
        public void TestLetterNameExists([ValueSource(nameof(LetterValues))] WireLetter letter)
        {
            Assert.DoesNotThrow(() => letter.Name());
        }

        [Test]
        public void TestLetterLetterExists([ValueSource(nameof(LetterValues))] WireLetter letter)
        {
            Assert.DoesNotThrow(() => letter.Letter());
        }
    }
}
