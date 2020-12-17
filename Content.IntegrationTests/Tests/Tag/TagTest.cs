using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Tag;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Tag
{
    [TestFixture]
    [TestOf(typeof(TagComponent))]
    public class TagTest : ContentIntegrationTest
    {
        private static readonly string TagEntityId = "TagTestDummy";

        private static readonly string StartingTag = "A";

        private static readonly string AddedTag = "EIOU";

        private static readonly string Prototypes = $@"
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
            var server = StartServerDummyTicker(options);

            await server.WaitIdleAsync();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();

            TagComponent sTagComponent = null;

            await server.WaitPost(() =>
            {
                sMapManager.CreateNewMapEntity(MapId.Nullspace);
                var sTagDummy = sEntityManager.SpawnEntity(TagEntityId, MapCoordinates.Nullspace);
                sTagComponent = sTagDummy.GetComponent<TagComponent>();
            });

            await server.WaitAssertion(() =>
            {
                // Has one tag, the starting tag
                Assert.That(sTagComponent.Tags.Count, Is.EqualTo(1));
                Assert.That(sTagComponent.Tags, Contains.Item(StartingTag));

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
