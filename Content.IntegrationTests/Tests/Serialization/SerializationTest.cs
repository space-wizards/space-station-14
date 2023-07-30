using System.Collections.Generic;
using Robust.Shared.Physics;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.IntegrationTests.Tests.Serialization;

[TestFixture]
public sealed class SerializationTest
{
    /// <summary>
    /// Check that serializing generic enums works as intended. This should really be in engine, but engine
    /// integrations tests block reflection and I am lazy..
    /// </summary>
    [Test]
    public async Task SerializeGenericEnums()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;
        var seriMan = server.ResolveDependency<ISerializationManager>();
        var refMan = server.ResolveDependency<IReflectionManager>();

        Enum value = BodyType.Static;

        var node = seriMan.WriteValue(value, notNullableOverride:true);
        var valueNode = node as ValueDataNode;
        Assert.NotNull(valueNode);

        // Workaround to a stupid bug
        refMan.TryParseEnumReference("enum.BodyType.Static", out _);
        refMan.TryParseEnumReference("enum.BodyType.Dynamic", out _);
        refMan.TryParseEnumReference("enum.BodyType.KinematicController", out _);
        refMan.TryParseEnumReference("enum.BodyType.Kinematic", out _);

        var expected = refMan.GetEnumReference(value);
        Assert.That(valueNode!.Value, Is.EqualTo(expected));

        var deserialized = seriMan.Read<Enum>(node, notNullableOverride:true);
        Assert.That(deserialized, Is.EqualTo(value));

        // Repeat test with enums in a data definitions.
        var data = new TestData
        {
            Value = BodyType.Dynamic,
            Sequence = new() {BodyType.KinematicController, BodyType.Kinematic}
        };

        node = seriMan.WriteValue(data, notNullableOverride:true);
        var deserializedData = seriMan.Read<TestData>(node, notNullableOverride:false);

        Assert.That(deserializedData.Value, Is.EqualTo(data.Value));
        Assert.That(deserializedData.Sequence.Count, Is.EqualTo(data.Sequence.Count));
        Assert.That(deserializedData.Sequence[0], Is.EqualTo(data.Sequence[0]));
        Assert.That(deserializedData.Sequence[1], Is.EqualTo(data.Sequence[1]));
    }

    [DataDefinition]
    private sealed class TestData
    {
        [DataField("value")] public Enum Value = default!;
        [DataField("sequence")] public List<Enum> Sequence = default!;
    }
}
