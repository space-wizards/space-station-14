using System;
using Content.Server.Commands.GameTicking;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(NewRoundCommand))]
    public class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void RestartRoundAfterStart(bool lobbyEnabled)
        {
            var (_, server) = StartConnectedServerClientPair();

            server.WaitIdleAsync();

            var gameTicker = server.ResolveDependency<IGameTicker>();
            var configManager = server.ResolveDependency<IConfigurationManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            server.WaitRunTicks(30);

            GameTick tickBeforeRestart = default;

            server.Assert(() =>
            {
                configManager.SetCVar(CCVars.GameLobbyEnabled, lobbyEnabled);

                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                tickBeforeRestart = entityManager.CurrentTick;

                var command = new NewRoundCommand();
                command.Execute(null, string.Empty, Array.Empty<string>());

                if (lobbyEnabled)
                {
                    Assert.That(gameTicker.RunLevel, Is.Not.EqualTo(GameRunLevel.InRound));
                }
            });

            server.WaitIdleAsync();
            server.WaitRunTicks(5);

            server.Assert(() =>
            {
                var tickAfterRestart = entityManager.CurrentTick;

                Assert.That(tickBeforeRestart < tickAfterRestart);
            });

            server.WaitRunTicks(60);
        }
    }
}
