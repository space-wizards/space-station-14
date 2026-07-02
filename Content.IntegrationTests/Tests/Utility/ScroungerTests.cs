using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.IntegrationTests.Utility;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Utility;

[TestOf(typeof(GameDataScrounger))]
public sealed class ScroungerTests
{
    private TestPair _pair;

    private static IEnumerable<Type> PrototypeTypes => GameDataScrounger.FindTypesWithAttribute<PrototypeAttribute>();

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _pair = await PoolManager.GetServerClient();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        // We never actually run anything on the pair, so we can just give it back always.
        await _pair.CleanReturnAsync();
    }

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

    [Test]
    [Description("Assert that the discovered prototypes correspond precisely with the real set of prototypes, minus test suite prototypes.")]
    [TestCaseSource(nameof(PrototypeTypes))]
    public void Prototypes_gh43275(Type t)
    {
        // TODO: EntryPoint based check for this, in another PR.
        var clientSided = t.FullName!.StartsWith("Robust.Client") || t.FullName.StartsWith("Content.Client");

        var protoMan = clientSided ? _pair.Client.ProtoMan : _pair.Server.ProtoMan;

        var scroungedProtos = GameDataScrounger.PrototypesOfKind(t);
        var realProtos = protoMan.EnumeratePrototypes(t)
            .Select(x => x.ID)
            .Where(x => !_pair.IsTestPrototype(t, x));

        Assert.That(scroungedProtos, Is.EquivalentTo(realProtos));
    }
}
