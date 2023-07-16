using System;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RestartRoundNowCommand))]
    public sealed class RestartRoundNowTest
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task RestartRoundAfterStart(bool lobbyEnabled)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings(){Dirty = true});
            var server = pairTracker.Pair.Server;

            var configManager = server.ResolveDependency<IConfigurationManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var gameTicker = entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            GameTick tickBeforeRestart = default;

            await server.WaitAssertion(() =>
            {
                Assert.That(configManager.GetCVar(CCVars.GameLobbyEnabled), Is.EqualTo(false));
                configManager.SetCVar(CCVars.GameLobbyEnabled, lobbyEnabled);

                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                tickBeforeRestart = entityManager.CurrentTick;

                var command = new RestartRoundNowCommand();
                command.Execute(null, string.Empty, Array.Empty<string>());

                if (lobbyEnabled)
                {
                    Assert.That(gameTicker.RunLevel, Is.Not.EqualTo(GameRunLevel.InRound));
                }
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 15);

            await server.WaitAssertion(() =>
            {
                var tickAfterRestart = entityManager.CurrentTick;

                Assert.That(tickBeforeRestart < tickAfterRestart);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);
            await pairTracker.CleanReturnAsync();
        }
    }
}
