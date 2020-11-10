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
    public class AlertManagerTests : RobustUnitTest
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
            prototypeManager.RegisterType(typeof(AlertPrototype));
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            IoCManager.RegisterInstance<AlertManager>(new AlertManager());
            var alertManager = IoCManager.Resolve<AlertManager>();
            alertManager.Initialize();

            Assert.That(alertManager.TryGet(AlertType.LowPressure, out var lowPressure));
            Assert.That(lowPressure.IconPath, Is.EqualTo("/Textures/Interface/Alerts/Pressure/lowpressure.png"));
            Assert.That(alertManager.TryGet(AlertType.HighPressure, out var highPressure));
            Assert.That(highPressure.IconPath, Is.EqualTo("/Textures/Interface/Alerts/Pressure/highpressure.png"));

            Assert.That(alertManager.TryGetWithEncoded(AlertType.LowPressure, out lowPressure, out var encodedLowPressure));
            Assert.That(lowPressure.IconPath, Is.EqualTo("/Textures/Interface/Alerts/Pressure/lowpressure.png"));
            Assert.That(alertManager.TryGetWithEncoded(AlertType.HighPressure, out highPressure, out var encodedHighPressure));
            Assert.That(highPressure.IconPath, Is.EqualTo("/Textures/Interface/Alerts/Pressure/highpressure.png"));

            Assert.That(alertManager.TryEncode(lowPressure, out var encodedLowPressure2));
            Assert.That(encodedLowPressure2, Is.EqualTo(encodedLowPressure));
            Assert.That(alertManager.TryEncode(highPressure, out var encodedHighPressure2));
            Assert.That(encodedHighPressure2, Is.EqualTo(encodedHighPressure));
            Assert.That(encodedLowPressure, Is.Not.EqualTo(encodedHighPressure));

            Assert.That(alertManager.TryDecode(encodedLowPressure, out var decodedLowPressure));
            Assert.That(decodedLowPressure, Is.EqualTo(lowPressure));
            Assert.That(alertManager.TryDecode(encodedHighPressure, out var decodedHighPressure));
            Assert.That(decodedHighPressure, Is.EqualTo(highPressure));

            Assert.False(alertManager.TryEncode(AlertType.Debug1, out _));
            Assert.False(alertManager.TryGetWithEncoded(AlertType.Debug1, out _, out _));

        }
    }
}
