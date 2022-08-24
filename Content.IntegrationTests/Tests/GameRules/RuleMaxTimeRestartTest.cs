using System;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Server.GameTicking.Rules;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
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
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var configManager = server.ResolveDependency<IConfigurationManager>();
            await server.WaitPost(() =>
            {
                configManager.SetCVar(CCVars.GameLobbyEnabled, true);
                var command = new RestartRoundNowCommand();
                command.Execute(null, string.Empty, Array.Empty<string>());
            });


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
            await PoolManager.RunTicksSync(pairTracker.Pair, ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTimeMaxTimeRestartRuleSystem.RoundEndDelay.TotalSeconds * 1.1f);
            await PoolManager.RunTicksSync(pairTracker.Pair, ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);
            await server.WaitPost(() =>
            {
                configManager.SetCVar(CCVars.GameLobbyEnabled, false);
                var command = new RestartRoundNowCommand();
                command.Execute(null, string.Empty, Array.Empty<string>());
            });
            await PoolManager.RunTicksSync(pairTracker.Pair, 30);

            await pairTracker.CleanReturnAsync();
        }
    }
}
