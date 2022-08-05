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
  upperBound: 5
  lowerBound: 1
  upperWarnAround: 0.5
  lowerWarnAround: 1.5
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
                Assert.That(threshold.UpperWarningBound, Is.EqualTo(5f * 0.5f));
                Assert.That(threshold.LowerWarningBound, Is.EqualTo(1f * 1.5f));

                // ensure that setting bounds to zero/
                // negative numbers is an invalid
                // set
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Upper, 0);
                Assert.That(threshold.UpperBound, Is.EqualTo(5f));
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Upper, -1);
                Assert.That(threshold.UpperBound, Is.EqualTo(5f));

                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Lower, 0);
                Assert.That(threshold.LowerBound, Is.EqualTo(1f));
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Lower, -1);
                Assert.That(threshold.LowerBound, Is.EqualTo(1f));


                // test if making the lower bound higher
                // than upper is invalid
                // aka just returns the previous value
                // instead of setting it to null
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Lower, 6f);
                Assert.That(threshold.LowerBound, Is.EqualTo(1f));

                // same as above, sets it lower
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Upper, 0.5f);
                Assert.That(threshold.UpperBound, Is.EqualTo(5f));

                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Upper, threshold.UpperBound + 1);
                Assert.That(threshold.UpperWarningPercentage, Is.EqualTo(0.5f));

                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Lower, threshold.LowerBound - 1);
                Assert.That(threshold.LowerWarningPercentage, Is.EqualTo(1.5f));

                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Upper, threshold.LowerBound - 1);
                Assert.That(threshold.UpperWarningPercentage, Is.EqualTo(0.5f));

                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Lower, threshold.UpperBound + 1);
                Assert.That(threshold.LowerWarningPercentage, Is.EqualTo(1.5f));

                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Upper, null);
                threshold.TrySetWarningBound(AtmosMonitorThresholdBound.Lower, null);

                Assert.That(threshold.UpperWarningBound, Is.EqualTo(null));
                Assert.That(threshold.LowerWarningBound, Is.EqualTo(null));

                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Upper, null);
                threshold.TrySetPrimaryBound(AtmosMonitorThresholdBound.Lower, null);

                Assert.That(threshold.UpperBound, Is.EqualTo(null));
                Assert.That(threshold.LowerBound, Is.EqualTo(null));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
