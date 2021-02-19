using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertOrderPrototype))]
    public class AlertOrderPrototypeTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alertOrder
  order:
    - alertType: Handcuffed
    - category: Pressure
    - category: Hunger
    - alertType: Hot
    - alertType: Stun
    - alertType: LowPressure
    - category: Temperature

- type: alert
  category: Pressure
  alertType: LowPressure

- type: alert
  category: Hunger
  alertType: Overfed

- type: alert
  category: Pressure
  alertType: HighPressure

- type: alert
  category: Hunger
  alertType: Peckish

- type: alert
  alertType: Stun

- type: alert
  alertType: Handcuffed

- type: alert
  category: Temperature
  alertType: Hot

- type: alert
  category: Temperature
  alertType: Cold

- type: alert
  alertType: Weightless

- type: alert
  alertType: PilotingShuttle
";

        [Test]
        public void TestAlertOrderPrototype()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));

            var alertOrder = prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();

            var alerts = prototypeManager.EnumeratePrototypes<AlertPrototype>();

            // ensure they sort according to our expected criteria
            var expectedOrder = new List<AlertType>();
            expectedOrder.Add(AlertType.Handcuffed);
            expectedOrder.Add(AlertType.HighPressure);
            // stuff with only category + same category ordered by enum value
            expectedOrder.Add(AlertType.Overfed);
            expectedOrder.Add(AlertType.Peckish);
            expectedOrder.Add(AlertType.Hot);
            expectedOrder.Add(AlertType.Stun);
            expectedOrder.Add(AlertType.LowPressure);
            expectedOrder.Add(AlertType.Cold);
            // stuff at end of list ordered by enum value
            expectedOrder.Add(AlertType.Weightless);
            expectedOrder.Add(AlertType.PilotingShuttle);

            var actual = alerts.ToList();
            actual.Sort(alertOrder);

            Assert.That(actual.Select(a => a.AlertType).ToList(), Is.EqualTo(expectedOrder));
        }
    }
}
