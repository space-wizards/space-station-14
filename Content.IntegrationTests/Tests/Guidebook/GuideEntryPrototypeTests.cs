using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Guidebook;
using Robust.Shared.Utility;
using Robust.Shared.Localization;

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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var loc = client.ResolveDependency<ILocalizationManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();
        var prototypes = protoMan.EnumeratePrototypes<GuideEntryPrototype>().ToList();

        foreach (var proto in prototypes)
        {
            await client.WaitAssertion(() =>
            {
                var path = proto.Text;

                if (path.Contains("loc") && loc.DefaultCulture is not null)
                {
                    path = path.Replace("loc", loc.DefaultCulture.ToString());
                }

                using var reader = resMan.ContentFileReadText(new ResPath(proto.Text));
                var text = reader.ReadToEnd();
                Assert.That(parser.TryAddMarkup(new Document(), text), $"Failed to parse guidebook: {proto.Id}");
            });

            // Avoid styleguide update limit
            await client.WaitRunTicks(1);
        }

        await pair.CleanReturnAsync();
    }
}
