using System.Reflection;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.UnitTesting.Shared.Serialization;

namespace Content.IntegrationTests.Tests.Chemistry
{
    public sealed class FixedPoint2SerializationTest : SerializationTest
    {
        protected override Assembly[] Assemblies => new[]
        {
            typeof(FixedPoint2SerializationTest).Assembly
        };

        [Test]
        public void DeserializeNullTest()
        {
            var node = ValueDataNode.Null();
            var unit = Serialization.Read<FixedPoint2?>(node);

            Assert.That(unit, Is.Null);
        }

        [Test]
        public void SerializeNullTest()
        {
            var node = Serialization.WriteValue<FixedPoint2?>(null);
            Assert.That(node.IsNull);
        }

        [Test]
        public void SerializeNullableValueTest()
        {
            var node = Serialization.WriteValue<FixedPoint2?>(FixedPoint2.New(2.5f));
#pragma warning disable NUnit2045 // Interdependent assertions
            Assert.That(node is ValueDataNode);
            Assert.That(((ValueDataNode) node).Value, Is.EqualTo("2.5"));
#pragma warning restore NUnit2045
        }

        [Test]
        public void DeserializeNullDefinitionTest()
        {
            var node = new MappingDataNode().Add("unit", ValueDataNode.Null());
            var definition = Serialization.Read<FixedPoint2TestDefinition>(node);

            Assert.That(definition.Unit, Is.Null);
        }
    }

    [DataDefinition]
    public sealed partial class FixedPoint2TestDefinition
    {
        [DataField("unit")] public FixedPoint2? Unit { get; set; } = FixedPoint2.New(5);
    }
}
