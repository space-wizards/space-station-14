#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Tag;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Tag
{
    [TestFixture]
    [TestOf(typeof(TagComponent))]
    public sealed class TagTest
    {
        private const string TagEntityId = "TagTestDummy";

        // Register these three into the prototype manager
        private const string StartingTag = "A";
        private const string AddedTag = "EIOU";
        private const string UnusedTag = "E";

        // Do not register this one
        private const string UnregisteredTag = "AAAAAAAAA";

        private static readonly string Prototypes = $@"
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var sMapManager = server.ResolveDependency<IMapManager>();
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
                Assert.That(sTagComponent.Tags.Count, Is.EqualTo(1));
                sPrototypeManager.Index<TagPrototype>(StartingTag);
                Assert.That(sTagComponent.Tags, Contains.Item(StartingTag));

                // Single
                Assert.True(tagSystem.HasTag(sTagDummy, StartingTag));
                Assert.True(tagSystem.HasTag(sTagComponent, StartingTag));

                // Any
                Assert.True(tagSystem.HasAnyTag(sTagDummy, StartingTag));
                Assert.True(tagSystem.HasAnyTag(sTagComponent, StartingTag));

                // All
                Assert.True(tagSystem.HasAllTags(sTagDummy, StartingTag));
                Assert.True(tagSystem.HasAllTags(sTagComponent, StartingTag));

                // Does not have the added tag
                var addedTagPrototype = sPrototypeManager.Index<TagPrototype>(AddedTag);
                Assert.That(sTagComponent.Tags, Does.Not.Contains(addedTagPrototype));

                // Single
                Assert.False(tagSystem.HasTag(sTagDummy, AddedTag));
                Assert.False(tagSystem.HasTag(sTagComponent, AddedTag));

                // Any
                Assert.False(tagSystem.HasAnyTag(sTagDummy, AddedTag));
                Assert.False(tagSystem.HasAnyTag(sTagComponent, AddedTag));

                // All
                Assert.False(tagSystem.HasAllTags(sTagDummy, AddedTag));
                Assert.False(tagSystem.HasAllTags(sTagComponent, AddedTag));

                // Does not have the unused tag
                var unusedTagPrototype = sPrototypeManager.Index<TagPrototype>(UnusedTag);
                Assert.That(sTagComponent.Tags, Does.Not.Contains(unusedTagPrototype));

                // Single
                Assert.False(tagSystem.HasTag(sTagDummy, UnusedTag));
                Assert.False(tagSystem.HasTag(sTagComponent, UnusedTag));

                // Any
                Assert.False(tagSystem.HasAnyTag(sTagDummy, UnusedTag));
                Assert.False(tagSystem.HasAnyTag(sTagComponent, UnusedTag));

                // All
                Assert.False(tagSystem.HasAllTags(sTagDummy, UnusedTag));
                Assert.False(tagSystem.HasAllTags(sTagComponent, UnusedTag));

                // Throws when checking for an unregistered tag
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sPrototypeManager.Index<TagPrototype>(UnregisteredTag);
                });

                // Single
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasTag(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasTag(sTagComponent, UnregisteredTag);
                });

                // Any
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasAnyTag(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasAnyTag(sTagComponent, UnregisteredTag);
                });

                // All
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasAllTags(sTagDummy, UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    tagSystem.HasAllTags(sTagComponent, UnregisteredTag);
                });

                // Cannot add the starting tag again
                Assert.That(tagSystem.AddTag(sTagComponent, StartingTag), Is.False);
                Assert.That(tagSystem.AddTags(sTagComponent, StartingTag, StartingTag), Is.False);
                Assert.That(tagSystem.AddTags(sTagComponent, new List<string> {StartingTag, StartingTag}), Is.False);

                // Has the starting tag
                Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> {StartingTag, StartingTag}), Is.True);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, StartingTag), Is.True);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> {StartingTag, StartingTag}), Is.True);

                // Does not have the added tag yet
                Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.False);
                Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, AddedTag), Is.False);
                Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> {AddedTag, AddedTag}), Is.False);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, AddedTag), Is.False);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> {AddedTag, AddedTag}), Is.False);

                // Has a combination of the two tags
                Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<string> {StartingTag, AddedTag}), Is.True);

                // Does not have both tags
                Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, AddedTag), Is.False);
                Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> {StartingTag, AddedTag}), Is.False);

                // Cannot remove a tag that does not exist
                Assert.That(tagSystem.RemoveTag(sTagComponent, AddedTag), Is.False);
                Assert.That(tagSystem.RemoveTags(sTagComponent, AddedTag, AddedTag), Is.False);
                Assert.That(tagSystem.RemoveTags(sTagComponent, new List<string> {AddedTag, AddedTag}), Is.False);

                // Can add the new tag
                Assert.That(tagSystem.AddTag(sTagComponent, AddedTag), Is.True);

                // Cannot add it twice
                Assert.That(tagSystem.AddTag(sTagComponent, AddedTag), Is.False);

                // Cannot add existing tags
                Assert.That(tagSystem.AddTags(sTagComponent, StartingTag, AddedTag), Is.False);
                Assert.That(tagSystem.AddTags(sTagComponent, new List<string> {StartingTag, AddedTag}), Is.False);

                // Now has two tags
                Assert.That(sTagComponent.Tags.Count, Is.EqualTo(2));

                // Has both tags
                Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);
                Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, StartingTag), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> {StartingTag, AddedTag}), Is.True);
                Assert.That(tagSystem.HasAllTags(sTagComponent, new List<string> {AddedTag, StartingTag}), Is.True);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, StartingTag), Is.True);

                // Remove the existing starting tag
                Assert.That(tagSystem.RemoveTag(sTagComponent, StartingTag), Is.True);

                // Remove the existing added tag
                Assert.That(tagSystem.RemoveTags(sTagComponent, AddedTag, AddedTag), Is.True);

                // No tags left to remove
                Assert.That(tagSystem.RemoveTags(sTagComponent, new List<string> {StartingTag, AddedTag}), Is.False);

                // No tags left in the component
                Assert.That(sTagComponent.Tags, Is.Empty);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
