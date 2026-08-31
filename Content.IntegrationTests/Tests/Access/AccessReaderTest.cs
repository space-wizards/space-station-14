#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Access;

[TestOf(typeof(AccessReaderComponent))]
public sealed class AccessReaderTest : GameTest
{
    private const string TestAccessReader = "TestAccessReader";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TestAccessReader}
  name: access reader
  components:
  - type: AccessReader
";

    [SidedDependency(Side.Server)] private readonly AccessReaderSystem _system = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task TestTags()
    {
        var ent = SSpawn(TestAccessReader);
        var reader = SEntity<AccessReaderComponent>(ent);

        // test empty
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["Foo"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["Bar"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
        }

        // test deny
        _system.AddDenyTag(reader, "A");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["Foo"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["A"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["A", "Foo"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
        }
        _system.ClearDenyTags(reader);

        // test one list
        _system.TryAddAccess(reader, "A");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["A"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["B"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["A", "B"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
        }
        _system.TryClearAccesses(reader);

        // test one list - two items
        _system.TryAddAccess(reader, ["A", "B"]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["A"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["B"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["A", "B"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
        }
        _system.TryClearAccesses(reader);

        // test two list
        var accesses = new List<HashSet<ProtoId<AccessLevelPrototype>>>() {
            new() { "A" },
            new() { "B", "C" }
        };
        _system.TryAddAccesses(reader, accesses);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["A"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["B"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["A", "B"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["C", "B"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["C", "B", "A"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
        }
        _system.TryClearAccesses(reader);

        // test deny list
        _system.TryAddAccess(reader, ["A"]);
        _system.AddDenyTag(reader, "B");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_system.AreAccessTagsAllowed(["A"], reader), Is.True);
            Assert.That(_system.AreAccessTagsAllowed(["B"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(["A", "B"], reader), Is.False);
            Assert.That(_system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
        }
        _system.TryClearAccesses(reader);
        _system.ClearDenyTags(reader);
    }

}
