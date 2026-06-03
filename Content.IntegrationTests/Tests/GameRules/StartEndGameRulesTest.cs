#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Shared.CCVar;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class StartEndGameRulesTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Map = PoolManager.TestStation
    };

    [SidedDependency(Side.Server)] private GameTicker _sGameTicker = null!;

    /// <summary>
    /// Tests that all game rules can be added/started/ended at the same time without exceptions.
    /// </summary>
    [Test]
    [Description("Tests that all game rules can be added/started/ended at the same time without exceptions.")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task TestAllConcurrent()
    {
        await Server.WaitAssertion(() =>
        {
            var rules = _sGameTicker.GetAllGameRulePrototypes().ToList();
            Assume.That(rules, Is.Not.Empty);
            rules.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

            // Start all rules
            foreach (var rule in rules)
            {
                _sGameTicker.StartGameRule(rule.ID);
            }

            Assert.That(_sGameTicker.GetAddedGameRules(), Is.Not.Empty);
        });

        // Wait three ticks for any random update loops that might happen
        await RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            // End all rules
            _sGameTicker.ClearGameRules();
            Assert.That(_sGameTicker.GetAddedGameRules(), Is.Empty);
        });
    }
}
