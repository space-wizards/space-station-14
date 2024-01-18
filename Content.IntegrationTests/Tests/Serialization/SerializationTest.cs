using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.IntegrationTests.Tests.Serialization;

[TestFixture]
public sealed partial class SerializationTest
{
    /// <summary>
    /// Check that serializing generic enums works as intended. This should really be in engine, but engine
    /// integrations tests block reflection and I am lazy..
    /// </summary>
    [Test]
    public async Task SerializeGenericEnums()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var seriMan = server.ResolveDependency<ISerializationManager>();
        var refMan = server.ResolveDependency<IReflectionManager>();

        Enum value = TestEnum.Bb;

        var node = seriMan.WriteValue(value, notNullableOverride:true);
        var valueNode = node as ValueDataNode;
        Assert.That(valueNode, Is.Not.Null);

        var expected = refMan.GetEnumReference(value);
        Assert.That(valueNode!.Value, Is.EqualTo(expected));

        var errors = seriMan.ValidateNode<Enum>(valueNode).GetErrors();
        Assert.That(errors.Any(), Is.False);

        var deserialized = seriMan.Read<Enum>(node, notNullableOverride:true);
        Assert.That(deserialized, Is.EqualTo(value));

        // Repeat test with enums in a data definitions.
        var data = new TestData
        {
            Value = TestEnum.Cc,
            Sequence = new() {TestEnum.Dd, TestEnum.Aa}
        };

        node = seriMan.WriteValue(data, notNullableOverride:true);

        errors = seriMan.ValidateNode<TestData>(node).GetErrors();
        Assert.That(errors.Any(), Is.False);

        var deserializedData = seriMan.Read<TestData>(node, notNullableOverride:false);

        Assert.That(deserializedData.Value, Is.EqualTo(data.Value));
        Assert.That(deserializedData.Sequence.Count, Is.EqualTo(data.Sequence.Count));
        Assert.That(deserializedData.Sequence[0], Is.EqualTo(data.Sequence[0]));
        Assert.That(deserializedData.Sequence[1], Is.EqualTo(data.Sequence[1]));

        // Check that Generic & non-generic serializers are incompativle.
        Enum genericValue = TestEnum.Bb;
        TestEnum typedValue = TestEnum.Bb;

        var genericNode = seriMan.WriteValue(genericValue, notNullableOverride:true);
        var typedNode = seriMan.WriteValue(typedValue);

        Assert.That(seriMan.ValidateNode<Enum>(genericNode).GetErrors().Any(), Is.False);
        Assert.That(seriMan.ValidateNode<TestEnum>(genericNode).GetErrors().Any(), Is.True);
        Assert.That(seriMan.ValidateNode<Enum>(typedNode).GetErrors().Any(), Is.True);
        Assert.That(seriMan.ValidateNode<TestEnum>(typedNode).GetErrors().Any(), Is.False);

        await pair.CleanReturnAsync();
    }

    private enum TestEnum : byte { Aa, Bb, Cc, Dd }

    [DataDefinition]
    private sealed partial class TestData
    {
        [DataField("value")] public Enum Value = default!;
        [DataField("sequence")] public List<Enum> Sequence = default!;
    }
}
