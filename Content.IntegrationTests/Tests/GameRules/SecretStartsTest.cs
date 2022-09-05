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
public sealed class SecretStartsTest
{
    /// <summary>
    ///     Tests that when secret is started, all of the game rules it successfully adds are also started.
    /// </summary>
    [Test]
    public async Task TestSecretStarts()
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
            gameTicker.StartGameRule(protoMan.Index<GameRulePrototype>("Secret"));
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            foreach (var rule in gameTicker.AddedGameRules)
            {
                Assert.That(gameTicker.StartedGameRules.Contains(rule));
            }

            // End all rules
            gameTicker.ClearGameRules();
        });

        await pairTracker.CleanReturnAsync();
    }
}
