﻿using System.IO;
using Content.Shared.Chemistry.Reagent;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared.Chemistry
{
    [TestFixture, TestOf(typeof(ReagentPrototype))]
    public sealed class ReagentPrototype_Tests : ContentUnitTest
    {
        [Test]
        public void DeserializeReagentPrototype()
        {
            using (TextReader stream = new StringReader(YamlReagentPrototype))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(stream);
                var document = yamlStream.Documents[0];
                var rootNode = (YamlSequenceNode)document.RootNode;
                var proto = (YamlMappingNode)rootNode[0];

                var defType = proto.GetNode("type").AsString();
                var serializationManager = IoCManager.Resolve<ISerializationManager>();
                serializationManager.Initialize();

                var newReagent = serializationManager.Read<ReagentPrototype>(new MappingDataNode(proto));

                Assert.That(defType, Is.EqualTo("reagent"));
                Assert.That(newReagent.ID, Is.EqualTo("H2"));
                Assert.That(newReagent.LocalizedName, Is.EqualTo("Hydrogen"));
                Assert.That(newReagent.LocalizedDescription, Is.EqualTo("A light, flammable gas."));
                // Can't access SubstanceColor.
                // Assert.That(newReagent.SubstanceColor, Is.EqualTo(Color.Teal));
            }
        }

        private const string YamlReagentPrototype = @"- type: reagent
  id: H2
  name: Hydrogen
  desc: A light, flammable gas.
  physicalDesc: A light, flammable gas.
  color: " + "\"#008080\"";
    }
}
