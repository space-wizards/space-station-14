using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertOrderPrototype))]
    public class AlertOrderPrototypeTests : RobustUnitTest
    {
        const string PROTOTYPES = @"
- type: alertOrder
  order:
    - cuffed
    - extremepressure
    - pressure
    - stunned
    - lowpressure
    - temperature

- type: alert
  category: pressure
  id: lowpressure

- type: alert
  category: pressure
  id: medpressure

- type: alert
  category: pressure
  id: highpressure

- type: alert
  category: pressure
  id: extremepressure

- type: alert
  id: stunned

- type: alert
  id: cuffed

- type: alert
  category: temperature
  id: hot

- type: alert
  category: temperature
  id: cold

- type: alert
  id: oops
";

        [Test]
        public void TestAlertOrderPrototype()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.RegisterType(typeof(AlertPrototype));
            prototypeManager.RegisterType(typeof(AlertOrderPrototype));
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));

            var alertOrder = prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();

            var alerts = prototypeManager.EnumeratePrototypes<AlertPrototype>();

            // ensure they sort according to our expected criteria
            var expectedOrder = new List<string>();
            expectedOrder.Add("cuffed");
            expectedOrder.Add("extremepressure");
            // order by id alpha within ties
            expectedOrder.Add("highpressure");
            expectedOrder.Add("medpressure");
            expectedOrder.Add("stunned");
            expectedOrder.Add("lowpressure");
            expectedOrder.Add("cold");
            expectedOrder.Add("hot");
            expectedOrder.Add("oops");

            var actual = alerts.ToList();
            actual.Sort(alertOrder);

            Assert.That(actual.Select(a => a.ID).ToList(), Is.EqualTo(expectedOrder));
        }
    }
}
