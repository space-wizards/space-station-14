using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RoundEndTest : ContentIntegrationTest, IEntityEventSubscriber
    {
        [Test]
        public async Task Test()
        {
            var eventCount = 0;

            var (_, server) = await StartConnectedServerClientPair();

            await server.WaitAssertion(() =>
            {
                var ticker = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>();
                ticker.RestartRound();
                var config = IoCManager.Resolve<IConfigurationManager>();
                config.SetCVar(CCVars.GameLobbyEnabled, true);

                var roundEndSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundEndSystem>();
                roundEndSystem.DefaultCooldownDuration = TimeSpan.FromMilliseconds(250);
                roundEndSystem.DefaultCountdownDuration = TimeSpan.FromMilliseconds(500);
                roundEndSystem.DefaultRestartRoundDuration = TimeSpan.FromMilliseconds(250);
            });

            await server.WaitAssertion(() =>
            {
                var bus = IoCManager.Resolve<IEntityManager>().EventBus;
                bus.SubscribeEvent<RoundEndSystemChangedEvent>(EventSource.Local, this, _ => {
                    Interlocked.Increment(ref eventCount);
                });
                var roundEndSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundEndSystem>();
                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was called, but countdown time was not set");
                Assert.That(roundEndSystem.CanCall(), Is.False, "Started the shuttle, but didn't have to wait cooldown to press cancel button");
                // Check that we can't recall the shuttle yet
                roundEndSystem.CancelRoundEndCountdown();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was cancelled, even though the button was on cooldown");
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                var roundEndSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundEndSystem>();

                Assert.That(roundEndSystem.CanCall(), Is.True, "We waited a while, but the cooldown is not expired");
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "We were waiting for the cooldown, but the round also ended");
                // Recall the shuttle, which should trigger the cooldown again
                roundEndSystem.CancelRoundEndCountdown();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Null, "Recalled shuttle, but countdown has not ended");
                Assert.That(roundEndSystem.CanCall(), Is.False, "Recalled shuttle, but cooldown has not been enabled");
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                var roundEndSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundEndSystem>();
                Assert.That(roundEndSystem.CanCall(), Is.True, "We waited a while, but the cooldown is not expired");
                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                var roundEndSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundEndSystem>();
                Assert.That(roundEndSystem.CanCall(), Is.True, "We waited a while, but the cooldown is not expired");
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "The countdown ended, but we just wanted the cooldown to end");
            });

            await WaitForEvent(); // Wait for countdown to end round

            await CheckRunLevel(GameRunLevel.PostRound);

            await WaitForEvent(); // Wait for Restart

            await CheckRunLevel(GameRunLevel.PreRoundLobby);

            Task CheckRunLevel(GameRunLevel level)
            {
                return server.WaitAssertion(() =>
                {
                    var ticker = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>();
                    Assert.That(ticker.RunLevel, Is.EqualTo(level));
                });
            }

            async Task WaitForEvent()
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(10));
                var currentCount = Thread.VolatileRead(ref eventCount);
                while (currentCount == Thread.VolatileRead(ref eventCount) && !timeout.IsCompleted)
                {
                    await server.WaitRunTicks(1);
                }
                if (timeout.IsCompleted) throw new TimeoutException("Event took too long to trigger");
            }
        }
    }
}
