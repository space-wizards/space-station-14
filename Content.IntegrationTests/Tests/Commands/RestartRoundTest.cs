using System;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Shared;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RestartRoundNowCommand))]
    public class RestartRoundNowTest : ContentIntegrationTest
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task RestartRoundAfterStart(bool lobbyEnabled)
        {
            var (_, server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CCVars.GameMap.Name] = "saltern"
                }
            });

            await server.WaitIdleAsync();

            var configManager = server.ResolveDependency<IConfigurationManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var gameTicker = entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

            await server.WaitRunTicks(30);

            GameTick tickBeforeRestart = default;

            server.Assert(() =>
            {
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

            await server.WaitIdleAsync();
            await server.WaitRunTicks(5);

            server.Assert(() =>
            {
                var tickAfterRestart = entityManager.CurrentTick;

                Assert.That(tickBeforeRestart < tickAfterRestart);
            });

            await server.WaitRunTicks(60);
        }
    }
}
