using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
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
        var prototypes = protoMan.EnumeratePrototypes<GuideEntryPrototype>().ToList();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var doc = new Document();
                foreach (var proto in prototypes)
                {
                    try
                    {
                        var text = resMan.ContentFileReadText(proto.Text).ReadToEnd();
                        Assert.That(doc.TryAddMarkup(text), $"Failed to add markup. while processing guide prototype {proto.ID}.");
                        doc.RemoveAllChildren();
                    }
                    catch (Exception e)
                    {
                        Assert.Fail($"Caught exception while processing guide prototype {proto.ID}. Exception: {e}");
                    }
                }
            });
        });
    }
}
