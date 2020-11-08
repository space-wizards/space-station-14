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
  id: lowpressure
  category: pressure
  icon: /Textures/Interface/StatusEffects/Pressure/lowpressure.png
  maxSeverity: 2
  name: Low Pressure
  description: TestDesc

- type: alert
  id: highpressure
  category: pressure
  icon: /Textures/Interface/StatusEffects/Pressure/highpressure.png
  maxSeverity: 2
  name: High presure
  description: test desc
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

            Assert.That(alertManager.TryGet("lowpressure", out var lowPressure), Is.True);
            Assert.That(lowPressure.IconPath, Is.EqualTo("/Textures/Interface/StatusEffects/Pressure/lowpressure.png"));
            Assert.That(alertManager.TryGet("highpressure", out var highPressure), Is.True);
            Assert.That(highPressure.IconPath, Is.EqualTo("/Textures/Interface/StatusEffects/Pressure/highpressure.png"));

            Assert.That(alertManager.TryGetWithEncoded("lowpressure", out lowPressure, out var encodedLowPressure), Is.True);
            Assert.That(lowPressure.IconPath, Is.EqualTo("/Textures/Interface/StatusEffects/Pressure/lowpressure.png"));
            Assert.That(alertManager.TryGetWithEncoded("highpressure", out highPressure, out var encodedHighPressure), Is.True);
            Assert.That(highPressure.IconPath, Is.EqualTo("/Textures/Interface/StatusEffects/Pressure/highpressure.png"));

            Assert.That(alertManager.TryEncode(lowPressure, out var encodedLowPressure2), Is.True);
            Assert.That(encodedLowPressure2, Is.EqualTo(encodedLowPressure));
            Assert.That(alertManager.TryEncode(highPressure, out var encodedHighPressure2), Is.True);
            Assert.That(encodedHighPressure2, Is.EqualTo(encodedHighPressure));
            Assert.That(encodedLowPressure, Is.Not.EqualTo(encodedHighPressure));

            Assert.That(alertManager.TryDecode(encodedLowPressure, out var decodedLowPressure), Is.True);
            Assert.That(decodedLowPressure, Is.EqualTo(lowPressure));
            Assert.That(alertManager.TryDecode(encodedHighPressure, out var decodedHighPressure), Is.True);
            Assert.That(decodedHighPressure, Is.EqualTo(highPressure));

            Assert.That(alertManager.TryDecode(-1, out _), Is.False);
            Assert.That(alertManager.TryDecode(999, out _), Is.False);
            Assert.That(alertManager.TryEncode("lowpressurenonexistent", out _), Is.False);
            Assert.That(alertManager.TryGetWithEncoded("lowpressurenonexistent", out _, out _), Is.False);

        }
    }
}
