using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        await server.WaitIdleAsync();
        var gameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
        var cfg = server.ResolveDependency<IConfigurationManager>();
        Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

        await server.WaitAssertion(() =>
        {
            var rules = gameTicker.GetAllGameRulePrototypes().ToList();
            rules.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

            // Start all rules
            foreach (var rule in rules)
            {
                gameTicker.StartGameRule(rule.ID);
            }
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            // End all rules
            gameTicker.ClearGameRules();
            Assert.That(!gameTicker.GetAddedGameRules().Any());
        });

        await pair.CleanReturnAsync();
    }
}
