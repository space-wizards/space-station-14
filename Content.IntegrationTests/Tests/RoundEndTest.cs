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
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RoundEndTest : IEntityEventSubscriber
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });

            var server = pairTracker.Pair.Server;

            var config = server.ResolveDependency<IConfigurationManager>();
            var sysManager = server.ResolveDependency<IEntitySystemManager>();
            var ticker = sysManager.GetEntitySystem<GameTicker>();
            var roundEndSystem = sysManager.GetEntitySystem<RoundEndSystem>();

            var eventCount = 0;

            await server.WaitAssertion(() =>
            {
                config.SetCVar(CCVars.GameLobbyEnabled, true);
                config.SetCVar(CCVars.EmergencyShuttleTransitTime, 1f);
                config.SetCVar(CCVars.EmergencyShuttleDockTime, 1f);

                roundEndSystem.DefaultCooldownDuration = TimeSpan.FromMilliseconds(100);
                roundEndSystem.DefaultCountdownDuration = TimeSpan.FromMilliseconds(300);
                roundEndSystem.DefaultRestartRoundDuration = TimeSpan.FromMilliseconds(100);
            });

            await server.WaitAssertion(() =>
            {
                var bus = IoCManager.Resolve<IEntityManager>().EventBus;
                bus.SubscribeEvent<RoundEndSystemChangedEvent>(EventSource.Local, this, _ => {
                    Interlocked.Increment(ref eventCount);
                });

                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was called, but countdown time was not set");
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.False, "Started the shuttle, but didn't have to wait cooldown to press cancel button");
                // Check that we can't recall the shuttle yet
                roundEndSystem.CancelRoundEndCountdown();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "Shuttle was cancelled, even though the button was on cooldown");
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Not.Null, "We were waiting for the cooldown, but the round also ended");
                // Recall the shuttle, which should trigger the cooldown again
                roundEndSystem.CancelRoundEndCountdown();
                Assert.That(roundEndSystem.ExpectedCountdownEnd, Is.Null, "Recalled shuttle, but countdown has not ended");
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.False, "Recalled shuttle, but cooldown has not been enabled");
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
                // Press the shuttle call button
                roundEndSystem.RequestRoundEnd();
            });

            await WaitForEvent(); // Wait for Cooldown

            await server.WaitAssertion(() =>
            {
                Assert.That(roundEndSystem.CanCallOrRecall(), Is.True, "We waited a while, but the cooldown is not expired");
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
                    Assert.That(ticker.RunLevel, Is.EqualTo(level));
                });
            }

            async Task WaitForEvent()
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(10));
                var currentCount = Thread.VolatileRead(ref eventCount);
                while (currentCount == Thread.VolatileRead(ref eventCount) && !timeout.IsCompleted)
                {
                    await PoolManager.RunTicksSync(pairTracker.Pair, 5);
                }
                if (timeout.IsCompleted) throw new TimeoutException("Event took too long to trigger");
            }

            // Need to clean self up
            await server.WaitAssertion(() =>
            {
                config.SetCVar(CCVars.GameLobbyEnabled, false);
                config.SetCVar(CCVars.EmergencyShuttleTransitTime, CCVars.EmergencyShuttleTransitTime.DefaultValue);
                config.SetCVar(CCVars.EmergencyShuttleDockTime, CCVars.EmergencyShuttleDockTime.DefaultValue);

                roundEndSystem.DefaultCooldownDuration = TimeSpan.FromSeconds(30);
                roundEndSystem.DefaultCountdownDuration = TimeSpan.FromMinutes(4);
                roundEndSystem.DefaultRestartRoundDuration = TimeSpan.FromMinutes(1);
                ticker.RestartRound();
            });
            await PoolManager.ReallyBeIdle(pairTracker.Pair, 10);

            await pairTracker.CleanReturnAsync();
        }
    }
}
