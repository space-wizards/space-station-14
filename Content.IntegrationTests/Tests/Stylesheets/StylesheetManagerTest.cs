using Content.Client.Stylesheets;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Client.UserInterface;

namespace Content.IntegrationTests.Tests.Stylesheets;

[TestOf(typeof(StylesheetManager))]
public sealed class StylesheetManagerTest : GameTest
{
    [SidedDependency(Side.Client)] private readonly IStylesheetManager _stylesheet = default!;

    [Test]
    [Description("Checks the basic functionality of Stylesheet Manager")]
    [RunOnSide(Side.Client)]
    public void TestStylesheetManager()
    {
        // This test should also fail if there's a missing constraint for the stylesheets provided by the manager

#pragma warning disable CS0618
        // Check the old obsolete direct accessors still work
        var nanotrasen = _stylesheet.SheetNanotrasen;
        var system = _stylesheet.SheetSystem;

        // Test the string accessor method works
        // Remember to update if you change any of the string names
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_stylesheet.TryGetStylesheet("Nanotrasen", out _), Is.True);
            Assert.That(_stylesheet.TryGetStylesheet("System", out _), Is.True);
            Assert.That(_stylesheet.TryGetStylesheet("GoodUI", out _), Is.False);
        }
#pragma warning restore CS0618

        var control = new Control();
        _stylesheet.UseStylesheet(control, a => a.SheetNanotrasen);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.Stylesheet, Is.Not.Null);
            Assert.That(control.Stylesheet, Is.SameAs(nanotrasen));
        }

        _stylesheet.UseStylesheet(control, a => a.SheetSystem);
        Assert.That(control.Stylesheet, Is.SameAs(system));

        // We don't currently reset to defaults right now.
        _stylesheet.StopStylesheet(control);
        Assert.That(control.Stylesheet, Is.SameAs(system));
    }
}
