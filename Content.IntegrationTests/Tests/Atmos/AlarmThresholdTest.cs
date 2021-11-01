using Content.Shared.Atmos.Monitor;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(AtmosAlarmThreshold))]
    public class AlarmThresholdTest : ContentIntegrationTest
    {
        [Test]
        public void TestAlarmThreshold()
        {
            var threshold = new AtmosAlarmThreshold();

            // we do a little bit of defaults
            threshold.UpperBound = 5f;
            threshold.LowerBound = 1f;
            threshold.UpperWarningPercentage = 0.5f;
            threshold.LowerWarningPercentage = 1.5f;

            // ensure upper/lower bounds are calculated
            Assert.That(threshold.UpperWarningBound, Is.EqualTo(5f * 0.5f));
            Assert.That(threshold.LowerWarningBound, Is.EqualTo(1f * 1.5f));

            // ensure that setting bounds to zero/
            // negative numbers is an invalid
            // set
            threshold.UpperBound = 0;
            Assert.That(threshold.UpperBound, Is.EqualTo(5f));
            threshold.UpperBound = -1;
            Assert.That(threshold.UpperBound, Is.EqualTo(5f));

            threshold.LowerBound = 0;
            Assert.That(threshold.LowerBound, Is.EqualTo(1f));
            threshold.LowerBound = -1;
            Assert.That(threshold.LowerBound, Is.EqualTo(1f));


            // test if making the lower bound higher
            // than upper is invalid
            // aka just returns the previous value
            // instead of setting it to null
            threshold.LowerBound = 6f;
            Assert.That(threshold.LowerBound, Is.EqualTo(1f));

            // same as above, sets it lower
            threshold.UpperBound = 0.5f;
            Assert.That(threshold.UpperBound, Is.EqualTo(5f));

            // test if setting upper warning percentage
            // over 1f invalidates it
            threshold.UpperWarningPercentage = 1.1f;
            Assert.That(threshold.UpperWarningPercentage, Is.EqualTo(0.5f));

            // same as above, for lower warning
            threshold.LowerWarningPercentage = 0.9f;
            Assert.That(threshold.LowerWarningPercentage, Is.EqualTo(1.5f));

            // arbitrarily small percentages with
            // a lower warning bound defined
            // should immediately return
            // if the percentage makes it lower than
            // the warning bound
            threshold.UpperWarningPercentage = 0.00001f;
            Assert.That(threshold.UpperWarningPercentage, Is.EqualTo(0.5f));

            // same as above, but with a large percent
            threshold.LowerWarningPercentage = 100f;
            Assert.That(threshold.LowerWarningPercentage, Is.EqualTo(1.5f));

            threshold.UpperWarningPercentage = null;
            threshold.LowerWarningPercentage = null;

            Assert.That(threshold.UpperWarningBound, Is.EqualTo(null));
            Assert.That(threshold.LowerWarningBound, Is.EqualTo(null));

            threshold.UpperBound = null;
            threshold.LowerBound = null;

            Assert.That(threshold.UpperBound, Is.EqualTo(null));
            Assert.That(threshold.LowerBound, Is.EqualTo(null));
        }
    }
}
