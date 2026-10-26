using System;
using Content.Client.UserInterface.Controls;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Client.UserInterface;

[TestFixture]
[TestOf(typeof(RadialMenu))]
public sealed class RadialMenuOptionComparerTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    private sealed class TestRadialMenuOption : RadialMenuOptionBase;
    private sealed class OtherTestRadialMenuOption : RadialMenuOptionBase;

    [Test]
    [Description("Tests that radial menu options are sorted correctly.")]
    public void TestSortingArray()
    {
        var comparer = new RadialMenuOptionComparer();

        // Named based on their expected position in the returned array, for sanity.
        var option1 = new TestRadialMenuOption
        {
            Order = 1,
            ToolTip = "First.",
        };
        var option2 = new OtherTestRadialMenuOption
        {
            Order = 2,
            ToolTip = "Before Option3, despite the equal order!"
        };
        var option3 = new TestRadialMenuOption
        {
            Order = 2
        };
        var option4 = new OtherTestRadialMenuOption
        {
            Order = null,
            ToolTip = "Not last!"
        };
        var option5 = new TestRadialMenuOption
        {
            Order = null
        };

        // Out of order.
        var radialOptions = new RadialMenuOptionBase[] {
            option3,
            option4,
            option1,
            option5,
            option2
        };

        Array.Sort(radialOptions, comparer);

        Assert.That(radialOptions.Length, Is.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(radialOptions[0], Is.SameAs(option1));
            Assert.That(radialOptions[1], Is.SameAs(option2));
            Assert.That(radialOptions[2], Is.SameAs(option3));
            Assert.That(radialOptions[3], Is.SameAs(option4));
            Assert.That(radialOptions[4], Is.SameAs(option5));
        }
    }
}
