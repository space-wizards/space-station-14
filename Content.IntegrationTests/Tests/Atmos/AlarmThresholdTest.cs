using System.Threading.Tasks;
using Content.Shared.Atmos.Monitor;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(AtmosAlarmThreshold))]
    public sealed class AlarmThresholdTest
    {
        private const string Prototypes = @"
- type: alarmThreshold
  id: testThreshold
  upperBound: !type:AlarmThresholdSetting
    threshold: 5
  lowerBound: !type:AlarmThresholdSetting
    threshold: 1
  upperWarnAround: !type:AlarmThresholdSetting
    threshold: 0.5
  lowerWarnAround: !type:AlarmThresholdSetting
    threshold: 1.5
";

        [Test]
        public async Task TestAlarmThreshold()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            AtmosAlarmThreshold threshold = default!;

            await server.WaitPost(() =>
            {
                threshold = prototypeManager.Index<AtmosAlarmThreshold>("testThreshold");
            });

            await server.WaitAssertion(() =>
            {
                // ensure upper/lower bounds are calculated
                Assert.That(threshold.UpperWarningBound.Value, Is.EqualTo(5f * 0.5f));
                Assert.That(threshold.LowerWarningBound.Value, Is.EqualTo(1f * 1.5f));

                // ensure that setting bounds to zero/
                // negative numbers is an invalid set
                {
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, 0f);
                    Assert.That(threshold.UpperBound.Value, Is.EqualTo(5f));
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, -1f);
                    Assert.That(threshold.UpperBound.Value, Is.EqualTo(5f));

                    threshold.SetLimit(AtmosMonitorLimitType.LowerDanger, 0f);
                    Assert.That(threshold.LowerBound.Value, Is.EqualTo(1f));
                    threshold.SetLimit(AtmosMonitorLimitType.LowerDanger, -1f);
                    Assert.That(threshold.LowerBound.Value, Is.EqualTo(1f));
                }


                // test if making the lower bound higher
                // than upper will adjust the upper value
                {
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, 5f);
                    threshold.SetLimit(AtmosMonitorLimitType.LowerDanger, 6f);
                    Assert.That(threshold.LowerBound.Value, Is.LessThanOrEqualTo(threshold.UpperBound.Value));
                }

                // same as above, sets it lower
                {
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, 5f);
                    threshold.SetLimit(AtmosMonitorLimitType.LowerDanger, 6f);
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, 1f);
                    Assert.That(threshold.LowerBound.Value, Is.LessThanOrEqualTo(threshold.UpperBound.Value));
                }


                // Check that the warning percentage is calculated correcly
                {
                    threshold.SetLimit(AtmosMonitorLimitType.UpperWarning, threshold.UpperBound.Value * 0.5f);
                    Assert.That(threshold.UpperWarningPercentage.Value, Is.EqualTo(0.5f));

                    threshold.SetLimit(AtmosMonitorLimitType.LowerWarning, threshold.LowerBound.Value * 1.5f);
                    Assert.That(threshold.LowerWarningPercentage.Value, Is.EqualTo(1.5f));

                    threshold.SetLimit(AtmosMonitorLimitType.UpperWarning, threshold.UpperBound.Value * 0.5f);
                    Assert.That(threshold.UpperWarningPercentage.Value, Is.EqualTo(0.5f));

                    threshold.SetLimit(AtmosMonitorLimitType.LowerWarning, threshold.LowerBound.Value * 1.5f);
                    Assert.That(threshold.LowerWarningPercentage.Value, Is.EqualTo(1.5f));
                }

                // Check that the threshold reporting works correctly:
                {
                    // Set threshold to some known state
                    threshold.SetLimit(AtmosMonitorLimitType.UpperDanger, 5f);
                    threshold.SetEnabled(AtmosMonitorLimitType.UpperDanger, true);
                    threshold.SetLimit(AtmosMonitorLimitType.LowerDanger, 1f);
                    threshold.SetEnabled(AtmosMonitorLimitType.LowerDanger, true);
                    threshold.SetLimit(AtmosMonitorLimitType.UpperWarning, 4f);
                    threshold.SetEnabled(AtmosMonitorLimitType.UpperWarning, true);
                    threshold.SetLimit(AtmosMonitorLimitType.LowerWarning, 2f);
                    threshold.SetEnabled(AtmosMonitorLimitType.LowerWarning, true);

                    // Check a value that's in between each upper/lower warning/panic:
                    threshold.CheckThreshold(3f, out AtmosAlarmType alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Normal));
                    threshold.CheckThreshold(1.5f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Warning));
                    threshold.CheckThreshold(4.5f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Warning));
                    threshold.CheckThreshold(5.5f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Danger));
                    threshold.CheckThreshold(0.5f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Danger));

                    // Check that enable/disable is respected:
                    threshold.CheckThreshold(123.4f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Danger));
                    threshold.SetEnabled(AtmosMonitorLimitType.UpperDanger, false);
                    threshold.CheckThreshold(123.4f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Warning));
                    threshold.SetEnabled(AtmosMonitorLimitType.UpperWarning, false);
                    threshold.CheckThreshold(123.4f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Normal));

                    // And for lower thresholds:
                    threshold.CheckThreshold(0.01f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Danger));
                    threshold.SetEnabled(AtmosMonitorLimitType.LowerDanger, false);
                    threshold.CheckThreshold(0.01f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Warning));
                    threshold.SetEnabled(AtmosMonitorLimitType.LowerWarning, false);
                    threshold.CheckThreshold(0.01f, out alarmType);
                    Assert.That(alarmType, Is.EqualTo(AtmosAlarmType.Normal));
                }
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
