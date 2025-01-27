using System.Collections.Generic;
using System.Linq;
using Content.Server.Heretic.Ritual;
using Content.Shared.Dataset;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation.Heretic;

[TestFixture, TestOf(typeof(RitualKnowledgeBehavior))]
public sealed class RitualKnowledgeTests
{
    [Test]
    public async Task ValidateEligibleTags()
    {
        // As far as I can tell, there's no annotation to validate
        // a dataset of tag prototype IDs, so we'll have to do it
        // in a test fixture. Sad.

        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            // Get the eligible tags prototype
            var dataset = protoMan.Index<DatasetPrototype>(RitualKnowledgeBehavior.EligibleTagsDataset);

            // Validate that every value is a valid tag
            Assert.Multiple(() =>
            {
                foreach (var tagId in dataset.Values)
                {
                    Assert.That(protoMan.TryIndex<TagPrototype>(tagId, out var tagProto), Is.True, $"\"{tagId}\" is not a valid tag prototype ID");
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ValidateTagsHaveItems()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFactory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            // Get the eligible tags prototype
            var dataset = protoMan.Index<DatasetPrototype>(RitualKnowledgeBehavior.EligibleTagsDataset).Values.ToHashSet();

            // Loop through every entity prototype and assemble a used tags set
            var usedTags = new HashSet<string>();

            // Ensure that every tag is used by a non-abstract entity
            foreach (var entProto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (entProto.Abstract)
                    continue;

                if (entProto.TryGetComponent<TagComponent>(out var tags, compFactory))
                {
                    usedTags.UnionWith(tags.Tags.Select(t => t.Id));
                }
            }

            var unusedTags = dataset.Except(usedTags).ToHashSet();
            Assert.That(unusedTags, Is.Empty, $"The following ritual item tags are not used by any obtainable entity prototypes: {string.Join(", ", unusedTags)}");
        });

        await pair.CleanReturnAsync();
    }
}
