#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Access
{
    [TestOf(typeof(AccessReaderComponent))]
    public sealed class AccessReaderTest : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  id: TestAccessReader
  name: access reader
  components:
  - type: AccessReader
";

        [System(Side.Server)] private readonly AccessReaderSystem _system = null!;

        [Test]
        [RunOnSide(Side.Server)]
        public async Task TestTags()
        {
            var ent = SSpawn("TestAccessReader");
            var reader = new Entity<AccessReaderComponent>(ent, SComp<AccessReaderComponent>(ent));

            // test empty
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Foo" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Bar" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
            });

            // test deny
            _system.AddDenyTag(reader, "A");
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Foo" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "Foo" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
            });
            _system.ClearDenyTags(reader);

            // test one list
            _system.TryAddAccess(reader, "A");
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
            });
            _system.TryClearAccesses(reader);

            // test one list - two items
            _system.TryAddAccess(reader, new HashSet<ProtoId<AccessLevelPrototype>> { "A", "B" });
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
            });
            _system.TryClearAccesses(reader);

            // test two list
            var accesses = new List<HashSet<ProtoId<AccessLevelPrototype>>>() {
                new HashSet<ProtoId<AccessLevelPrototype>> () { "A" },
                new HashSet<ProtoId<AccessLevelPrototype>> () { "B", "C" }
            };
            _system.TryAddAccesses(reader, accesses);
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "C", "B" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "C", "B", "A" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
            });
            _system.TryClearAccesses(reader);

            // test deny list
            _system.TryAddAccess(reader, new HashSet<ProtoId<AccessLevelPrototype>> { "A" });
            _system.AddDenyTag(reader, "B");
            Assert.Multiple(() =>
            {
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.False);
                Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
            });
            _system.TryClearAccesses(reader);
            _system.ClearDenyTags(reader);
        }

    }
}
