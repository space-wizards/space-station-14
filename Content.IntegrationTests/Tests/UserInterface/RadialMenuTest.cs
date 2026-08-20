using Content.Client.UserInterface.Controls;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.UserInterface;

[TestFixture]
[TestOf(typeof(RadialMenu))]
public sealed class SimpleRadialMenuTest : GameTest
{
    private sealed class TestRadialMenuOption : RadialMenuOptionBase;
    private sealed class OtherTestRadialMenuOption : RadialMenuOptionBase;

    [Test]
    [RunOnSide(Side.Client)]
    [Description("Tests radial menu sorting order.")]
    public async Task TestSortingArray()
    {
        var radialMenu = new SimpleRadialMenu();

        var option1 = new TestRadialMenuOption
        {
            Order = 1,
            ToolTip = "First.",
        };
        var option2 = new TestRadialMenuOption
        {
            Order = 2
        };
        var option3 = new OtherTestRadialMenuOption
        {
            Order = 2,
            ToolTip = "Before OptionB!"
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

        radialMenu.SetButtons(radialOptions);

        Assert.That(radialMenu.ChildCount, Is.EqualTo(radialOptions.Length));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(radialMenu.GetChild(0), Is.EqualTo(option1));
            Assert.That(radialMenu.GetChild(1), Is.EqualTo(option2));
            Assert.That(radialMenu.GetChild(2), Is.EqualTo(option3));
            Assert.That(radialMenu.GetChild(3), Is.EqualTo(option4));
            Assert.That(radialMenu.GetChild(4), Is.EqualTo(option5));
        }
    }
}
