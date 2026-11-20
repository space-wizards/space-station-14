#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Linter;

/// <summary>
/// Verify that the yaml linter successfully validates static fields
/// </summary>
public sealed class StaticFieldValidationTest : GameTest
{
    [Test]
    [Description("Verify that the yaml linter successfully validates static fields.")]
    public async Task TestStaticFieldValidation()
    {
        var protos = new Dictionary<Type, HashSet<string>>();
        foreach (var kind in SProtoMan.EnumeratePrototypeKinds())
        {
            var ids = SProtoMan.EnumeratePrototypes(kind).Select(x => x.ID).ToHashSet();
            protos.Add(kind, ids);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(SProtoMan.ValidateStaticFields(typeof(StringValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(StringArrayValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdTValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdArrayValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdTArrayValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdTestValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdArrayValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdListValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdSetValid), protos), Is.Empty);
            Assert.That(SProtoMan.ValidateStaticFields(typeof(PrivateProtoIdArrayValid), protos), Is.Empty);

            Assert.That(SProtoMan.ValidateStaticFields(typeof(StringInvalid), protos), Has.Count.EqualTo(1));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(StringArrayInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdInvalid), protos), Has.Count.EqualTo(1));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdTInvalid), protos), Has.Count.EqualTo(1));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdArrayInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(EntProtoIdTArrayInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdTestInvalid), protos), Has.Count.EqualTo(1));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdArrayInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdListInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(ProtoIdSetInvalid), protos), Has.Count.EqualTo(2));
            Assert.That(SProtoMan.ValidateStaticFields(typeof(PrivateProtoIdArrayInvalid), protos), Has.Count.EqualTo(2));
        }
    }

    private const string StaticFieldTestEnt = "StaticFieldTestEnt";
    private const string StaticFieldTestTag = "StaticFieldTestTag";

    [TestPrototypes]
    private const string TestPrototypes = $@"
- type: entity
  id: {StaticFieldTestEnt}

- type: Tag
  id: {StaticFieldTestTag}
";

    [Reflect(false)]
    private sealed class StringValid
    {
        public static readonly ProtoId<TagPrototype> Tag = StaticFieldTestTag;
    }

    [Reflect(false)]
    private sealed class StringInvalid
    {
        public static readonly ProtoId<TagPrototype> Tag = string.Empty;
    }

    [Reflect(false)]
    private sealed class StringArrayValid
    {
        public static readonly ProtoId<TagPrototype>[] Tag = [StaticFieldTestTag, StaticFieldTestTag];
    }

    [Reflect(false)]
    private sealed class StringArrayInvalid
    {
        public static readonly ProtoId<TagPrototype>[] Tag = [string.Empty, StaticFieldTestTag, string.Empty];
    }

    [Reflect(false)]
    private sealed class EntProtoIdValid
    {
        public static EntProtoId Tag = StaticFieldTestEnt;
    }

    [Reflect(false)]
    private sealed class EntProtoIdTValid
    {
        public static EntProtoId<TransformComponent> Tag = StaticFieldTestEnt;
    }

    [Reflect(false)]
    private sealed class EntProtoIdInvalid
    {
        public static EntProtoId Tag = string.Empty;
    }

    [Reflect(false)]
    private sealed class EntProtoIdTInvalid
    {
        public static EntProtoId<TransformComponent> Tag = string.Empty;
    }

    [Reflect(false)]
    private sealed class EntProtoIdArrayValid
    {
        public static EntProtoId[] Tag = [StaticFieldTestEnt, StaticFieldTestEnt];
    }

    [Reflect(false)]
    private sealed class EntProtoIdTArrayValid
    {
        public static EntProtoId<TransformComponent>[] Tag = [StaticFieldTestEnt, StaticFieldTestEnt];
    }

    [Reflect(false)]
    private sealed class EntProtoIdArrayInvalid
    {
        public static EntProtoId[] Tag = [string.Empty, StaticFieldTestEnt, string.Empty];
    }

    [Reflect(false)]
    private sealed class EntProtoIdTArrayInvalid
    {
        public static EntProtoId<TransformComponent>[] Tag = [string.Empty, StaticFieldTestEnt, string.Empty];
    }

    [Reflect(false)]
    private sealed class ProtoIdTestValid
    {
        public static ProtoId<TagPrototype> Tag = StaticFieldTestTag;
    }

    [Reflect(false)]
    private sealed class ProtoIdTestInvalid
    {
        public static ProtoId<TagPrototype> Tag = string.Empty;
    }

    [Reflect(false)]
    private sealed class ProtoIdArrayValid
    {
        public static ProtoId<TagPrototype>[] Tag = [StaticFieldTestTag, StaticFieldTestTag];
    }

    [Reflect(false)]
    private sealed class ProtoIdArrayInvalid
    {
        public static ProtoId<TagPrototype>[] Tag = [string.Empty, StaticFieldTestTag, string.Empty];
    }

    [Reflect(false)]
    private sealed class ProtoIdListValid
    {
        public static List<ProtoId<TagPrototype>> Tag = [StaticFieldTestTag, StaticFieldTestTag];
    }

    [Reflect(false)]
    private sealed class ProtoIdListInvalid
    {
        public static List<ProtoId<TagPrototype>> Tag = [string.Empty, StaticFieldTestTag, string.Empty];
    }

    [Reflect(false)]
    private sealed class ProtoIdSetValid
    {
        public static HashSet<ProtoId<TagPrototype>> Tag = [StaticFieldTestTag, StaticFieldTestTag];
    }

    [Reflect(false)]
    private sealed class ProtoIdSetInvalid
    {
        public static HashSet<ProtoId<TagPrototype>> Tag = [string.Empty, StaticFieldTestTag, string.Empty, " "];
    }

    [Reflect(false)]
    private sealed class PrivateProtoIdArrayValid
    {
        private static readonly ProtoId<TagPrototype>[] Tag = [StaticFieldTestTag, StaticFieldTestTag];
    }

    [Reflect(false)]
    private sealed class PrivateProtoIdArrayInvalid
    {
        private static readonly ProtoId<TagPrototype>[] Tag = [string.Empty, StaticFieldTestTag, string.Empty];
    }
}
