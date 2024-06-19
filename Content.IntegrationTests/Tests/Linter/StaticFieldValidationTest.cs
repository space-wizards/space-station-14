using System.Collections.Generic;
using System.Linq;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests.Linter;

/// <summary>
/// Verify that the yaml linter successfully validates static fields
/// </summary>
[TestFixture]
public sealed class StaticFieldValidationTest
{
    [Test]
    public async Task TestStaticFieldValidation()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoMan = pair.Server.ProtoMan;

        var protos = new Dictionary<Type, HashSet<string>>();
        foreach (var kind in protoMan.EnumeratePrototypeKinds())
        {
            var ids = protoMan.EnumeratePrototypes(kind).Select(x => x.ID).ToHashSet();
            protos.Add(kind, ids);
        }

        Assert.That(protoMan.ValidateStaticFields(typeof(StringValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(StringArrayValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(EntProtoIdValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(EntProtoIdArrayValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdTestValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdArrayValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdListValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdSetValid), protos).Count, Is.Zero);
        Assert.That(protoMan.ValidateStaticFields(typeof(PrivateProtoIdArrayValid), protos).Count, Is.Zero);
        
        Assert.That(protoMan.ValidateStaticFields(typeof(StringInvalid), protos).Count, Is.EqualTo(1));
        Assert.That(protoMan.ValidateStaticFields(typeof(StringArrayInvalid), protos).Count, Is.EqualTo(2));
        Assert.That(protoMan.ValidateStaticFields(typeof(EntProtoIdInvalid), protos).Count, Is.EqualTo(1));
        Assert.That(protoMan.ValidateStaticFields(typeof(EntProtoIdArrayInvalid), protos).Count, Is.EqualTo(2));
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdTestInvalid), protos).Count, Is.EqualTo(1));
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdArrayInvalid), protos).Count, Is.EqualTo(2));
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdListInvalid), protos).Count, Is.EqualTo(2));
        Assert.That(protoMan.ValidateStaticFields(typeof(ProtoIdSetInvalid), protos).Count, Is.EqualTo(2));
        Assert.That(protoMan.ValidateStaticFields(typeof(PrivateProtoIdArrayInvalid), protos).Count, Is.EqualTo(2));
        
        await pair.CleanReturnAsync();
    }

    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: StaticFieldTestEnt

- type: Tag
  id: StaticFieldTestTag
";

    [Reflect(false)] private sealed class StringValid
    {
        [ValidatePrototypeId<TagPrototype>] public static string Tag = "StaticFieldTestTag";
    }

    [Reflect(false)] private sealed class StringInvalid
    {
        [ValidatePrototypeId<TagPrototype>] public static string Tag = string.Empty;
    }

    [Reflect(false)] private sealed class StringArrayValid
    {
        [ValidatePrototypeId<TagPrototype>] public static string[] Tag = {"StaticFieldTestTag", "StaticFieldTestTag"};
    }

    [Reflect(false)] private sealed class StringArrayInvalid
    {
        [ValidatePrototypeId<TagPrototype>] public static string[] Tag = {string.Empty, "StaticFieldTestTag", string.Empty};
    }

    [Reflect(false)] private sealed class EntProtoIdValid
    {
        public static EntProtoId Tag = "StaticFieldTestEnt";
    }

    [Reflect(false)] private sealed class EntProtoIdInvalid
    {
        public static EntProtoId Tag = string.Empty;
    }

    [Reflect(false)] private sealed class EntProtoIdArrayValid
    {
        public static EntProtoId[] Tag = {"StaticFieldTestEnt", "StaticFieldTestEnt"};
    }

    [Reflect(false)] private sealed class EntProtoIdArrayInvalid
    {
        public static EntProtoId[] Tag = {string.Empty, "StaticFieldTestEnt", string.Empty};
    }

    [Reflect(false)] private sealed class ProtoIdTestValid
    {
        public static ProtoId<TagPrototype> Tag = "StaticFieldTestTag";
    }

    [Reflect(false)] private sealed class ProtoIdTestInvalid
    {
        public static ProtoId<TagPrototype> Tag = string.Empty;
    }

    [Reflect(false)] private sealed class ProtoIdArrayValid
    {
        public static ProtoId<TagPrototype>[] Tag = {"StaticFieldTestTag", "StaticFieldTestTag"};
    }

    [Reflect(false)] private sealed class ProtoIdArrayInvalid
    {
        public static ProtoId<TagPrototype>[] Tag = {string.Empty, "StaticFieldTestTag", string.Empty};
    }

    [Reflect(false)] private sealed class ProtoIdListValid
    {
        public static List<ProtoId<TagPrototype>> Tag = new() {"StaticFieldTestTag", "StaticFieldTestTag"};
    }

    [Reflect(false)] private sealed class ProtoIdListInvalid
    {
        public static List<ProtoId<TagPrototype>> Tag = new() {string.Empty, "StaticFieldTestTag", string.Empty};
    }

    [Reflect(false)] private sealed class ProtoIdSetValid
    {
        public static HashSet<ProtoId<TagPrototype>> Tag = new() {"StaticFieldTestTag", "StaticFieldTestTag"};
    }

    [Reflect(false)] private sealed class ProtoIdSetInvalid
    {
        public static HashSet<ProtoId<TagPrototype>> Tag = new() {string.Empty, "StaticFieldTestTag", string.Empty, " "};
    }

    [Reflect(false)] private sealed class PrivateProtoIdArrayValid
    {
        private static ProtoId<TagPrototype>[] Tag = {"StaticFieldTestTag", "StaticFieldTestTag"};
    }

    [Reflect(false)] private sealed class PrivateProtoIdArrayInvalid
    {
        private static ProtoId<TagPrototype>[] Tag = {string.Empty, "StaticFieldTestTag", string.Empty};
    }
}
