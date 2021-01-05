using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertManager))]
    public class AlertManagerTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alert
  alertType: LowPressure
  icon: /Textures/Interface/Alerts/Pressure/lowpressure.png

- type: alert
  alertType: HighPressure
  icon: /Textures/Interface/Alerts/Pressure/highpressure.png
";

        [Test]
        public void TestAlertManager()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            var alertManager = IoCManager.Resolve<AlertManager>();
            alertManager.Initialize();

            Assert.That(alertManager.TryGet(AlertType.LowPressure, out var lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(alertManager.TryGet(AlertType.HighPressure, out var highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));

            Assert.That(alertManager.TryGet(AlertType.LowPressure, out lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(alertManager.TryGet(AlertType.HighPressure, out highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));
        }
    }
}
