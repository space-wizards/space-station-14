using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertOrderPrototype))]
    public sealed class AlertOrderPrototypeTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alertOrder
  id: testAlertOrder
  order:
    - alertType: Handcuffed
    - alertType: Ensnared
    - category: Pressure
    - category: Hunger
    - alertType: Hot
    - alertType: Stun
    - alertType: LowPressure
    - category: Temperature

- type: alert
  id: LowPressure
  icons: []
  category: Pressure

- type: alert
  id: HighPressure
  icons: []
  category: Pressure

- type: alert
  id: Peckish
  icons: []
  category: Hunger

- type: alert
  id: Stun
  icons: []

- type: alert
  id: Handcuffed
  icons: []

- type: alert
  id: Ensnared
  icons: []

- type: alert
  id: Hot
  icons: []
  category: Temperature

- type: alert
  id: Cold
  icons: []
  category: Temperature

- type: alert
  id: Weightless
  icons: []

- type: alert
  id: PilotingShuttle
  icons: []
";

        [Test]
        public void TestAlertOrderPrototype()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.ResolveResults();

            var alertOrder = prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();

            var alerts = prototypeManager.EnumeratePrototypes<AlertPrototype>();

            // ensure they sort according to our expected criteria
            var expectedOrder = new List<AlertType>();
            expectedOrder.Add(AlertType.Handcuffed);
            expectedOrder.Add(AlertType.Ensnared);
            expectedOrder.Add(AlertType.HighPressure);
            // stuff with only category + same category ordered by enum value
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
