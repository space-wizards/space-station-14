using System.Threading.Tasks;
using Content.Server.StationEvents;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.StationEvents
{
    [TestFixture]
    public class StationEventsSystemTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                // Idle each event
                var stationEventsSystem = EntitySystem.Get<StationEventSystem>();
                var dummyFrameTime = (float) IoCManager.Resolve<IGameTiming>().TickPeriod.TotalSeconds;

                foreach (var stationEvent in stationEventsSystem.StationEvents)
                {
                    stationEvent.Announce();
                    stationEvent.Update(dummyFrameTime);
                    stationEvent.Startup();
                    stationEvent.Update(dummyFrameTime);
                    stationEvent.Running = false;
                    stationEvent.Shutdown();
                    // Due to timings some events might startup twice when in reality they wouldn't.
                    Assert.That(stationEvent.Occurrences > 0);
                }

                stationEventsSystem.Reset();

                foreach (var stationEvent in stationEventsSystem.StationEvents)
                {
                    Assert.That(stationEvent.Occurrences, Is.EqualTo(0));
                }
            });

            await server.WaitIdleAsync();
        }
    }
}
