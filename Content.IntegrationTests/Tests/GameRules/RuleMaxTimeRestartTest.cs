using System;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules
{
    [TestFixture]
    [TestOf(typeof(MaxTimeRestartRuleSystem))]
    public sealed class RuleMaxTimeRestartTest : ContentIntegrationTest
    {
        [Test]
        public async Task RestartTest()
        {
            var options = new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    ["game.lobbyenabled"] = "true",
                    ["game.dummyticker"] = "false",
                    ["game.defaultpreset"] = "", // No preset.
                }
            };
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var sGameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
            var maxTimeMaxTimeRestartRuleSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MaxTimeRestartRuleSystem>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

                sGameTicker.StartGameRule(IoCManager.Resolve<IPrototypeManager>().Index<GameRulePrototype>(maxTimeMaxTimeRestartRuleSystem.Prototype));
                maxTimeMaxTimeRestartRuleSystem.RoundMaxTime = TimeSpan.FromSeconds(3);

                sGameTicker.StartRound();
            });

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            var ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTimeMaxTimeRestartRuleSystem.RoundMaxTime.TotalSeconds * 1.1f);
            await server.WaitRunTicks(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTimeMaxTimeRestartRuleSystem.RoundEndDelay.TotalSeconds * 1.1f);
            await server.WaitRunTicks(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });
        }
    }
}
