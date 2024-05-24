#nullable enable
using System.Collections.Generic;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Tag
{
    [TestFixture]
    [TestOf(typeof(TagComponent))]
    public sealed class TagTest
    {
        private const string TagEntityId = "TagTestDummy";

        // Register these three into the prototype manager
        private const string StartingTag = "StartingTagDummy";
        private const string AddedTag = "AddedTagDummy";
        private const string UnusedTag = "UnusedTagDummy";

        // Do not register this one
        private const string UnregisteredTag = "AAAAAAAAA";

        [TestPrototypes]
        private const string Prototypes = $@"
- type: Tag
  id: {StartingTag}

- type: Tag
  id: {AddedTag}

- type: Tag
  id: {UnusedTag}

- type: entity
  id: {TagEntityId}
  name: {TagEntityId}
  components:
  - type: Tag
    tags:
    - {StartingTag}";

        [Test]
        public async Task TagComponentTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var entManager = server.ResolveDependency<IEntitySystemManager>();

            EntityUid sTagDummy = default;
            TagComponent sTagComponent = null!;

            await server.WaitPost(() =>
            {
                sTagDummy = sEntityManager.SpawnEntity(TagEntityId, MapCoordinates.Nullspace);
                sTagComponent = sEntityManager.GetComponent<TagComponent>(sTagDummy);
            });

            await server.WaitAssertion(() =>
            {
                var tagSystem = entManager.GetEntitySystem<TagSystem>();
                // Has one tag, the starting tag
                Assert.That(sTagComponent.Tags, Has.Count.EqualTo(1));
                sPrototypeManager.Index<TagPrototype>(StartingTag);
                Assert.Multiple(() =>
                {
                    Assert.That(sTagComponent.Tags, Contains.Item(StartingTag));

                    // Single
                    Assert.That(tagSystem.HasTag(sTagDummy, StartingTag));
                    Assert.That(tagSystem.HasTag(sTagComponent, StartingTag));

                    // Any
                    Assert.That(tagSystem.HasAnyTag(sTagDummy, StartingTag));
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag));

                    // All
                    Assert.That(tagSystem.HasAllTags(sTagDummy, StartingTag));
                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag));
                });

                // Does not have the added tag
                var addedTagPrototype = sPrototypeManager.Index<TagPrototype>(AddedTag);
                Assert.Multiple(() =>
                {
                    Assert.That(sTagComponent.Tags, Does.Not.Contains(addedTagPrototype));

                    // Single
                    Assert.That(tagSystem.HasTag(sTagDummy, AddedTag), Is.False);
                    Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.False);

                    // Any
                    Assert.That(tagSystem.HasAnyTag(sTagDummy, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag), Is.False);

                    // All
                    Assert.That(tagSystem.HasAllTags(sTagDummy, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag), Is.False);
                });

                // Does not have the unused tag
                var unusedTagPrototype = sPrototypeManager.Index<TagPrototype>(UnusedTag);
                Assert.Multiple(() =>
                {
                    Assert.That(sTagComponent.Tags, Does.Not.Contains(unusedTagPrototype));

                    // Single
                    Assert.That(tagSystem.HasTag(sTagDummy, UnusedTag), Is.False);
                    Assert.That(tagSystem.HasTag(sTagComponent, UnusedTag), Is.False);

                    // Any
                    Assert.That(tagSystem.HasAnyTag(sTagDummy, UnusedTag), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, UnusedTag), Is.False);

                    // All
                    Assert.That(tagSystem.HasAllTags(sTagDummy, UnusedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, UnusedTag), Is.False);
                });

                // Throws when checking for an unregistered tag
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sPrototypeManager.Index<TagPrototype>(UnregisteredTag);
                });

                Assert.Multiple(() =>
                {
                    // Cannot add the starting tag again
                    Assert.That(tagSystem.AddTag(sTagDummy, sTagComponent, StartingTag), Is.False);
                    Assert.That(tagSystem.AddTags(sTagDummy, sTagComponent, StartingTag, StartingTag), Is.False);
                    Assert.That(tagSystem.AddTags(sTagDummy, sTagComponent, new List<string> { StartingTag, StartingTag }), Is.False);

                    // Has the starting tag
                    Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> { StartingTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> { StartingTag, StartingTag }), Is.True);

                    // Does not have the added tag yet
                    Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> { AddedTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> { AddedTag, AddedTag }), Is.False);

                    // Has a combination of the two tags
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> { StartingTag, AddedTag }), Is.True);

                    // Does not have both tags
                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> { StartingTag, AddedTag }), Is.False);

                    // Cannot remove a tag that does not exist
                    Assert.That(tagSystem.RemoveTag(sTagDummy, sTagComponent, AddedTag), Is.False);
                    Assert.That(tagSystem.RemoveTags(sTagDummy, sTagComponent, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.RemoveTags(sTagDummy, sTagComponent, new List<string> { AddedTag, AddedTag }), Is.False);
                });

                // Can add the new tag
                Assert.That(tagSystem.AddTag(sTagDummy, sTagComponent, AddedTag), Is.True);

                Assert.Multiple(() =>
                {
                    // Cannot add it twice
                    Assert.That(tagSystem.AddTag(sTagDummy, sTagComponent, AddedTag), Is.False);

                    // Cannot add existing tags
                    Assert.That(tagSystem.AddTags(sTagDummy, sTagComponent, StartingTag, AddedTag), Is.False);
                    Assert.That(tagSystem.AddTags(sTagDummy, sTagComponent, new List<string> { StartingTag, AddedTag }), Is.False);

                    // Now has two tags
                    Assert.That(sTagComponent.Tags, Has.Count.EqualTo(2));

                    // Has both tags
                    Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);
                    Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> { AddedTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, StartingTag), Is.True);
                });

                Assert.Multiple(() =>
                {
                    // Remove the existing starting tag
                    Assert.That(tagSystem.RemoveTag(sTagDummy, sTagComponent, StartingTag), Is.True);

                    // Remove the existing added tag
                    Assert.That(tagSystem.RemoveTags(sTagDummy, sTagComponent, AddedTag, AddedTag), Is.True);
                });

                Assert.Multiple(() =>
                {
                    // No tags left to remove
                    Assert.That(tagSystem.RemoveTags(sTagDummy, sTagComponent, new List<string> { StartingTag, AddedTag }), Is.False);

                    // No tags left in the component
                    Assert.That(sTagComponent.Tags, Is.Empty);
                });

#if !DEBUG
                return;
#endif

                // Single
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasTag(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasTag(sTagComponent, UnregisteredTag);
                });

                // Any
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasAnyTag(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasAnyTag(sTagComponent, UnregisteredTag);
                });

                // All
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasAllTags(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<DebugAssertException>(() =>
                {
                    tagSystem.HasAllTags(sTagComponent, UnregisteredTag);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
