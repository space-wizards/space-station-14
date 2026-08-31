#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class SecretStartsTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
    };

    private static readonly EntProtoId SecretGameRule = "Secret";

    [SidedDependency(Side.Server)] private GameTicker _sGameTicker = null!;

    /// <summary>
    /// Tests that when secret is started, all of the game rules it successfully adds are also started.
    /// </summary>
    [Test]
    [Description("Tests that when secret is started, all of the game rules it successfully adds are also started.")]
    public async Task TestSecretStarts()
    {
        await Server.WaitAssertion(() =>
        {
            // this mimics roundflow:
            // rules added, then round starts
            _sGameTicker.AddGameRule(SecretGameRule);
            _sGameTicker.StartGamePresetRules();
        });

        // Wait three ticks for any random update loops that might happen
        await RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sGameTicker.GetAddedGameRules().ToList(), Has.Count.GreaterThan(1), $"No additional rules started by secret rule.");

            // End all rules
            _sGameTicker.ClearGameRules();
        });
    }
}
