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
            Entity<TagComponent> sTagEntity = default;

            await server.WaitPost(() =>
            {
                sTagDummy = sEntityManager.SpawnEntity(TagEntityId, MapCoordinates.Nullspace);
                sTagComponent = sEntityManager.GetComponent<TagComponent>(sTagDummy);
                sTagEntity = new Entity<TagComponent>(sTagDummy, sTagComponent);
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
                    Assert.That(tagSystem.AddTag(sTagEntity, StartingTag), Is.False);

                    Assert.That(tagSystem.AddTags(sTagEntity, StartingTag, StartingTag), Is.False);
                    Assert.That(tagSystem.AddTags(sTagEntity, new List<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.False);
                    Assert.That(tagSystem.AddTags(sTagEntity, new HashSet<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.False);

                    // Has the starting tag
                    Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);

                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.True);

                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, StartingTag }), Is.True);

                    // Does not have the added tag yet
                    Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.False);

                    Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);

                    Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);

                    // Has a combination of the two tags
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);

                    // Does not have both tags
                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, AddedTag), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.False);

                    // Cannot remove a tag that does not exist
                    Assert.That(tagSystem.RemoveTag(sTagEntity, AddedTag), Is.False);

                    Assert.That(tagSystem.RemoveTags(sTagEntity, AddedTag, AddedTag), Is.False);
                    Assert.That(tagSystem.RemoveTags(sTagEntity, new List<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.RemoveTags(sTagEntity, new HashSet<ProtoId<TagPrototype>> { AddedTag, AddedTag }), Is.False);
                });

                // Can add the new tag
                Assert.That(tagSystem.AddTag(sTagEntity, AddedTag), Is.True);

                Assert.Multiple(() =>
                {
                    // Cannot add it twice
                    Assert.That(tagSystem.AddTag(sTagEntity, AddedTag), Is.False);

                    // Cannot add existing tags
                    Assert.That(tagSystem.AddTags(sTagEntity, StartingTag, AddedTag), Is.False);
                    Assert.That(tagSystem.AddTags(sTagEntity, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.False);
                    Assert.That(tagSystem.AddTags(sTagEntity, new HashSet<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.False);

                    // Now has two tags
                    Assert.That(sTagComponent.Tags, Has.Count.EqualTo(2));

                    // Has both tags
                    Assert.That(tagSystem.HasTag(sTagComponent, StartingTag), Is.True);
                    Assert.That(tagSystem.HasTag(sTagComponent, AddedTag), Is.True);

                    Assert.That(tagSystem.HasAllTags(sTagComponent, StartingTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, AddedTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { AddedTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { AddedTag, StartingTag }), Is.True);

                    Assert.That(tagSystem.HasAnyTag(sTagComponent, StartingTag, AddedTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, AddedTag, StartingTag), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { AddedTag, StartingTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.True);
                    Assert.That(tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { AddedTag, StartingTag }), Is.True);
                });

                Assert.Multiple(() =>
                {
                    // Remove the existing starting tag
                    Assert.That(tagSystem.RemoveTag(sTagEntity, StartingTag), Is.True);

                    // Remove the existing added tag
                    Assert.That(tagSystem.RemoveTags(sTagEntity, AddedTag, AddedTag), Is.True);
                });

                Assert.Multiple(() =>
                {
                    // No tags left to remove
                    Assert.That(tagSystem.RemoveTags(sTagEntity, new List<ProtoId<TagPrototype>> { StartingTag, AddedTag }), Is.False);

                    // No tags left in the component
                    Assert.That(sTagComponent.Tags, Is.Empty);
                });

                // It is run only in DEBUG build,
                // as the checks are performed only in DEBUG build.
#if DEBUG
                // Has single
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasTag(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasTag(sTagComponent, UnregisteredTag); });

                // HasAny entityUid methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagDummy, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagDummy, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagDummy, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // HasAny component methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagComponent, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagComponent, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagComponent, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAnyTag(sTagComponent, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // HasAll entityUid methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagDummy, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagDummy, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagDummy, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // HasAll component methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagComponent, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagComponent, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagComponent, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.HasAllTags(sTagComponent, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // RemoveTag single
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTag(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTag(sTagEntity, UnregisteredTag); });

                // RemoveTags entityUid methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagDummy, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagDummy, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagDummy, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // RemoveTags entity methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagEntity, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagEntity, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagEntity, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.RemoveTags(sTagEntity, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // AddTag single
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTag(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTag(sTagEntity, UnregisteredTag); });

                // AddTags entityUid methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagDummy, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagDummy, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagDummy, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagDummy, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });

                // AddTags entity methods
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagEntity, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagEntity, UnregisteredTag, UnregisteredTag); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagEntity, new List<ProtoId<TagPrototype>> { UnregisteredTag }); });
                Assert.Throws<DebugAssertException>(() => { tagSystem.AddTags(sTagEntity, new HashSet<ProtoId<TagPrototype>> { UnregisteredTag }); });
#endif
            });
            await pair.CleanReturnAsync();
        }
    }
}
