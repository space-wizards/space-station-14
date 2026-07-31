using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules
{
    [TestFixture]
    [TestOf(typeof(MaxTimeRestartRuleSystem))]
    public sealed class RuleMaxTimeRestartTest
    {
        [Test]
        public async Task RestartTest()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
            var server = pair.Server;

            Assert.That(server.EntMan.Count<GameRuleComponent>(), Is.Zero);
            Assert.That(server.EntMan.Count<ActiveGameRuleComponent>(), Is.Zero);

            var entityManager = server.ResolveDependency<IEntityManager>();
            var sGameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();

            MaxTimeRestartRuleComponent maxTime = null;
            EntityUid ruleEntity = default; // DS14
            await server.WaitPost(() =>
            {
                sGameTicker.StartGameRule("MaxTimeRestart", out ruleEntity); // DS14
                Assert.That(entityManager.TryGetComponent<MaxTimeRestartRuleComponent>(ruleEntity, out maxTime));
            });

            Assert.That(server.EntMan.Count<GameRuleComponent>(), Is.EqualTo(1));
            Assert.That(server.EntMan.Count<ActiveGameRuleComponent>(), Is.EqualTo(1));

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
                maxTime.RoundMaxTime = TimeSpan.FromSeconds(3);
                sGameTicker.StartRound();
            });

            Assert.That(server.EntMan.Count<GameRuleComponent>(), Is.EqualTo(1));
            Assert.That(server.EntMan.Count<ActiveGameRuleComponent>(), Is.EqualTo(1));

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            var ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundMaxTime.TotalSeconds * 1.1f);
            await pair.RunTicksSync(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
                // DS14-start
                Assert.That(sGameTicker.EndGameRule(ruleEntity), Is.False);
                Assert.That(sGameTicker.IsGameRuleActive(ruleEntity), Is.True);
                // DS14-end
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundEndDelay.TotalSeconds * 1.1f);
            await pair.RunTicksSync(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });

            await pair.CleanReturnAsync();
        }

        // DS14-start
        [Test]
        public async Task ManualRestartCancelsStaleDelayedRestart()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                InLobby = true,
                Dirty = true,
            });
            var server = pair.Server;
            var ticker = server.System<GameTicker>();
            var timing = server.ResolveDependency<IGameTiming>();
            MaxTimeRestartRuleComponent maxTime = null;

            await server.WaitAssertion(() =>
            {
                Assert.That(ticker.StartGameRule("MaxTimeRestart", out var rule), Is.True);
                Assert.That(server.EntMan.TryGetComponent(rule, out maxTime), Is.True);
                maxTime!.RoundMaxTime = TimeSpan.FromMilliseconds(100);
                maxTime.RoundEndDelay = TimeSpan.FromSeconds(1);
                ticker.StartRound();
            });

            await pair.RunTicksSync((int) Math.Ceiling(timing.TickRate * 0.2f));
            await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound)));

            var restartedRoundId = 0;
            await server.WaitAssertion(() =>
            {
                ticker.RestartRound();
                restartedRoundId = ticker.RoundId;
                Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });

            await pair.RunTicksSync(timing.TickRate * 2);
            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(ticker.RoundId, Is.EqualTo(restartedRoundId));
                    Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task DeletedRuleDoesNotFireStaleTimer()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                InLobby = true,
                Dirty = true,
            });
            var server = pair.Server;
            var ticker = server.System<GameTicker>();
            var timing = server.ResolveDependency<IGameTiming>();
            MaxTimeRestartRuleComponent maxTime = null;
            EntityUid ruleEntity = default;

            await server.WaitAssertion(() =>
            {
                Assert.That(ticker.StartGameRule("MaxTimeRestart", out ruleEntity), Is.True);
                Assert.That(server.EntMan.TryGetComponent(ruleEntity, out maxTime), Is.True);
                maxTime!.RoundMaxTime = TimeSpan.FromMilliseconds(100);
                ticker.StartRound();
            });
            await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound)));
            await server.WaitAssertion(() => server.EntMan.DeleteEntity(ruleEntity));

            await pair.RunTicksSync((int) Math.Ceiling(timing.TickRate * 0.2f));
            await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound)));

            await server.WaitAssertion(() => ticker.RestartRound());
            await pair.CleanReturnAsync();
        }
        // DS14-end
    }
}
