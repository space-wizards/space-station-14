using System.IO;
using Content.Server.Alert;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Alert
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public sealed class ServerAlertsComponentTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alertCategory
  id: Pressure

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

            var entManager = IoCManager.Resolve<IEntityManager>();
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.RegisterClass<AlertsComponent>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.ResolveResults();

            var entSys = entManager.EntitySysManager;
            entSys.LoadExtraSystemType<ServerAlertsSystem>();

            var alertsComponent = new AlertsComponent();
            alertsComponent = IoCManager.InjectDependencies(alertsComponent);

            Assert.That(entManager.System<AlertsSystem>().TryGet("LowPressure", out var lowpressure));
            Assert.That(entManager.System<AlertsSystem>().TryGet("HighPressure", out var highpressure));

            entManager.System<AlertsSystem>().ShowAlert(alertsComponent.Owner, "LowPressure");

            var getty = new ComponentGetState();
            entManager.EventBus.RaiseComponentEvent(alertsComponent, getty);

            var alertState = (AlertsComponent.AlertsComponent_AutoState) getty.State!;
            Assert.That(alertState, Is.Not.Null);
            Assert.That(alertState.Alerts.Count, Is.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(lowpressure!.AlertKey));

            entManager.System<AlertsSystem>().ShowAlert(alertsComponent.Owner, "HighPressure");

            // Lazy
            entManager.EventBus.RaiseComponentEvent(alertsComponent, getty);
            alertState = (AlertsComponent.AlertsComponent_AutoState) getty.State!;
            Assert.That(alertState.Alerts.Count, Is.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(highpressure!.AlertKey));

            entManager.System<AlertsSystem>().ClearAlertCategory(alertsComponent.Owner, "Pressure");

            entManager.EventBus.RaiseComponentEvent(alertsComponent, getty);
            alertState = (AlertsComponent.AlertsComponent_AutoState) getty.State!;
            Assert.That(alertState.Alerts.Count, Is.EqualTo(0));
        }
    }
}
