using System;
using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertPrototype))]
    public class AlertPrototypeTests : ContentUnitTest
    {
        private const string Prototypes = @"
- type: alert
  alertType: HumanHealth
  category: Health
  icon: /Textures/Interface/Alerts/Human/human.rsi/human.png
  name: Health
  description: ""[color=green]Green[/color] good. [color=red]Red[/color] bad.""
  minSeverity: 0
  maxSeverity: 6";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        [Test]
        public void TestAlertKey()
        {
            Assert.That(new AlertKey(AlertType.HumanHealth, null), Is.Not.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
            Assert.That((new AlertKey(null, AlertCategory.Health)), Is.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
            Assert.That((new AlertKey(AlertType.Buckled, AlertCategory.Health)), Is.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
        }

        [TestCase(0, "/Textures/Interface/Alerts/Human/human.rsi/human0.png")]
        [TestCase(1, "/Textures/Interface/Alerts/Human/human.rsi/human1.png")]
        [TestCase(6, "/Textures/Interface/Alerts/Human/human.rsi/human6.png")]
        public void GetsIconPath(short? severity, string expected)
        {
            var alert = GetTestPrototype();
            Assert.That(alert.GetIcon(severity), Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath(expected))));
        }

        [TestCase(null, "/Textures/Interface/Alerts/Human/human.rsi/human0.png")]
        [TestCase(7, "/Textures/Interface/Alerts/Human/human.rsi/human1.png")]
        public void GetsIconPathThrows(short? severity, string expected)
        {
            var alert = GetTestPrototype();

            try
            {
                alert.GetIcon(severity);
            }
            catch (ArgumentException)
            {
                Assert.Pass();
            }
            catch (Exception e)
            {
                Assert.Fail($"Unexpected exception: {e}");
            }
        }

        private AlertPrototype GetTestPrototype()
        {
            using TextReader stream = new StringReader(Prototypes);

            var yamlStream = new YamlStream();
            yamlStream.Load(stream);

            var document = yamlStream.Documents[0];
            var rootNode = (YamlSequenceNode) document.RootNode;
            var proto = (YamlMappingNode) rootNode[0];
            var serMan = IoCManager.Resolve<ISerializationManager>();

            return serMan.ReadValue<AlertPrototype>(new MappingDataNode(proto));
        }
    }
}
