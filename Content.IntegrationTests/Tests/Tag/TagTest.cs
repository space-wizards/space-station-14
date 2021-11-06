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
    public class TagTest : ContentIntegrationTest
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
            var options = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();

            IEntity sTagDummy = null!;
            TagComponent sTagComponent = null!;

            await server.WaitPost(() =>
            {
                sMapManager.CreateNewMapEntity(MapId.Nullspace);
                sTagDummy = sEntityManager.SpawnEntity(TagEntityId, MapCoordinates.Nullspace);
                sTagComponent = sTagDummy.GetComponent<TagComponent>();
            });

            await server.WaitAssertion(() =>
            {
                // Has one tag, the starting tag
                Assert.That(sTagComponent.Tags.Count, Is.EqualTo(1));
                sPrototypeManager.Index<TagPrototype>(StartingTag);
                Assert.That(sTagComponent.Tags, Contains.Item(StartingTag));

                // Single
                Assert.True(sTagDummy.HasTag(StartingTag));
                Assert.True(sTagComponent.HasTag(StartingTag));

                // Any
                Assert.True(sTagDummy.HasAnyTag(StartingTag));
                Assert.True(sTagComponent.HasAnyTag(StartingTag));

                // All
                Assert.True(sTagDummy.HasAllTags(StartingTag));
                Assert.True(sTagComponent.HasAllTags(StartingTag));

                // Does not have the added tag
                var addedTagPrototype = sPrototypeManager.Index<TagPrototype>(AddedTag);
                Assert.That(sTagComponent.Tags, Does.Not.Contains(addedTagPrototype));

                // Single
                Assert.False(sTagDummy.HasTag(AddedTag));
                Assert.False(sTagComponent.HasTag(AddedTag));

                // Any
                Assert.False(sTagDummy.HasAnyTag(AddedTag));
                Assert.False(sTagComponent.HasAnyTag(AddedTag));

                // All
                Assert.False(sTagDummy.HasAllTags(AddedTag));
                Assert.False(sTagComponent.HasAllTags(AddedTag));

                // Does not have the unused tag
                var unusedTagPrototype = sPrototypeManager.Index<TagPrototype>(UnusedTag);
                Assert.That(sTagComponent.Tags, Does.Not.Contains(unusedTagPrototype));

                // Single
                Assert.False(sTagDummy.HasTag(UnusedTag));
                Assert.False(sTagComponent.HasTag(UnusedTag));

                // Any
                Assert.False(sTagDummy.HasAnyTag(UnusedTag));
                Assert.False(sTagComponent.HasAnyTag(UnusedTag));

                // All
                Assert.False(sTagDummy.HasAllTags(UnusedTag));
                Assert.False(sTagComponent.HasAllTags(UnusedTag));

                // Throws when checking for an unregistered tag
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sPrototypeManager.Index<TagPrototype>(UnregisteredTag);
                });

                // Single
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagDummy.HasTag(UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagComponent.HasTag(UnregisteredTag);
                });

                // Any
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagDummy.HasAnyTag(UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagComponent.HasAnyTag(UnregisteredTag);
                });

                // All
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagDummy.HasAllTags(UnregisteredTag);
                });
                Assert.Throws<UnknownPrototypeException>(() =>
                {
                    sTagComponent.HasAllTags(UnregisteredTag);
                });

                // Cannot add the starting tag again
                Assert.That(sTagComponent.AddTag(StartingTag), Is.False);
                Assert.That(sTagComponent.AddTags(StartingTag, StartingTag), Is.False);
                Assert.That(sTagComponent.AddTags(new List<string> {StartingTag, StartingTag}), Is.False);

                // Has the starting tag
                Assert.That(sTagComponent.HasTag(StartingTag), Is.True);
                Assert.That(sTagComponent.HasAllTags(StartingTag, StartingTag), Is.True);
                Assert.That(sTagComponent.HasAllTags(new List<string> {StartingTag, StartingTag}), Is.True);
                Assert.That(sTagComponent.HasAnyTag(StartingTag, StartingTag), Is.True);
                Assert.That(sTagComponent.HasAnyTag(new List<string> {StartingTag, StartingTag}), Is.True);

                // Does not have the added tag yet
                Assert.That(sTagComponent.HasTag(AddedTag), Is.False);
                Assert.That(sTagComponent.HasAllTags(AddedTag, AddedTag), Is.False);
                Assert.That(sTagComponent.HasAllTags(new List<string> {AddedTag, AddedTag}), Is.False);
                Assert.That(sTagComponent.HasAnyTag(AddedTag, AddedTag), Is.False);
                Assert.That(sTagComponent.HasAnyTag(new List<string> {AddedTag, AddedTag}), Is.False);

                // Has a combination of the two tags
                Assert.That(sTagComponent.HasAnyTag(StartingTag, AddedTag), Is.True);
                Assert.That(sTagComponent.HasAnyTag(new List<string> {StartingTag, AddedTag}), Is.True);

                // Does not have both tags
                Assert.That(sTagComponent.HasAllTags(StartingTag, AddedTag), Is.False);
                Assert.That(sTagComponent.HasAllTags(new List<string> {StartingTag, AddedTag}), Is.False);

                // Cannot remove a tag that does not exist
                Assert.That(sTagComponent.RemoveTag(AddedTag), Is.False);
                Assert.That(sTagComponent.RemoveTags(AddedTag, AddedTag), Is.False);
                Assert.That(sTagComponent.RemoveTags(new List<string> {AddedTag, AddedTag}), Is.False);

                // Can add the new tag
                Assert.That(sTagComponent.AddTag(AddedTag), Is.True);

                // Cannot add it twice
                Assert.That(sTagComponent.AddTag(AddedTag), Is.False);

                // Cannot add existing tags
                Assert.That(sTagComponent.AddTags(StartingTag, AddedTag), Is.False);
                Assert.That(sTagComponent.AddTags(new List<string> {StartingTag, AddedTag}), Is.False);

                // Now has two tags
                Assert.That(sTagComponent.Tags.Count, Is.EqualTo(2));

                // Has both tags
                Assert.That(sTagComponent.HasTag(StartingTag), Is.True);
                Assert.That(sTagComponent.HasTag(AddedTag), Is.True);
                Assert.That(sTagComponent.HasAllTags(StartingTag, StartingTag), Is.True);
                Assert.That(sTagComponent.HasAllTags(AddedTag, StartingTag), Is.True);
                Assert.That(sTagComponent.HasAllTags(new List<string> {StartingTag, AddedTag}), Is.True);
                Assert.That(sTagComponent.HasAllTags(new List<string> {AddedTag, StartingTag}), Is.True);
                Assert.That(sTagComponent.HasAnyTag(StartingTag, AddedTag), Is.True);
                Assert.That(sTagComponent.HasAnyTag(AddedTag, StartingTag), Is.True);

                // Remove the existing starting tag
                Assert.That(sTagComponent.RemoveTag(StartingTag), Is.True);

                // Remove the existing added tag
                Assert.That(sTagComponent.RemoveTags(AddedTag, AddedTag), Is.True);

                // No tags left to remove
                Assert.That(sTagComponent.RemoveTags(new List<string> {StartingTag, AddedTag}), Is.False);

                // No tags left in the component
                Assert.That(sTagComponent.Tags, Is.Empty);
            });
        }
    }
}
