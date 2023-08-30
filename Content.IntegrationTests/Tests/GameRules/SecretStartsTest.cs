using System.Linq;
using Content.Server.GameTicking;
using Robust.Shared.GameObjects;

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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });

        var server = pair.Server;
        await server.WaitIdleAsync();
        var gameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();

        await server.WaitAssertion(() =>
        {
            gameTicker.StartGameRule("Secret");
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            foreach (var rule in gameTicker.GetAddedGameRules())
            {
                Assert.That(gameTicker.GetActiveGameRules(), Does.Contain(rule));
            }

            // End all rules
            gameTicker.ClearGameRules();
        });

        await pair.CleanReturnAsync();
    }
}
