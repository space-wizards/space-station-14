using System.Reflection;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
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
            var node = new ValueDataNode("null");
            var unit = Serialization.ReadValue<FixedPoint2?>(node);

            Assert.That(unit, Is.Null);
        }

        [Test]
        public void DeserializeNullDefinitionTest()
        {
            var node = new MappingDataNode().Add("unit", "null");
            var definition = Serialization.ReadValueOrThrow<FixedPoint2TestDefinition>(node);

            Assert.That(definition.Unit, Is.Null);
        }
    }

    [DataDefinition]
    public sealed class FixedPoint2TestDefinition
    {
        [DataField("unit")] public FixedPoint2? Unit { get; set; } = FixedPoint2.New(5);
    }
}
