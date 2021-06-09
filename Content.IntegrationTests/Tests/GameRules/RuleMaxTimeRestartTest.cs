using System;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using NUnit.Framework;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules
{
    [TestFixture]
    [TestOf(typeof(RuleMaxTimeRestart))]
    public class RuleMaxTimeRestartTest : ContentIntegrationTest
    {
        [Test]
        public async Task RestartTest()
        {
            var options = new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    ["game.lobbyenabled"] = "true"
                }
            };
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var sGameTicker = server.ResolveDependency<IGameTicker>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();

            RuleMaxTimeRestart maxTimeRule = null;

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

                maxTimeRule = sGameTicker.AddGameRule<RuleMaxTimeRestart>();
                maxTimeRule.RoundMaxTime = TimeSpan.FromSeconds(3);

                sGameTicker.StartRound();
            });

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            var ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTimeRule.RoundMaxTime.TotalSeconds * 1.1f);
            await server.WaitRunTicks(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTimeRule.RoundEndDelay.TotalSeconds * 1.1f);
            await server.WaitRunTicks(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });
        }
    }
}
