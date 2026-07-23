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
        var i = 0;
        var sheet = false;

        // Test subscribing to a stylesheet
        _stylesheet.StyleChanged += OnStyleChanged;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.Stylesheet, Is.Not.Null);
            Assert.That(control.Stylesheet, Is.SameAs(nanotrasen));
            Assert.That(i, Is.EqualTo(1));
        }

        // TODO: implement functionality that would actually trigger a rebuild and call it here.

        _stylesheet.StyleChanged -= OnStyleChanged;
        // Verify that it doesn't reset any values nor calls OnStyleChanged again.
        sheet = true;
        Assert.That(control.Stylesheet, Is.SameAs(nanotrasen));
        Assert.That(i, Is.EqualTo(1));

        // Test that it is called on subscription and changes the sheet.
        _stylesheet.StyleChanged += OnStyleChanged;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.Stylesheet, Is.SameAs(system));
            Assert.That(i, Is.EqualTo(2));
        }

        _stylesheet.StyleChanged -= OnStyleChanged;
        return;

        void OnStyleChanged(IStylesheetAccessor accessor)
        {
            i++;
            control.Stylesheet = sheet ? accessor.SheetSystem : accessor.SheetNanotrasen;
        }
    }
}
