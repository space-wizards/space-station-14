using System.IO;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Tests.Server.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(ServerAlertsComponent))]
    public class ServerAlertsComponentTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alert
  alertType: LowPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/lowpressure.png

- type: alert
  alertType: HighPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/highpressure.png
";

        [Test]
        public void ShowAlerts()
        {
            // this is kind of unnecessary because there's integration test coverage of Alert components
            // but wanted to keep it anyway to see what's possible w.r.t. testing components
            // in a unit test

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.RegisterType(typeof(AlertPrototype));
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.Register<ServerAlertsComponent>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.Resync();
            var alertManager = IoCManager.Resolve<AlertManager>();
            alertManager.Initialize();


            var alertsComponent = new ServerAlertsComponent();
            alertsComponent = IoCManager.InjectDependencies(alertsComponent);

            Assert.That(alertManager.TryGetWithEncoded(AlertType.LowPressure, out var lowpressure, out var lpencoded));
            Assert.That(alertManager.TryGetWithEncoded(AlertType.HighPressure, out var highpressure, out var hpencoded));

            alertsComponent.ShowAlert(AlertType.LowPressure);
            var alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.NotNull(alertState);
            Assert.That(alertState.Alerts.Length, Is.EqualTo(1));
            Assert.That(alertState.Alerts[0], Is.EqualTo(new AlertState{AlertEncoded = lpencoded}));

            alertsComponent.ShowAlert(AlertType.HighPressure);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts.Length, Is.EqualTo(1));
            Assert.That(alertState.Alerts[0], Is.EqualTo(new AlertState{AlertEncoded = hpencoded}));

            alertsComponent.ClearAlertCategory(AlertCategory.Pressure);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts.Length, Is.EqualTo(0));
        }
    }
}
