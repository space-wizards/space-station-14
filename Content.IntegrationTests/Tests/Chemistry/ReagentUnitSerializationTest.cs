using System.Reflection;
using Content.Shared.Chemistry.Reagent;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.UnitTesting.Shared.Serialization;

namespace Content.IntegrationTests.Tests.Chemistry
{
    public class ReagentUnitSerializationTest : SerializationTest
    {
        protected override Assembly[] Assemblies => new[]
        {
            typeof(ReagentUnitSerializationTest).Assembly
        };

        [Test]
        public void DeserializeNullTest()
        {
            var node = new ValueDataNode("null");
            var unit = Serialization.ReadValue<ReagentUnit?>(node);

            Assert.That(unit, Is.Null);
        }

        [Test]
        public void DeserializeNullDefinitionTest()
        {
            var node = new MappingDataNode().Add("unit", "null");
            var definition = Serialization.ReadValueOrThrow<ReagentUnitTestDefinition>(node);

            Assert.That(definition.Unit, Is.Null);
        }
    }

    [DataDefinition]
    public class ReagentUnitTestDefinition
    {
        [DataField("unit")] public ReagentUnit? Unit { get; set; } = ReagentUnit.New(5);
    }
}
