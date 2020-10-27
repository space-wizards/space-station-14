using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using NUnit.Framework;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

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
                // Idle each event once
                var stationEventsSystem = EntitySystem.Get<StationEventSystem>();
                var dummyFrameTime = (float) IoCManager.Resolve<IGameTiming>().TickPeriod.TotalSeconds;

                foreach (var stationEvent in stationEventsSystem.StationEvents)
                {
                    stationEvent.Startup();
                    stationEvent.Update(dummyFrameTime);
                    stationEvent.Shutdown();
                    Assert.That(stationEvent.Occurrences == 1);
                }

                stationEventsSystem.Reset();

                foreach (var stationEvent in stationEventsSystem.StationEvents)
                {
                    Assert.That(stationEvent.Occurrences == 0);
                }
            });

            await server.WaitIdleAsync();
        }
    }
}
