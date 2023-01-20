using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideEntryPrototypeTests
{
    [Test]
    public async Task ValidatePrototypeContents()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var client = pairTracker.Pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();
        var prototypes = protoMan.EnumeratePrototypes<GuideEntryPrototype>().ToList();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in prototypes)
                {
                    var text = resMan.ContentFileReadText(proto.Text).ReadToEnd();
                    Assert.That(parser.TryAddMarkup(new Document(), text), $"Failed to parse guidebook: {proto.Id}");
                }
            });
        });

        await pairTracker.CleanReturnAsync();
    }
}
