using System;
using System.Collections.Generic;
using Content.Shared.Wires;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Shared;

/// <summary>
/// Test that wire enums are setup properly.
/// </summary>

[TestFixture, TestOf(typeof(WireColor)), TestOf(typeof(WireLetter)), Parallelizable(ParallelScope.Self)]
public sealed class WireHackingTest : RobustUnitTest
{
    private static readonly IEnumerable<WireColor> ColorValues = (WireColor[]) Enum.GetValues(typeof(WireColor));
    private static readonly IEnumerable<WireLetter> LetterValues = (WireLetter[]) Enum.GetValues(typeof(WireLetter));

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
