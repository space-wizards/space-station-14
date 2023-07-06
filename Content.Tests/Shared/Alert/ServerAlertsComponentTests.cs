using System.IO;
using Content.Server.Alert;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Alert
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public sealed class ServerAlertsComponentTests : ContentUnitTest
    {
        private const string PROTOTYPES = @"
- type: entity
  id: AlertsTestDummy
  components:
  - type: Alerts
- type: alert
  id: LowPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/lowpressure.png

- type: alert
  id: HighPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/highpressure.png
";

        [Test]
        [Ignore("There is no way to load extra Systems in a unit test, fixing RobustUnitTest is out of scope.")]
        public void ShowAlerts()
        {
            // this is kind of unnecessary because there's integration test coverage of Alert components
            // but wanted to keep it anyway to see what's possible w.r.t. testing components
            // in a unit test

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.RegisterClass<AlertsComponent>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.ResolveResults();

            var entMan = IoCManager.Resolve<IEntityManager>();
            entMan.EntitySysManager.LoadExtraSystemType<ServerAlertsSystem>();
            var alertsSystem = entMan.System<AlertsSystem>();

            var alertsEntity = entMan.SpawnEntity("AlertsTestDummy", MapCoordinates.Nullspace);
            Assert.That(entMan.TryGetComponent<AlertsComponent>(alertsEntity, out var alertsComponent));

            AlertPrototype lowPressure = default!;
            AlertPrototype highPressure = default!;
            Assert.Multiple(() =>
            {
                Assert.That(alertsSystem.TryGet(AlertType.LowPressure, out lowPressure));
                Assert.That(alertsSystem.TryGet(AlertType.HighPressure, out highPressure));
            });

            alertsSystem.ShowAlert(alertsEntity, AlertType.LowPressure, null, null);
            var alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState, Is.Not.Null);
            Assert.That(alertState.Alerts, Has.Count.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(lowPressure.AlertKey));

            alertsSystem.ShowAlert(alertsEntity, AlertType.HighPressure, null, null);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts, Has.Count.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(highPressure.AlertKey));

            alertsSystem.ClearAlertCategory(alertsEntity, AlertCategory.Pressure);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts, Has.Count.EqualTo(0));

            entMan.DeleteEntity(alertsEntity);
        }
    }
}
