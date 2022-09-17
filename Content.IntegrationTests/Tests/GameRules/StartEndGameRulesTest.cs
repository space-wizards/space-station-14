using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;


[TestFixture]
public sealed class StartEndGameRulesTest
{
    /// <summary>
    ///     Tests that all game rules can be added/started/ended at the same time without exceptions.
    /// </summary>
    [Test]
    public async Task TestAllConcurrent()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings()
        {
            NoClient = true,
            Dirty = true,
        });
        var server = pairTracker.Pair.Server;
        await server.WaitIdleAsync();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var gameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();

        await server.WaitAssertion(() =>
        {
            var rules = protoMan.EnumeratePrototypes<GameRulePrototype>().ToList();
            rules.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

            // Start all rules
            foreach (var rule in rules)
            {
                gameTicker.StartGameRule(rule);
            }

            Assert.That(gameTicker.AddedGameRules, Has.Count.EqualTo(rules.Count));
            Assert.That(gameTicker.AddedGameRules, Has.Count.EqualTo(gameTicker.StartedGameRules.Count));
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            // End all rules
            gameTicker.ClearGameRules();
            Assert.That(!gameTicker.AddedGameRules.Any());
        });

        await pairTracker.CleanReturnAsync();
    }
}
