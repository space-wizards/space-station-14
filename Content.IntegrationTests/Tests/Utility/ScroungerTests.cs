using Content.IntegrationTests.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Utility;

[TestOf(typeof(GameDataScrounger))]
public sealed class ScroungerTests
{
    [Test]
    [Description("Assert that the data scrounger finds prototypes by type successfully.")]
    public void ScroungeByType()
    {
        var scrounged = GameDataScrounger.PrototypesOfKind<EntityPrototype>();
        Assert.That(scrounged, Is.Not.Empty);
    }

    [Test]
    [Description("Assert that the data scrounger finds all files by pattern in a directory successfully.")]
    [TestCase("*.yml")]
    [TestCase("*.txt")]
    public void ScroungeByPattern(string pattern)
    {
        var files = GameDataScrounger.FilesInDirectory("/", pattern);

        Assert.That(files, Is.Not.Empty);
    }

    [Test]
    [Description("Assert that the data scrounger finds all files by pattern in a directory successfully, and returns valid VFS paths.")]
    public void ScroungeByPatternInVfs()
    {
        var files = GameDataScrounger.FilesInDirectoryInVfs("/Maps", "*.yml");

        Assert.That(files, Is.Not.Empty);

        Assert.That(files[0].IsRooted, Is.True);
        Assert.That(files[0].ToString(), Does.StartWith("/Maps/"));
    }

    [Test]
    [Description("Assert that the data scrounger finds entities by component successfully.")]
    public void ScroungeByComponent()
    {
        var items = GameDataScrounger.EntitiesWithComponent("Item");

        Assert.That(items, Is.Not.Empty);
    }
}
