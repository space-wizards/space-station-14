using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

/// <summary>
///     Tests that all game rules can be added/started/ended at the same time without exceptions.
/// </summary>
[TestFixture]
public sealed class StartEndGameRulesTest
{
    [Test]
    public async Task Test()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        await server.WaitAssertion(() =>
        {
            var gameTicker = EntitySystem.Get<GameTicker>();
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            var rules = protoMan.EnumeratePrototypes<GameRulePrototype>().ToArray();

            // Start all rules
            foreach (var rule in rules)
            {
                gameTicker.StartGameRule(rule);
            }

            Assert.That(gameTicker.AddedGameRules.Count == rules.Length);
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var gameTicker = EntitySystem.Get<GameTicker>();

            // End all rules
            gameTicker.ClearGameRules();
            Assert.That(!gameTicker.AddedGameRules.Any());
        });

        await pairTracker.CleanReturnAsync();
    }
}
